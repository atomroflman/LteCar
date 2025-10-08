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
using LteCar.Shared;

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
    public CarConfigurationService CarConfigurationService { get; }

    private HubConnection _connection;
    private string? _sessionId;
    private DateTime _lastControlUpdate = DateTime.Now;
    private ICarControlServer _server;

    public ControlService(ILogger<ControlService> logger, TelemetryService telemetryService, ControlExecutionService control, IServiceProvider serviceProvider, IConfiguration configuration, ServerConnectionService serverConnectionService, SshKeyService sshKeyService, CarConfigurationService carConfigurationService)
    {
        Logger = logger;
        TelemetryService = telemetryService;
        Control = control;
        ServiceProvider = serviceProvider;
        Configuration = configuration;
        ServerConnectionService = serverConnectionService;
        SshKeyService = sshKeyService;
        CarConfigurationService = carConfigurationService;
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
        var carId = CarConfigurationService.ServerAssignedCarId;
        if (!carId.HasValue)
        {
            Logger.LogError("Cannot connect to control server: ServerAssignedCarId not available");
            return;
        }
        await _server.RegisterForControl(carId.Value);
        Logger.LogInformation($"Connected to control server with CarId: {carId}");
        await TelemetryService.UpdateTelemetry("Control Server", "Connected");
    }

    public async Task TestControlsAsync() {
        await Control.RunControlTestsAsync();
    }
    
    public async Task<string?> AquireCarControl(SshAuthenticationRequest authRequest)
    {
        if (_sessionId != null && _lastControlUpdate.AddSeconds(30) > DateTime.Now) {
            Logger.LogError("Cannot aquire control: Already connected to driver session.");
            return null;
        }

        // Verify SSH signature
        if (SshKeyService.VerifySignature(authRequest.Challenge, authRequest.Signature))
        {
            // Generate a new session ID using ShortGuid (22 chars instead of 36)
            var newSessionId = ShortGuid.NewGuid().ToString();
            _sessionId = newSessionId;
            Logger.LogInformation($"Acquired control for car using SSH key. SessionID: {_sessionId}.");
            await TelemetryService.UpdateTelemetry("Control Session", "Connected (SSH)");
            return newSessionId; // Return the newly generated session ID
        }
        else
        {
            Logger.LogError("Cannot acquire control: Invalid SSH signature.");
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
        var carId = CarConfigurationService.ServerAssignedCarId;
        if (carId.HasValue)
        {
            await _server.RegisterForControl(carId.Value);
        }
        await TelemetryService.UpdateTelemetry("Control Server", "Connected");
    }

    public async Task OnReconnecting(Exception? exception)
    {
        Control.ReleaseControl();
        await TelemetryService.UpdateTelemetry("Control Server", "Disconnected");
    }

}