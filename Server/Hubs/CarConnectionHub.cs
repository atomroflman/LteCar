using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Diagnostics;
using LteCar.Server.Configuration;
using LteCar.Server.Data;
using LteCar.Server.Services;
using LteCar.Shared;
using LteCar.Shared.Channels;
using LteCar.Shared.FileTransfer;
using LteCar.Shared.HubClients;
using LteCar.Shared.Video;
using MessagePack;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Sqids;

namespace LteCar.Server.Hubs;

public class CarConnectionHub : Hub<IConnectionHubClient>, IConnectionHubServer
{
    // Handshake Overview:
    // 1. Car connects and (optionally) calls SyncChannelMap first sending full ChannelMap.
    // 2. Server upserts channels/video streams, assigns compact numeric IDs and returns:
    //      - Hash (SHA256 of canonical map) stored on both sides
    //      - Normalized ChannelMap + dictionaries name->int id for bandwidth-efficient future messages
    // 3. OpenCarConnection now only needs the hash to determine if a legacy update is required.
    // This reduces startup round trips and prepares for ID-based messaging.
    //
    // ponytail: this hub is the single vehicle-side hub. It used to be CarConnectionHub
    // + CarControlHub + TelemetryHub + CarVideoHub across four sockets. The Onboard now
    // opens one SignalR connection that carries control, telemetry, video signaling,
    // file transfer, channel CRUD, and connection state. Browser pages go to the same URL.
    public ILogger<CarConnectionHub> Logger { get; }
    private readonly VideoStreamReceiverService _streamService;
    private readonly IConfigurationService _configService;
    private readonly CarConnectionStore _connectionStore;
    private readonly SqidsEncoder<long> _sqidsEncoder;
    private readonly ActiveVideoStreamViewerRegistry _viewerRegistry;
    private readonly AvailableTypesRegistry _availableTypes;

    public CarConnectionHub(
        IConfigurationService configService,
        ILogger<CarConnectionHub> logger,
        VideoStreamReceiverService streamService,
        CarConnectionStore connectionStore,
        SqidsEncoder<long> sqidsEncoder,
        ActiveVideoStreamViewerRegistry viewerRegistry,
        AvailableTypesRegistry availableTypes)
    {
        Logger = logger;
        _streamService = streamService;
        _configService = configService;
        _connectionStore = connectionStore;
        _sqidsEncoder = sqidsEncoder;
        _viewerRegistry = viewerRegistry;
        _availableTypes = availableTypes;
    }

    public VideoStreamReceiverService VideoStreamReceiverService => _streamService;

    public Task<CarStateModel[]> UiClientConnected()
    {
        var dbContext = Context.GetHttpContext()!.RequestServices.GetRequiredService<LteCarContext>();
        var states = dbContext.Cars
            .AsNoTracking()
            .ToList()
            .Select(car => {
                var hasConnectionInfo = _connectionStore.TryGetValue(car.Id.ToString(), out var connectionInfo);
                return new CarStateModel
                {
                    Id = car.Id.ToString(),
                    IsConnected = hasConnectionInfo,
                    DriverId = hasConnectionInfo ? connectionInfo?.DriverId : null,
                    DriverName = hasConnectionInfo ? connectionInfo?.DriverName : null
                };
            })
            .ToArray();
        return Task.FromResult(states);
    }
    
    public async Task<CarConfiguration> OpenCarConnection(string carIdentityKey, string channelMapHash)
    {
        Logger.LogInformation("Car with identity key '{CarIdentityKey}' attempting to connect: ChannelHash '{ChannelMapHash}'", carIdentityKey, channelMapHash);
        var dbContext = Context.GetHttpContext()!.RequestServices.GetRequiredService<LteCarContext>();
        var car = dbContext.Cars.FirstOrDefault(c => c.CarIdentityKey == carIdentityKey);
        if (car == null)
        {
            Logger.LogWarning($"Car with identity key {carIdentityKey} not found. Creating a new one.");
            car = new Car() 
            { 
                CarIdentityKey = carIdentityKey,
                Name = carIdentityKey // Initial name is the identity key, user can change it later
            };
            dbContext.Cars.Add(car);
            car.ChannelMapHash = new Guid().ToString();
        }
        car.LastSeen = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
        
        // Add connection to SignalR group by CarId for targeted messaging
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Car-{car.Id}");
        Logger.LogInformation($"Car '{car.Name}' (ID: {car.Id}) connected and added to group 'Car-{car.Id}'");
        
        var janusServerHost = _configService.Janus.HostName;
        if (string.IsNullOrEmpty(janusServerHost))
        {
            janusServerHost = System.Net.Dns.GetHostName();
            Logger.LogWarning($"Janus server host is not configured. Using default: {janusServerHost}");
        }

        CarConfiguration carConfig = new CarConfiguration();
        carConfig.ServerAssignedCarId = car.Id;
        carConfig.RequiresChannelMapUpdate = car.ChannelMapHash != channelMapHash;
        if (carConfig.RequiresChannelMapUpdate)
        {
            Logger.LogInformation($"Car ID {car.Id} channel map hash mismatch. Server: '{car.ChannelMapHash}' Client: '{channelMapHash}'");
        }
        
        var connectionInfo = _connectionStore.RegisterConnection(car.Id.ToString(), Context.ConnectionId);
        connectionInfo.CarConfiguration = carConfig;

        await Clients.All.CarStateUpdated(new CarStateModel() {
            Id = car.Id.ToString(),
            IsConnected = true,
            DriverId = connectionInfo.DriverId,
            DriverName = connectionInfo.DriverName
        });
        return carConfig;
    }

    public async Task Test() 
    {
        await Task.CompletedTask;
        Logger.LogInformation("Test Invoked");
    }

    public Task ReportOnboardVersion(string branch, string? commit)
    {
        var dbContext = Context.GetHttpContext()!.RequestServices.GetRequiredService<LteCarContext>();
        var car = dbContext.Cars.FirstOrDefault(c => Context.ConnectionId != null && _connectionStore.Values.Any(v => v.ConnectionId == Context.ConnectionId));
        var carId = car?.Id.ToString()
            ?? _connectionStore.FirstOrDefault(kv => kv.Value.ConnectionId == Context.ConnectionId).Key;
        if (carId == null)
        {
            Logger.LogWarning("ReportOnboardVersion received without an active car connection");
            return Task.CompletedTask;
        }
        _connectionStore.TrySetOnboardVersion(carId, branch, commit);
        Logger.LogInformation("Car {CarId} reports onboard version {Branch}@{Commit}", carId, branch, commit);
        return Task.CompletedTask;
    }

    public Task RegisterAvailableChannelTypes(int carId, AvailableChannelTypes types)
    {
        _availableTypes.Set(carId, types);
        Logger.LogInformation("Car {CarId} registered {Control} control types and {Telemetry} telemetry types",
            carId, types.ControlTypes.Length, types.TelemetryTypes.Length);
        return Task.CompletedTask;
    }

    public override Task OnConnectedAsync()
    {
        Logger.LogInformation($"Client connected: {Clients.Caller}");
        return base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Logger.LogWarning($"Client disconnected: {exception}");
        if (_connectionStore.TryRemoveConnection(Context.ConnectionId, out var carId, out var connectionInfo) && carId != null)
        {
            await Clients.All.CarStateUpdated(new CarStateModel()
            {
                Id = carId,
                IsConnected = false,
                DriverId = connectionInfo?.DriverId,
                DriverName = connectionInfo?.DriverName
            });
        }

        var stoppedStreamIds = _viewerRegistry.RemoveConnection(Context.ConnectionId);
        foreach (var streamId in stoppedStreamIds)
        {
            var dbContext = Context.GetHttpContext()!.RequestServices.GetRequiredService<LteCarContext>();
            var stream = await dbContext.CarVideoStreams.FirstOrDefaultAsync(s => s.Id == streamId);
            if (stream == null)
                continue;
            await StopStreamForViewersAsync(stream);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task UpdateChannelMap(int carId, ChannelMap channelMap)
    {
        var dbContext = Context.GetHttpContext()!.RequestServices.GetRequiredService<LteCarContext>();
        var car = dbContext.Cars.FirstOrDefault(c => c.Id == carId);
        if (car == null)
        {
            Logger.LogWarning($"Car with ID {carId} not found.");
            return;        
        }
        car.ChannelMapHash = ChannelMapHashProvider.GenerateHash(channelMap);
        // Add control channels
        foreach (var channel in channelMap.ControlChannels)
        {
            var channelDb = dbContext.CarChannels.FirstOrDefault(c => c.ChannelName == channel.Key && c.CarId == car.Id);
            if (channelDb == null)
            {
                Logger.LogWarning($"Channel with ID {channel.Key} not found. Creating a new one.");
                channelDb = new CarChannel() { ChannelName = channel.Key, CarId = car.Id };
                dbContext.CarChannels.Add(channelDb);
            }
            channelDb.MaxResendInterval = channel.Value.MaxResendInterval;
        }
        // Remove missing control channels
        foreach (var channel in dbContext.CarChannels.Where(c => c.CarId == car.Id))
        {
            if (!channelMap.ControlChannels.ContainsKey(channel.ChannelName))
            {
                Logger.LogWarning($"Channel with ID {channel.ChannelName} not found in the new channel map. Removing it.");
                dbContext.Set<UserSetupCarChannelNode>().Where(n => n.CarChannelId == channel.Id)
                    .ToList()
                    .ForEach(n => dbContext.Set<UserSetupCarChannelNode>().Remove(n));

                dbContext.CarChannels.Remove(channel);
            }
        }

        // Add telemetry channels
        foreach (var channel in channelMap.TelemetryChannels)
        {
            var channelDb = dbContext.CarTelemetry.FirstOrDefault(c => c.ChannelName == channel.Key && c.CarId == car.Id);
            if (channelDb == null)
            {
                Logger.LogWarning($"Telemetry channel with ID {channel.Key} not found. Creating a new one.");
                channelDb = new CarTelemetry() { ChannelName = channel.Key, CarId = car.Id };
                dbContext.CarTelemetry.Add(channelDb);
            }
            channelDb.TelemetryType = channel.Value.TelemetryType;
            channelDb.ReadIntervalTicks = channel.Value.ReadIntervalTicks;
            channelDb.DataType = channel.Value.DataType;
            channelDb.Unit = channel.Value.Unit;
            channelDb.Decimals = channel.Value.Decimals;
        }
        // Remove missing telemetry channels
        foreach (var channel in dbContext.CarTelemetry.Where(c => c.CarId == car.Id))
        {
            if (!channelMap.TelemetryChannels.ContainsKey(channel.ChannelName))
            {
                Logger.LogWarning($"Telemetry channel with ID {channel.ChannelName} not found in the new channel map. Removing it.");
                dbContext.CarTelemetry.Remove(channel);
            }
        }

        // Add video streams
        foreach (var stream in channelMap.VideoStreams)
        {
            var streamDb = dbContext.CarVideoStreams.FirstOrDefault(s => s.StreamId == stream.Value.StreamId && s.CarId == car.Id);
            if (streamDb == null)
            {
                Logger.LogInformation($"Video stream with ID {stream.Value.StreamId} not found. Creating a new one.");
                streamDb = new CarVideoStream()
                {
                    StreamId = stream.Value.StreamId,
                    CarId = car.Id,
                };
                dbContext.CarVideoStreams.Add(streamDb);
            }

            streamDb.Name = stream.Value.Name ?? stream.Value.StreamId;
            streamDb.Type = stream.Value.Type ?? "unknown";
            streamDb.Location = stream.Value.Location;            
            streamDb.IsActive = stream.Value.Enabled;
        }

        // Remove video streams that are no longer in the channel map
        foreach (var stream in dbContext.CarVideoStreams.Where(s => s.CarId == car.Id))
        {
            if (!channelMap.VideoStreams.Values.Any(vs => vs.StreamId == stream.StreamId))
            {
                Logger.LogWarning($"Video stream with ID {stream.StreamId} not found in the new channel map. Removing it.");
                dbContext.CarVideoStreams.Remove(stream);
            }
        }

        await dbContext.SaveChangesAsync();
        Logger.LogInformation($"Channel map updated for car {carId}. Channel map hash: {car.ChannelMapHash}");
    }

    public async Task<ChannelMapSyncResponse> SyncChannelMap(ChannelMapSyncRequest request)
    {
        try
        {
            Logger.LogInformation("SyncChannelMap invoked for car {CarId}", request.CarId);
        var dbContext = Context.GetHttpContext()!.RequestServices.GetRequiredService<LteCarContext>();
        var car = dbContext.Cars.FirstOrDefault(c => c.Id == request.CarId);
        if (car == null)
        {
            Logger.LogError($"Car with ID {request.CarId} not found in SyncChannelMap. This should not happen - OpenCarConnection should be called first.");
            throw new InvalidOperationException($"Car with ID {request.CarId} not found. Please call OpenCarConnection first.");
        }

        var map = request.ChannelMap ?? new ChannelMap();

        // Last-write-wins per channel. The incoming item's ModifiedAt is the onboard's
        // last-write timestamp. Server compares against its own ModifiedAt and takes the
        // newer side. After this loop, `map` carries the merged values for the response,
        // and `car.ChannelMapHash` is computed from this merged map.
        var controlIds = new Dictionary<string,int>();
        foreach (var kv in map.ControlChannels)
        {
            var db = dbContext.CarChannels.FirstOrDefault(c => c.ChannelName == kv.Key && c.CarId == car.Id);
            var incomingTime = kv.Value.ModifiedAt;
            if (db == null)
            {
                db = new CarChannel
                {
                    ChannelName = kv.Key,
                    CarId = car.Id,
                    MaxResendInterval = kv.Value.MaxResendInterval,
                    ControlType = kv.Value.ControlType,
                    PinManager = string.IsNullOrEmpty(kv.Value.PinManager) ? "default" : kv.Value.PinManager,
                    Address = kv.Value.Address,
                    OptionsJson = kv.Value.Options != null && kv.Value.Options.Count > 0
                        ? JsonSerializer.Serialize(kv.Value.Options)
                        : null,
                    TestDisabled = kv.Value.TestDisabled,
                    ModifiedAt = incomingTime ?? DateTime.UtcNow,
                };
                dbContext.CarChannels.Add(db);
                await dbContext.SaveChangesAsync();
            }
            else if (incomingTime.HasValue && (db.ModifiedAt == null || incomingTime.Value > db.ModifiedAt.Value))
            {
                db.MaxResendInterval = kv.Value.MaxResendInterval;
                db.ControlType = kv.Value.ControlType;
                db.PinManager = string.IsNullOrEmpty(kv.Value.PinManager) ? "default" : kv.Value.PinManager;
                db.Address = kv.Value.Address;
                db.OptionsJson = kv.Value.Options != null && kv.Value.Options.Count > 0
                    ? JsonSerializer.Serialize(kv.Value.Options)
                    : null;
                db.TestDisabled = kv.Value.TestDisabled;
                db.ModifiedAt = incomingTime;
            }
            // Patch merged values back into the map so the response carries DB's view
            kv.Value.MaxResendInterval = db.MaxResendInterval;
            kv.Value.ControlType = db.ControlType;
            kv.Value.PinManager = db.PinManager;
            kv.Value.Address = db.Address;
            kv.Value.Options = !string.IsNullOrEmpty(db.OptionsJson)
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(db.OptionsJson) ?? new()
                : new Dictionary<string, object>();
            kv.Value.TestDisabled = db.TestDisabled;
            kv.Value.ModifiedAt = db.ModifiedAt;
            controlIds[kv.Key] = db.Id;
        }
        foreach (var stale in dbContext.CarChannels.Where(c => c.CarId == car.Id).ToList())
        {
            if (!map.ControlChannels.ContainsKey(stale.ChannelName))
            {
                dbContext.CarChannels.Remove(stale);
            }
        }

        var telemetryIds = new Dictionary<string,int>();
        foreach (var kv in map.TelemetryChannels)
        {
            var db = dbContext.CarTelemetry.FirstOrDefault(c => c.ChannelName == kv.Key && c.CarId == car.Id);
            var incomingTime = kv.Value.ModifiedAt;
            if (db == null)
            {
                db = new CarTelemetry
                {
                    ChannelName = kv.Key,
                    CarId = car.Id,
                    TelemetryType = kv.Value.TelemetryType,
                    ReadIntervalTicks = kv.Value.ReadIntervalTicks,
                    DataType = kv.Value.DataType,
                    Unit = kv.Value.Unit,
                    Decimals = kv.Value.Decimals,
                    ModifiedAt = incomingTime ?? DateTime.UtcNow,
                };
                dbContext.CarTelemetry.Add(db);
                await dbContext.SaveChangesAsync();
            }
            else if (incomingTime.HasValue && (db.ModifiedAt == null || incomingTime.Value > db.ModifiedAt.Value))
            {
                db.TelemetryType = kv.Value.TelemetryType;
                db.ReadIntervalTicks = kv.Value.ReadIntervalTicks;
                db.DataType = kv.Value.DataType;
                db.Unit = kv.Value.Unit;
                db.Decimals = kv.Value.Decimals;
                db.ModifiedAt = incomingTime;
            }
            kv.Value.TelemetryType = db.TelemetryType;
            kv.Value.ReadIntervalTicks = db.ReadIntervalTicks;
            kv.Value.DataType = db.DataType;
            kv.Value.Unit = db.Unit;
            kv.Value.Decimals = db.Decimals;
            kv.Value.ModifiedAt = db.ModifiedAt;
            telemetryIds[kv.Key] = db.Id;
        }
        foreach (var stale in dbContext.CarTelemetry.Where(c => c.CarId == car.Id).ToList())
        {
            if (!map.TelemetryChannels.ContainsKey(stale.ChannelName))
            {
                dbContext.CarTelemetry.Remove(stale);
            }
        }

        var videoIds = new Dictionary<string,int>();
        foreach (var kv in map.VideoStreams)
        {
            var value = kv.Value;
            var db = dbContext.CarVideoStreams.FirstOrDefault(s => s.StreamId == value.StreamId && s.CarId == car.Id);
            var incomingTime = value.ModifiedAt;
            if (db == null)
            {
                db = new CarVideoStream
                {
                    StreamId = value.StreamId,
                    CarId = car.Id,
                    StartTime = DateTime.UtcNow,
                    Name = value.Name ?? value.StreamId,
                    Type = value.Type ?? "unknown",
                    Location = value.Location,
                    IsActive = value.Enabled,
                    Enabled = value.Enabled,
                    ModifiedAt = incomingTime ?? DateTime.UtcNow,
                };
                dbContext.CarVideoStreams.Add(db);
                await dbContext.SaveChangesAsync();
            }
            else if (incomingTime.HasValue && (db.ModifiedAt == null || incomingTime.Value > db.ModifiedAt.Value))
            {
                db.Name = value.Name ?? value.StreamId;
                db.Type = value.Type ?? "unknown";
                db.Location = value.Location;
                db.IsActive = value.Enabled;
                db.Enabled = value.Enabled;
                db.ModifiedAt = incomingTime;
            }
            value.Name = db.Name;
            value.Type = db.Type;
            value.Location = db.Location;
            value.Enabled = db.Enabled;
            value.ModifiedAt = db.ModifiedAt;
            videoIds[value.StreamId] = db.Id;
        }
        foreach (var stale in dbContext.CarVideoStreams.Where(s => s.CarId == car.Id).ToList())
        {
            if (!map.VideoStreams.Values.Any(v => v.StreamId == stale.StreamId))
            {
                dbContext.CarVideoStreams.Remove(stale);
            }
        }

        await dbContext.SaveChangesAsync();

        // Hash from the merged map (server's view of the world)
        var hash = ChannelMapHashProvider.GenerateHash(map);
        car.ChannelMapHash = hash;
        car.LastSeen = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        var response = new ChannelMapSyncResponse
        {
            Hash = hash,
            ChannelMap = map,
            ControlIds = controlIds,
            TelemetryIds = telemetryIds,
            VideoIds = videoIds,
            GeneratedAtUtc = DateTime.UtcNow
        };

        // Annotate map items with their server ids for persistence on the client side
        foreach (var kv in map.ControlChannels)
        {
            if (controlIds.TryGetValue(kv.Key, out var id))
            {
                kv.Value.ServerId = id;
            }
        }
        foreach (var kv in map.TelemetryChannels)
        {
            if (telemetryIds.TryGetValue(kv.Key, out var id))
            {
                kv.Value.ServerId = id;
            }
        }
        foreach (var kv in map.VideoStreams)
        {
            if (videoIds.TryGetValue(kv.Value.StreamId, out var id))
            {
                kv.Value.ServerId = id;
            }
        }

        Logger.LogInformation("ChannelMap sync complete for {CarId} hash {Hash}", request.CarId, hash);
        return response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "FATAL ERROR in SyncChannelMap for car {CarId}", request?.CarId.ToString() ?? "UNKNOWN");
            throw;
        }
    }

    public async Task ReportFileTransferStatus(FileTransferStatusUpdate update)
    {
        Logger.LogInformation("Device reports transfer {Token} status: {Status}", update.Token, update.Status);

        var dbContext = Context.GetHttpContext()!.RequestServices.GetRequiredService<LteCarContext>();
        var transfer = await dbContext.FileTransfers
            .FirstOrDefaultAsync(f => f.DownloadToken == update.Token);
        if (transfer == null)
        {
            Logger.LogWarning("Transfer with token {Token} not found", update.Token);
            return;
        }

        if (update.Status == FileTransferStatus.Completed)
        {
            if (string.IsNullOrEmpty(update.Sha256Hash) ||
                !string.Equals(transfer.Sha256Hash, update.Sha256Hash, StringComparison.OrdinalIgnoreCase))
            {
                Logger.LogWarning("Hash mismatch for transfer {Token}: expected {Expected}, got {Actual}",
                    update.Token, transfer.Sha256Hash, update.Sha256Hash);
                return;
            }

            Logger.LogInformation("Transfer {Token} completed, hash verified. Cleaning up.", update.Token);

            if (!string.IsNullOrEmpty(transfer.StoragePath) && File.Exists(transfer.StoragePath))
                File.Delete(transfer.StoragePath);

            dbContext.FileTransfers.Remove(transfer);
            await dbContext.SaveChangesAsync();
            return;
        }

        transfer.Status = update.Status;
        await dbContext.SaveChangesAsync();
    }

    // -- Control session methods (relay to the car's ControlService) ----------------

    public async Task RegisterForControl(int carId)
    {
        Logger.LogDebug($"Invoked: RegisterForControl({carId}) => connection {Context.ConnectionId}");
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Car-{carId}");
    }

    public async Task<string?> AquireCarControl(int carId, SshAuthenticationRequest authRequest)
    {
        Logger.LogDebug($"Invoked: AquireCarControl({carId}, challenge={authRequest.Challenge[..Math.Min(10, authRequest.Challenge.Length)]}...) as {Context.User?.Identity?.Name}");
        if (!_connectionStore.TryGetValue(carId.ToString(), out var connectionInfo))
        {
            Logger.LogDebug("Car not connected");
            return null;
        }
        var session = await Clients.Client(connectionInfo.ConnectionId).AquireCarControl(authRequest);
        Logger.LogDebug($"Session returned: {session}");

        if (!string.IsNullOrEmpty(session))
        {
            await EnsureUserCarSetupExists(carId);
            await MarkUserAsActiveVehicle(carId);
            await MarkUserAsHasControlledCar(carId);
            await UpdateCarUiDriverStateAsync(carId);
        }
        return session;
    }

    public async Task ReleaseCarControl(int carId, string sessionId)
    {
        Logger.LogDebug($"Invoked: ReleaseCarControl({carId}, {sessionId})");
        if (!_connectionStore.TryGetValue(carId.ToString(), out var connectionInfo))
            return;
        await Clients.Client(connectionInfo.ConnectionId).ReleaseCarControl(sessionId);
        await ClearUserActiveVehicle(carId);
        await UpdateCarUiDriverStateAsync(carId);
    }

    public async Task UpdateChannel(int carId, string sessionId, int channelId, decimal value)
    {
        Logger.LogDebug($"Invoked: UpdateChannel({carId}, {sessionId}, {channelId}, {value})");
        if (!_connectionStore.TryGetValue(carId.ToString(), out var connectionInfo))
            return;
        // TODO: Cache einbauen
        var dbContext = Context.GetHttpContext()!.RequestServices.GetRequiredService<LteCarContext>();
        var channelName = dbContext.Set<CarChannel>().FirstOrDefault(e => e.Id == channelId)?.ChannelName;
        if (channelName == null)
        {
            Logger.LogError($"Channel ID: {channelId} unknown");
            return;
        }
        await Clients.Client(connectionInfo.ConnectionId).UpdateChannel(sessionId, channelName, value);
    }

    public async Task<string?> GetChallenge(int carId)
    {
        Logger.LogDebug($"Invoked: GetChallenge({carId})");
        if (!_connectionStore.TryGetValue(carId.ToString(), out var connectionInfo))
            return null;
        var challenge = await Clients.Client(connectionInfo.ConnectionId).GetChallenge();
        Logger.LogDebug($"Challenge returned: {challenge?[..Math.Min(20, challenge?.Length ?? 0)]}...");
        return challenge;
    }

    public async Task<FileUploadApproval?> RequestFileUpload(int carId, string sessionId, string filePath)
    {
        if (!_connectionStore.TryGetValue(carId.ToString(), out var connectionInfo))
            return null;

        var approved = await Clients.Client(connectionInfo.ConnectionId).ApproveFileUpload(sessionId, filePath);
        if (!approved)
            return null;

        var dbContext = Context.GetHttpContext()!.RequestServices.GetRequiredService<LteCarContext>();
        var transfer = new FileTransfer
        {
            CarId = carId,
            FileName = filePath,
            Status = FileTransferStatus.Uploading
        };
        dbContext.FileTransfers.Add(transfer);
        await dbContext.SaveChangesAsync();

        Logger.LogInformation("File upload approved for car {CarId}, path '{FilePath}', transfer {TransferId}",
            carId, filePath, transfer.Id);

        return new FileUploadApproval { Token = transfer.DownloadToken };
    }

    public async Task<ListFilesResponse?> ListFilesOnDevice(int carId, string sessionId, string path)
    {
        if (!_connectionStore.TryGetValue(carId.ToString(), out var connectionInfo))
            return null;
        return await Clients.Client(connectionInfo.ConnectionId).ListFiles(sessionId, path);
    }

    public async Task<bool> DeleteFileOnDevice(int carId, string sessionId, string filePath)
    {
        if (!_connectionStore.TryGetValue(carId.ToString(), out var connectionInfo))
            return false;
        return await Clients.Client(connectionInfo.ConnectionId).DeleteFile(sessionId, filePath);
    }

    public async Task<PingCarResult?> PingCar(int carId)
    {
        if (!_connectionStore.TryGetValue(carId.ToString(), out var connectionInfo))
        {
            Logger.LogDebug($"PingCar: Car {carId} not connected");
            return null;
        }
        var sw = Stopwatch.StartNew();
        var carTimestamp = await Clients.Client(connectionInfo.ConnectionId).Ping();
        sw.Stop();
        return new PingCarResult(carTimestamp, sw.Elapsed.TotalMilliseconds);
    }

    public async Task SendBashOutput(int carId, string output, bool isError)
    {
        await Clients.All.SendBashOutput(carId, output, isError);
    }

    // -- User state plumbing --------------------------------------------------------

    private async Task EnsureUserCarSetupExists(int carId)
    {
        try
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                Logger.LogWarning($"No authenticated user found for car {carId}");
                return;
            }
            var dbContext = Context.GetHttpContext()!.RequestServices.GetRequiredService<LteCarContext>();
            var car = await dbContext.Cars.FirstOrDefaultAsync(c => c.Id == carId);
            if (car == null)
            {
                Logger.LogWarning($"Car with ID {carId} not found. Car should have been registered via OpenCarConnection.");
                return;
            }
            var existingSetup = await dbContext.UserSetups
                .FirstOrDefaultAsync(u => u.UserId == user.Id && u.CarId == car.Id);
            if (existingSetup == null)
            {
                Logger.LogInformation($"Creating UserCarSetup for user {user.Id} and car {carId}");
                dbContext.UserSetups.Add(new UserCarSetup { UserId = user.Id, CarId = car.Id });
                await dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Error ensuring UserCarSetup exists for car {carId}");
        }
    }

    private async Task MarkUserAsHasControlledCar(int carId)
    {
        try
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return;
            if (!user.HasControlledCar)
            {
                user.HasControlledCar = true;
                var dbContext = Context.GetHttpContext()!.RequestServices.GetRequiredService<LteCarContext>();
                await dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Error marking user as having controlled car {carId}");
        }
    }

    private async Task MarkUserAsActiveVehicle(int carId)
    {
        try
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return;
            if (user.ActiveVehicleId == carId) return;
            user.ActiveVehicleId = carId;
            var dbContext = Context.GetHttpContext()!.RequestServices.GetRequiredService<LteCarContext>();
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error marking active vehicle for car {CarId}", carId);
        }
    }

    private async Task ClearUserActiveVehicle(int carId)
    {
        try
        {
            var user = await GetCurrentUserAsync();
            if (user == null || user.ActiveVehicleId != carId) return;
            user.ActiveVehicleId = null;
            var dbContext = Context.GetHttpContext()!.RequestServices.GetRequiredService<LteCarContext>();
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error clearing active vehicle for car {CarId}", carId);
        }
    }

    private async Task UpdateCarUiDriverStateAsync(int carId)
    {
        if (!_connectionStore.TryGetValue(carId.ToString(), out var connectionInfo))
            return;
        var dbContext = Context.GetHttpContext()!.RequestServices.GetRequiredService<LteCarContext>();
        var activeDriver = await dbContext.Users.FirstOrDefaultAsync(user => user.ActiveVehicleId == carId);
        connectionInfo.DriverId = activeDriver?.Id.ToString();
        connectionInfo.DriverName = activeDriver?.Name ?? activeDriver?.LoginName;
        await Clients.All.CarStateUpdated(new CarStateModel
        {
            Id = carId.ToString(),
            IsConnected = true,
            DriverId = connectionInfo.DriverId,
            DriverName = connectionInfo.DriverName
        });
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        if (Context.User?.Identity?.IsAuthenticated != true)
            return null;
        var sessionToken = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(sessionToken))
            return null;
        var sessionId = _sqidsEncoder.Decode(sessionToken).FirstOrDefault();
        var dbContext = Context.GetHttpContext()!.RequestServices.GetRequiredService<LteCarContext>();
        return await dbContext.Users.FirstOrDefaultAsync(u => u.SessionId == sessionId);
    }

    // -- Telemetry (formerly TelemetryHub) ------------------------------------------

    public Task UpdateTelemetry(string carId, string valueName, string value)
    {
        return Clients.Group($"car:{carId}").UpdateTelemetry(valueName, value);
    }

    public Task SubscribeToCarTelemetry(string carId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, $"car:{carId}");
    }

    public Task UnsubscribeFromCarTelemetry(string carId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, $"car:{carId}");
    }

    public Task RegisterAsOnboard(string carId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, $"onboard-{carId}");
    }

    public async Task SubscribeToChannel(string carId, string channelName)
    {
        await Clients.Group($"onboard-{carId}").SubscribeToTelemetryChannel(channelName);
        await PersistTelemetrySubscription(carId, channelName, add: true);
    }

    public async Task UnsubscribeFromChannel(string carId, string channelName)
    {
        await Clients.Group($"onboard-{carId}").UnsubscribeFromTelemetryChannel(channelName);
        await PersistTelemetrySubscription(carId, channelName, add: false);
    }

    private async Task PersistTelemetrySubscription(string carId, string channelName, bool add)
    {
        var db = Context.GetHttpContext()?.RequestServices.GetService<LteCarContext>();
        if (db == null) return;
        if (!int.TryParse(carId, out var carIdInt)) return;

        var user = await HubUserHelper.GetUserAsync(Context.GetHttpContext()!, db);
        if (user == null) return;

        var setup = await db.UserSetups
            .FirstOrDefaultAsync(s => s.UserId == user.Id && s.CarId == carIdInt);
        if (setup == null) return;

        var telemetry = await db.CarTelemetry
            .FirstOrDefaultAsync(t => t.CarId == carIdInt && t.ChannelName == channelName);
        if (telemetry == null) return;

        var existing = await db.UserSetupTelemetries
            .FirstOrDefaultAsync(t => t.UserSetupId == setup.Id && t.CarTelemetryId == telemetry.Id);

        if (add && existing == null)
        {
            db.UserSetupTelemetries.Add(new UserSetupTelemetry
            {
                UserSetupId = setup.Id,
                CarTelemetryId = telemetry.Id,
            });
            await db.SaveChangesAsync();
        }
        else if (!add && existing != null)
        {
            db.UserSetupTelemetries.Remove(existing);
            await db.SaveChangesAsync();
        }
    }

    // -- Video (formerly CarVideoHub) ----------------------------------------------

    public async Task ConnectCar(string carIdentityKey)
    {
        var dbContext = Context.GetHttpContext()!.RequestServices.GetRequiredService<LteCarContext>();
        var car = dbContext.Cars
            .Include(c => c.VideoStreams)
            .FirstOrDefault(c => c.CarIdentityKey == carIdentityKey);
        if (car == null)
        {
            Logger.LogWarning("Car with identity key {CarIdentityKey} not found", carIdentityKey);
            throw new InvalidOperationException($"Car with identity key {carIdentityKey} not found!");
        }
        await this.AddCarToGroupAsync(car.Id);
        Logger.LogInformation("Car {CarIdentityKey} connected with ID {CarId}. Synchronizing viewer-driven video streams.", carIdentityKey, car.Id);

        foreach (var stream in car.VideoStreams.Where(stream => stream.Enabled && _viewerRegistry.GetViewerCount(stream.Id) > 0))
        {
            await StartStreamForViewersAsync(stream);
        }
    }

    public async Task StartVideoStream(int streamId)
    {
        var stream = await GetStreamAsync(streamId);
        await StartStreamForViewersAsync(stream);
    }

    private async Task SanitizeStreamSettings(CarVideoStream s)
    {
        if (s.BitrateKbps < 256 || s.BitrateKbps > 100000)
        {
            Logger.LogWarning("Sanitizing bitrate {BitrateKbps} for stream {StreamId}", s.BitrateKbps, s.StreamId);
            s.BitrateKbps = Math.Clamp(s.BitrateKbps, 256, 100000);
        }
        if (s.Framerate < 1 || s.Framerate > 60)
        {
            Logger.LogWarning("Sanitizing framerate {Framerate} for stream {StreamId}", s.Framerate, s.StreamId);
            s.Framerate = Math.Clamp(s.Framerate, 1, 60);
        }
        if (s.Width < 160 || s.Width > 4096)
        {
            Logger.LogWarning("Sanitizing width {Width} for stream {StreamId}", s.Width, s.StreamId);
            s.Width = Math.Clamp(s.Width, 160, 4096);
        }
        if (s.Height < 120 || s.Height > 2160)
        {
            Logger.LogWarning("Sanitizing height {Height} for stream {StreamId}", s.Height, s.StreamId);
            s.Height = Math.Clamp(s.Height, 120, 2160);
        }
        if (s.Brightness < -1 || s.Brightness > 1)
        {
            Logger.LogWarning("Sanitizing brightness {Brightness} for stream {StreamId}", s.Brightness, s.StreamId);
            s.Brightness = Math.Clamp(s.Brightness, -1, 1);
        }
        if (s.BitrateKbps % 64 != 0)
        {
            var original = s.BitrateKbps;
            s.BitrateKbps = (s.BitrateKbps / 64) * 64;
            Logger.LogWarning("Adjusting bitrate {OriginalBitrateKbps} to nearest multiple of 64: {AdjustedBitrateKbps} for stream {StreamId}", original, s.BitrateKbps, s.StreamId);
        }
        if (s.Port < _configService.Janus.PortRangeStart || s.Port > _configService.Janus.PortRangeEnd)
        {
            Logger.LogWarning("Sanitizing port {Port} for stream {StreamId}", s.Port, s.StreamId);
            s.Port = _streamService.FindFreePort(s.Protocol);
        }
    }

    public async Task<IReadOnlyList<VideoStreamInfoModel>> GetVideoStreamsForCar(int carId)
    {
        var dbContext = Context.GetHttpContext()!.RequestServices.GetRequiredService<LteCarContext>();
        var streams = await dbContext.CarVideoStreams
            .Where(s => s.CarId == carId)
            .OrderBy(s => s.Priority)
            .ThenBy(s => s.Name)
            .ToListAsync();

        return streams
            .Select(s => new VideoStreamInfoModel()
            {
                Id = s.Id,
                Name = s.Name,
                StreamId = s.StreamId,
                Type = s.Type,
                Location = s.Location,
                Priority = s.Priority,
                Width = s.Width,
                Height = s.Height,
                BitrateKbps = s.BitrateKbps,
                Framerate = s.Framerate,
                Brightness = s.Brightness,
                Enabled = s.Enabled,
                IsActive = s.IsActive,
                ViewerCount = _viewerRegistry.GetViewerCount(s.Id)
            })
            .ToList();
    }

    public async Task ActivateStream(int streamId)
    {
        var stream = await GetStreamAsync(streamId);
        if (!stream.Enabled)
        {
            throw new HubException($"Stream {stream.Name} is disabled.");
        }
        var firstViewer = _viewerRegistry.Activate(Context.ConnectionId, streamId);
        Logger.LogInformation("Connection {ConnectionId} activated stream {StreamId}. Viewers: {ViewerCount}", Context.ConnectionId, streamId, _viewerRegistry.GetViewerCount(streamId));
        if (firstViewer)
        {
            await StartStreamForViewersAsync(stream);
        }
    }

    public async Task DeactivateStream(int streamId)
    {
        var stream = await GetStreamAsync(streamId);
        var lastViewer = _viewerRegistry.Deactivate(Context.ConnectionId, streamId);
        Logger.LogInformation("Connection {ConnectionId} deactivated stream {StreamId}. Viewers: {ViewerCount}", Context.ConnectionId, streamId, _viewerRegistry.GetViewerCount(streamId));
        if (lastViewer)
        {
            await StopStreamForViewersAsync(stream);
        }
    }

    public async Task StopVideoStream(int streamId)
    {
        var stream = await GetStreamAsync(streamId);
        _viewerRegistry.ClearStream(streamId);
        await StopStreamForViewersAsync(stream);
    }

    public async Task ChangeVideoStreamSettings(int streamId, VideoSettingsModel settings)
    {
        var stream = await GetStreamAsync(streamId);
        var dbContext = Context.GetHttpContext()!.RequestServices.GetRequiredService<LteCarContext>();
        Logger.LogInformation("Stopping video stream {StreamId} ({StreamName}) for car {CarId}", stream.Id, stream.Name, stream.CarId);
        await Clients.Car(stream.CarId).StopVideoStream(stream.StreamId);
        await _streamService.StopStream(streamId);
        Logger.LogInformation("Changing video stream settings for stream {StreamId} ({StreamName}) for car {CarId}", stream.Id, stream.Name, stream.CarId);
        settings.ApplySettings(stream);
        await SanitizeStreamSettings(stream);
        await dbContext.SaveChangesAsync();

        if (_viewerRegistry.GetViewerCount(streamId) == 0)
        {
            return;
        }

        Logger.LogInformation("Restarting video stream {StreamId} ({StreamName}) for car {CarId} with new settings", stream.Id, stream.Name, stream.CarId);
        var settingsToApply = new VideoSettings()
        {
            Height = stream.Height,
            Width = stream.Width,
            Framerate = stream.Framerate,
            BitrateKbps = stream.BitrateKbps,
            Brightness = stream.Brightness,
            Protocol = stream.Protocol,
            TargetPort = stream.Port
        };
        await Clients.Car(stream.CarId).StartVideoStream(stream.StreamId, settingsToApply);
    }

    public async Task SetVideoStreamEnabled(int carId, int streamId, bool enabled)
    {
        await EnsureDriverCanManageStreamAsync(carId);

        var dbContext = Context.GetHttpContext()!.RequestServices.GetRequiredService<LteCarContext>();
        var stream = await dbContext.CarVideoStreams
            .FirstOrDefaultAsync(s => s.Id == streamId && s.CarId == carId)
            ?? throw new InvalidOperationException($"Video stream with ID {streamId} not found for car {carId}.");

        if (stream.Enabled == enabled)
        {
            return;
        }

        stream.Enabled = enabled;
        await dbContext.SaveChangesAsync();
        Logger.LogInformation("Driver changed enabled state for stream {StreamId} on car {CarId} to {Enabled}", streamId, carId, enabled);

        if (enabled)
        {
            return;
        }

        _viewerRegistry.ClearStream(streamId);
        await StopStreamForViewersAsync(stream);
    }

    private async Task<CarVideoStream> GetStreamAsync(int streamId)
    {
        var dbContext = Context.GetHttpContext()!.RequestServices.GetRequiredService<LteCarContext>();
        return await dbContext.CarVideoStreams
            .FirstOrDefaultAsync(s => s.Id == streamId)
            ?? throw new InvalidOperationException($"Video stream with ID {streamId} not found.");
    }

    private async Task StartStreamForViewersAsync(CarVideoStream stream)
    {
        var dbContext = Context.GetHttpContext()!.RequestServices.GetRequiredService<LteCarContext>();
        Logger.LogInformation("Starting video stream {StreamId} ({StreamName}) for car {CarId}", stream.Id, stream.Name, stream.CarId);
        var settings = await _streamService.StartStreamAsync(stream.Id);
        stream.IsActive = true;
        await dbContext.SaveChangesAsync();
        await Clients.Car(stream.CarId).StartVideoStream(stream.StreamId, settings);
    }

    private async Task StopStreamForViewersAsync(CarVideoStream stream)
    {
        var dbContext = Context.GetHttpContext()!.RequestServices.GetRequiredService<LteCarContext>();
        Logger.LogInformation("Stopping video stream {StreamId} ({StreamName}) for car {CarId}", stream.Id, stream.Name, stream.CarId);
        await Clients.Car(stream.CarId).StopVideoStream(stream.StreamId);
        await _streamService.StopStream(stream.Id);
        stream.IsActive = false;
        await dbContext.SaveChangesAsync();
    }

    private async Task EnsureDriverCanManageStreamAsync(int carId)
    {
        var httpContext = Context.GetHttpContext()
            ?? throw new HubException("No HTTP context available for video hub request.");
        var dbContext = httpContext.RequestServices.GetRequiredService<LteCarContext>();
        var user = await HubUserHelper.GetUserAsync(httpContext, dbContext);
        if (user == null || string.IsNullOrWhiteSpace(user.LoginName))
        {
            throw new HubException("You must be logged in to enable or disable streams.");
        }

        var hasSetup = await dbContext.UserSetups.AnyAsync(setup => setup.UserId == user.Id && setup.CarId == carId);
        if (!hasSetup)
        {
            throw new HubException("You do not have access to manage this vehicle.");
        }

        if (user.ActiveVehicleId != carId)
        {
            throw new HubException("You must actively control this vehicle to change stream enable state.");
        }
    }
}