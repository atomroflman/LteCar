using System.Diagnostics;
using System.Text.Json;
using LteCar.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LteCar.Onboard.Video;

public class GStreamerVideoService : IDisposable
{
    private VideoSettings? _videoSettings;
    private Process? _gstreamerProcess;
    private CancellationTokenSource? _gstreamerToken;
    private readonly string _serverHost;
    private readonly int _serverPort = 10000;
    private bool _isRunning = false;
    
    public ILogger<GStreamerVideoService> Logger { get; }
    public CarConfigurationService ConfigService { get; }
    public IConfiguration Configuration { get; }

    public GStreamerVideoService(ILogger<GStreamerVideoService> logger, CarConfigurationService configService, IConfiguration configuration)
    {
        Logger = logger;
        ConfigService = configService;
        Configuration = configuration;
        
        // Server-Host aus Konfiguration lesen
        _serverHost = configuration.GetConnectionString("DefaultServer") ?? "localhost";
        
        ConfigService.OnConfigurationChanged += () =>
        {
            var config = ConfigService.Configuration;
            if (config?.VideoSettings != null && 
                (_videoSettings == null || ConfigService.CheckForChanges(config.VideoSettings, _videoSettings)))
            {
                _videoSettings = config.VideoSettings;
                Logger.LogInformation($"Video settings updated: {JsonSerializer.Serialize(_videoSettings)}");
                
                // Stream neu starten bei Änderungen
                if (_isRunning)
                {
                    _ = Task.Run(async () =>
                    {
                        await StopGStreamerAsync();
                        await StartGStreamerAsync();
                    });
                }
            }
        };
    }

    public async Task StartAsync()
    {
        if (_isRunning) return;
        
        Logger.LogInformation("GStreamer Video Service starting...");
        
        // Warten auf Konfiguration
        var config = ConfigService.Configuration;
        if (config?.VideoSettings != null)
        {
            _videoSettings = config.VideoSettings;
            Logger.LogInformation($"Video settings loaded: {JsonSerializer.Serialize(_videoSettings)}");
        }
        else
        {
            Logger.LogWarning("Video settings not available, using defaults");
            _videoSettings = VideoSettings.Default;
        }

        _isRunning = true;
        await StartGStreamerAsync();
        
        // Background task für Überwachung
        _ = Task.Run(MonitorGStreamerProcess);
    }

    private async Task MonitorGStreamerProcess()
    {
        while (_isRunning)
        {
            await Task.Delay(5000);
            
            // Prüfen ob Prozess noch läuft
            if (_gstreamerProcess?.HasExited == true && _isRunning)
            {
                Logger.LogWarning("GStreamer process died, restarting...");
                await StartGStreamerAsync();
            }
        }
    }

    private async Task StartGStreamerAsync()
    {
        if (_videoSettings == null)
        {
            Logger.LogWarning("Video settings not available, using defaults");
            _videoSettings = VideoSettings.Default;
        }

        await StopGStreamerAsync();

        try
        {
            var gstreamerCommand = BuildGStreamerCommand();
            Logger.LogInformation($"Starting GStreamer with command: {gstreamerCommand}");

            _gstreamerProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "bash",
                    Arguments = $"-c \"{gstreamerCommand}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            _gstreamerProcess.Exited += OnGStreamerProcessExited;
            _gstreamerProcess.Start();
            
            Logger.LogInformation($"GStreamer process started with PID: {_gstreamerProcess.Id}");
            
            _gstreamerToken = new CancellationTokenSource();

            // Output und Error Streams überwachen
            _ = Task.Run(async () =>
            {
                try
                {
                    while (!_gstreamerProcess.StandardOutput.EndOfStream && !_gstreamerToken.Token.IsCancellationRequested)
                    {
                        var line = await _gstreamerProcess.StandardOutput.ReadLineAsync();
                        if (!string.IsNullOrEmpty(line))
                            Logger.LogDebug($"GStreamer stdout: {line}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error reading GStreamer stdout");
                }
            }, _gstreamerToken.Token);

            _ = Task.Run(async () =>
            {
                try
                {
                    while (!_gstreamerProcess.StandardError.EndOfStream && !_gstreamerToken.Token.IsCancellationRequested)
                    {
                        var line = await _gstreamerProcess.StandardError.ReadLineAsync();
                        if (!string.IsNullOrEmpty(line))
                            Logger.LogWarning($"GStreamer stderr: {line}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error reading GStreamer stderr");
                }
            }, _gstreamerToken.Token);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to start GStreamer process");
        }
    }

    private async Task StopGStreamerAsync()
    {
        if (_gstreamerProcess != null && !_gstreamerProcess.HasExited)
        {
            try
            {
                Logger.LogInformation("Stopping GStreamer process...");
                _gstreamerToken?.Cancel();
                
                _gstreamerProcess.Kill();
                await _gstreamerProcess.WaitForExitAsync();
                
                _gstreamerProcess.Dispose();
                _gstreamerProcess = null;
                
                Logger.LogInformation("GStreamer process stopped");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error stopping GStreamer process");
            }
        }
    }

    private string BuildGStreamerCommand()
    {
        var cameraSource = GetCameraSource();
        var width = _videoSettings?.Width ?? 640;
        var height = _videoSettings?.Height ?? 480;
        var framerate = _videoSettings?.Framerate ?? 25;
        var quality = 80; // Default Qualität

        // Adaptive Qualität basierend auf LTE-Signal (falls verfügbar)
        var adaptiveQuality = GetAdaptiveQuality();
        if (adaptiveQuality > 0)
            quality = adaptiveQuality;

        return $"gst-launch-1.0 -v " +
               $"{cameraSource} ! " +
               $"video/x-raw,width={width},height={height},framerate={framerate}/1 ! " +
               $"videoconvert ! " +
               $"videoscale ! " +
               $"jpegenc quality={quality} ! " +
               $"rtpjpegpay ! " +
               $"udpsink host={_serverHost} port={_serverPort} " +
               $"buffer-size=200000 sync=false async=false";
    }

    private string GetCameraSource()
    {
        var cameraOption = Configuration.GetSection("CameraOptions:CameraOptions").Get<string>();
        
        return cameraOption?.ToLower() switch
        {
            "libcamera" or "libcamera-vid" => "libcamerasrc",
            "rpicam" or "rpicam-vid" => "libcamerasrc", // Newer Raspberry Pi OS
            "v4l2" => "v4l2src device=/dev/video0",
            _ => "libcamerasrc" // Default für Raspberry Pi
        };
    }

    private int GetAdaptiveQuality()
    {
        try
        {
            // LTE-Signalstärke auslesen (falls verfügbar)
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "bash",
                Arguments = "-c \"cat /proc/net/wireless 2>/dev/null | tail -1 | awk '{print $4}' | cut -d. -f1\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });

            if (process != null)
            {
                var output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();

                if (int.TryParse(output, out var signal))
                {
                    // Adaptive Qualität basierend auf Signalstärke
                    return signal switch
                    {
                        > -60 => 90,  // Sehr gutes Signal
                        > -70 => 85,  // Gutes Signal
                        > -80 => 75,  // Mittleres Signal
                        > -90 => 65,  // Schwaches Signal
                        _ => 60       // Sehr schwaches Signal
                    };
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Could not read LTE signal strength for adaptive quality");
        }

        return 0; // Keine Anpassung
    }

    private void OnGStreamerProcessExited(object? sender, EventArgs e)
    {
        var exitCode = _gstreamerProcess?.ExitCode ?? -1;
        Logger.LogWarning($"GStreamer process exited with code: {exitCode}");
    }

    public async Task StopAsync()
    {
        Logger.LogInformation("GStreamer Video Service stopping...");
        _isRunning = false;
        await StopGStreamerAsync();
    }

    public void Dispose()
    {
        Logger.LogInformation("Disposing GStreamer Video Service...");
        _isRunning = false;
        StopGStreamerAsync().Wait(5000);
        _gstreamerToken?.Dispose();
    }
}