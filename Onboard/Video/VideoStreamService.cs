using System.Diagnostics;
using System.Text.Json;
using LteCar.Shared;
using Microsoft.Extensions.Configuration;
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
    public CameraProcessParameterBuilder CameraProcessParameterBuilder { get; }

    public VideoStreamService(ILogger<VideoStreamService> logger, CarConfigurationService configService, CameraProcessParameterBuilder cameraProcessParameterBuilder)
    {
        Logger = logger;
        ConfigService = configService;
        CameraProcessParameterBuilder = cameraProcessParameterBuilder;
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

        var parameters = CameraProcessParameterBuilder.BuildParameters(_videoSettings)
            + CameraProcessParameterBuilder.AppendJanusConfig(_janusConfiguration);
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

public class CameraProcessParameterBuilder {
    IConfiguration _configuration;
    public CameraProcessParameterBuilder(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string BuildParameters(VideoSettings videoSettings) {
        var camOption = _configuration.GetSection("CameraOptions:CameraOptions").Get<string>();
        switch (camOption) {
            case "libcamera-vid":
                return BuildLibcameraParameters(videoSettings);
            case "rpicam-vid":
                return BuildRaspividParameters(videoSettings);
            default:
                throw new NotSupportedException($"Camera option '{camOption}' is not supported.");
        }
    }

    private string BuildRaspividParameters(VideoSettings videoSettings)
    {
        return $"rpicam-vid -t 0 --inline --framerate {videoSettings.Framerate} --width {videoSettings.Width} --height {videoSettings.Height}"
        + $" --codec yuv420 --nopreview -o";
    }

    private string BuildLibcameraParameters(VideoSettings videoSettings)
    {
        return $"libcamera-vid -t 0 --inline --framerate {videoSettings.Framerate} --width {videoSettings.Width} --height {videoSettings.Height}"
        + $" --codec yuv420 --nopreview -o";
    }

    public string AppendJanusConfig(JanusConfiguration janusConfiguration)
    {
        return " - | gst-launch-1.0 fdsrc ! videoparse format=i420 width={videoSettings.Width} height={videoSettings.Height} framerate={videoSettings.Framerate}/1"
        + $" ! vp8enc deadline=1 ! rtpvp8pay pt=100 ! udpsink host={janusConfiguration.JanusServerHost} port={janusConfiguration.JanusUdpPort}";
    }
}