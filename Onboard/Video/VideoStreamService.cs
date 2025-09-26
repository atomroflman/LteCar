using System.Diagnostics;
using System.Text.Json;
using LteCar.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LteCar.Onboard.Video;

public class VideoStreamService : IDisposable
{
    private VideoSettings _videoSettings;
    private Process _libcameraProcess;
    private CancellationTokenSource _libcameraToken;
    private JanusConfiguration _janusConfiguration;
    private Task _outputReaderTask;
    private Task _errorReaderTask;
    private int _restartAttempts;
    private const int MaxRestartAttempts = 3;
    private const int RestartDelayMs = 5000;
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

        var parameters = CameraProcessParameterBuilder.BuildParameters(_videoSettings, _janusConfiguration);
        Logger.LogInformation($"Starting video with parameters: {parameters}");
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
        _outputReaderTask = Task.Run(async () =>
        {
            try
            {
                while (!process.StandardOutput.EndOfStream && !_libcameraToken.Token.IsCancellationRequested)
                {
                    string line = await process.StandardOutput.ReadLineAsync();
                    if (line != null) Logger.LogDebug(line);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error reading output stream");
            }
        }, _libcameraToken.Token);

        // Read the error stream
        _errorReaderTask = Task.Run(async () =>
        {
            try
            {
                while (!process.StandardError.EndOfStream && !_libcameraToken.Token.IsCancellationRequested)
                {
                    string line = await process.StandardError.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line)) 
                        continue;
                    
                    // Nur echte Fehler als Error loggen
                    if (line.ToLower().Contains("error") || line.Contains("FATAL") || 
                        line.Contains("exception") || line.Contains("failed") ||
                        line.Contains("Cannot"))
                    {
                        Logger.LogError(line);
                    }
                    // H.264/libcamera/GStreamer Info als Debug loggen
                    else if (line.StartsWith("[") || line.Contains("Pipeline") || 
                             line.Contains("Setting") || line.Contains("libcamera") ||
                             line.Contains("profile") || line.Contains("Stream"))
                    {
                        Logger.LogDebug(line);
                    }
                    // Rest als Information
                    else
                    {
                        Logger.LogInformation(line);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error reading error stream");
            }
        }, _libcameraToken.Token);
    }

    private async void OnVideoProcessEnded(object? sender, EventArgs e)
    {
        Logger.LogWarning("LibCamera Process ended.");
        
        try
        {
            if (_libcameraProcess != null && _libcameraProcess.ExitCode != 0)
            {
                if (_restartAttempts < MaxRestartAttempts)
                {
                    _restartAttempts++;
                    Logger.LogInformation($"Attempting to restart video stream (attempt {_restartAttempts}/{MaxRestartAttempts})");
                    await Task.Delay(RestartDelayMs);
                    StartLibcameraProcess();
                }
                else
                {
                    Logger.LogError($"Video stream failed after {MaxRestartAttempts} attempts. Manual intervention required.");
                }
            }
            else
            {
                _restartAttempts = 0;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in video process ended handler");
        }
    }

    public async void Dispose()
    {
        Logger.LogInformation("Disposing VideoStreamService...");
        try
        {
            if (_libcameraToken != null)
            {
                _libcameraToken.Cancel();
            }

            if (_libcameraProcess != null && !_libcameraProcess.HasExited)
            {
                Logger.LogInformation("Kill LibCamera Process...");
                _libcameraProcess.Kill();
                _libcameraProcess.Dispose();
            }

            // Wait for reader tasks to complete with timeout
            if (_outputReaderTask != null || _errorReaderTask != null)
            {
                await Task.WhenAll(
                    _outputReaderTask ?? Task.CompletedTask,
                    _errorReaderTask ?? Task.CompletedTask
                ).WaitAsync(TimeSpan.FromSeconds(5));
            }

            _libcameraToken?.Dispose();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error disposing VideoStreamService");
        }
    }
}

public class CameraProcessParameterBuilder {
    IConfiguration _configuration;
    public CameraProcessParameterBuilder(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string BuildParameters(VideoSettings videoSettings, JanusConfiguration janusConfiguration) {
        var camOption = _configuration.GetSection("CameraOptions:CameraLib").Get<string>();
        switch (camOption) {
            case "custom":
                var customCommand = _configuration.GetSection("CameraOptions:CustomCameraCommand").Get<string>();
                if (string.IsNullOrWhiteSpace(customCommand))
                {
                    throw new ArgumentException("Custom camera command is not defined in configuration.");
                }
                return customCommand;
            case "libcamera-vid":
                return $"libcamera-vid -t 0 -n --libav-format h264 --nopreview --low-latency --framerate {videoSettings.Framerate} --width {videoSettings.Width} --height {videoSettings.Height} -q 50 --inline -o tcp://{janusConfiguration.JanusServerHost}:{janusConfiguration.JanusUdpPort}";
            case "rpicam-vid":
                return $"rpicam-vid -t 0 -n --libav-format h264 --nopreview --low-latency --framerate {videoSettings.Framerate} --width {videoSettings.Width} --height {videoSettings.Height} -q 50 --inline -o tcp://{janusConfiguration.JanusServerHost}:{janusConfiguration.JanusUdpPort}";
            default:
                throw new NotSupportedException($"Camera option '{camOption}' is not supported.");
        }
    }
}