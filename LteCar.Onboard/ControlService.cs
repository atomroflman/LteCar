using CSharpVitamins;
using LteCar.Onboard.Control;
using LteCar.Server.Hubs;
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
    public ServerConnectionService ServerConnectionService { get; }

    private HubConnection _connection;
    private string? _sessionId;
    private DateTime _lastControlUpdate = DateTime.Now;
    private ICarControlServer _server;

    public ControlService(ILogger<ControlService> logger, ControlExecutionService control, IServiceProvider serviceProvider, IConfiguration configuration, ServerConnectionService serverConnectionService)
    {
        Logger = logger;
        Control = control;
        ServiceProvider = serviceProvider;
        Configuration = configuration;
        ServerConnectionService = serverConnectionService;
    }

    public void Initialize() 
    {
        Control.Initialize();
    }

    public async Task ConnectToServer()
    {
        // TODO: Send the setup to the server
        _connection = ServerConnectionService.ConnectToHub("control");
        _connection.Register<ICarControlClient>(this);
        await _connection.StartAsync();
        _server = _connection.CreateHubProxy<ICarControlServer>();
        await _server.RegisterForControl(Configuration.GetValue<string>("carId"));
        Logger.LogInformation("Connected to server.");
    }

    public async Task TestControlsAsync() {
        await Control.RunControlTestsAsync();
    }
    
    public async Task<string?> AquireCarControl(string carSecret)
    {
        if (_sessionId != null && _lastControlUpdate.AddSeconds(30) > DateTime.Now) {
            Logger.LogError("Cannot aquire control: Already connected to driver session.");
            return null;
        }
        var secret = Configuration.GetValue<string>("CarSecret");
        if (string.IsNullOrWhiteSpace(secret))
            Logger.LogWarning("CarSecret is not set!");
        if (secret != carSecret) {
            Logger.LogError("Cannot aquire control: Invalid car secret.");
            return null;
        }
        var sessionId = ShortGuid.NewGuid().ToString();
        _sessionId = sessionId;
        Logger.LogInformation($"Aquired control for car. SessionID: {_sessionId}.");
        return sessionId;
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
        _lastControlUpdate = DateTime.Now;
        return Task.CompletedTask;
    }

    public Task OnClosed(Exception? exception)
    {
        Control.ReleaseControl();
        return Task.CompletedTask;
    }

    public async Task OnReconnected(string? connectionId)
    {
        await _server.RegisterForControl(Configuration.GetValue<string>("carId"));
    }

    public Task OnReconnecting(Exception? exception)
    {
        Control.ReleaseControl();
        return Task.CompletedTask;
    }
}