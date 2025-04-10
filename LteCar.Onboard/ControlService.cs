using CSharpVitamins;
using LteCar.Onboard.Control;
using LteCar.Shared.HubClients;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TypedSignalR.Client;

namespace LteCar.Onboard;

public class ControlService : ICarControlClient, IHubConnectionObserver
{
    public IServiceProvider ServiceProvider { get; }
    public ILogger<ControlService> Logger { get; }
    public ControlExecutionService Control { get; }
    public IConfiguration Configuration { get; }
    private HubConnection _connection;
    private string? _sessionId;

    public ControlService(ILogger<ControlService> logger, ControlExecutionService control, IServiceProvider serviceProvider, IConfiguration configuration, IConfiguration hubConfiguration)
    {
        Logger = logger;
        Control = control;
        ServiceProvider = serviceProvider;
        Configuration = configuration;
    }
    public async Task ConnectToServer(string carId)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(Configuration.GetSection("ServerAddress").GetValue<string>("Url") + "/carconnectionhub")
            .WithAutomaticReconnect()
            .Build();
        _connection.Register<ICarControlClient>(this);
        
        await _connection.StartAsync();
        Logger.LogInformation("Connected to server.");
    }
    
    public Task<string> AquireCarControl(string carSecret)
    {
        if (_sessionId != null)
            throw new ApplicationException("Already connected to driver session.");
        var secret = Configuration.GetValue<string>("CarSecret");
        if (secret != carSecret)
            throw new ApplicationException("Invalid car secret.");
        var sessionId = ShortGuid.NewGuid().ToString();
        _sessionId = sessionId;
        Logger.LogInformation($"Aquired control for car. SessionID: {_sessionId}.");
        return Task.FromResult(sessionId);
    }

    public Task ReleaseCarControl(string sessionId)
    {
        if (_sessionId != sessionId)
            return Task.CompletedTask;
        Logger.LogInformation($"Release control for session {sessionId}");
        Control.ReleaseControl();
        _sessionId = null;
        return Task.CompletedTask;
    }

    public Task UpdateChannel(string sessionId, string channelId, decimal value)
    {
        if (_sessionId != sessionId)
            return Task.CompletedTask;
        Logger.LogDebug($"Update channel {channelId} to {value}");
        Control.SetControl(channelId, value);
        return Task.CompletedTask;
    }

    public Task OnClosed(Exception? exception)
    {
        Control.ReleaseControl();
        return Task.CompletedTask;
    }

    public Task OnReconnected(string? connectionId)
    {
        return Task.CompletedTask;
    }

    public Task OnReconnecting(Exception? exception)
    {
        Control.ReleaseControl();
        return Task.CompletedTask;
    }
}