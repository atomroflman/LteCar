using CSharpVitamins;
using LteCar.Onboard.Telemetry;
using LteCar.Shared;
using LteCar.Server.Hubs;
using LteCar.Shared.HubClients;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TypedSignalR.Client;
using LteCar.Onboard;

namespace LteCar.Onboard.Control;

public class ControlService : ICarControlClient, IHubConnectionObserver
{
    public IServiceProvider ServiceProvider { get; }
    public ILogger<ControlService> Logger { get; }
    public TelemetryService TelemetryService { get; }
    public ControlExecutionService Control { get; }
    public IConfiguration Configuration { get; }
    public ServerConnectionService ServerConnectionService { get; }
    public SshKeyService SshKeyService { get; }

    private HubConnection _connection;
    private string? _sessionId;
    private DateTime _lastControlUpdate = DateTime.Now;
    private ICarControlServer _server;

    public ControlService(ILogger<ControlService> logger, TelemetryService telemetryService, ControlExecutionService control, IServiceProvider serviceProvider, IConfiguration configuration, ServerConnectionService serverConnectionService, SshKeyService sshKeyService)
    {
        Logger = logger;
        TelemetryService = telemetryService;
        Control = control;
        ServiceProvider = serviceProvider;
        Configuration = configuration;
        ServerConnectionService = serverConnectionService;
        SshKeyService = sshKeyService;
    }

    public void Initialize()
    {
        Control.Initialize();
        Control.ReleaseControl();
    }

    public async Task ConnectToServer()
    {
        _connection = ServerConnectionService.ConnectToHub(HubPaths.CarControlHub);
        _connection.Register<ICarControlClient>(this);
        await _connection.StartAsync();
        _server = _connection.CreateHubProxy<ICarControlServer>();
        var carId = Configuration.GetValue<string>("carId");
        await _server.RegisterForControl(carId);
        Logger.LogInformation("Connected to server.");
        await TelemetryService.UpdateTelemetry("Control Server", "Connected");
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

        // SSH key authentication only
        if (!carSecret.StartsWith("ssh:"))
        {
            Logger.LogError("Cannot aquire control: Only SSH key authentication is supported.");
            return null;
        }

        var sshData = carSecret.Substring(4); // Remove "ssh:" prefix
        var parts = sshData.Split('|');
        if (parts.Length != 3)
        {
            Logger.LogError("Cannot aquire control: Invalid SSH authentication format. Expected: challenge|signature|sessionId");
            return null;
        }

        var challenge = parts[0];
        var signature = parts[1];
        var requestedSessionId = parts[2];
        
        if (SshKeyService.VerifySignature(challenge, signature))
        {
            // Use the session ID provided by the client (generated from their private key)
            _sessionId = requestedSessionId;
            Logger.LogInformation($"Aquired control for car using SSH key. SessionID: {_sessionId}.");
            await TelemetryService.UpdateTelemetry("Control Session", "Connected (SSH)");
            return requestedSessionId;
        }
        else
        {
            Logger.LogError("Cannot aquire control: Invalid SSH signature.");
            return null;
        }
    }

    public Task<string?> GetChallenge()
    {
        var challenge = SshKeyService.GenerateChallenge();
        return Task.FromResult(challenge);
    }

    public async Task ReleaseCarControl(string sessionId)
    {
        if (_sessionId != sessionId)
            return;
        Logger.LogInformation($"Release control for session {sessionId}");
        Control.ReleaseControl();
        await TelemetryService.UpdateTelemetry("Control Session", "Ended");
        _sessionId = null;
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

    public async Task OnClosed(Exception? exception)
    {
        Control.ReleaseControl();
        await TelemetryService.UpdateTelemetry("Control Server", "Disconnected");
    }

    public async Task OnReconnected(string? connectionId)
    {
        await _server.RegisterForControl(Configuration.GetValue<string>("carId"));
        await TelemetryService.UpdateTelemetry("Control Server", "Connected");
    }

    public async Task OnReconnecting(Exception? exception)
    {
        Control.ReleaseControl();
        await TelemetryService.UpdateTelemetry("Control Server", "Disconnected");
    }

}