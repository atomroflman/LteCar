using System.Diagnostics;
using System.Text.Json;
using LteCar.Shared;
using LteCar.Shared.Channels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LteCar.Onboard.Video;

public class VideoStreamManager : IDisposable
{
    private readonly Dictionary<string, VideoStreamInstance> _activeStreams = new();
    private readonly ChannelMap _channelMap;
    private JanusConfiguration? _janusConfiguration;
    
    public ILogger<VideoStreamManager> Logger { get; }
    public CarConfigurationService ConfigService { get; }
    public CameraProcessParameterBuilder CameraProcessParameterBuilder { get; }

    public VideoStreamManager(ILogger<VideoStreamManager> logger, CarConfigurationService configService, 
        CameraProcessParameterBuilder cameraProcessParameterBuilder, ChannelMap channelMap)
    {
        Logger = logger;
        ConfigService = configService;
        CameraProcessParameterBuilder = cameraProcessParameterBuilder;
        _channelMap = channelMap;
        
        ConfigService.OnConfigurationChanged += OnConfigurationChanged;
    }

    private void OnConfigurationChanged()
    {
        var config = ConfigService.Configuration;
        var hasJanusChanges = false;
        
        if (ConfigService.CheckForChanges(config, _janusConfiguration) && config.JanusConfiguration != null)
        {
            _janusConfiguration = config.JanusConfiguration;
            Logger.LogInformation($"Janus configuration updated: {JsonSerializer.Serialize(_janusConfiguration)}");
            hasJanusChanges = true;
        }

        // Update or restart streams if Janus configuration changed
        if (hasJanusChanges)
        {
            RestartAllStreams();
        }
    }

    public void StartAllStreams()
    {
        if (_janusConfiguration == null)
        {
            Logger.LogWarning("Janus configuration not available yet. Streams will start when configuration is received.");
            return;
        }

        foreach (var streamConfig in _channelMap.VideoStreams)
        {
            if (streamConfig.Value.Enabled)
            {
                StartStream(streamConfig.Key, streamConfig.Value);
            }
        }
    }

    public void StartStream(string streamName, VideoStreamMapItem streamConfig)
    {
        if (_janusConfiguration == null)
        {
            Logger.LogWarning($"Cannot start stream {streamName}: Janus configuration not available.");
            return;
        }

        if (_activeStreams.ContainsKey(streamName))
        {
            Logger.LogInformation($"Stream {streamName} is already running. Stopping it first.");
            StopStream(streamName);
        }

        try
        {
            var streamInstance = new VideoStreamInstance(streamName, streamConfig, Logger, CameraProcessParameterBuilder, _janusConfiguration);
            streamInstance.Start();
            _activeStreams[streamName] = streamInstance;
            
            // Logger.LogInformation($"Started video stream: {streamName} ({streamConfig.Purpose})");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Failed to start video stream: {streamName}");
        }
    }

    public void StopStream(string streamName)
    {
        if (_activeStreams.TryGetValue(streamName, out var stream))
        {
            stream.Stop();
            stream.Dispose();
            _activeStreams.Remove(streamName);
            Logger.LogInformation($"Stopped video stream: {streamName}");
        }
    }

    public void RestartAllStreams()
    {
        Logger.LogInformation("Restarting all video streams due to configuration change.");
        
        // Stop all streams
        foreach (var streamName in _activeStreams.Keys.ToList())
        {
            StopStream(streamName);
        }
        
        // Start enabled streams
        StartAllStreams();
    }

    public void Dispose()
    {
        foreach (var stream in _activeStreams.Values)
        {
            stream.Stop();
            stream.Dispose();
        }
        _activeStreams.Clear();
    }
}

public class VideoStreamInstance : IDisposable
{
    private Process? _process;
    private CancellationTokenSource? _cancellationToken;
    
    public string StreamName { get; }
    public VideoStreamMapItem Config { get; }
    public ILogger Logger { get; }
    public CameraProcessParameterBuilder ParameterBuilder { get; }
    public JanusConfiguration JanusConfig { get; }

    public VideoStreamInstance(string streamName, VideoStreamMapItem config, ILogger logger, 
        CameraProcessParameterBuilder parameterBuilder, JanusConfiguration janusConfig)
    {
        StreamName = streamName;
        Config = config;
        Logger = logger;
        ParameterBuilder = parameterBuilder;
        JanusConfig = janusConfig;
    }

    public void Start()
    {
        if (_process != null && !_process.HasExited)
        {
            Logger.LogWarning($"Stream {StreamName} process is already running.");
            return;
        }

        _cancellationToken = new CancellationTokenSource();
        _process = new Process();

        // var parameters = ParameterBuilder.BuildParameters(Config.VideoSettings) 
        //     + ParameterBuilder.AppendJanusConfig(JanusConfig);
            
        var parameters = "rpicam-vid -t 0 --intra 1 --libav-format h264 --nopreview -o - | gst-launch-1.0 fdsrc ! h264parse ! rtph264pay pt=126 ! tcpsink host=lte-rc.northeurope.cloudapp.azure.com port=11000 sync=false ";
        _process.StartInfo.FileName = "bash";
        _process.StartInfo.Arguments = $"-c \"{parameters}\"";
        _process.StartInfo.UseShellExecute = false;
        _process.StartInfo.RedirectStandardOutput = true;
        _process.StartInfo.RedirectStandardError = true;
        
        _process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Logger.LogDebug($"[{StreamName}] {e.Data}");
            }
        };
        
        _process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Logger.LogError($"[{StreamName}] {e.Data}");
            }
        };

        _process.EnableRaisingEvents = true;
        _process.Exited += OnProcessExited;

        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
        
        Logger.LogInformation($"Started stream process for {StreamName} with PID: {_process.Id}");
    }

    public void Stop()
    {
        if (_process != null && !_process.HasExited)
        {
            _cancellationToken?.Cancel();
            _process.Kill();
            Logger.LogInformation($"Stopped stream process for {StreamName}");
        }
    }

    private void OnProcessExited(object? sender, EventArgs e)
    {
        Logger.LogWarning($"Stream process for {StreamName} has exited unexpectedly.");
    }

    public void Dispose()
    {
        Stop();
        _process?.Dispose();
        _cancellationToken?.Dispose();
    }
}