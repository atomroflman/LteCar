using Microsoft.AspNetCore.SignalR;
using LteCar.Shared;
using LteCar.Server.Configuration;
using MessagePack;
using System.Net.Sockets;
using LteCar.Server.Data;
using LteCar.Shared.Video;
using Microsoft.EntityFrameworkCore;

namespace LteCar.Server.Hubs;

public class CarVideoHub : Hub<ICarVideoClient>, ICarVideoServer
{
    private readonly LteCarContext _lteCarContext;
    private readonly ILogger<CarVideoHub> _logger;
    private readonly IHubContext<CarConnectionHub, IConnectionHubClient> _carConnectionHub;
    private readonly IConfigurationService _configService;

    public VideoStreamReceiverService VideoStreamReceiverService { get; }

    public CarVideoHub(
        LteCarContext lteCarContext,
        VideoStreamReceiverService videoStreamReceiverService,
        ILogger<CarVideoHub> logger,
        IHubContext<CarConnectionHub, IConnectionHubClient> carConnectionHub,
        IConfigurationService configService)
    {
        _lteCarContext = lteCarContext;
        VideoStreamReceiverService = videoStreamReceiverService;
        _logger = logger;
        _carConnectionHub = carConnectionHub;
        _configService = configService;
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
        _logger.LogInformation("Car {CarIdentityKey} connected with ID {CarId}. Starting enabled video streams...", carIdentityKey, car.Id);
        foreach (var stream in car.VideoStreams.Where(s => s.Enabled))
        {
            await StartVideoStream(stream.Id);
        }
    }

    public async Task StartVideoStream(int streamId)
    {
        var stream = _lteCarContext.CarVideoStreams
            .Select(s => new { s.Car.CarIdentityKey, s.Name, s.Id, s.CarId, s.Height, s.Width, s.BitrateKbps, s.Framerate, s.Brightness, s.Port, s.Protocol, s })
            .FirstOrDefault(s => s.Id == streamId)
            ?? throw new InvalidOperationException($"Video stream with ID {streamId} not found.");
        _logger.LogInformation("Starting video stream {StreamId} ({StreamName}) for car {CarId}", stream.Id, stream.Name, stream.CarId);
        await VideoStreamReceiverService.StartStreamAsync(streamId);
        stream.s.Enabled = true;
        await _lteCarContext.SaveChangesAsync();   
        await Clients.Car(stream.CarId).StartVideoStream(stream.Name, new VideoSettings()
        {
            Height = stream.Height,
            Width = stream.Width,
            Framerate = stream.Framerate,
            BitrateKbps = stream.BitrateKbps,
            Brightness = stream.Brightness,
            Protocol = stream.Protocol,
            TargetPort = stream.Port
        });     
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

    public async Task<Dictionary<int, VideoSettingsModel>> GetVideoStreamsForCar(int carId)
    {
        var result = new Dictionary<int, VideoSettingsModel>();
        var streams = _lteCarContext.CarVideoStreams
            .Where(s => s.CarId == carId)
            .ToList();

        foreach (var s in streams)
        {
            result[s.Id] = new VideoSettingsModel()
            {
                Width = s.Width,
                Height = s.Height,
                BitrateKbps = s.BitrateKbps,
                Framerate = s.Framerate,
                Brightness = s.Brightness,
                Enabled = s.Enabled
            };
        }
        return result;
    }

    public async Task StopVideoStream(int streamId)
    {
        var stream = _lteCarContext.CarVideoStreams
            .Select(s => new { s.Car.CarIdentityKey, s.Name, s.Id, s.CarId, s })
            .FirstOrDefault(s => s.Id == streamId)
            ?? throw new InvalidOperationException($"Video stream with ID {streamId} not found.");
        _logger.LogInformation("Stopping video stream {StreamId} ({StreamName}) for car {CarId}", stream.Id, stream.Name, stream.CarId);
        await Clients.Car(stream.CarId).StopVideoStream(stream.Name);
        stream.s.Enabled = false;
        await _lteCarContext.SaveChangesAsync();
    }

    public async Task ChangeVideoStreamSettings(int streamId, VideoSettingsModel settings)
    {
         var stream = _lteCarContext.CarVideoStreams
            .Select(s => new { s.Car.CarIdentityKey, s.Name, s.Id, s.CarId, s })
            .FirstOrDefault(s => s.Id == streamId)
            ?? throw new InvalidOperationException($"Video stream with ID {streamId} not found.");
        _logger.LogInformation("Stopping video stream {StreamId} ({StreamName}) for car {CarId}", stream.Id, stream.Name, stream.CarId);
        await Clients.Car(stream.CarId).StopVideoStream(stream.Name);
        _logger.LogInformation("Changing video stream settings for stream {StreamId} ({StreamName}) for car {CarId}", stream.Id, stream.Name, stream.CarId);
        // Only update settings that are provided (non-null) and synchronize back to Model
        settings.ApplySettings(stream.s);
        await SanitizeStreamSettings(stream.s);
        await _lteCarContext.SaveChangesAsync();
        _logger.LogInformation("Restarting video stream {StreamId} ({StreamName}) for car {CarId} with new settings", stream.Id, stream.Name, stream.CarId);
        var settingsToApply = new VideoSettings()
        {
            Height = stream.s.Height,
            Width = stream.s.Width,
            Framerate = stream.s.Framerate,
            BitrateKbps = stream.s.BitrateKbps,
            Brightness = stream.s.Brightness,
            Protocol = stream.s.Protocol,
            TargetPort = stream.s.Port
        };
        await Clients.Car(stream.CarId).StartVideoStream(stream.Name, settingsToApply);
    }
}