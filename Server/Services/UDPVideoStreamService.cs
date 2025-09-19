using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LteCar.Server.Services;

public class UDPVideoStreamService : BackgroundService
{
    private readonly ILogger<UDPVideoStreamService> _logger;
    private UdpClient? _udpClient;
    private readonly ConcurrentQueue<byte[]> _frameBuffer = new();
    private readonly List<HttpResponse> _connectedClients = new();
    private readonly object _clientsLock = new();
    private volatile bool _isStreaming = false;
    private readonly int _videoPort = 10000;

    public UDPVideoStreamService(ILogger<UDPVideoStreamService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _udpClient = new UdpClient(_videoPort);
            _udpClient.Client.ReceiveBufferSize = 1024 * 1024; // 1MB Buffer für bessere Performance
            
            var endpoint = new IPEndPoint(IPAddress.Any, _videoPort);
            _logger.LogInformation($"UDP Video Server listening on port {_videoPort}");

            // Frame cleanup task - hält Buffer klein für niedrige Latenz
            _ = Task.Run(async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    // Nur die letzten 3 Frames behalten für minimale Latenz
                    while (_frameBuffer.Count > 3)
                        _frameBuffer.TryDequeue(out _);
                        
                    await Task.Delay(33, stoppingToken); // ~30 FPS cleanup
                }
            }, stoppingToken);

            // UDP receive loop
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = await _udpClient.ReceiveAsync();
                    
                    // RTP Payload extrahieren
                    var payload = ExtractRTPPayload(result.Buffer);
                    if (payload.Length > 0)
                    {
                        _frameBuffer.Enqueue(payload);
                        
                        // Streaming Task starten falls noch nicht aktiv
                        if (!_isStreaming && _frameBuffer.Count > 0)
                        {
                            _isStreaming = true;
                            _ = Task.Run(() => StreamToClients(stoppingToken));
                        }
                    }
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionReset)
                {
                    // Normale Unterbrechung bei UDP - ignorieren
                    _logger.LogDebug("UDP connection reset - continuing");
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "UDP receive error");
                    await Task.Delay(100, stoppingToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in UDP Video Service");
        }
        finally
        {
            _udpClient?.Close();
            _udpClient?.Dispose();
        }
    }

    private byte[] ExtractRTPPayload(byte[] rtpPacket)
    {
        try
        {
            // RTP Header prüfen und entfernen (normalerweise 12 Bytes)
            if (rtpPacket.Length > 12 && (rtpPacket[0] & 0xC0) == 0x80) // Version 2
            {
                var headerLength = 12;
                
                // CSRC Count berücksichtigen
                var csrcCount = rtpPacket[0] & 0x0F;
                headerLength += csrcCount * 4;
                
                // Extension Header berücksichtigen
                if ((rtpPacket[0] & 0x10) != 0 && rtpPacket.Length > headerLength + 4)
                {
                    var extensionLength = (rtpPacket[headerLength + 2] << 8) | rtpPacket[headerLength + 3];
                    headerLength += 4 + (extensionLength * 4);
                }
                
                if (headerLength < rtpPacket.Length)
                {
                    return rtpPacket[headerLength..];
                }
            }
            
            // Falls kein gültiger RTP Header, gesamtes Paket zurückgeben
            return rtpPacket;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error extracting RTP payload, using raw packet");
            return rtpPacket;
        }
    }

    private async Task StreamToClients(CancellationToken cancellationToken)
    {
        var mjpegBoundary = "\r\n--myboundary\r\nContent-Type: image/jpeg\r\nContent-Length: ";
        var endBoundary = "\r\n\r\n";
        
        _logger.LogInformation("Started streaming to clients");
        
        while (!cancellationToken.IsCancellationRequested && _isStreaming)
        {
            if (_frameBuffer.TryDequeue(out var frame))
            {
                try
                {
                    var header = mjpegBoundary + frame.Length + endBoundary;
                    var headerBytes = System.Text.Encoding.ASCII.GetBytes(header);
                    
                    await BroadcastToClients(headerBytes, cancellationToken);
                    await BroadcastToClients(frame, cancellationToken);
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "Error broadcasting frame");
                }
            }
            else
            {
                // Kurz warten wenn keine Frames verfügbar
                await Task.Delay(10, cancellationToken);
            }
        }
        
        _logger.LogInformation("Stopped streaming to clients");
    }

    private async Task BroadcastToClients(byte[] data, CancellationToken cancellationToken)
    {
        var clientsToRemove = new List<HttpResponse>();
        List<HttpResponse> currentClients;
        
        // Copy clients list to avoid holding lock during async operations
        lock (_clientsLock)
        {
            currentClients = new List<HttpResponse>(_connectedClients);
        }
        
        foreach (var client in currentClients)
        {
            try
            {
                await client.Body.WriteAsync(data, cancellationToken);
                await client.Body.FlushAsync(cancellationToken);
            }
            catch
            {
                // Client disconnected
                clientsToRemove.Add(client);
            }
        }

        // Remove disconnected clients
        if (clientsToRemove.Count > 0)
        {
            lock (_clientsLock)
            {
                foreach (var client in clientsToRemove)
                {
                    _connectedClients.Remove(client);
                }
            }
            
            _logger.LogDebug($"Removed {clientsToRemove.Count} disconnected clients");
        }
    }

    public void AddClient(HttpResponse response)
    {
        // MJPEG Headers setzen
        response.ContentType = "multipart/x-mixed-replace; boundary=myboundary";
        response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
        response.Headers["Pragma"] = "no-cache";
        response.Headers["Connection"] = "close";
        response.Headers["Access-Control-Allow-Origin"] = "*";
        
        lock (_clientsLock)
        {
            _connectedClients.Add(response);
        }
        
        _logger.LogInformation($"Client connected. Total clients: {_connectedClients.Count}");
    }

    public void RemoveClient(HttpResponse response)
    {
        lock (_clientsLock)
        {
            _connectedClients.Remove(response);
        }
        
        _logger.LogInformation($"Client disconnected. Total clients: {_connectedClients.Count}");
        
        // Streaming stoppen wenn keine Clients mehr da sind
        if (_connectedClients.Count == 0)
        {
            _isStreaming = false;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping UDP Video Stream Service...");
        _isStreaming = false;
        
        // Alle Clients benachrichtigen
        lock (_clientsLock)
        {
            _connectedClients.Clear();
        }
        
        _udpClient?.Close();
        await base.StopAsync(cancellationToken);
    }
}