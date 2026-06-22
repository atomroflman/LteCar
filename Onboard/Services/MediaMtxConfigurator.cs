using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Text;
using LteCar.Shared.Channels;
using LteCar.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LteCar.Onboard;

public class MediaMtxConfiguration
{
    public string Host { get; set; } = string.Empty;
    public int VideoPort { get; set; } = 10001;
    public int AudioPort { get; set; } = 11001;
    public string StreamName { get; set; } = "rpi0";
    public int Width { get; set; } = 1024;
    public int Height { get; set; } = 768;
    public int Framerate { get; set; } = 22;
    public int Bitrate { get; set; } = 1000000;
    public string CameraLib { get; set; } = "libcamera-vid";
}

public interface IMediaMtxConfigurator
{
    Task<string> GenerateConfigurationAsync(MediaMtxConfiguration config);
    Task GenerateFromChannelMapAsync(ChannelMap channelMap, IReadOnlyDictionary<string, int> streamPorts);
    Task UpdateServerAddressAsync(string serverHost, int videoPort, int audioPort);
    Task StartProcessAsync();
    Task StopAsync();
    Task RestartAsync();
    MediaMtxConfiguration CurrentConfiguration { get; }
}

public class MediaMtxConfigurator : IMediaMtxConfigurator, IDisposable
{
    private readonly ILogger<MediaMtxConfigurator> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _configPath;
    private readonly string _backupPath;
    private MediaMtxConfiguration _currentConfig;
    private string _originalConfig;
    private Process? _mediamtxProcess;

    public MediaMtxConfiguration CurrentConfiguration => _currentConfig;

    public MediaMtxConfigurator(
        ILogger<MediaMtxConfigurator> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _configPath = Path.GetFullPath("./Extern/mediamtx.yml");
        _backupPath = Path.GetFullPath("./Extern/mediamtx.yml.backup");
        
        _currentConfig = LoadCurrentConfiguration();
        _originalConfig = File.ReadAllText(_configPath);
    }

    private MediaMtxConfiguration LoadCurrentConfiguration()
    {
        var config = new MediaMtxConfiguration();
        
        config.Host = _configuration.GetValue<string>("ServerName") ?? "localhost";
        config.VideoPort = _configuration.GetValue<int?>("VideoPort") ?? 10001;
        config.AudioPort = _configuration.GetValue<int?>("AudioPort") ?? 11001;
        config.Width = _configuration.GetValue<int?>("VideoWidth") ?? 1024;
        config.Height = _configuration.GetValue<int?>("VideoHeight") ?? 768;
        config.Framerate = _configuration.GetValue<int?>("VideoFramerate") ?? 22;
        config.Bitrate = _configuration.GetValue<int?>("VideoBitrate") ?? 1000000;
        config.CameraLib = _configuration.GetValue<string>("CameraOptions:CameraLib") ?? "libcamera-vid";
        config.StreamName = _configuration.GetValue<string>("VideoStreamName") ?? "rpi0";
        
        return config;
    }

    public async Task<string> GenerateConfigurationAsync(MediaMtxConfiguration config)
    {
        var template = await File.ReadAllTextAsync(_configPath);
        
        template = ReplaceOrAddLine(template, "webrtcLocalUDPAddress", $"webrtcLocalUDPAddress: :{config.AudioPort + 100}");
        template = ReplaceOrAddLine(template, "rtpAddress", $"rtpAddress: :{config.VideoPort}");
        template = ReplaceOrAddLine(template, "rtcpAddress", $"rtcpAddress: :{config.VideoPort + 1}");
        
        var rpiCameraSection = $@"
  {config.StreamName}:
    source: rpiCamera
    runOnInit: ffmpeg -t 2147483647 -i rtsp://localhost:8554/{config.StreamName} -c copy -f rtp rtp://{config.Host}:{config.VideoPort}?pkt_size=1300
    runOnInitRestart: yes
    rpiCameraCamID: 0
    rpiCameraWidth: {config.Width}
    rpiCameraHeight: {config.Height}
    rpiCameraFPS: {config.Framerate}
    rpiCameraTextOverlay: '%Y-%m-%d %H:%M:%S - {config.StreamName}'
    rpiCameraBrightness: 0.3
    rpiCameraBitrate: {config.Bitrate}
    rpiCameraIDRPeriod: 60";

        template = UpdateOrAddPathSection(template, config.StreamName, rpiCameraSection);
        
        _currentConfig = config;

        return template;
    }

    public async Task GenerateFromChannelMapAsync(ChannelMap channelMap, IReadOnlyDictionary<string, int> streamPorts)
    {
        var template = await File.ReadAllTextAsync(_configPath);
        var generatedPaths = new StringBuilder();

        foreach (var stream in channelMap.VideoStreams.Where(s => s.Value.Enabled))
        {
            if (!streamPorts.TryGetValue(stream.Value.StreamId, out var targetPort))
            {
                continue;
            }

            generatedPaths.AppendLine(BuildPathSection(stream.Key, stream.Value, targetPort));
        }

        generatedPaths.AppendLine("  all_others:");

        template = ReplacePathsSection(template, generatedPaths.ToString().TrimEnd());

        await File.WriteAllTextAsync(_configPath, template);
    }

    private string BuildPathSection(string pathName, VideoStreamMapItem stream, int targetPort)
    {
        var width = stream.Width ?? _currentConfig.Width;
        var height = stream.Height ?? _currentConfig.Height;
        var framerate = stream.Framerate ?? _currentConfig.Framerate;
        var bitrate = stream.Bitrate ?? _currentConfig.Bitrate;
        var cameraDevice = string.IsNullOrWhiteSpace(stream.CameraDevice) ? "/dev/video0" : stream.CameraDevice;
        var camId = stream.RpiCamId ?? 0;
        var sourceHost = string.IsNullOrWhiteSpace(_currentConfig.Host) ? "localhost" : _currentConfig.Host;

        if (string.Equals(stream.Type, "v4l2", StringComparison.OrdinalIgnoreCase))
        {
            return $@"  {pathName}:
    source: publisher
    runOnInit: ffmpeg -f v4l2 -framerate {framerate} -video_size {width}x{height} -i {cameraDevice} -c:v libx264 -preset veryfast -tune zerolatency -b:v {bitrate} -f rtp rtp://{sourceHost}:{targetPort}?pkt_size=1300
    runOnInitRestart: yes";
        }

        return $@"  {pathName}:
    source: rpiCamera
    runOnInit: ffmpeg -t 2147483647 -i rtsp://localhost:8554/{pathName} -c copy -f rtp rtp://{sourceHost}:{targetPort}?pkt_size=1300
    runOnInitRestart: yes
    rpiCameraCamID: {camId}
    rpiCameraWidth: {width}
    rpiCameraHeight: {height}
    rpiCameraFPS: {framerate}
    rpiCameraTextOverlay: '%Y-%m-%d %H:%M:%S - {pathName}'
    rpiCameraBrightness: 0.3
    rpiCameraBitrate: {bitrate}
    rpiCameraIDRPeriod: 60";
    }

    private string ReplacePathsSection(string content, string newPathsSection)
    {
        var pathsStart = content.IndexOf("paths:", StringComparison.Ordinal);
        if (pathsStart < 0)
        {
            return content;
        }

        var allOthersIndex = content.IndexOf("all_others:", pathsStart, StringComparison.Ordinal);
        if (allOthersIndex < 0)
        {
            return content[..pathsStart] + "paths:\n" + newPathsSection + "\n";
        }

        var allOthersLineEnd = content.IndexOf('\n', allOthersIndex);
        var prefix = content[..pathsStart];
        var suffix = allOthersLineEnd >= 0 ? content[(allOthersLineEnd + 1)..] : string.Empty;
        return prefix + "paths:\n" + newPathsSection + "\n" + suffix;
    }

    private string ReplaceOrAddLine(string content, string key, string newValue)
    {
        var lines = content.Split('\n').ToList();
        var keyPattern = $"^{key}:";
        
        for (int i = 0; i < lines.Count; i++)
        {
            if (Regex.IsMatch(lines[i], keyPattern))
            {
                lines[i] = newValue;
                return string.Join('\n', lines);
            }
        }
        
        var insertIndex = FindInsertIndexForKey(lines, key);
        lines.Insert(insertIndex, newValue);
        return string.Join('\n', lines);
    }

    private int FindInsertIndexForKey(List<string> lines, string key)
    {
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].StartsWith(key.Split(':')[0] + ":"))
            {
                return i;
            }
        }
        return lines.Count;
    }

    private string UpdateOrAddPathSection(string content, string pathName, string newSection)
    {
        var lines = content.Split('\n').ToList();
        
        var pathPattern = $"^\\s*{pathName}\\s*:";
        int startIndex = -1;
        int endIndex = lines.Count;
        
        for (int i = 0; i < lines.Count; i++)
        {
            if (Regex.IsMatch(lines[i], pathPattern))
            {
                startIndex = i;
                int braceCount = 0;
                for (int j = i + 1; j < lines.Count; j++)
                {
                    if (lines[j].Trim().StartsWith("all_others:"))
                    {
                        endIndex = j;
                        break;
                    }
                }
                break;
            }
        }
        
        if (startIndex >= 0)
        {
            lines.RemoveRange(startIndex, endIndex - startIndex);
            lines.Insert(startIndex, newSection);
        }
        else
        {
            var pathsIndex = lines.FindIndex(l => l.TrimStart().StartsWith("paths:"));
            if (pathsIndex >= 0)
            {
                lines.Insert(pathsIndex + 1, newSection);
            }
        }
        
        return string.Join('\n', lines);
    }

    public async Task UpdateServerAddressAsync(string serverHost, int videoPort, int audioPort)
    {
        _logger.LogInformation("Updating MediaMTX config: Server={Host}, Video={VideoPort}, Audio={AudioPort}",
            serverHost, videoPort, audioPort);

        var newConfig = new MediaMtxConfiguration
        {
            Host = serverHost,
            VideoPort = videoPort,
            AudioPort = audioPort,
            Width = _currentConfig.Width,
            Height = _currentConfig.Height,
            Framerate = _currentConfig.Framerate,
            Bitrate = _currentConfig.Bitrate,
            CameraLib = _currentConfig.CameraLib,
            StreamName = _currentConfig.StreamName
        };

        var newConfigContent = await GenerateConfigurationAsync(newConfig);
        
        await File.WriteAllTextAsync(_configPath + ".new", newConfigContent);
        
        if (File.Exists(_configPath + ".backup"))
        {
            File.Delete(_configPath + ".backup");
        }
        File.Copy(_configPath, _configPath + ".backup");
        File.Copy(_configPath + ".new", _configPath, true);
        File.Delete(_configPath + ".new");
        
        _logger.LogInformation("MediaMTX configuration updated successfully");
    }

    public async Task StopAsync()
    {
        if (_mediamtxProcess != null && !_mediamtxProcess.HasExited)
        {
            _logger.LogInformation("Stopping MediaMTX process...");
            _mediamtxProcess.Kill();
            await _mediamtxProcess.WaitForExitAsync();
        }
    }

    public async Task RestartAsync()
    {
        _logger.LogInformation("Restarting MediaMTX...");
        
        await StopAsync();
        
        await StartProcessAsync();
    }

    public async Task StartProcessAsync()
    {
        if (_mediamtxProcess != null && !_mediamtxProcess.HasExited)
        {
            _logger.LogInformation("MediaMTX is already running.");
            return;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "bash",
            Arguments = $"-c \"{Path.GetFullPath("./Extern/mediamtx")} {Path.GetFullPath("./Extern/mediamtx.yml")}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        _mediamtxProcess = new Process { StartInfo = startInfo };
        
        _mediamtxProcess.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                _logger.LogInformation("[MediaMTX] {Data}", e.Data);
            }
        };

        _mediamtxProcess.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                if (e.Data.StartsWith("ERR"))
                {
                    _logger.LogError("[MediaMTX ERROR] {Data}", e.Data);
                }
                else
                {
                    _logger.LogWarning("[MediaMTX] {Data}", e.Data);
                }
            }
        };

        _mediamtxProcess.EnableRaisingEvents = true;
        _mediamtxProcess.Start();
        _mediamtxProcess.BeginOutputReadLine();
        _mediamtxProcess.BeginErrorReadLine();
        
        _logger.LogInformation("MediaMTX process started with PID {PID}", _mediamtxProcess.Id);
    }

    public void Stop()
    {
        StopAsync().Wait();
    }

    public async Task RestoreOriginalConfigurationAsync()
    {
        if (File.Exists(_backupPath))
        {
            File.Copy(_backupPath, _configPath, true);
            _logger.LogInformation("MediaMTX configuration restored to original");
        }
    }

    public void Dispose()
    {
        Stop();
        _mediamtxProcess?.Dispose();
    }
}
