using System.Text;
using System.Text.Json;
using LteCar.Onboard;
using LteCar.Server;
using LteCar.Server.Data;
using LteCar.Server.Services;
using LteCar.Shared;
using LteCar.Shared.Channels;
using Microsoft.AspNetCore.SignalR;

namespace LteCar.Server.Hubs;

public class CarConnectionHub : Hub<IConnectionHubClient>, ICarConnectionServer
{
    public IHubContext<CarUiHub, ICarUiClient> UiHub { get; }
    public IConfiguration Configuration { get; }
    public ILogger<CarConnectionHub> Logger { get; }

    protected int JanusUdpPortMin => Configuration.GetSection("JanusConfiguration").GetValue<int?>("UdpPortRangeStart") ?? 10000;
    protected int JanusUdpPortMax => Configuration.GetSection("JanusConfiguration").GetValue<int?>("UdpPortRangeEnd") ?? 10200;
    
    public CarConnectionHub(IHubContext<CarUiHub, ICarUiClient> uiHub, IConfiguration configuration, ILogger<CarConnectionHub> logger)
    {
        Configuration = configuration;
        UiHub = uiHub;
        Logger = logger;
    }
    
    public async Task<CarConfiguration> OpenCarConnection(string carId, string channelMapHash)
    {
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
        var janusServerHost = Configuration.GetSection("JanusConfiguration").GetValue<string>("HostName");
        if (string.IsNullOrEmpty(janusServerHost))
        {
            janusServerHost = System.Net.Dns.GetHostName();
            Logger.LogWarning($"Janus server host is not configured. Using default: {janusServerHost}");
        }

        CarConfiguration carConfig = new CarConfiguration();
        carConfig.JanusConfiguration = new JanusConfiguration() {
            JanusServerHost = janusServerHost,
            JanusUdpPort = car.VideoStreamPort ?? GetNextAvailablePort(),
        };
        carConfig.VideoSettings = car.VideoSettings;
        carConfig.RequiresChannelMapUpdate = car.ChannelMapHash != channelMapHash;
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
        await dbContext.SaveChangesAsync();
        Logger.LogInformation($"Channel map updated for car {carId}. Channel map hash: {car.ChannelMapHash}");
    }
}