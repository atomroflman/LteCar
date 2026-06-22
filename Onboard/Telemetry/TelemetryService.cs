using LteCar.Shared.Channels;
using LteCar.Shared.HubClients;
using LteCar.Shared.Hubs;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.Json;
using TypedSignalR.Client;

namespace LteCar.Onboard.Telemetry;

public class TelemetryService : IHubConnectionObserver, ITelemetryClient
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

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
    public ServerCarConfigurationService CarConfigurationService { get; }
    
    public TelemetryService(ChannelMap channelMap, ServerConnectionService serverConnectionService, IConfiguration configuration, ILogger<TelemetryService> logger, IServiceProvider serviceProvider, ServerCarConfigurationService carConfigurationService)
    {
        ServiceProvider = serviceProvider;
        ChannelMap = channelMap;
        ServerConnectionService = serverConnectionService;
        Configuration = configuration;
        Logger = logger;
        CarConfigurationService = carConfigurationService;
    }

    public async Task ConnectToServer()
    {
        _connection = ServerConnectionService.ConnectToHub(HubPaths.TelemetryHub);
        await _connection.StartAsync();
        _server = _connection.CreateHubProxy<ITelemetryServer>();
        _connection.Register<ITelemetryClient>(this);
        var carId = CarConfigurationService.ServerAssignedCarId;
        _carId = carId?.ToString();
        if (string.IsNullOrEmpty(_carId))
        {
            Logger.LogWarning("ServerAssignedCarId not available yet. Telemetry updates will fail until CarId is set.");
        }
        else
        {
            await _server.RegisterAsOnboard(_carId);
        }
        Logger.LogInformation($"Connected to telemetry server with CarId: {_carId}");
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
        _tick++;
        foreach (var reader in _telemetryReaders.ToList())
        {
            var interval = reader.Value.ReadIntervalTicks;
            if (interval <= 0 || _tick % interval == 0)
            {
                try
                {
                    var value = await reader.Value.ReadTelemetry();
                    if (value != null)
                    {
                        Logger.LogDebug("Telemetry from {Channel}: {Value}", reader.Key, value);
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

    public Task OnClosed(Exception? exception)
    {
        if (exception == null)
        {
            Logger.LogError("Telemetry connection closed.");
            return Task.CompletedTask;
        }

        Logger.LogError(exception, "Telemetry connection closed unexpectedly.");
        return Task.CompletedTask;
    }

    public async Task OnReconnected(string? connectionId)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
        {
            Logger.LogWarning("Telemetry connection reconnected without a connection id.");
            return;
        }

        await UpdateTelemetry("Telemetry Connection", connectionId!);
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
        
        var resolvedReaderType = Type.GetType(definition.TelemetryType);
        if (resolvedReaderType == null)
        {
            Logger.LogError("Telemetry reader type {Reader} could not be resolved.", definition.TelemetryType);
            return null;
        }

        var reader = ServiceProvider.GetRequiredService(resolvedReaderType) as TelemetryReaderBase;
        if (reader == null)
        {
            Logger.LogError("Telemetry reader type {Reader} not found.", resolvedReaderType);
            return null;
        }
        reader.ReadIntervalTicks = definition.ReadIntervalTicks;
        foreach (var option in definition.Options)
        {
            var property = resolvedReaderType.GetProperty(option.Key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (property != null && property.CanWrite)
            {
                try
                {
                    if (IsNullOptionValue(option.Value))
                    {
                        Logger.LogWarning("Option {Option} for channel {Channel} has null value. Skipping.", option.Key, channelName);
                        continue;
                    }

                    var convertedValue = ConvertOptionValue(option.Value, property.PropertyType);
                    property.SetValue(reader, convertedValue);
                    Logger.LogDebug("Set option {Option} for channel {Channel} to {Value}", option.Key, channelName, option.Value);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to set option {Option} for channel {Channel} with value {Value}", option.Key, channelName, option.Value);
                }
            }
            else
            {
                Logger.LogWarning("Option {Option} not found or not writable on reader type {Reader} for channel {Channel}", option.Key, resolvedReaderType.Name, channelName);
            }
        }
        _telemetryReaders.Add(channelName, reader);
        Logger.LogInformation("Subscribed to telemetry channel: {Channel}", channelName);
        return reader;
    }

    private static bool IsNullOptionValue(object? value) =>
        value == null || value is JsonElement { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined };

    private static object? ConvertOptionValue(object value, Type targetType)
    {
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (value is JsonElement jsonElement)
        {
            return jsonElement.Deserialize(underlyingType, JsonSerializerOptions);
        }

        if (underlyingType.IsInstanceOfType(value))
        {
            return value;
        }

        if (underlyingType.IsEnum)
        {
            return value is string enumName
                ? Enum.Parse(underlyingType, enumName, ignoreCase: true)
                : Enum.ToObject(underlyingType, value);
        }

        if (underlyingType == typeof(Guid) && value is string guidText)
        {
            return Guid.Parse(guidText);
        }

        try
        {
            return JsonSerializer.Deserialize(JsonSerializer.Serialize(value), underlyingType, JsonSerializerOptions);
        }
        catch (JsonException)
        {
            return Convert.ChangeType(value, underlyingType);
        }
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