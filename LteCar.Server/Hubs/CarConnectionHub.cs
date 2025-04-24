using System.Text;
using System.Text.Json;
using LteCar.Server;
using LteCar.Shared;
using Microsoft.AspNetCore.SignalR;

namespace LteCar.Server.Hubs;

public class CarConnectionHub : Hub<IConnectionHubClient>, ICarConnectionServer
{
    public IConfiguration Configuration { get; }
    public ILogger<CarConnectionHub> Logger { get; }
    private readonly CarConnectionStore _carConnectionStore;

    public CarConnectionHub(CarConnectionStore carConnectionStore, IConfiguration configuration, ILogger<CarConnectionHub> logger)
    {
        Configuration = configuration;
        _carConnectionStore = carConnectionStore;
        Logger = logger;
    }
    
    public async Task<CarConfiguration> OpenCarConnection(string carId)
    {
        Logger.LogInformation($"Open Car Connection: {carId}");
        if (_carConnectionStore.TryGetValue(carId, out var carConfiguration))
        {
            return carConfiguration.CarConfiguration;
        }
        var storagePath = Configuration.GetValue<string>("StoragePath");
        Logger.LogDebug($"Storage Path: {storagePath}");
        var filePath = Path.Combine(storagePath, carId);
        CarConfiguration carConfig = null;
        if (File.Exists(filePath))
        {
            var config = JsonSerializer.Deserialize<CarConfiguration>(File.ReadAllText(filePath));
            _carConnectionStore.Add(carId, new CarConnectionInfo() { CarConfiguration = config });
            carConfig = config;
            Logger.LogDebug("Loaded car config...");
        }
        if (carConfig == null) 
        {
            carConfig = new CarConfiguration();
            _carConnectionStore.Add(carId, new CarConnectionInfo() { CarConfiguration = carConfig });
            var json = JsonSerializer.Serialize(carConfig);
            File.WriteAllText(filePath, json);
        }
        
        if (carConfig.JanusConfiguration == null)
            carConfig.JanusConfiguration = new JanusConfiguration();
        carConfig.JanusConfiguration.JanusServerHost 
            = Configuration.GetSection("JanusConfiguration").GetValue<string>("HostName");
        
        // TODO: Track Open Ports
        carConfig.JanusConfiguration.JanusUdpPort = 10000;
        
        return carConfig;
    }
}