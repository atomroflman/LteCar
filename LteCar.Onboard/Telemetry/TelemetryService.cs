using LteCar.Shared.Hubs;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TypedSignalR.Client;

namespace LteCar.Onboard.Telemetry;

public class TelemetryService : IHubConnectionObserver
{
    private HubConnection? _connection;
    private ITelemetryServer? _server;
    private string? _carId;
    public IConfiguration Configuration { get; set; }
    public ILogger<TelemetryService> Logger { get; }
    public ServerConnectionService ServerConnectionService { get; }
    
    public TelemetryService(ServerConnectionService serverConnectionService, IConfiguration configuration, ILogger<TelemetryService> logger)
    {
        ServerConnectionService = serverConnectionService;
        Configuration = configuration;
        Logger = logger;
    }

    public async Task ConnectToServer()
    {
        _connection = ServerConnectionService.ConnectToHub("telemetry");
        await _connection.StartAsync();
        _server = _connection.CreateHubProxy<ITelemetryServer>();
        _carId = Configuration.GetValue<string>("carId");
        Logger.LogInformation("Connected to server.");
    }
    
    public async Task UpdateTelemetry(string valueName, string value)
    {
        if (_connection == null)
        {
            Logger.LogError("Cannot send telemetry: Connection is not established.");
            return;
        }
        if (_server == null)
        {
            Logger.LogError("Cannot send telemetry: Server proxy is not set.");
            return;
        }
        if (_connection.State != HubConnectionState.Connected)
        {
            Logger.LogError("Cannot send telemetry: Connection is not in a connected state. State: {State}", _connection.State);
            return;
        }
        if (_carId == null)
        {
            Logger.LogError("Cannot send telemetry: CarId is not set.");
            return;
        }
        await _server.UpdateTelemetry(_carId, valueName, value);
    }

    public async Task OnClosed(Exception? exception)
    {
        Logger.LogError(exception, exception.Message);
    }

    public async Task OnReconnected(string? connectionId)
    {
        await UpdateTelemetry("Telemetry Connection", connectionId);
    }

    public async Task OnReconnecting(Exception? exception)
    {
        Logger.LogError($"Reconnecting after: {exception}");
    }
}