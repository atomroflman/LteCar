using System.Diagnostics;
using LteCar.Server.Hubs;
using LteCar.Shared;
using LteCar.Shared.Channels;
using LteCar.Shared.HubClients;
using LteCar.Shared.Hubs;
using LteCar.Shared.Video;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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
    public ILogger<VideoStreamService> Logger { get; }
    public ServerCarConfigurationService ConfigService { get; }
    public ServerConnectionService ServerConnectionService { get; }
    public IConfiguration Configuration { get; }
    public IConnectionHubServer CarVideoServer { get; set; } = null!;
    private readonly ChannelMap _channelMap;
    private readonly IMediaMtxConfigurator _mediaMtxConfigurator;
    private readonly Dictionary<string, int> _activeStreamPorts = new();
    private readonly object _sync = new();

    public VideoStreamService(
        ILogger<VideoStreamService> logger,
        ServerCarConfigurationService configService,
        ServerConnectionService serverConnectionService,
        IConfiguration configuration,
        ChannelMap channelMap,
        IMediaMtxConfigurator mediaMtxConfigurator)
    {
        Logger = logger;
        ConfigService = configService;
        ServerConnectionService = serverConnectionService;
        Configuration = configuration;
        _channelMap = channelMap;
        _mediaMtxConfigurator = mediaMtxConfigurator;
    }

    public void RestartCameraProcesses()
    {
        RestartCameraProcessesAsync().Wait();
    }

    private async Task RestartCameraProcessesAsync()
    {
        var activePorts = SnapshotPorts();
        if (activePorts.Count == 0)
        {
            await _mediaMtxConfigurator.StopAsync();
            return;
        }

        Logger.LogInformation("Rebuilding MediaMTX config for {Count} active video streams.", activePorts.Count);
        await _mediaMtxConfigurator.GenerateFromChannelMapAsync(_channelMap, activePorts);
        await _mediaMtxConfigurator.StopAsync();
        await _mediaMtxConfigurator.StartProcessAsync();
    }

    private Dictionary<string, int> SnapshotPorts()
    {
        lock (_sync)
        {
            return _activeStreamPorts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }

    public void Dispose()
    {
        _mediaMtxConfigurator.StopAsync().Wait();
    }

    public async Task StartVideoStream(string streamId, VideoSettings settings)
    {
        lock (_sync)
        {
            _activeStreamPorts[streamId] = settings.TargetPort;
        }

        await RestartCameraProcessesAsync();
    }

    public async Task StopVideoStream(string streamId)
    {
        lock (_sync)
        {
            _activeStreamPorts.Remove(streamId);
        }

        await RestartCameraProcessesAsync();
    }

    public async Task OnClosed(Exception? exception)
    {
        Logger.LogWarning("VideoStreamService is closed.");
        await _mediaMtxConfigurator.StopAsync();
    }

    public async Task OnReconnected(string? connectionId)
    {
        Logger.LogInformation("VideoStreamService reconnected. ConnectionId: {ConnectionId}", connectionId);
        await CarVideoServer.ConnectCar(Configuration.GetValue<string>("CarIdentityKey")!);
    }

    public async Task OnReconnecting(Exception? exception)
    {
        Logger.LogWarning("VideoStreamService tries to reconnect...");
    }

    public async Task Connect()
    {
        CarVideoServer = ServerConnectionService.GetProxy();
        await CarVideoServer.ConnectCar(Configuration.GetValue<string>("CarIdentityKey")!);
    }
}
