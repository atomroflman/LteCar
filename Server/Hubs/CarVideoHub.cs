using Microsoft.AspNetCore.SignalR;
using LteCar.Shared;
using LteCar.Server.Configuration;
using MessagePack;
using System.Net.Sockets;
using LteCar.Onboard;
using LteCar.Server.Data;

namespace LteCar.Server.Hubs;

public interface ICarVideoClient
{
    /// <summary>
    /// Used to start a video stream on the car with specified settings or change the settings of a running Stream.
    /// </summary>
    Task StartVideoStream(string carId, string streamId, VideoSettings settings);
    /// <summary>
    /// Used to stop a running video stream on the car.
    /// </summary>
    Task StopVideoStream(string carId, string streamId);
}

public interface ICarVideoServer
{
    Task<Dictionary<string, VideoStreamConfiguration>> RequestVideoStreamEndpointFor(VideoStreamEndpointRequest request);
}

public class VideoStreamEndpointRequest
{
    [Key(0)]
    public string CarId { get; set; } = string.Empty;
    [Key(1)]
    public List<string> StreamIds { get; set; } = new List<string>();
}

public class VideoStreamConfiguration
{
    [Key(0)]
    public string StreamId { get; set; } = string.Empty;
    [Key(1)]
    public ProtocolType Protocol { get; set; } = ProtocolType.Tcp;
    [Key(2)]
    public int Port { get; set; } = 10000;
    [Key(3)]
    public string Encoding { get; set; } = "H264";
}

public class CarVideoHub : Hub<ICarVideoClient>, ICarVideoServer
{
    private readonly LteCarContext _lteCarContext;
    private readonly ILogger<CarVideoHub> _logger;
    private readonly IHubContext<CarConnectionHub, IConnectionHubClient> _carConnectionHub;
    private readonly IConfigurationService _configService;


    public CarVideoHub(LteCarContext lteCarContext, ILogger<CarVideoHub> logger, IHubContext<CarConnectionHub, IConnectionHubClient> carConnectionHub, IConfigurationService configService)
    {
        _lteCarContext = lteCarContext;
        _logger = logger;
        _carConnectionHub = carConnectionHub;
        _configService = configService;
    }

    public async Task<Dictionary<string, VideoStreamConfiguration>> RequestVideoStreamEndpointFor(VideoStreamEndpointRequest request)
    {
        _logger.LogInformation("Video stream endpoint requested for car {CarId} streams {StreamIds}", request.CarId, string.Join(", ", request.StreamIds));
        var result = new Dictionary<string, VideoStreamConfiguration>();
        var knownStreams = _lteCarContext.CarVideoStreams.Where(s => s.Car.CarIdentityKey == request.CarId && request.StreamIds.Contains(s.StreamId)).ToList();
        
        foreach (var streamId in request.StreamIds)
        {
            var stream = knownStreams.FirstOrDefault(s => s.StreamId == streamId);
            if (stream != null)
            {
                result[streamId] = new VideoStreamConfiguration
                {
                    StreamId = stream.StreamId,
                    Protocol = Enum.Parse<ProtocolType>(stream.Protocol),
                    Port = stream.Port,
                    Encoding = "H264" // Assuming H264 for now; could be extended to store encoding in DB
                };
            }
            else
            {
                var usedPorts = _lteCarContext.CarVideoStreams.Select(s => s.Port);
                var current = _configService.Janus.TcpPortRangeStart;
                while (usedPorts.Contains(current))
                {
                    current++;
                    if (current > _configService.Janus.TcpPortRangeEnd)
                    {
                        throw new InvalidOperationException("No available ports for video streaming.");
                    }
                }
                _lteCarContext.CarVideoStreams.Add(new CarVideoStream
                {
                    Car = _lteCarContext.Cars.First(c => c.CarIdentityKey == request.CarId),
                    StreamId = streamId,
                    Protocol = ProtocolType.Tcp.ToString(),
                    Port = current
                });
                await _lteCarContext.SaveChangesAsync();
                result[streamId] = new VideoStreamConfiguration
                {
                    StreamId = streamId,
                    Protocol = ProtocolType.Tcp,
                    Port = current,
                    Encoding = "H264"
                };
            }
        }

        return result;
    }
}