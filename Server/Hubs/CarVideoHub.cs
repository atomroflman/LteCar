using Microsoft.AspNetCore.SignalR;
using LteCar.Shared;
using LteCar.Server.Configuration;
using MessagePack;
using System.Net.Sockets;
using LteCar.Server.Data;
using LteCar.Shared.Video;
using Microsoft.EntityFrameworkCore;
using LteCar.Server.Services;

namespace LteCar.Server.Hubs;

public class CarVideoHub : Hub<ICarVideoClient>, ICarVideoServer
{
    private readonly LteCarContext _lteCarContext;
    private readonly ILogger<CarVideoHub> _logger;
    private readonly IHubContext<CarConnectionHub, IConnectionHubClient> _carConnectionHub;
    private readonly IConfigurationService _configService;
    private readonly ActiveVideoStreamViewerRegistry _viewerRegistry;

    public VideoStreamReceiverService VideoStreamReceiverService { get; }

    public CarVideoHub(
        LteCarContext lteCarContext,
        VideoStreamReceiverService videoStreamReceiverService,
        ILogger<CarVideoHub> logger,
        IHubContext<CarConnectionHub, IConnectionHubClient> carConnectionHub,
        IConfigurationService configService,
        ActiveVideoStreamViewerRegistry viewerRegistry)
    {
        _lteCarContext = lteCarContext;
        VideoStreamReceiverService = videoStreamReceiverService;
        _logger = logger;
        _carConnectionHub = carConnectionHub;
        _configService = configService;
        _viewerRegistry = viewerRegistry;
    }

    public async Task ConnectCar(string carIdentityKey)
    {
        var car = _lteCarContext.Cars
            .Include(c => c.VideoStreams)
            .FirstOrDefault(c => c.CarIdentityKey == carIdentityKey);
        if (car == null)
        {
            _logger.LogWarning("Car with identity key {CarIdentityKey} not found", carIdentityKey);
            throw new InvalidOperationException($"Car with identity key {carIdentityKey} not found!");
        }
        await this.AddCarToGroupAsync(car.Id);
        _logger.LogInformation("Car {CarIdentityKey} connected with ID {CarId}. Synchronizing viewer-driven video streams.", carIdentityKey, car.Id);

        foreach (var stream in car.VideoStreams.Where(stream => stream.Enabled && _viewerRegistry.GetViewerCount(stream.Id) > 0))
        {
            await StartStreamForViewersAsync(stream);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var stoppedStreamIds = _viewerRegistry.RemoveConnection(Context.ConnectionId);
        foreach (var streamId in stoppedStreamIds)
        {
            var stream = await _lteCarContext.CarVideoStreams.FirstOrDefaultAsync(s => s.Id == streamId);
            if (stream == null)
                continue;

            await StopStreamForViewersAsync(stream);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task StartVideoStream(int streamId)
    {
        var stream = await GetStreamAsync(streamId);
        await StartStreamForViewersAsync(stream);
    }

    private async Task SanitizeStreamSettings(CarVideoStream s)
    {
        if (s.BitrateKbps < 256 || s.BitrateKbps > 100000)
        {
            _logger.LogWarning("Sanitizing bitrate {BitrateKbps} for stream {StreamId}", s.BitrateKbps, s.StreamId);
            s.BitrateKbps = Math.Clamp(s.BitrateKbps, 256, 100000);
        }
        if (s.Framerate < 1 || s.Framerate > 60)
        {
            _logger.LogWarning("Sanitizing framerate {Framerate} for stream {StreamId}", s.Framerate, s.StreamId);
            s.Framerate = Math.Clamp(s.Framerate, 1, 60);
        }
        if (s.Width < 160 || s.Width > 4096)
        {
            _logger.LogWarning("Sanitizing width {Width} for stream {StreamId}", s.Width, s.StreamId);
            s.Width = Math.Clamp(s.Width, 160, 4096);
        }
        if (s.Height < 120 || s.Height > 2160)
        {
            _logger.LogWarning("Sanitizing height {Height} for stream {StreamId}", s.Height, s.StreamId);
            s.Height = Math.Clamp(s.Height, 120, 2160);
        }
        if (s.Brightness < -1 || s.Brightness > 1)
        {
            _logger.LogWarning("Sanitizing brightness {Brightness} for stream {StreamId}", s.Brightness, s.StreamId);
            s.Brightness = Math.Clamp(s.Brightness, -1, 1);
        }
        if (s.BitrateKbps % 64 != 0)
        {
            var original = s.BitrateKbps;
            s.BitrateKbps = (s.BitrateKbps / 64) * 64;
            _logger.LogWarning("Adjusting bitrate {OriginalBitrateKbps} to nearest multiple of 64: {AdjustedBitrateKbps} for stream {StreamId}", original, s.BitrateKbps, s.StreamId);
        }
        if (s.Port < _configService.Janus.PortRangeStart || s.Port > _configService.Janus.PortRangeEnd)
        {
            _logger.LogWarning("Sanitizing port {Port} for stream {StreamId}", s.Port, s.StreamId);
            s.Port = this.VideoStreamReceiverService.FindFreePort(s.Protocol);
        }
    }

    public async Task<IReadOnlyList<VideoStreamInfoModel>> GetVideoStreamsForCar(int carId)
    {
        var streams = await _lteCarContext.CarVideoStreams
            .Where(s => s.CarId == carId)
            .OrderBy(s => s.Priority)
            .ThenBy(s => s.Name)
            .ToListAsync();

        return streams
            .Select(s => new VideoStreamInfoModel()
            {
                Id = s.Id,
                Name = s.Name,
                StreamId = s.StreamId,
                Type = s.Type,
                Location = s.Location,
                Priority = s.Priority,
                Width = s.Width,
                Height = s.Height,
                BitrateKbps = s.BitrateKbps,
                Framerate = s.Framerate,
                Brightness = s.Brightness,
                Enabled = s.Enabled,
                IsActive = s.IsActive,
                ViewerCount = _viewerRegistry.GetViewerCount(s.Id)
            })
            .ToList();
    }

    public async Task ActivateStream(int streamId)
    {
        var stream = await GetStreamAsync(streamId);
        if (!stream.Enabled)
        {
            throw new HubException($"Stream {stream.Name} is disabled.");
        }

        var firstViewer = _viewerRegistry.Activate(Context.ConnectionId, streamId);
        _logger.LogInformation("Connection {ConnectionId} activated stream {StreamId}. Viewers: {ViewerCount}", Context.ConnectionId, streamId, _viewerRegistry.GetViewerCount(streamId));
        if (firstViewer)
        {
            await StartStreamForViewersAsync(stream);
        }
    }

    public async Task DeactivateStream(int streamId)
    {
        var stream = await GetStreamAsync(streamId);
        var lastViewer = _viewerRegistry.Deactivate(Context.ConnectionId, streamId);
        _logger.LogInformation("Connection {ConnectionId} deactivated stream {StreamId}. Viewers: {ViewerCount}", Context.ConnectionId, streamId, _viewerRegistry.GetViewerCount(streamId));
        if (lastViewer)
        {
            await StopStreamForViewersAsync(stream);
        }
    }

    public async Task StopVideoStream(int streamId)
    {
        var stream = await GetStreamAsync(streamId);
        _viewerRegistry.ClearStream(streamId);
        await StopStreamForViewersAsync(stream);
    }

    public async Task ChangeVideoStreamSettings(int streamId, VideoSettingsModel settings)
    {
        var stream = await GetStreamAsync(streamId);
        _logger.LogInformation("Stopping video stream {StreamId} ({StreamName}) for car {CarId}", stream.Id, stream.Name, stream.CarId);
        await Clients.Car(stream.CarId).StopVideoStream(stream.StreamId);
        await VideoStreamReceiverService.StopStream(streamId);
        _logger.LogInformation("Changing video stream settings for stream {StreamId} ({StreamName}) for car {CarId}", stream.Id, stream.Name, stream.CarId);
        // Only update settings that are provided (non-null) and synchronize back to Model
        settings.ApplySettings(stream);
        await SanitizeStreamSettings(stream);
        await _lteCarContext.SaveChangesAsync();

        if (_viewerRegistry.GetViewerCount(streamId) == 0)
        {
            return;
        }

        _logger.LogInformation("Restarting video stream {StreamId} ({StreamName}) for car {CarId} with new settings", stream.Id, stream.Name, stream.CarId);
        var settingsToApply = new VideoSettings()
        {
            Height = stream.Height,
            Width = stream.Width,
            Framerate = stream.Framerate,
            BitrateKbps = stream.BitrateKbps,
            Brightness = stream.Brightness,
            Protocol = stream.Protocol,
            TargetPort = stream.Port
        };
        await Clients.Car(stream.CarId).StartVideoStream(stream.StreamId, settingsToApply);
    }

    public async Task SetVideoStreamEnabled(int carId, int streamId, bool enabled)
    {
        await EnsureDriverCanManageStreamAsync(carId);

        var stream = await _lteCarContext.CarVideoStreams
            .FirstOrDefaultAsync(s => s.Id == streamId && s.CarId == carId)
            ?? throw new InvalidOperationException($"Video stream with ID {streamId} not found for car {carId}.");

        if (stream.Enabled == enabled)
        {
            return;
        }

        stream.Enabled = enabled;
        await _lteCarContext.SaveChangesAsync();
        _logger.LogInformation("Driver changed enabled state for stream {StreamId} on car {CarId} to {Enabled}", streamId, carId, enabled);

        if (enabled)
        {
            return;
        }

        _viewerRegistry.ClearStream(streamId);
        await StopStreamForViewersAsync(stream);
    }

    private async Task<CarVideoStream> GetStreamAsync(int streamId)
    {
        return await _lteCarContext.CarVideoStreams
            .FirstOrDefaultAsync(s => s.Id == streamId)
            ?? throw new InvalidOperationException($"Video stream with ID {streamId} not found.");
    }

    private async Task StartStreamForViewersAsync(CarVideoStream stream)
    {
        _logger.LogInformation("Starting video stream {StreamId} ({StreamName}) for car {CarId}", stream.Id, stream.Name, stream.CarId);
        var settings = await VideoStreamReceiverService.StartStreamAsync(stream.Id);
        stream.IsActive = true;
        await _lteCarContext.SaveChangesAsync();
        await Clients.Car(stream.CarId).StartVideoStream(stream.StreamId, settings);
    }

    private async Task StopStreamForViewersAsync(CarVideoStream stream)
    {
        _logger.LogInformation("Stopping video stream {StreamId} ({StreamName}) for car {CarId}", stream.Id, stream.Name, stream.CarId);
        await Clients.Car(stream.CarId).StopVideoStream(stream.StreamId);
        await VideoStreamReceiverService.StopStream(stream.Id);
        stream.IsActive = false;
        await _lteCarContext.SaveChangesAsync();
    }

    private async Task EnsureDriverCanManageStreamAsync(int carId)
    {
        var httpContext = Context.GetHttpContext()
            ?? throw new HubException("No HTTP context available for video hub request.");
        var user = await HubUserHelper.GetUserAsync(httpContext, _lteCarContext);
        if (user == null || string.IsNullOrWhiteSpace(user.LoginName))
        {
            throw new HubException("You must be logged in to enable or disable streams.");
        }

        var hasSetup = await _lteCarContext.UserSetups.AnyAsync(setup => setup.UserId == user.Id && setup.CarId == carId);
        if (!hasSetup)
        {
            throw new HubException("You do not have access to manage this vehicle.");
        }

        if (user.ActiveVehicleId != carId)
        {
            throw new HubException("You must actively control this vehicle to change stream enable state.");
        }
    }
}