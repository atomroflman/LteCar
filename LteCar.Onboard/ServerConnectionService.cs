using System.ComponentModel.DataAnnotations;
using LteCar.Shared;
using Microsoft.AspNetCore.Http.Connections;
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
    
    public ServerConnectionService(IConfiguration configuration, IServiceProvider serviceProvider, ILogger<ServerConnectionService> logger)
    {
        ServiceProvider = serviceProvider;
        Logger = logger;
        _configuration = configuration;
    }

    public UriBuilder GetServerUriBuilder() 
    {
        var serverAddressBuilder = new UriBuilder();
        serverAddressBuilder.Host = _configuration.GetValue<string>("ServerName");
        serverAddressBuilder.Scheme = (_configuration.GetValue<bool?>("UseHttps") ?? true) ? "https" : "http";
        serverAddressBuilder.Port = _configuration.GetValue<int?>("ServerPort") ?? 5000;
        return serverAddressBuilder;
    }
    
    public async Task ConnectToServer(string carId)
    {
        var serverUriBuilder = GetServerUriBuilder();
        serverUriBuilder.Path = "carconnectionhub";
        var connectionHubEndpoint = serverUriBuilder.Uri;
        Logger.LogInformation($"Connecting to server: {connectionHubEndpoint}");
        _connection = new HubConnectionBuilder()
            .WithUrl(connectionHubEndpoint)
            .WithAutomaticReconnect()
            .Build();
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
        
        Logger.LogInformation($"Connected to server: {_connection.State}");

        var connectionServer = _connection.CreateHubProxy<ICarConnectionServer>();
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