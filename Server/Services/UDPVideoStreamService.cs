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

            // RTP/JPEG-Fragmente zu vollständigen JPEGs zusammensetzen
            // RFC 2435: RTP/JPEG zu vollständigen JPEGs zusammensetzen
            var jpegFrames = new Dictionary<uint, List<byte[]>>(); // key: timestamp, value: list of payloads
            var jpegHeaders = new Dictionary<uint, byte[]>(); // key: timestamp, value: JPEG header (aus erstem Fragment)
            var jpegFrameLengths = new Dictionary<uint, int>(); // key: timestamp, value: total length (nur JPEG-Daten, ohne Header)
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = await _udpClient.ReceiveAsync();
                    var packet = result.Buffer;
                    if (packet.Length < 12) continue; // Ungültiges RTP-Paket

                    // RTP Header parsen
                    int csrcCount = packet[0] & 0x0F;
                    int headerLen = 12 + csrcCount * 4;
                    if ((packet[0] & 0x10) != 0 && packet.Length > headerLen + 4)
                    {
                        int extLen = (packet[headerLen + 2] << 8) | packet[headerLen + 3];
                        headerLen += 4 + (extLen * 4);
                    }
                    if (headerLen >= packet.Length) continue;

                    // RTP Felder
                    bool marker = (packet[1] & 0x80) != 0;
                    uint timestamp = (uint)((packet[4] << 24) | (packet[5] << 16) | (packet[6] << 8) | packet[7]);

                    // JPEG-Payload nach RFC 2435: https://datatracker.ietf.org/doc/html/rfc2435
                    // JPEG-Payload-Header ist 8 Bytes (Type-specific, Fragment Offset, Type, Q, Width, Height)
                    int payloadOffset = headerLen;
                    if (packet.Length < payloadOffset + 8) continue;
                    int fragOffset = (packet[payloadOffset + 1] << 16) | (packet[payloadOffset + 2] << 8) | packet[payloadOffset + 3];
                    int jpegPayloadHeaderLen = 8;
                    int quantLen = 0;
                    if (fragOffset == 0)
                    {
                        // Im ersten Fragment können Quantisierungstabellen folgen
                        if ((packet[payloadOffset + 4] & 0x10) != 0)
                        {
                            // Q-Table present
                            quantLen = (packet[payloadOffset + 6] << 8) | packet[payloadOffset + 7];
                            jpegPayloadHeaderLen += quantLen;
                        }
                    }
                    int jpegDataOffset = payloadOffset + jpegPayloadHeaderLen;
                    if (jpegDataOffset > packet.Length) continue;
                    int jpegDataLen = packet.Length - jpegDataOffset;
                    var jpegData = new byte[jpegDataLen];
                    Buffer.BlockCopy(packet, jpegDataOffset, jpegData, 0, jpegDataLen);

                    if (!jpegFrames.ContainsKey(timestamp))
                    {
                        jpegFrames[timestamp] = new List<byte[]>();
                        jpegFrameLengths[timestamp] = 0;
                    }
                    // Header nur beim ersten Fragment speichern
                    if (fragOffset == 0)
                    {
                        // JPEG-Header bauen (SOI, JFIF, Quantization Tables, ...)
                        var header = BuildJpegHeader(packet, payloadOffset, quantLen);
                        jpegHeaders[timestamp] = header;
                    }
                    jpegFrames[timestamp].Add(jpegData);
                    jpegFrameLengths[timestamp] += jpegDataLen;

                    if (marker && jpegHeaders.ContainsKey(timestamp))
                    {
                        // JPEG zusammensetzen: Header + Daten + EOI
                        var header = jpegHeaders[timestamp];
                        int totalLen = header.Length + jpegFrameLengths[timestamp] + 2; // +2 für EOI
                        var jpeg = new byte[totalLen];
                        int pos = 0;
                        Buffer.BlockCopy(header, 0, jpeg, pos, header.Length);
                        pos += header.Length;
                        foreach (var frag in jpegFrames[timestamp])
                        {
                            Buffer.BlockCopy(frag, 0, jpeg, pos, frag.Length);
                            pos += frag.Length;
                        }
                        // EOI (End of Image)
                        jpeg[pos++] = 0xFF;
                        jpeg[pos++] = 0xD9;
                        _frameBuffer.Enqueue(jpeg);
                        // Cleanup
                        jpegFrames.Remove(timestamp);
                        jpegHeaders.Remove(timestamp);
                        jpegFrameLengths.Remove(timestamp);
                    }
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionReset)
                {
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

    // Hilfsfunktion: JPEG-Header aus RTP/JPEG-Payload bauen (SOI, JFIF, ggf. Quantization Tables)
    private static byte[] BuildJpegHeader(byte[] packet, int payloadOffset, int quantLen)
    {
        // Minimaler Header: SOI + JFIF + ggf. Quantization Tables
        // Für viele Kameras reicht das, für komplexe Profile müsste man noch mehr auswerten
        var header = new List<byte>();
        // SOI
        header.Add(0xFF); header.Add(0xD8);
        // JFIF APP0 Marker (minimal, keine Exif)
        header.AddRange(new byte[] { 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00 });
        // Quantization Tables falls vorhanden
        if (quantLen > 0)
        {
            header.AddRange(new ArraySegment<byte>(packet, payloadOffset + 8, quantLen));
        }
        // (Weitere Marker wie SOF, DHT, SOS werden von jpegenc/rtpjpegpay meist schon in den Daten geliefert)
        return header.ToArray();
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

    // Die Extraktion des RTP-Payloads entfällt, da die Pakete direkt weitergeleitet werden

    private async Task StreamToClients(CancellationToken cancellationToken)
    {
        var firstBoundary = "--myboundary\r\nContent-Type: image/jpeg\r\nContent-Length: ";
        var nextBoundary = "\r\n--myboundary\r\nContent-Type: image/jpeg\r\nContent-Length: ";
        var endBoundary = "\r\n\r\n";
        bool isFirstFrame = true;

        _logger.LogInformation("Started streaming to clients");

        while (!cancellationToken.IsCancellationRequested && _isStreaming)
        {
            if (_frameBuffer.TryDequeue(out var frame))
            {
                try
                {
                    string header = (isFirstFrame ? firstBoundary : nextBoundary) + frame.Length + endBoundary;
                    var headerBytes = System.Text.Encoding.ASCII.GetBytes(header);

                    await BroadcastToClients(headerBytes, cancellationToken);
                    await BroadcastToClients(frame, cancellationToken);
                    isFirstFrame = false;
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
        _logger.LogInformation($"Client ruft Stream ab: {response.HttpContext?.Connection.RemoteIpAddress}");
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