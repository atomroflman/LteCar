using System.Text;
using System.Text.Json;
using LteCar.Server;
using LteCar.Shared;
using Microsoft.AspNetCore.SignalR;

public class CarConnectionHub : Hub<IConnectionHubClient>, ICarConnectionServer
{
    public IConfiguration Configuration { get; }
    private readonly CarConnectionStore _carConnectionStore;

    public CarConnectionHub(CarConnectionStore carConnectionStore, IConfiguration configuration)
    {
        Configuration = configuration;
        _carConnectionStore = carConnectionStore;
    }
    
    public async Task<CarConfiguration> OpenCarConnection(string carId)
    {
        if (_carConnectionStore.TryGetValue(carId, out var carConfiguration))
        {
            return carConfiguration.CarConfiguration;
        }
        var storagePath = Configuration.GetValue<string>("StoragePath");
        var filePath = Path.Combine(storagePath, carId);
        CarConfiguration carConfig = null;
        if (File.Exists(filePath))
        {
            var config = JsonSerializer.Deserialize<CarConfiguration>(File.ReadAllText(filePath));
            _carConnectionStore.Add(carId, new CarConnectionInfo() { CarConfiguration = config });
            carConfig = config;
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