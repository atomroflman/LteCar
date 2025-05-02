using System.Diagnostics;

namespace LteCar.Server;

public class VideoStreamRecieverService
{
    private Process? _janusProcess;

    public VideoStreamRecieverService(ILogger<VideoStreamRecieverService> logger)
    {
        Logger = logger;
    }

    public ILogger<VideoStreamRecieverService> Logger { get; }

    public void RunVideoStreamServer()
    {
        var startParams = new ProcessStartInfo("/opt/janus/bin/janus") {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };
        _janusProcess = new Process();
        _janusProcess.OutputDataReceived += (obj, e) => {
            Logger.LogInformation(e.Data);
        };
        _janusProcess.StartInfo = startParams;
        _janusProcess.Start();
    }
}