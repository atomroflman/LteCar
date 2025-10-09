using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LteCar.Onboard;
using LteCar.Server;
using LteCar.Server.Configuration;
using LteCar.Server.Data;
using LteCar.Server.Services;
using LteCar.Shared;
using LteCar.Shared.Channels;
using Microsoft.AspNetCore.SignalR;

namespace LteCar.Server.Hubs;

public class CarConnectionHub : Hub<IConnectionHubClient>, ICarConnectionServer
{
    // Handshake Overview:
    // 1. Car connects and (optionally) calls SyncChannelMap first sending full ChannelMap.
    // 2. Server upserts channels/video streams, assigns compact numeric IDs and returns:
    //      - Hash (SHA256 of canonical map) stored on both sides
    //      - Normalized ChannelMap + dictionaries name->int id for bandwidth-efficient future messages
    // 3. OpenCarConnection now only needs the hash to determine if a legacy update is required.
    // This reduces startup round trips and prepares for ID-based messaging.
    public IHubContext<CarUiHub, ICarUiClient> UiHub { get; }
    public ILogger<CarConnectionHub> Logger { get; }
    private readonly VideoStreamReceiverService _streamService;
    private readonly IConfigurationService _configService;

    protected int JanusUdpPortMin => _configService.Janus.UdpPortRangeStart;
    protected int JanusUdpPortMax => _configService.Janus.UdpPortRangeEnd;
    
    public CarConnectionHub(IHubContext<CarUiHub, ICarUiClient> uiHub, IConfigurationService configService, ILogger<CarConnectionHub> logger, VideoStreamReceiverService streamService)
    {
        UiHub = uiHub;
        Logger = logger;
        _streamService = streamService;
        _configService = configService;
    }
    
    public async Task<CarConfiguration> OpenCarConnection(string carId, string channelMapHash)
    {
        Logger.LogInformation("Car '{CarId}' attempting to connect: ChannelHash '{ChannelMapHash}'", carId, channelMapHash);
        var dbContext = Context.GetHttpContext()!.RequestServices.GetRequiredService<LteCarContext>();
        var car = dbContext.Cars.FirstOrDefault(c => c.CarId == carId);
        if (car == null)
        {
            Logger.LogWarning($"Car with ID {carId} not found. Creating a new one.");
            car = new Car() { CarId = carId };
            dbContext.Cars.Add(car);
            car.ChannelMapHash = new Guid().ToString();
        }
        car.LastSeen = DateTime.Now;
        var janusServerHost = _configService.Janus.HostName;
        if (string.IsNullOrEmpty(janusServerHost))
        {
            janusServerHost = System.Net.Dns.GetHostName();
            Logger.LogWarning($"Janus server host is not configured. Using default: {janusServerHost}");
        }

        CarConfiguration carConfig = new CarConfiguration();
        carConfig.JanusConfiguration = new LteCar.Shared.JanusConfiguration() {
            JanusServerHost = janusServerHost,
            JanusUdpPort = 10000
        };
        carConfig.VideoSettings = car.VideoSettings;
        carConfig.RequiresChannelMapUpdate = car.ChannelMapHash != channelMapHash;
        if (carConfig.RequiresChannelMapUpdate)
        {
            Logger.LogInformation($"Car '{carId}' channel map hash mismatch. Server: '{car.ChannelMapHash}' Client: '{channelMapHash}'");
        }
        await dbContext.SaveChangesAsync();  
        
        await UiHub.Clients.All.CarStateUpdated(new CarStateModel() {
            Id = carId
        });
        return carConfig;
    }

    private int GetNextAvailablePort()
    {
        var dbContext = Context.GetHttpContext()!.RequestServices.GetRequiredService<LteCarContext>();

        // Get all allocated ports
        var allocatedPorts = dbContext.Cars
            .Where(c => c.VideoStreamPort != null)
            .Select(c => c.VideoStreamPort)
            .ToHashSet();

        // Find the first unallocated port in the range
        for (int port = JanusUdpPortMin; port <= JanusUdpPortMax; port++)
        {
            if (!allocatedPorts.Contains(port))
            {
                return port;
            }
        }

        // If no ports are available, take the longest inactive port
        Logger.LogWarning("No available ports. All ports are in use. Taking the longest inactive port.");
        var kickedCar = dbContext.Cars.OrderBy(c => c.LastSeen).FirstOrDefault(c => c.VideoStreamPort.HasValue);
        if (kickedCar != null && kickedCar.VideoStreamPort.HasValue)
        {
            Logger.LogWarning($"Kicking car {kickedCar.CarId} from port {kickedCar.VideoStreamPort}");
            var port = kickedCar.VideoStreamPort.Value;
            kickedCar.VideoStreamPort = null;
            dbContext.SaveChanges();
            return port;
        }

        throw new InvalidOperationException("No available ports and no cars to kick.");
    }

    public async Task Test() 
    {
        await Task.CompletedTask;
        Logger.LogInformation("Test Invoked");
    }

    public override Task OnConnectedAsync()
    {
        Logger.LogInformation($"Client connected: {Clients.Caller}");
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        Logger.LogWarning($"Client disconnected: {exception}");
        return base.OnDisconnectedAsync(exception);
    }

    public async Task UpdateChannelMap(string carId, ChannelMap channelMap)
    {
        var dbContext = Context.GetHttpContext()!.RequestServices.GetRequiredService<LteCarContext>();
        var car = dbContext.Cars.FirstOrDefault(c => c.CarId == carId);
        if (car == null)
        {
            Logger.LogWarning($"Car with ID {carId} not found.");
            return;        
        }
        car.ChannelMapHash = ChannelMapHashProvider.GenerateHash(channelMap);
        foreach (var channel in channelMap.ControlChannels)
        {
            var channelDb = dbContext.CarChannels.FirstOrDefault(c => c.ChannelName == channel.Key && c.CarId == car.Id);
            if (channelDb == null)
            {
                Logger.LogWarning($"Channel with ID {channel.Key} not found. Creating a new one.");
                channelDb = new CarChannel() { ChannelName = channel.Key, CarId = car.Id };
                dbContext.CarChannels.Add(channelDb);
            }
        }
        foreach (var channel in dbContext.CarChannels.Where(c => c.CarId == car.Id))
        {
            if (!channelMap.ControlChannels.ContainsKey(channel.ChannelName))
            {
                Logger.LogWarning($"Channel with ID {channel.ChannelName} not found in the new channel map. Removing it.");
                dbContext.Set<UserSetupCarChannelNode>().Where(n => n.CarChannelId == channel.Id)
                    .ToList()
                    .ForEach(n => dbContext.Set<UserSetupCarChannelNode>().Remove(n));

                dbContext.CarChannels.Remove(channel);
            }
        }

        foreach (var channel in channelMap.TelemetryChannels)
        {
            var channelDb = dbContext.CarTelemetry.FirstOrDefault(c => c.ChannelName == channel.Key && c.CarId == car.Id);
            if (channelDb == null)
            {
                Logger.LogWarning($"Telemetry channel with ID {channel.Key} not found. Creating a new one.");
                channelDb = new CarTelemetry() { ChannelName = channel.Key, CarId = car.Id };
                dbContext.CarTelemetry.Add(channelDb);
            }
            channelDb.TelemetryType = channel.Value.TelemetryType;
            channelDb.ReadIntervalTicks = channel.Value.ReadIntervalTicks;
        }
        foreach (var channel in dbContext.CarTelemetry.Where(c => c.CarId == car.Id))
        {
            if (!channelMap.TelemetryChannels.ContainsKey(channel.ChannelName))
            {
                Logger.LogWarning($"Telemetry channel with ID {channel.ChannelName} not found in the new channel map. Removing it.");
                dbContext.CarTelemetry.Remove(channel);
            }
        }

        // Handle video streams
        foreach (var stream in channelMap.VideoStreams)
        {
            var streamDb = dbContext.CarVideoStreams.FirstOrDefault(s => s.StreamId == stream.Value.StreamId && s.CarId == car.Id);
            if (streamDb == null)
            {
                Logger.LogInformation($"Video stream with ID {stream.Value.StreamId} not found. Creating a new one.");
                streamDb = new CarVideoStream() 
                { 
                    StreamId = stream.Value.StreamId, 
                    CarId = car.Id,
                    StartTime = DateTime.Now
                };
                dbContext.CarVideoStreams.Add(streamDb);
            }
            
            streamDb.IsActive = stream.Value.Enabled;
            
            // Store stream configuration as JSON for later use
            var streamConfigJson = System.Text.Json.JsonSerializer.Serialize(stream.Value);
            streamDb.ProcessArguments = streamConfigJson;
        }

        // Remove video streams that are no longer in the channel map
        foreach (var stream in dbContext.CarVideoStreams.Where(s => s.CarId == car.Id))
        {
            if (!channelMap.VideoStreams.Values.Any(vs => vs.StreamId == stream.StreamId))
            {
                Logger.LogWarning($"Video stream with ID {stream.StreamId} not found in the new channel map. Removing it.");
                dbContext.CarVideoStreams.Remove(stream);
            }
        }

        await dbContext.SaveChangesAsync();
        Logger.LogInformation($"Channel map updated for car {carId}. Channel map hash: {car.ChannelMapHash}");
    }

    public async Task<ChannelMapSyncResponse> SyncChannelMap(ChannelMapSyncRequest request)
    {
        Logger.LogInformation("SyncChannelMap invoked for car {CarId}", request.CarId);
        var dbContext = Context.GetHttpContext()!.RequestServices.GetRequiredService<LteCarContext>();
        var car = dbContext.Cars.FirstOrDefault(c => c.CarId == request.CarId);
        if (car == null)
        {
            car = new Car() { CarId = request.CarId, LastSeen = DateTime.Now };
            dbContext.Cars.Add(car);
            await dbContext.SaveChangesAsync();
        }

        var map = request.ChannelMap ?? new ChannelMap();

        // Ensure DB entries and build numeric ID maps
        var controlIds = new Dictionary<string,int>();
        foreach (var kv in map.ControlChannels)
        {
            var db = dbContext.CarChannels.FirstOrDefault(c => c.ChannelName == kv.Key && c.CarId == car.Id);
            if (db == null)
            {
                db = new CarChannel { ChannelName = kv.Key, CarId = car.Id };
                dbContext.CarChannels.Add(db);
                await dbContext.SaveChangesAsync();
            }
            controlIds[kv.Key] = db.Id; // numeric id from DB identity
        }
        // Remove stale
        foreach (var stale in dbContext.CarChannels.Where(c => c.CarId == car.Id).ToList())
        {
            if (!map.ControlChannels.ContainsKey(stale.ChannelName))
            {
                dbContext.CarChannels.Remove(stale);
            }
        }

        var telemetryIds = new Dictionary<string,int>();
        foreach (var kv in map.TelemetryChannels)
        {
            var db = dbContext.CarTelemetry.FirstOrDefault(c => c.ChannelName == kv.Key && c.CarId == car.Id);
            if (db == null)
            {
                db = new CarTelemetry
                {
                    ChannelName = kv.Key,
                    CarId = car.Id,
                    TelemetryType = kv.Value.TelemetryType,
                    ReadIntervalTicks = kv.Value.ReadIntervalTicks
                };
                dbContext.CarTelemetry.Add(db);
                await dbContext.SaveChangesAsync();
            }
            db.TelemetryType = kv.Value.TelemetryType;
            db.ReadIntervalTicks = kv.Value.ReadIntervalTicks;
            telemetryIds[kv.Key] = db.Id;
        }
        foreach (var stale in dbContext.CarTelemetry.Where(c => c.CarId == car.Id).ToList())
        {
            if (!map.TelemetryChannels.ContainsKey(stale.ChannelName))
            {
                dbContext.CarTelemetry.Remove(stale);
            }
        }

        var videoIds = new Dictionary<string,int>();
        foreach (var kv in map.VideoStreams)
        {
            var value = kv.Value;
            var db = dbContext.CarVideoStreams.FirstOrDefault(s => s.StreamId == value.StreamId && s.CarId == car.Id);
            if (db == null)
            {
                db = new CarVideoStream { StreamId = value.StreamId, CarId = car.Id, StartTime = DateTime.Now };
                dbContext.CarVideoStreams.Add(db);
                await dbContext.SaveChangesAsync();
            }
            db.IsActive = value.Enabled;
            db.ProcessArguments = JsonSerializer.Serialize(value);
            videoIds[value.StreamId] = db.Id;
        }
        foreach (var stale in dbContext.CarVideoStreams.Where(s => s.CarId == car.Id).ToList())
        {
            if (!map.VideoStreams.Values.Any(v => v.StreamId == stale.StreamId))
            {
                dbContext.CarVideoStreams.Remove(stale);
            }
        }

        await dbContext.SaveChangesAsync();


        // Compute hash (canonical JSON with sorted keys)
        string hash;
        {
            var options = new JsonSerializerOptions { WriteIndented = false };
            var canonical = JsonSerializer.Serialize(map, options);
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(canonical));
            hash = Convert.ToHexString(bytes);
        }
        car.ChannelMapHash = hash;
        car.LastSeen = DateTime.Now;
        await dbContext.SaveChangesAsync();

        var response = new ChannelMapSyncResponse
        {
            Hash = hash,
            ChannelMap = map,
            ControlIds = controlIds,
            TelemetryIds = telemetryIds,
            VideoIds = videoIds,
            GeneratedAtUtc = DateTime.UtcNow
        };

        // Annotate map items with their server ids for persistence on the client side
        foreach (var kv in map.ControlChannels)
        {
            if (controlIds.TryGetValue(kv.Key, out var id))
            {
                kv.Value.ServerId = id;
            }
        }
        foreach (var kv in map.TelemetryChannels)
        {
            if (telemetryIds.TryGetValue(kv.Key, out var id))
            {
                kv.Value.ServerId = id;
            }
        }
        foreach (var kv in map.VideoStreams)
        {
            if (videoIds.TryGetValue(kv.Value.StreamId, out var id))
            {
                kv.Value.ServerId = id;
            }
        }

        Logger.LogInformation("ChannelMap sync complete for {CarId} hash {Hash}", request.CarId, hash);
        return response;
    }
}