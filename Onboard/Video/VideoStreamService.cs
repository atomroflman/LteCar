using System.Diagnostics;
using System.Text.Json;
using LteCar.Server.Hubs;
using LteCar.Shared;
using LteCar.Shared.Video;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TypedSignalR.Client;

namespace LteCar.Onboard.Video;

// public class CameraProcessInfo
// {
//     public Process? Process { get; set; }
//     public VideoSettings VideoSettings { get; set; }
//     public int RestartCount { get; set; } = 0;
//     public DateTime StartTime { get; set; } = DateTime.Now;
//     public bool IsProcessRunning => Process != null && !Process.HasExited;

//     public CameraProcessInfo(VideoSettings videoSettings)
//     {
//         VideoSettings = videoSettings;
//     }
// }

public class VideoStreamService : IDisposable, ICarVideoClient, IHubConnectionObserver
{
    // public Dictionary<string, CameraProcessInfo> CameraProcesses { get; set; } = new Dictionary<string, CameraProcessInfo>();
    public ILogger<VideoStreamService> Logger { get; }
    public ServerCarConfigurationService ConfigService { get; }
    public ServerConnectionService ServerConnectionService { get; }
    public IConfiguration Configuration { get; }
    public ICarVideoServer CarVideoServer { get; set; }
    public Process? MediamtxProcess { get; set; }

    public VideoStreamService(ILogger<VideoStreamService> logger, ServerCarConfigurationService configService, ServerConnectionService serverConnectionService, IConfiguration configuration)
    {
        Logger = logger;
        ConfigService = configService;
        ServerConnectionService = serverConnectionService;
        Configuration = configuration;
        ConfigService.OnConfigurationChanged += () =>
        {
            RestartCameraProcesses();
        };
    }

    public void RestartCameraProcesses()
    {
        Logger.LogInformation("Restarting camera processes due to configuration change...");
        StopCameraProcess().Wait();
        StartCameraProcess().Wait();
    }

    private async Task StartCameraProcess()
    {
    //     if (!CameraProcesses.ContainsKey(streamId))
    //     {
    //         Logger.LogWarning($"Cannot start camera process, streamId '{streamId}' has not been initialized.");
    //     }
    //     var cameraProcessInfo = CameraProcesses[streamId];
    //     if (cameraProcessInfo.IsProcessRunning)
    //     {
    //         Logger.LogInformation($"Camera process for streamId '{streamId}' is already running.");
    //         return;
    //     }

    //     var videoSettings = cameraProcessInfo.VideoSettings;
    //     var newProcessStart = new ProcessStartInfo();
    //     newProcessStart.FileName = "/bin/bash"; // Start with bash to allow piping and binaries in PATH
    //     var cameraLib = Configuration.GetSection("CameraOptions:CameraLib").Get<string>();
    //     var cameraBinary = cameraLib switch
    //     {
    //         "libcamera-vid" => "libcamera-vid",
    //         "rpicam-vid" => "rpicam-vid",
    //         _ => throw new NotSupportedException($"Camera library '{cameraLib}' is not supported.")
    //     };
    //     var cameraParameters = $"-t 0 --inline --framerate {videoSettings.Framerate} --width {videoSettings.Width} --height {videoSettings.Height} --libav-format mpeg --codec h264 --nopreview -o -";
    //     var transportFfmpegParameters = $"-i - -c copy -f mpegts tcp://{videoSettings.JanusServer}:{videoSettings.TargetPort}";
    //     if (videoSettings.Protocol == StreamProtocol.UDP)
    //     {
    //         transportFfmpegParameters = $"-i - -c copy -f rtp rtp://{videoSettings.JanusServer}:{videoSettings.TargetPort}";
    //     }
    //     var command = $"{cameraBinary} {cameraParameters} | /usr/bin/ffmpeg {transportFfmpegParameters}";
        

        var newProcessStart = new ProcessStartInfo();
        

        var command = $"{Path.GetFullPath("./Extern/mediamtx")} {Path.GetFullPath("./Extern/mediamtx.yml")}";
        Logger.LogInformation($"Starting camera process with command: {command}");
        newProcessStart.FileName = "bash"; // await GetSetsidPathAsync(); // Use setsid to run the processes as child of init, avoiding zombie processes
        newProcessStart.Arguments = $"-c \"{command}\"";
        newProcessStart.UseShellExecute = false;
        newProcessStart.RedirectStandardOutput = true;
        newProcessStart.RedirectStandardError = true;
        newProcessStart.CreateNoWindow = true;
        MediamtxProcess = new Process();
        MediamtxProcess.StartInfo = newProcessStart;
        MediamtxProcess.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                Logger.LogInformation(e.Data);
            }
        };
        MediamtxProcess.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null && e.Data.StartsWith("ERR"))
            {
                Logger.LogError($"Mediamtx process exited with error: {e.Data}");
            }

            Logger.LogWarning(e.Data);
        };
        MediamtxProcess.Exited += (sender, e) =>
        {
            Logger.LogWarning("Mediamtx process has exited!");
            Logger.LogInformation(MediamtxProcess.StandardOutput.ReadToEnd());
            Logger.LogInformation(MediamtxProcess.StandardError.ReadToEnd());
            //Task.WhenAll(StartCameraProcess(), Task.Delay(5000));
        };
        MediamtxProcess.EnableRaisingEvents = true;
        MediamtxProcess.Start();

        // cameraProcessInfo.StartTime = DateTime.Now;
        // cameraProcessInfo.Process = new Process();
        // cameraProcessInfo.Process.StartInfo = newProcessStart;
        // cameraProcessInfo.Process.OutputDataReceived += (sender, e) =>
        // {
        //     if (e.Data != null)
        //     {
        //         Logger.LogInformation(e.Data);
        //     }
        // };

        // cameraProcessInfo.Process.ErrorDataReceived += (sender, e) =>
        // {
        //     if (e.Data != null && e.Data.StartsWith("ERR"))
        //     {
        //         cameraProcessInfo.RestartCount++;
        //         var runtime = DateTime.Now - cameraProcessInfo.StartTime;
        //         if (runtime.TotalMinutes + 1 < cameraProcessInfo.RestartCount)
        //         {
        //             Logger.LogError($"Camera process for streamId '{streamId}' is crashing too frequently. Stopping further restarts.");
        //             Task.Run(() => StopCameraProcess(streamId));
        //             return;
        //         }
        //         Logger.LogError($"Camera process for streamId '{streamId}' exited with code: {e.Data}");
        //         var errorParts = e.Data.Split(' ');
        //         if (errorParts.Length == 2 && int.TryParse(errorParts[1], out var exitCode))
        //         {
        //             switch (exitCode)
        //             {
        //                 case 0:
        //                     Logger.LogInformation($"Camera process for streamId '{streamId}' exited normally.");
        //                     break;
        //                 case 1:
        //                     Logger.LogWarning("Camera process encountered an error starting the camera. Check camera connection, previous output and settings.");
        //                     break;
        //                 default:
        //                     Logger.LogError("Camera process exited with unexpected code: {ExitCode}", exitCode);
        //                     break;
        //             }
        //         }
        //     }

        //     Logger.LogWarning(e.Data);
        // };
        // cameraProcessInfo.Process.Exited += (sender, e) =>
        // {
        //     Logger.LogWarning($"Camera process for streamId '{streamId}' has exited.");
        //     using var scope = Logger.BeginScope(new {StreamId = streamId});
        //     Logger.LogInformation(cameraProcessInfo.Process.StandardError.ReadToEnd());
        // };
        // cameraProcessInfo.Process.EnableRaisingEvents = true;
        // cameraProcessInfo.Process.Start();
    }

    private async Task<string> GetSetsidPathAsync()
    {
        var commonPaths = new[] { "/usr/bin/setsid", "/bin/setsid" };

        foreach (var path in commonPaths)
        {
            if (File.Exists(path))
            {
                Logger.LogDebug("Found setsid at: {Path}", path);
                return path;
            }
        }

        // Fallback: try using 'which' command
        try
        {
            var whichProcess = Process.Start(new ProcessStartInfo
            {
                FileName = "/usr/bin/which",
                Arguments = "setsid",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });

            if (whichProcess != null)
            {
                await whichProcess.WaitForExitAsync();
                if (whichProcess.ExitCode == 0)
                {
                    var output = await whichProcess.StandardOutput.ReadToEndAsync();
                    var path = output.Trim();
                    Logger.LogDebug("Found setsid via which: {Path}", path);
                    return path;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to locate setsid using which command");
        }

        // Final fallback
        Logger.LogWarning("setsid not found, using 'setsid' and hoping it's in PATH");
        return "setsid";
    }

    private async Task StopCameraProcess()
    {
        if (MediamtxProcess != null && !MediamtxProcess.HasExited)
        {
            Logger.LogInformation($"Stopping Mediamtx process...");
            MediamtxProcess.Kill();
            await MediamtxProcess.WaitForExitAsync();
            MediamtxProcess.Dispose();
            MediamtxProcess = null;
            Logger.LogInformation($"Mediamtx process stopped.");
        }
        // var cameraProcessInfo = CameraProcesses[streamId];
        // if (cameraProcessInfo.Process != null && !cameraProcessInfo.Process.HasExited)
        // {
        //     Logger.LogInformation($"Stopping camera process for streamId '{streamId}'...");
        //     cameraProcessInfo.Process.Kill();
        //     await cameraProcessInfo.Process.WaitForExitAsync();
        //     cameraProcessInfo.Process.Dispose();
        //     cameraProcessInfo.Process = null;
        //     Logger.LogInformation($"Camera process for streamId '{streamId}' stopped.");
        //     CameraProcesses.Remove(streamId);
        // }
    }

    public void Dispose()
    {
        StopCameraProcess().Wait();
    }

    public async Task StartVideoStream(string streamId, VideoSettings settings)
    {
        await StartCameraProcess();
        // if (!CameraProcesses.TryGetValue(streamId, out var cameraProcessInfo) )
        // {
        //     cameraProcessInfo = new CameraProcessInfo(settings);
        //     CameraProcesses.Add(streamId, cameraProcessInfo);
        //     await StartCameraProcess(streamId);
        //     return;
        // }
        // if (JsonSerializer.Serialize(cameraProcessInfo.VideoSettings) != JsonSerializer.Serialize(settings))
        // {
        //     Logger.LogInformation($"Video settings for streamId '{streamId}' have changed. Updating settings.");
        //     cameraProcessInfo.VideoSettings = settings;
        //     await StopCameraProcess(streamId);
        //     await StartCameraProcess(streamId);
        // }
        // else
        // {
        //     Logger.LogInformation($"Video settings for streamId '{streamId}' already exists.");
        // }
    }

    public async Task StopVideoStream(string streamId)
    {
        // await StopCameraProcess(streamId);
    }

    public async Task OnClosed(Exception? exception)
    {
        Logger.LogWarning("VideoStreamService is closed.");
        // foreach (var streamId in CameraProcesses.Keys.ToList())
        //     await StopCameraProcess(streamId);
    }

    public async Task OnReconnected(string? connectionId)
    {
        Logger.LogInformation("VideoStreamService reconnected. ConnectionId: {ConnectionId}", connectionId);
        await CarVideoServer.ConnectCar(Configuration.GetValue<string>("CarIdentityKey"));
    }

    public async Task OnReconnecting(Exception? exception)
    {
        Logger.LogWarning("VideoStreamService tries to reconnect...");
    }

    public async Task Connect()
    {
        var hubConnection = ServerConnectionService.ConnectToHub(HubPaths.CarVideoHub);
        CarVideoServer = hubConnection.CreateHubProxy<ICarVideoServer>();
        hubConnection.Register<ICarVideoClient>(this);
        await hubConnection.StartAsync();
        await CarVideoServer.ConnectCar(Configuration.GetValue<string>("CarIdentityKey"));
    }
}