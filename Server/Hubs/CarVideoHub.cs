using LteCar.Shared.Video;
using Microsoft.AspNetCore.SignalR;
using LteCar.Server.Services;
using LteCar.Shared;
using LteCar.Server.Configuration;

namespace LteCar.Server.Hubs;

public interface ICarVideoClient
{
    Task VideoStreamStarted(VideoStreamStartedEvent evt);
}

public interface ICarVideoServer
{
    Task RequestVideoStream(VideoStreamStartCommand command);
}

public class CarVideoHub : Hub<ICarVideoClient>, ICarVideoServer
{
    private readonly ILogger<CarVideoHub> _logger;
    private readonly IHubContext<CarConnectionHub, IConnectionHubClient> _carConnectionHub;
    private readonly IConfigurationService _configService;

    public CarVideoHub(ILogger<CarVideoHub> logger, IHubContext<CarConnectionHub, IConnectionHubClient> carConnectionHub, IConfigurationService configService)
    {
        _logger = logger;
        _carConnectionHub = carConnectionHub;
        _configService = configService;
    }

    public async Task RequestVideoStream(VideoStreamStartCommand command)
    {
        _logger.LogInformation("Video stream requested for car {CarId} stream {StreamId}", command.CarId, command.StreamId);
        // Determine settings: prefer explicit command settings, else server default config
        var settings = command.Settings ?? LteCar.Onboard.VideoSettings.Default;
        // For now just log; actual forwarding to car hardware would be via CarConnectionHub -> specific client group when implemented.
        // TODO: integrate with a per-car client invocation once car exposes a handler.
        await Clients.Caller.VideoStreamStarted(new VideoStreamStartedEvent
        {
            CarId = command.CarId,
            StreamId = command.StreamId,
            Settings = settings,
            TimestampUtc = DateTime.UtcNow
        });
    }
}