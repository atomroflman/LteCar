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

    private const string GroupKeyOption = "groupKey";

    private int _tick = 0;
    private HubConnection? _connection;
    private ITelemetryServer? _server;
    private string? _carId;
    private bool _reconnectHandlersAttached;

    // Channels the UI has asked us to publish. Key = channel name (e.g. "battery.voltage").
    private readonly HashSet<string> _subscribedChannels = new(StringComparer.Ordinal);

    // All channels discovered in the ChannelMap, keyed by channel name. Allows
    // us to look up group/options without re-scanning the map on every tick.
    private readonly Dictionary<string, TelemetryChannelMapItem> _channelIndex = new(StringComparer.Ordinal);

    // Reader instances, keyed by groupKey (e.g. "battery"). Multiple channels
    // that share a source share a single reader instance and a single tick.
    private readonly Dictionary<string, TelemetryReaderBase> _sourceReaders = new(StringComparer.Ordinal);

    // groupKey -> list of channel names that belong to this source.
    private readonly Dictionary<string, List<string>> _sourceChannels = new(StringComparer.Ordinal);

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

        CarConfigurationService.OnConfigurationChanged += HandleCarConfigurationChanged;

        foreach (var kv in ChannelMap.TelemetryChannels)
        {
            _channelIndex[kv.Key] = kv.Value;
        }
    }

    public async Task ConnectToServer()
    {
        _connection = ServerConnectionService.ConnectToHub(HubPaths.TelemetryHub);
        AttachReconnectHandlers(_connection);
        await _connection.StartAsync();
        _server = _connection.CreateHubProxy<ITelemetryServer>();
        _connection.Register<ITelemetryClient>(this);
        _carId = CarConfigurationService.ServerAssignedCarId?.ToString();
        if (string.IsNullOrEmpty(_carId))
        {
            Logger.LogWarning("ServerAssignedCarId not available yet. Telemetry updates will fail until CarId is set.");
        }
        else
        {
            await _server.RegisterAsOnboard(_carId);
        }
        Logger.LogInformation("Connected to telemetry server with CarId: {CarId}", _carId);
    }

    private void AttachReconnectHandlers(HubConnection connection)
    {
        if (_reconnectHandlersAttached)
        {
            return;
        }
        _reconnectHandlersAttached = true;

        connection.Reconnecting += error =>
        {
            Logger.LogWarning(error, "Telemetry connection reconnecting.");
            return Task.CompletedTask;
        };

        connection.Reconnected += async connectionId =>
        {
            Logger.LogInformation("Telemetry connection reconnected ({ConnectionId}).", connectionId);
            await ReregisterOnboardAsync();
        };

        connection.Closed += async error =>
        {
            Logger.LogError(error, "Telemetry connection closed.");
            // Closed is terminal for the SignalR client; the next call will
            // start a fresh HubConnection via ConnectToServer.
            await Task.CompletedTask;
        };
    }

    private async Task ReregisterOnboardAsync()
    {
        if (_server == null)
        {
            return;
        }
        _carId = CarConfigurationService.ServerAssignedCarId?.ToString();
        if (string.IsNullOrEmpty(_carId))
        {
            Logger.LogWarning("Cannot re-register as onboard: ServerAssignedCarId still missing.");
            return;
        }
        try
        {
            await _server.RegisterAsOnboard(_carId);
            Logger.LogInformation("Re-registered as onboard for car {CarId}.", _carId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to re-register as onboard after reconnect.");
        }
    }

    private void HandleCarConfigurationChanged()
    {
        var newId = CarConfigurationService.ServerAssignedCarId?.ToString();
        if (string.IsNullOrEmpty(newId) || newId == _carId)
        {
            return;
        }

        _carId = newId;
        Logger.LogInformation("ServerAssignedCarId changed to {CarId}. Re-registering as onboard.", _carId);

        if (_server != null && _connection?.State == HubConnectionState.Connected)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _server.RegisterAsOnboard(_carId!);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to register as onboard after CarId change.");
                }
            });
        }
    }

    public Task<IEnumerable<string>> GetAvailableTelemetryChannels()
    {
        return Task.FromResult<IEnumerable<string>>(_channelIndex.Keys.ToList());
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
        if (_tick <= 0)
        {
            _tick = 1;
        }
        foreach (var (sourceKey, reader) in _sourceReaders.ToList())
        {
            var interval = Math.Max(1, reader.ReadIntervalTicks);
            if (_tick % interval != 0)
            {
                continue;
            }

            IReadOnlyDictionary<string, string>? values;
            try
            {
                values = await reader.ReadAllTelemetryAsync(sourceKey);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error reading telemetry from source {Source}", sourceKey);
                continue;
            }
            if (values == null || values.Count == 0)
            {
                continue;
            }

            if (!_sourceChannels.TryGetValue(sourceKey, out var channels) || channels.Count == 0)
            {
                continue;
            }

            foreach (var channelName in channels)
            {
                if (!_subscribedChannels.Contains(channelName))
                {
                    continue;
                }
                if (!TryResolveValue(channelName, values, out var value))
                {
                    continue;
                }
                Logger.LogDebug("Telemetry {Channel}: {Value}", channelName, value);
                try
                {
                    await UpdateTelemetry(channelName, value);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to send telemetry update for {Channel}", channelName);
                }
            }
        }
    }

    private static bool TryResolveValue(string channelName, IReadOnlyDictionary<string, string> values, out string value)
    {
        if (values.TryGetValue(channelName, out value!))
        {
            return true;
        }
        var dotIndex = channelName.LastIndexOf('.');
        if (dotIndex > 0 && dotIndex < channelName.Length - 1)
        {
            var suffix = channelName[(dotIndex + 1)..];
            if (values.TryGetValue(suffix, out value!))
            {
                return true;
            }
        }
        value = string.Empty;
        return false;
    }

    public Task SubscribeToTelemetryChannel(string channelName)
    {
        if (!_channelIndex.TryGetValue(channelName, out var definition))
        {
            Logger.LogWarning("Unknown telemetry channel {Channel}; ignoring subscribe request.", channelName);
            return Task.CompletedTask;
        }

        _subscribedChannels.Add(channelName);

        var groupKey = ResolveGroupKey(channelName, definition);
        if (!_sourceChannels.TryGetValue(groupKey, out var channels))
        {
            channels = new List<string>();
            _sourceChannels[groupKey] = channels;
        }
        if (!channels.Contains(channelName))
        {
            channels.Add(channelName);
        }

        if (!_sourceReaders.ContainsKey(groupKey))
        {
            var reader = CreateReader(channelName, definition);
            if (reader == null)
            {
                return Task.CompletedTask;
            }
            _sourceReaders[groupKey] = reader;
            Logger.LogInformation("Created telemetry reader for source {Source} (channel {Channel}).", groupKey, channelName);
        }

        Logger.LogInformation("Subscribed to telemetry channel: {Channel}", channelName);
        return Task.CompletedTask;
    }

    public Task UnsubscribeFromTelemetryChannel(string channelName)
    {
        if (!_subscribedChannels.Remove(channelName))
        {
            return Task.CompletedTask;
        }

        var groupKey = _channelIndex.TryGetValue(channelName, out var def)
            ? ResolveGroupKey(channelName, def)
            : channelName;

        if (_sourceChannels.TryGetValue(groupKey, out var channels))
        {
            channels.Remove(channelName);
            if (channels.Count == 0)
            {
                _sourceChannels.Remove(groupKey);
                if (_sourceReaders.TryGetValue(groupKey, out var reader))
                {
                    reader.Dispose();
                    _sourceReaders.Remove(groupKey);
                    Logger.LogInformation("Disposed telemetry reader for source {Source}.", groupKey);
                }
            }
        }

        Logger.LogInformation("Unsubscribed from telemetry channel: {Channel}", channelName);
        return Task.CompletedTask;
    }

    private static string ResolveGroupKey(string channelName, TelemetryChannelMapItem definition)
    {
        if (definition.Options.TryGetValue(GroupKeyOption, out var raw) && raw != null)
        {
            var s = raw.ToString();
            if (!string.IsNullOrWhiteSpace(s))
            {
                return s;
            }
        }
        return channelName;
    }

    private TelemetryReaderBase? CreateReader(string channelName, TelemetryChannelMapItem definition)
    {
        var resolvedReaderType = Type.GetType(definition.TelemetryType);
        if (resolvedReaderType == null)
        {
            Logger.LogError("Telemetry reader type {Reader} could not be resolved.", definition.TelemetryType);
            return null;
        }

        var reader = ServiceProvider.GetRequiredService(resolvedReaderType) as TelemetryReaderBase;
        if (reader == null)
        {
            Logger.LogError("Telemetry reader type {Reader} is not a TelemetryReaderBase.", resolvedReaderType);
            return null;
        }
        reader.ReadIntervalTicks = Math.Max(1, definition.ReadIntervalTicks);

        foreach (var option in definition.Options)
        {
            var property = resolvedReaderType.GetProperty(option.Key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (property == null || !property.CanWrite)
            {
                Logger.LogWarning("Option {Option} not found or not writable on reader type {Reader} for channel {Channel}", option.Key, resolvedReaderType.Name, channelName);
                continue;
            }
            try
            {
                if (IsNullOptionValue(option.Value))
                {
                    continue;
                }
                var convertedValue = ConvertOptionValue(option.Value, property.PropertyType);
                property.SetValue(reader, convertedValue);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to set option {Option} for channel {Channel} with value {Value}", option.Key, channelName, option.Value);
            }
        }

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

    public Task OnReconnected(string? connectionId)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
        {
            Logger.LogWarning("Telemetry connection reconnected without a connection id.");
            return Task.CompletedTask;
        }
        Logger.LogInformation("Telemetry connection reconnected: {ConnectionId}", connectionId);
        return Task.CompletedTask;
    }

    public Task OnReconnecting(Exception? exception)
    {
        Logger.LogWarning(exception, "Reconnecting telemetry connection.");
        return Task.CompletedTask;
    }
}
