using System.Diagnostics;
using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using LteCar.Shared;
using LteCar.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace LteCar.Server;

public enum StreamProtocol
{
    TCP,
    UDP
}

public class StreamInfo
{
    public string Id { get; set; } = string.Empty;
    public string? CarId { get; set; }
    public StreamProtocol Protocol { get; set; }
    public int Port { get; set; }
    public Process? Process { get; set; }
    public DateTime StartTime { get; set; }
    public string? StreamPurpose { get; set; }
    public int? DatabaseId { get; set; } // ID aus der Datenbank
    public bool IsRunning => Process?.HasExited == false;
}

public class StreamConfiguration
{
    public string HostName { get; set; } = "localhost";
    public int UdpPortRangeStart { get; set; } = 10000;
    public int UdpPortRangeEnd { get; set; } = 10200;
    public int TcpPortRangeStart { get; set; } = 11000;
    public int TcpPortRangeEnd { get; set; } = 11200;
}

public class VideoStreamReceiverService
{
    private Process? _janusProcess;
    private Process? _ffmpegProcess;
    private readonly ConcurrentDictionary<string, StreamInfo> _activeStreams = new();
    private readonly StreamConfiguration _streamConfig;
    private readonly IServiceProvider _serviceProvider;

    public VideoStreamReceiverService(ILogger<VideoStreamReceiverService> logger, IOptions<StreamConfiguration> streamConfig, IServiceProvider serviceProvider)
    {
        Logger = logger;
        _streamConfig = streamConfig.Value;
        _serviceProvider = serviceProvider;
        
        // Aktive Streams aus der Datenbank laden
        _ = Task.Run(LoadActiveStreamsFromDatabase);
    }

    public ILogger<VideoStreamReceiverService> Logger { get; }

    public async Task<StreamInfo?> StartNewStream(StreamProtocol protocol, string? carId = null, string? streamId = null, string? streamPurpose = null)
    {
        streamId ??= Guid.NewGuid().ToString("N")[..8];
        
        var port = await Task.Run(() => FindFreePort(protocol));
        if (port == 0)
        {
            Logger.LogError($"No free port available for {protocol} stream");
            return null;
        }

        var streamInfo = new StreamInfo
        {
            Id = streamId,
            CarId = carId,
            Protocol = protocol,
            Port = port,
            StartTime = DateTime.UtcNow,
            StreamPurpose = streamPurpose
        };

        try
        {
            var dbStreamId = await SaveStreamToDatabase(streamInfo);
            streamInfo.DatabaseId = dbStreamId;

            Process process;
            
            if (protocol == StreamProtocol.TCP)
            {
                // TCP Stream mit FFmpeg
                process = StartTcpStream(port);
            }
            else
            {
                // UDP Stream direkt von Janus
                process = StartUdpStream(port);
            }

            streamInfo.Process = process;
            
            _activeStreams[streamId] = streamInfo;
            
            Logger.LogInformation($"Started {protocol} stream '{streamId}' for car '{carId}' on port {port} (DB ID: {dbStreamId})");
            
            // Process monitoring
            _ = Task.Run(() => MonitorStreamProcess(streamInfo));
            
            return streamInfo;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Failed to start {protocol} stream on port {port}");
            
            // Cleanup bei Fehler
            if (streamInfo.DatabaseId.HasValue)
            {
                await MarkStreamAsInactive(streamInfo.DatabaseId.Value);
            }
            
            return null;
        }
    }

    private int FindFreePort(StreamProtocol protocol)
    {
        var (startPort, endPort) = protocol switch
        {
            StreamProtocol.TCP => (_streamConfig.TcpPortRangeStart, _streamConfig.TcpPortRangeEnd),
            StreamProtocol.UDP => (_streamConfig.UdpPortRangeStart, _streamConfig.UdpPortRangeEnd),
            _ => (0, 0)
        };

        // Bereits verwendete Ports sammeln
        var usedPorts = _activeStreams.Values
            .Where(s => s.Protocol == protocol && s.IsRunning)
            .Select(s => s.Port)
            .ToHashSet();

        // Ersten freien Port finden
        for (int port = startPort; port <= endPort; port++)
        {
            if (!usedPorts.Contains(port) && IsPortAvailable(port, protocol))
            {
                return port;
            }
        }

        return 0; // Kein freier Port gefunden
    }

    private bool IsPortAvailable(int port, StreamProtocol protocol)
    {
        try
        {
            if (protocol == StreamProtocol.TCP)
            {
                using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Any, port);
                listener.Start();
                listener.Stop();
                return true;
            }
            else
            {
                using var client = new System.Net.Sockets.UdpClient(port);
                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    private Process StartTcpStream(int port)
    {
        var ffmpegArgs = $"-i tcp://0.0.0.0:{port}?listen -reconnect 1 -c:v copy -f rtp rtp://127.0.0.1:10000";
        
        var startInfo = new ProcessStartInfo("ffmpeg", ffmpegArgs)
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };

        var process = new Process { StartInfo = startInfo };
        
        process.OutputDataReceived += (obj, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                Logger.LogDebug($"FFmpeg TCP:{port} - {e.Data}");
        };
        
        process.ErrorDataReceived += (obj, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                Logger.LogDebug($"FFmpeg TCP:{port} Error - {e.Data}");
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        
        return process;
    }

    private Process StartUdpStream(int port)
    {
        // Für UDP direkt von Janus - hier könnten wir einen einfachen UDP Relay starten
        // oder direkt Janus mit dem spezifischen Port konfigurieren
        
        // Beispiel: netcat als UDP Relay zu Janus
        var ncArgs = $"-l -u -p {port} -c 'nc -u 127.0.0.1 10000'";
        
        var startInfo = new ProcessStartInfo("sh", $"-c \"nc {ncArgs}\"")
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };

        var process = new Process { StartInfo = startInfo };
        
        process.OutputDataReceived += (obj, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                Logger.LogDebug($"UDP Relay:{port} - {e.Data}");
        };
        
        process.ErrorDataReceived += (obj, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                Logger.LogDebug($"UDP Relay:{port} Error - {e.Data}");
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        
        return process;
    }

    private async Task MonitorStreamProcess(StreamInfo streamInfo)
    {
        var process = streamInfo.Process;
        if (process == null) return;

        try
        {
            await process.WaitForExitAsync();
            
            Logger.LogWarning($"Stream '{streamInfo.Id}' ({streamInfo.Protocol}:{streamInfo.Port}) process exited with code {process.ExitCode}");
            
            // Stream aus aktiven Streams entfernen
            _activeStreams.TryRemove(streamInfo.Id, out _);
            
            // Stream in Datenbank als inaktiv markieren
            if (streamInfo.DatabaseId.HasValue)
            {
                await MarkStreamAsInactive(streamInfo.DatabaseId.Value);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Error monitoring stream '{streamInfo.Id}'");
        }
    }

    public async Task<bool> StopStream(string streamId)
    {
        if (!_activeStreams.TryRemove(streamId, out var streamInfo))
        {
            Logger.LogWarning($"Stream '{streamId}' not found");
            return false;
        }

        try
        {
            streamInfo.Process?.Kill();
            streamInfo.Process?.Dispose();
            
            // Stream in Datenbank als inaktiv markieren
            if (streamInfo.DatabaseId.HasValue)
            {
                await MarkStreamAsInactive(streamInfo.DatabaseId.Value);
            }
            
            Logger.LogInformation($"Stopped stream '{streamId}' ({streamInfo.Protocol}:{streamInfo.Port})");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Error stopping stream '{streamId}'");
            return false;
        }
    }

    public IEnumerable<StreamInfo> GetActiveStreams()
    {
        return _activeStreams.Values.Where(s => s.IsRunning).ToList();
    }

    public StreamInfo? GetStream(string streamId)
    {
        _activeStreams.TryGetValue(streamId, out var streamInfo);
        return streamInfo;
    }

    public IEnumerable<StreamInfo> GetStreamsByCar(string carId)
    {
        return _activeStreams.Values.Where(s => s.CarId == carId && s.IsRunning).ToList();
    }

    public async Task<bool> StopStreamsByCar(string carId)
    {
        var carStreams = _activeStreams.Values.Where(s => s.CarId == carId).ToList();
        var stoppedCount = 0;

        foreach (var stream in carStreams)
        {
            if (await StopStream(stream.Id))
            {
                stoppedCount++;
            }
        }

        Logger.LogInformation($"Stopped {stoppedCount} streams for car '{carId}'");
        return stoppedCount > 0;
    }

    public StreamInfo? GetActiveStreamByCar(string carId)
    {
        return _activeStreams.Values.FirstOrDefault(s => s.CarId == carId && s.IsRunning);
    }

    public void RunVideoStreamServer()
    {
        var startParams = new ProcessStartInfo("/opt/janus/bin/janus")
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };
        _janusProcess = new Process();
        _janusProcess.OutputDataReceived += (obj, e) =>
        {
            Logger.LogInformation("Janus Server: " + e.Data);
        };
        _janusProcess.StartInfo = startParams;
        _janusProcess.Start();

        var ffmpegParams = new ProcessStartInfo("ffmpeg", "-i tcp://0.0.0.0:11000?listen -reconnect 1 -c:v copy -f rtp rtp://127.0.0.1:10000")
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };

        _ffmpegProcess = new Process();
        _ffmpegProcess.OutputDataReceived += (obj, e) =>
        {
            Logger.LogInformation("FFMPEG: " + e.Data);
        };
        _ffmpegProcess.StartInfo = ffmpegParams;
        _ffmpegProcess.Exited += (obj, e) =>
        {
            Logger.LogWarning("FFMPEG process exited!");
            _ffmpegProcess.Start();
        };
        _ffmpegProcess.Start();
    }

    private async Task<int> SaveStreamToDatabase(StreamInfo streamInfo)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LteCarContext>();

        // Car aus Datenbank finden
        Car? car = null;
        if (!string.IsNullOrEmpty(streamInfo.CarId) && int.TryParse(streamInfo.CarId, out var carId))
        {
            car = await dbContext.Cars.FirstOrDefaultAsync(c => c.Id == carId);
            if (car == null)
            {
                Logger.LogWarning($"Car with ID '{streamInfo.CarId}' not found in database when saving stream");
            }
        }

        var dbStream = new CarVideoStream
        {
            StreamId = streamInfo.Id,
            CarId = car?.Id ?? 0, // 0 wenn kein Fahrzeug zugeordnet
            Protocol = streamInfo.Protocol.ToString(),
            Port = streamInfo.Port,
            StartTime = streamInfo.StartTime,
            IsActive = true,
            StreamPurpose = streamInfo.StreamPurpose
        };

        dbContext.CarVideoStreams.Add(dbStream);
        await dbContext.SaveChangesAsync();

        Logger.LogDebug($"Saved stream '{streamInfo.Id}' to database with ID {dbStream.Id}");
        return dbStream.Id;
    }



    private async Task MarkStreamAsInactive(int streamDbId)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LteCarContext>();

            var dbStream = await dbContext.CarVideoStreams.FindAsync(streamDbId);
            if (dbStream != null)
            {
                dbStream.IsActive = false;
                dbStream.EndTime = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
                Logger.LogDebug($"Marked stream {streamDbId} as inactive");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Failed to mark stream {streamDbId} as inactive");
        }
    }

    private async Task LoadActiveStreamsFromDatabase()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LteCarContext>();

            var activeStreams = await dbContext.CarVideoStreams
                .Include(s => s.Car)
                .Where(s => s.IsActive && s.EndTime == null)
                .ToListAsync();

            foreach (var dbStream in activeStreams)
            {
                // Da wir keine Prozess-ID speichern, markieren wir alle Streams beim Start als inaktiv
                // und lassen sie nur bei Bedarf neu erstellen
                dbStream.IsActive = false;
                dbStream.EndTime = DateTime.UtcNow;
                Logger.LogInformation($"Marked stream {dbStream.StreamId} as inactive during service restart");
            }

            await dbContext.SaveChangesAsync();
            Logger.LogInformation($"Loaded {activeStreams.Count(s => s.IsActive)} active streams from database");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load active streams from database");
        }
    }
}