using LteCar.Shared.Hubs;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TypedSignalR.Client;

namespace LteCar.Onboard.Telemetry;

public class TelemetryService : IHubConnectionObserver, ITelemetryClient
{
    private int _tick = 0;
    private HubConnection? _connection;
    private ITelemetryServer? _server;
    private string? _carId;
    public IConfiguration Configuration { get; set; }
    public ILogger<TelemetryService> Logger { get; }
    public ServerConnectionService ServerConnectionService { get; }
    public ChannelMap ChannelMap { get; }
    
    public TelemetryService(ChannelMap channelMap, ServerConnectionService serverConnectionService, IConfiguration configuration, ILogger<TelemetryService> logger)
    {
        ServerConnectionService = serverConnectionService;
        Configuration = configuration;
        Logger = logger;
        ChannelMap = channelMap;
    }

    public async Task ConnectToServer()
    {
        _connection = ServerConnectionService.ConnectToHub(HubPaths.TelemetryHub);
        await _connection.StartAsync();
        _server = _connection.CreateHubProxy<ITelemetryServer>();
        _connection.Register<ITelemetryClient>(this);
        _connection.RegisterObserver(this);
        _carId = Configuration.GetValue<string>("carId");
        Logger.LogInformation("Connected to server.");

        foreach (var telemetryChannel in ChannelMap.TelemetryChannels)
        {
            Logger.LogInformation("Registering telemetry channel: {Channel}", telemetryChannel);

            await _server.RegisterTelemetryChannel(_carId, telemetryChannel);
        }
    }

    public async Task<IEnumerable<string>> GetAvailableTelemetryChannels() 
    {
        return TelemetryReaders.Select(x => x.GetType().Name);
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

    public void Tick()
    {
        foreach (var reader in _telemetryReaders)
        {
            if (reader.ShouldRead())
            {
                reader.ReadTelemetry();
            }
        }
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