using System.Net.Http.Json;
using System.Text.Json;
using LteCar.Onboard.Services;
using LteCar.Shared.Channels;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TypedSignalR.Client;

namespace LteCar.Onboard;

public class ServerConnectionService
{
    // Initialization sequence (new handshake):
    // 1. Establish SignalR hub connection.
    // 2. Call SyncChannelMapAsync (preferred) to push full ChannelMap & receive numeric ID mapping + hash.
    // 3. Call OpenCarConnection using the server-provided hash to avoid redundant UpdateChannelMap traffic.
    // 4. If server indicates mismatch (legacy cases or server side reset) we trigger a fresh SyncChannelMap.
    // Persisted artifacts: channelMap.server.json (server-normalized map + ids) & channelMap.hash.
    public IServiceProvider ServiceProvider { get; }
    public ILogger<ServerConnectionService> Logger { get; }

    private readonly ChannelMap _channelMap;
    private readonly IConfiguration _configuration;
    private readonly IOnboardBuildInfoService _buildInfo;
    private readonly HttpClient _http;
    private HubConnection _connection;
    private ChannelMapSyncResponse? _lastSync;
    private int? _serverAssignedCarId;

    public ServerConnectionService(
        ChannelMap channelMap,
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<ServerConnectionService> logger,
        IOnboardBuildInfoService buildInfo)
    {
        ServiceProvider = serviceProvider;
        Logger = logger;
        _channelMap = channelMap;
        _configuration = configuration;
        _buildInfo = buildInfo;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
    }

    public UriBuilder GetServerUriBuilder() 
    {
        var serverAddressBuilder = new UriBuilder();
        serverAddressBuilder.Host = _configuration.GetValue<string>("ServerName");
        serverAddressBuilder.Scheme = (_configuration.GetValue<bool?>("UseHttps") ?? true) ? "https" : "http";
        serverAddressBuilder.Port = _configuration.GetValue<int?>("ServerPort") ?? 5000;
        return serverAddressBuilder;
    }

    public HubConnection ConnectToHub(string name) 
    {
        var serverUriBuilder = GetServerUriBuilder();
        serverUriBuilder.Path = name;
        var connectionHubEndpoint = serverUriBuilder.Uri;
        Logger.LogInformation($"Connecting to server: {connectionHubEndpoint}");
        return new HubConnectionBuilder()
            .WithUrl(connectionHubEndpoint)
            .WithAutomaticReconnect(Enumerable.Range(0, 50).Select(e => TimeSpan.FromMilliseconds(Math.Pow(1.25d, e) * 1000)).ToArray())
            .AddMessagePackProtocol()
            .Build();
    }
    
    public async Task ConnectToServer(string carIdentityKey)
    {
        _connection = ConnectToHub(HubPaths.CarConnectionHub);
        _connection.Reconnected += async (connectionId) =>
        {
            Logger.LogInformation($"Connection {connectionId} reestablished.");
        };
        _connection.Reconnecting += (connectionId) =>
        {
            Logger.LogWarning($"Reconnecting to server with connection ID: {connectionId}");
            return Task.CompletedTask;
        };
        _connection.Closed += (error) =>
        {
            Logger.LogError($"Connection closed: {error}");
            return Task.CompletedTask;
        };
        await _connection.StartAsync();

        Logger.LogInformation($"Connected to server: {_connection.State}");
        await _connection.InvokeAsync("Test");
        Logger.LogDebug($"Tested... Open connection with carIdentityKey: {carIdentityKey}");

        await CheckServerVersionAsync();
        
        var connectionServer = _connection.CreateHubProxy<ICarConnectionServer>();
        Logger.LogDebug("Proxy created...");
        
        // Prefer hash from last SyncChannelMap (if sync already performed before OpenCarConnection is called)
        var channelMapHash = _lastSync?.Hash ?? ChannelMapHashProvider.GenerateHash(_channelMap);
        var config = await connectionServer.OpenCarConnection(carIdentityKey, channelMapHash);
        if (config == null)
        {
            Logger.LogError("Failed to open car connection.");
            return;
        }
        
        // Store server-assigned CarId for all future operations
        _serverAssignedCarId = config.ServerAssignedCarId;
        Logger.LogInformation($"Server assigned CarId: {_serverAssignedCarId}");
        
        // If server still requests channel map update (e.g., initial legacy path or mismatch), perform sync now
        if (config.RequiresChannelMapUpdate)
        {
            Logger.LogInformation("Server indicates channel map mismatch. Triggering SyncChannelMap now.");
            await SyncChannelMapAsync();
        }
        
        Logger.LogDebug($"OpenCarConnection called: {JsonSerializer.Serialize(config)}");
        var configService = ServiceProvider.GetRequiredService<ServerCarConfigurationService>();
        configService.UpdateConfiguration(config);

        var buildInfo = _buildInfo.GetBuildInfo();
        try
        {
            await connectionServer.ReportOnboardVersion(buildInfo.Branch, buildInfo.Commit);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to report onboard version to server");
        }
    }

    public async Task<ChannelMapSyncResponse?> SyncChannelMapAsync()
    {
        if (_connection == null)
        {
            Logger.LogError("Cannot sync channel map. Connection not established.");
            return null;
        }
        if (!_serverAssignedCarId.HasValue)
        {
            Logger.LogError("Cannot sync channel map. Server-assigned CarId not available. Call ConnectToServer first.");
            return null;
        }
        var proxy = _connection.CreateHubProxy<ICarConnectionServer>();
        var request = new ChannelMapSyncRequest { CarId = _serverAssignedCarId.Value, ChannelMap = _channelMap };
        Logger.LogInformation("Sending ChannelMapSyncRequest for CarId {CarId} with {Control} control, {Telemetry} telemetry, {Video} video streams", 
            _serverAssignedCarId.Value, _channelMap.ControlChannels.Count, _channelMap.TelemetryChannels.Count, _channelMap.VideoStreams.Count);
        var response = await _connection.InvokeAsync<ChannelMapSyncResponse>("SyncChannelMap", request);
        _lastSync = response;
        try
        {
            await File.WriteAllTextAsync("channelMap.server.json", JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist channel map sync response");
        }
        Logger.LogInformation("Channel map synced. Hash {Hash}", response.Hash);
        return response;
    }

    public bool TryLoadPreviousSync()
    {
        try
        {
            if (!File.Exists("channelMap.server.json")) return false;
            var json = File.ReadAllText("channelMap.server.json");
            var stored = JsonSerializer.Deserialize<ChannelMapSyncResponse>(json);
            if (stored == null || stored.ChannelMap == null) return false;
            // Copy serverIds into current in-memory map if matching keys exist
            foreach (var kv in stored.ChannelMap.ControlChannels)
            {
                if (_channelMap.ControlChannels.TryGetValue(kv.Key, out var current))
                {
                    current.ServerId = kv.Value.ServerId;
                }
            }
            foreach (var kv in stored.ChannelMap.TelemetryChannels)
            {
                if (_channelMap.TelemetryChannels.TryGetValue(kv.Key, out var current))
                {
                    current.ServerId = kv.Value.ServerId;
                }
            }
            foreach (var kv in stored.ChannelMap.VideoStreams)
            {
                if (_channelMap.VideoStreams.TryGetValue(kv.Key, out var current))
                {
                    current.ServerId = kv.Value.ServerId;
                }
            }
            _lastSync = stored;
            Logger.LogInformation("Loaded previous channel map sync with hash {Hash}", stored.Hash);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to load previous channel map sync");
            return false;
        }
    }

    // ponytail: fetches /api/version once on connect and warns on branch/commit mismatch.
    // Single-shot, fire-and-log: we don't retry on transient failures and we don't act
    // (auto-update would need a separate opt-in flag — out of scope here).
    private async Task CheckServerVersionAsync()
    {
        try
        {
            var builder = GetServerUriBuilder();
            builder.Path = "/api/version";
            var url = builder.Uri.ToString();

            var remote = await _http.GetFromJsonAsync<ServerVersionDto>(url);
            if (remote is null)
            {
                Logger.LogWarning("Server version endpoint returned empty body ({Url})", url);
                return;
            }

            var local = _buildInfo.GetBuildInfo();
            var shortLocal = ShortenCommit(local.Commit);
            var shortRemote = ShortenCommit(remote.Commit);

            Logger.LogInformation("Version: local {LocalBranch}@{LocalCommit} | server {RemoteBranch}@{RemoteCommit}",
                local.Branch, shortLocal, remote.Branch ?? "?", shortRemote);

            var branchMismatch = !string.Equals(local.Branch, remote.Branch, StringComparison.OrdinalIgnoreCase);
            var commitMismatch = local.Commit is not null && remote.Commit is not null
                && !string.Equals(local.Commit, remote.Commit, StringComparison.OrdinalIgnoreCase);

            if (branchMismatch || commitMismatch)
            {
                Logger.LogWarning(
                    "Version mismatch with server. Local={LocalBranch}@{LocalCommit}, Server={RemoteBranch}@{RemoteCommit}. Run 'dotnet run -- update' to sync.",
                    local.Branch, shortLocal, remote.Branch ?? "?", shortRemote);
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to check server version");
        }
    }

    private static string ShortenCommit(string? commit) =>
        string.IsNullOrEmpty(commit) || commit.Length < 8 ? commit ?? "?" : commit[..8];

    private sealed record ServerVersionDto(string? Branch, string? Commit);
}