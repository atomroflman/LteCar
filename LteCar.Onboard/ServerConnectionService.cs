using LteCar.Shared;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;

namespace LteCar.Onboard;

public class ServerConnectionService
{
    private readonly IConfiguration _configuration;
    private HubConnection _connection;

    public ServerConnectionService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public async Task ConnectToServer(string carId)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(_configuration.GetSection("ServerAddress").GetValue<string>("Url") + "/carconnectionhub")
            .WithAutomaticReconnect()
            .Build();
        _connection.Reconnected += async (connectionId) =>
        {
            Console.WriteLine($"Reconnected to server with connection ID: {connectionId}");
            await _connection.SendAsync("UpdateCarAddress", carId);
        };
        _connection.Reconnecting += (connectionId) =>
        {
            Console.WriteLine($"Reconnecting to server with connection ID: {connectionId}");
            return Task.CompletedTask;
        };
        _connection.On("UpdateCarConfiguration", (JanusConfiguration address) =>
        {
            // Handle the new car address here
        });
        await _connection.StartAsync();
    }

    public async Task RequestJanusConfigAsync()
    {
        if (_connection == null)
        {
            throw new InvalidOperationException("Connection not established.");
        }
        await _connection.SendAsync(nameof(RequestJanusConfigAsync));
    }
}