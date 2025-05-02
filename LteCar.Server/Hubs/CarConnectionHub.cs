using System.Text;
using System.Text.Json;
using LteCar.Onboard;
using LteCar.Server;
using LteCar.Server.Services;
using LteCar.Shared;
using Microsoft.AspNetCore.SignalR;

namespace LteCar.Server.Hubs;

public class CarConnectionHub : Hub<IConnectionHubClient>, ICarConnectionServer
{
    public IHubContext<CarUiHub, ICarUiClient> UiHub { get; }
    public IConfiguration Configuration { get; }
    public ILogger<CarConnectionHub> Logger { get; }
    private readonly CarConnectionStore _carConnectionStore;
    
    public CarConnectionHub(CarConnectionStore carConnectionStore, IHubContext<CarUiHub, ICarUiClient> uiHub, IConfiguration configuration, ILogger<CarConnectionHub> logger)
    {
        Configuration = configuration;
        _carConnectionStore = carConnectionStore;
        UiHub = uiHub;
        Logger = logger;
    }
    
    public async Task<CarConfiguration> OpenCarConnection(string carId)
    {
        Logger.LogInformation($"Open Car Connection: {carId}");
        if (_carConnectionStore.TryGetValue(carId, out var carConfiguration))
        {
            return carConfiguration.CarConfiguration;
        }
        var storagePath = Configuration.GetSection("CarConfigStore").GetValue<string>("Path");
        Logger.LogDebug($"Storage Path: {storagePath}");
        if (storagePath == null)
            throw new Exception("CarConfigStore.Path needs to be configured.");

        var filePath = Path.Combine(storagePath, carId);
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Directory!.Exists)
            fileInfo.Directory.Create();
        CarConfiguration carConfig = null;
        if (fileInfo.Exists)
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
        }
        
        if (carConfig.JanusConfiguration == null)
            carConfig.JanusConfiguration = new JanusConfiguration();
        carConfig.JanusConfiguration.JanusServerHost 
            = Configuration.GetSection("JanusConfiguration").GetValue<string>("HostName");
        
        // TODO: Track Open Ports
        carConfig.JanusConfiguration.JanusUdpPort = 10000;
        
        if (carConfig.VideoSettings == null)
            carConfig.VideoSettings = VideoSettings.Default;

        var json = JsonSerializer.Serialize(carConfig);
            File.WriteAllText(filePath, json);
        await UiHub.Clients.All.CarStateUpdated(new CarStateModel() {
            Id = carId
        });
        return carConfig;
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
}