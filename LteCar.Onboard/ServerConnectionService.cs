using LteCar.Shared;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TypedSignalR.Client;

namespace LteCar.Onboard;

public class ServerConnectionService
{
    public IServiceProvider ServiceProvider { get; }
    public ILogger<ServerConnectionService> Logger { get; }
    private readonly IConfiguration _configuration;
    private HubConnection _connection;
    // private CarConfigurationService _configService;
    
    public ServerConnectionService(IConfiguration configuration, IServiceProvider serviceProvider, ILogger<ServerConnectionService> logger)
    {
        ServiceProvider = serviceProvider;
        Logger = logger;
        _configuration = configuration;
        // _configService = configService;
    }
    
    public async Task ConnectToServer(string carId)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(_configuration.GetSection("ServerAddress").GetValue<string>("Url") + "/carconnectionhub")
            .WithAutomaticReconnect()
            .Build();
        var connectionServer = _connection.CreateHubProxy<ICarConnectionServer>();
        _connection.Reconnected += async (connectionId) =>
        {
            Logger.LogInformation($"Connection {connectionId} reestablished.");
        };
        _connection.Reconnecting += (connectionId) =>
        {
            Logger.LogWarning($"Reconnecting to server with connection ID: {connectionId}");
            return Task.CompletedTask;
        };
        _connection.Closed += (error) =>
        {
            Logger.LogError($"Connection closed: {error}");
            return Task.CompletedTask;
        };
        await _connection.StartAsync();
        Logger.LogInformation("Connected to server.");
        var config = await connectionServer.OpenCarConnection(carId);
        if (config == null)
        {
            Logger.LogError("Failed to open car connection.");
            return;
        }
        var configService = ServiceProvider.GetRequiredService<CarConfigurationService>();
        configService.UpdateConfiguration(config);
    }
}