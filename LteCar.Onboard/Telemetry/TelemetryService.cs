using LteCar.Shared.Channels;
using LteCar.Shared.HubClients;
using LteCar.Shared.Hubs;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TypedSignalR.Client;

namespace LteCar.Onboard.Telemetry;

public class TelemetryService : IHubConnectionObserver, ITelemetryClient
{
    private int _tick = 0;
    private HubConnection? _connection;
    private ITelemetryServer? _server;
    private string? _carId;
    private readonly Dictionary<string, TelemetryReaderBase> _telemetryReaders = new();
    public IConfiguration Configuration { get; set; }
    public ILogger<TelemetryService> Logger { get; }
    public ServerConnectionService ServerConnectionService { get; }
    public IServiceProvider ServiceProvider { get; }
    public ChannelMap ChannelMap { get; }
    
    public TelemetryService(ChannelMap channelMap, ServerConnectionService serverConnectionService, IConfiguration configuration, ILogger<TelemetryService> logger, IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        ChannelMap = channelMap;
        ServerConnectionService = serverConnectionService;
        Configuration = configuration;
        Logger = logger;
    }

    public async Task ConnectToServer()
    {
        _connection = ServerConnectionService.ConnectToHub(HubPaths.TelemetryHub);
        await _connection.StartAsync();
        _server = _connection.CreateHubProxy<ITelemetryServer>();
        _connection.Register<ITelemetryClient>(this);
        // _connection.RegisterObserver(this);
        _carId = Configuration.GetValue<string>("carId");
        Logger.LogInformation("Connected to server.");

        
    }

    public Task<IEnumerable<string>> GetAvailableTelemetryChannels() 
    {
        return Task.FromResult(ChannelMap.TelemetryChannels.Select(x => x.GetType().Name));
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

    public async Task Tick()
    {
        foreach (var reader in _telemetryReaders)
        {
            if (_tick % reader.Value.ReadIntervalTicks == 0)
            {
                try
                {
                    var value = await reader.Value.ReadTelemetry();
                    if (value != null)
                    {
                        Logger.LogInformation("Telemetry from {Channel}: {Value}", reader.Key, value);
                        await UpdateTelemetry(reader.Key, value);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error reading telemetry from {Channel}", reader.Key);
                }
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

    public Task SubscribeToTelemetryChannel(string channelName)
    {
        if (_telemetryReaders.ContainsKey(channelName))
        {
            Logger.LogWarning("Already subscribed to telemetry channel: {Channel}", channelName);
            return Task.CompletedTask;
        }

        var reader = CreateTelemetryReader(channelName);
        if (reader == null)
        {
            Logger.LogError("Failed to create telemetry reader for channel: {Channel}", channelName);
            return Task.CompletedTask;
        }

        Logger.LogInformation("Subscribed to telemetry channel: {Channel}", channelName);
        return Task.CompletedTask;
    }

    private TelemetryReaderBase? CreateTelemetryReader(string channelName)
    {
        var definition = ChannelMap.TelemetryChannels.TryGetValue(channelName, out var channel) 
            ? channel 
            : throw new ArgumentException($"Telemetry channel {channelName} not found.");
        
        var readerType = Type.GetType(definition.TelemetryType);
        var reader = ServiceProvider.GetRequiredService(readerType) as TelemetryReaderBase;
        if (reader == null)
        {
            Logger.LogError("Telemetry reader type {Reader} not found.", readerType);
            return null;
        }
        reader.ReadIntervalTicks = definition.ReadIntervalTicks;
        _telemetryReaders.Add(channelName, reader);
        Logger.LogInformation("Subscribed to telemetry channel: {Channel}", channelName);
        return reader;
    }

    public Task UnsubscribeFromTelemetryChannel(string channelName)
    {
        if (_telemetryReaders.ContainsKey(channelName))
        {
            _telemetryReaders[channelName].Dispose();
            _telemetryReaders.Remove(channelName);
            Logger.LogInformation("Unsubscribed from telemetry channel: {Channel}", channelName);
        }
        else
        {
            Logger.LogWarning("No subscription found for telemetry channel: {Channel}", channelName);
        }
        return Task.CompletedTask;
    }
}