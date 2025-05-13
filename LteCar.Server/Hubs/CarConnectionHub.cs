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
    
    public CarConnectionHub(IHubContext<CarUiHub, ICarUiClient> uiHub, IConfiguration configuration, ILogger<CarConnectionHub> logger)
    {
        Configuration = configuration;
        UiHub = uiHub;
        Logger = logger;
    }
    
    public async Task<CarConfiguration> OpenCarConnection(string carId, string channelMapHash)
    {
        var dbContext = Context.GetHttpContext().RequestServices.GetRequiredService<LteCarContext>();
        var car = dbContext.Cars.FirstOrDefault(c => c.CarId == carId);
        if (car == null)
        {
            Logger.LogWarning($"Car with ID {carId} not found. Creating a new one.");
            car = new Car() { CarId = carId };
            dbContext.Cars.Add(car);
            await dbContext.SaveChangesAsync();            
        }
        car.LastSeen = DateTime.Now;

        CarConfiguration carConfig = new CarConfiguration();
        carConfig.JanusConfiguration = new JanusConfiguration() {
            JanusServerHost = Configuration.GetSection("JanusConfiguration").GetValue<string>("HostName"),
            JanusUdpPort = car.VideoStreamPort ?? GetNextAvailablePort(),
        };
        carConfig.VideoSettings = car.VideoSettings;
        carConfig.RequiresChannelMapUpdate = car.ChannelMapHash != channelMapHash;
        
        await UiHub.Clients.All.CarStateUpdated(new CarStateModel() {
            Id = carId
        });
        return carConfig;
    }

    private int GetNextAvailablePort()
    {
        var dbContext = Context.GetHttpContext().RequestServices.GetRequiredService<LteCarContext>();
        var lastPort = (dbContext.Cars.OrderByDescending(c => c.VideoStreamPort).FirstOrDefault()?.VideoStreamPort ?? 9999) + 1;
        if (lastPort > 10200) {
            Logger.LogWarning("No available ports. All ports are in use. Take the longest inactive port.");
            var kickedCar = dbContext.Cars.OrderBy(c => c.LastSeen).FirstOrDefault(e => e.VideoStreamPort != null);
            if (kickedCar != null) {
                Logger.LogWarning($"Kicking car {kickedCar} from port {kickedCar.VideoStreamPort}");
                kickedCar.VideoStreamPort = null;
                dbContext.SaveChanges();
                lastPort = kickedCar.VideoStreamPort!.Value;
            }
        }
        return lastPort;
    }

    public async Task Test() 
    {
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
        var dbContext = Context.GetHttpContext().RequestServices.GetRequiredService<LteCarContext>();
        var car = dbContext.Cars.FirstOrDefault(c => c.CarId == carId);
        if (car == null)
        {
            Logger.LogWarning($"Car with ID {carId} not found.");
            return;        
        }
        car.ChannelMapHash = ChannelMapHashProvider.GenerateHash(channelMap);
        foreach (var channel in channelMap)
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
            if (!channelMap.ContainsKey(channel.ChannelName))
            {
                Logger.LogWarning($"Channel with ID {channel.ChannelName} not found in the new channel map. Removing it.");
                dbContext.CarChannels.Remove(channel);
            }
        }
        await dbContext.SaveChangesAsync();
        Logger.LogInformation($"Channel map updated for car {carId}. Channel map hash: {car.ChannelMapHash}");
    }
}