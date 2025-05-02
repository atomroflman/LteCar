using System.Diagnostics;
using System.Text.Json;
using LteCar.Shared;
using Microsoft.Extensions.Logging;

namespace LteCar.Onboard;

public class VideoStreamService : IDisposable
{
    private VideoSettings _videoSettings;
    private Process _libcameraProcess;
    private CancellationTokenSource _libcameraToken;
    private JanusConfiguration _janusConfiguration;
    public ILogger<VideoStreamService> Logger { get; }
    public CarConfigurationService ConfigService { get; }

    public VideoStreamService(ILogger<VideoStreamService> logger, CarConfigurationService configService)
    {
        Logger = logger;
        ConfigService = configService;
        ConfigService.OnConfigurationChanged += () =>
        {
            var config = ConfigService.Configuration;
            var hasSignificantChanges = false;
            if (ConfigService.CheckForChanges(config, _janusConfiguration) && config.JanusConfiguration != null)
            {
                _janusConfiguration = config.JanusConfiguration;
                Logger.LogInformation($"Janus configuration updated: {JsonSerializer.Serialize(_janusConfiguration)}");
                hasSignificantChanges = true;
            }
            if (ConfigService.CheckForChanges(config.VideoSettings, _videoSettings))
            {
                _videoSettings = config.VideoSettings;
                Logger.LogInformation($"Video settings updated: {JsonSerializer.Serialize(_videoSettings)}");
                hasSignificantChanges = true;
            }
            if (hasSignificantChanges)
            {
                StartLibcameraProcess();
            }
        };
    }
    
    public void StartLibcameraProcess()
    {
        if (_videoSettings == null)
        {
            Logger.LogWarning("Video settings not defined using default.");
            _videoSettings = VideoSettings.Default;
        }

        if (_janusConfiguration == null)
        {
            Logger.LogWarning("Janus configuration not defined get server information first. Exit video stream.");
            return;
        }
        
        // Kill the existing process if it's running
        if (_libcameraProcess != null && !_libcameraProcess.HasExited)
        {
            _libcameraToken.Cancel();
            _libcameraProcess.Kill();
            _libcameraProcess.Dispose();
            _libcameraProcess = null;
            Logger.LogInformation("Libcamera process killed for restart with new parameters.");
        }

        var process = new Process();
        var parameters = $"libcamera-vid -t 0 --inline --framerate {_videoSettings.Framerate} --width {_videoSettings.Width} --height {_videoSettings.Height}"
        + $" --codec yuv420 --nopreview -o - | gst-launch-1.0 fdsrc ! videoparse format=i420 width={_videoSettings.Width} height={_videoSettings.Height} framerate={_videoSettings.Framerate}/1" 
        + $" ! vp8enc deadline=1 ! rtpvp8pay pt=100 ! udpsink host={_janusConfiguration.JanusServerHost} port={_janusConfiguration.JanusUdpPort}";

//         var parameters = $@"libcamera-vid -t 0 --inline --width {_videoSettings.Width} --height {_videoSettings.Height} --framerate {_videoSettings.Framerate} \
//   --codec h264 --profile high \
//   -o - | gst-launch-1.0 -v fdsrc ! h264parse ! rtph264pay config-interval=1 pt=96 ! \
//   udpsink host={_janusConfiguration.JanusServerHost} port={_janusConfiguration.JanusUdpPort}";
        process.StartInfo.FileName = "bash";
        process.StartInfo.Arguments = $"-c \"{parameters}\"";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.Exited += OnVideoProcessEnded;
        process.Start();
        _libcameraProcess = process;
        Logger.LogInformation($"Libcamera process started with PID: {process.Id}");
        _libcameraToken = new CancellationTokenSource();
        
        // Read the output stream
        Task.Run(() =>
        {
            while (!process.StandardOutput.EndOfStream)
            {
                string line = process.StandardOutput.ReadLine();
                Logger.LogInformation(line);
            }
        }, _libcameraToken.Token);

        // Read the error stream
        Task.Run(() =>
        {
            while (!process.StandardError.EndOfStream)
            {
                string line = process.StandardError.ReadLine();
                Logger.LogError(line);
            }
        }, _libcameraToken.Token);
    }

    private void OnVideoProcessEnded(object? sender, EventArgs e)
    {
        Logger.LogWarning("LibCamera Process ended.");
    }

    public void Dispose()
    {
        Logger.LogInformation("Disposing VideoStreamService...");
        if (_libcameraProcess != null && !_libcameraProcess.HasExited)
        {
            Logger.LogInformation("Kill LibCamera Process...");
            _libcameraProcess.Kill();
        }
    }
}