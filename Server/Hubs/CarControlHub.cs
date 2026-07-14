using System.Text.Json;
using System.Security.Claims;
using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using LteCar.Shared.HubClients;
using Sqids;
using LteCar.Server.Configuration;
using LteCar.Server.Data;
using LteCar.Server.Services;
using LteCar.Shared;
using LteCar.Shared.FileTransfer;
using Microsoft.EntityFrameworkCore;

namespace LteCar.Server.Hubs;

public class CarControlHub : Hub<ICarControlClient>, ICarControlServer
{
    public ILogger<CarControlHub> Logger { get; }
    public SqidsEncoder<long> SqidsEncoder { get; }
    public IHubContext<CarConnectionHub, IConnectionHubClient> ConnectionHubContext { get; }

    private readonly IConfigurationService _configService;
    private readonly LteCarContext _context;
    private readonly CarConnectionStore _connectionStore;

    private static BiDictionary<string, string> _connectionMap = new BiDictionary<string, string>();

    public CarControlHub(IConfigurationService configService, ILogger<CarControlHub> logger, LteCarContext context, SqidsEncoder<long> sqidsEncoder, IHubContext<CarConnectionHub, IConnectionHubClient> connectionHubContext, CarConnectionStore connectionStore)
    {
        _configService = configService;
        Logger = logger;
        _context = context;
        SqidsEncoder = sqidsEncoder;
        ConnectionHubContext = connectionHubContext;
        _connectionStore = connectionStore;
    }

    public async Task RegisterForControl(int carId) 
    {
        Logger.LogDebug($"Invoked: RegisterForControl({carId}) => saving Connection Id: {Context.ConnectionId}");
        _connectionMap.Add(carId.ToString(), Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Car-{carId}");
    }
    
    public async Task<string?> AquireCarControl(int carId, SshAuthenticationRequest authRequest)
    {
        Logger.LogDebug($"Invoked: AquireCarControl({carId}, challenge={authRequest.Challenge.Substring(0, Math.Min(10, authRequest.Challenge.Length))}...) as {this.Context?.User?.Identity?.Name}");
        var carIdStr = carId.ToString();
        if (!_connectionMap.TryGetByKey(carIdStr, out var carClientId))
        {
            Logger.LogDebug($"Car not found in connection dictionary. {JsonSerializer.Serialize(_connectionMap)}");
            return null;
        }
        var session = await Clients.Client(carClientId).AquireCarControl(authRequest);
        Logger.LogDebug($"Session returned: {session}");
        
        if (!string.IsNullOrEmpty(session))
        {
            await EnsureUserCarSetupExists(carIdStr);
            await MarkUserAsActiveVehicle(carId);
            await MarkUserAsHasControlledCar(carIdStr);
            await UpdateCarUiDriverStateAsync(carId);
        }
        
        return session;
    }
    
    public async Task ReleaseCarControl(int carId, string sessionId)
    {
        Logger.LogDebug($"Invoked: ReleaseCarControl({carId}, {sessionId})");
        if (!_connectionMap.TryGetByKey(carId.ToString(), out var carClientId))
            return;
        await Clients.Client(carClientId).ReleaseCarControl(sessionId);
        await ClearUserActiveVehicle(carId);
        await UpdateCarUiDriverStateAsync(carId);
    }
    
    public async Task UpdateChannel(int carId, string sessionId, int channelId, decimal value)
    {
        Logger.LogDebug($"Invoked: UpdateChannel({carId}, {sessionId}, {channelId}, {value})");
        if (!_connectionMap.TryGetByKey(carId.ToString(), out var carClientId))
            return;
        // TODO: Cache einbauen
        var httpContext = Context.GetHttpContext();
        if (httpContext == null)
            return;
        var channelName = httpContext.RequestServices.GetRequiredService<LteCarContext>()
            .Set<CarChannel>().FirstOrDefault(e => e.Id == channelId)?.ChannelName;
        if (channelName == null)
        {
            Logger.LogError($"Channel ID: {channelId} unknown");
            return;
        }
        await Clients.Client(carClientId).UpdateChannel(sessionId, channelName, value);
    }

    public async Task<string?> GetChallenge(int carId)
    {
        Logger.LogDebug($"Invoked: GetChallenge({carId})");
        if (!_connectionMap.TryGetByKey(carId.ToString(), out var carClientId))
        {
            Logger.LogDebug($"Car not found in connection dictionary. {JsonSerializer.Serialize(_connectionMap)}");
            return null;
        }
        var challenge = await Clients.Client(carClientId).GetChallenge();
        Logger.LogDebug($"Challenge returned: {challenge?[..Math.Min(20, challenge?.Length ?? 0)]}...");
        return challenge;
    }

    private async Task EnsureUserCarSetupExists(string carIdString)
    {
        try
        {
            // Parse carId from string
            if (!int.TryParse(carIdString, out var carId))
            {
                Logger.LogWarning($"Invalid carId format: {carIdString}");
                return;
            }

            // Get current user from context
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                Logger.LogWarning($"No authenticated user found for car {carId}");
                return;
            }

            // Car should already exist (created by OpenCarConnection)
            var car = await _context.Cars.FirstOrDefaultAsync(c => c.Id == carId);
            if (car == null)
            {
                Logger.LogWarning($"Car with ID {carId} not found. Car should have been registered via OpenCarConnection.");
                return;
            }

            // Check if UserCarSetup already exists
            var existingSetup = await _context.UserSetups
                .FirstOrDefaultAsync(u => u.UserId == user.Id && u.CarId == car.Id);

            if (existingSetup == null)
            {
                Logger.LogInformation($"Creating UserCarSetup for user {user.Id} and car {carId}");
                var userCarSetup = new UserCarSetup
                {
                    UserId = user.Id,
                    CarId = car.Id
                };
                _context.UserSetups.Add(userCarSetup);
                await _context.SaveChangesAsync();
                Logger.LogInformation($"UserCarSetup created successfully for user {user.Id} and car {carId}");
            }
            else
            {
                Logger.LogDebug($"UserCarSetup already exists for user {user.Id} and car {carId}");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Error ensuring UserCarSetup exists for car {carIdString}");
        }
    }

    private async Task MarkUserAsHasControlledCar(string carIdString)
    {
        try
        {
            if (!int.TryParse(carIdString, out var carId))
                return;

            var user = await GetCurrentUserAsync();
            if (user == null)
                return;

            if (!user.HasControlledCar)
            {
                user.HasControlledCar = true;
                await _context.SaveChangesAsync();
                Logger.LogInformation($"User {user.Id} marked as having controlled a car (CarId: {carId})");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Error marking user as having controlled car {carIdString}");
        }
    }

    private async Task MarkUserAsActiveVehicle(int carId)
    {
        try
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return;

            if (user.ActiveVehicleId == carId)
                return;

            user.ActiveVehicleId = carId;
            await _context.SaveChangesAsync();
            Logger.LogInformation("User {UserId} marked vehicle {CarId} as active control target", user.Id, carId);
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
            if (user == null || user.ActiveVehicleId != carId)
                return;

            user.ActiveVehicleId = null;
            await _context.SaveChangesAsync();
            Logger.LogInformation("User {UserId} cleared active vehicle {CarId}", user.Id, carId);
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

        var activeDriver = await _context.Users.FirstOrDefaultAsync(user => user.ActiveVehicleId == carId);
        connectionInfo.DriverId = activeDriver?.Id.ToString();
        connectionInfo.DriverName = activeDriver?.Name ?? activeDriver?.LoginName;

        await ConnectionHubContext.Clients.All.CarStateUpdated(new CarStateModel
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
        var sessionId = SqidsEncoder.Decode(sessionToken).FirstOrDefault();

        return await _context.Users.FirstOrDefaultAsync(u => u.SessionId == sessionId);
    }

    public Task ExecuteBashCommand(int carId, string sessionId, string command)
    {
        throw new NotImplementedException();
    }

    public async Task SendBashOutput(int carId, string output, bool isError)
    {
        var controller = _context.Users.Where(u => u.ActiveVehicleId == carId)
            .FirstOrDefault();
        await ConnectionHubContext.Clients.All.SendBashOutput(carId, output, isError);
    }

    /// <summary>
    /// Browser asks car to approve a file upload. Car validates session.
    /// On approval, server creates a temporary transfer record and returns the upload token.
    /// </summary>
    public async Task<FileUploadApproval?> RequestFileUpload(int carId, string sessionId, string filePath)
    {
        if (!_connectionMap.TryGetByKey(carId.ToString(), out var carClientId))
            return null;

        var approved = await Clients.Client(carClientId).ApproveFileUpload(sessionId, filePath);
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

    /// <summary>
    /// Browser asks car for a file listing. Pure relay — car validates session.
    /// </summary>
    public async Task<ListFilesResponse?> ListFilesOnDevice(int carId, string sessionId, string path)
    {
        if (!_connectionMap.TryGetByKey(carId.ToString(), out var carClientId))
            return null;

        return await Clients.Client(carClientId).ListFiles(sessionId, path);
    }

    /// <summary>
    /// Browser asks car to delete a file. Pure relay — car validates session and deletes locally.
    /// </summary>
    public async Task<bool> DeleteFileOnDevice(int carId, string sessionId, string filePath)
    {
        if (!_connectionMap.TryGetByKey(carId.ToString(), out var carClientId))
            return false;

        return await Clients.Client(carClientId).DeleteFile(sessionId, filePath);
    }

    public async Task<PingCarResult?> PingCar(int carId)
    {
        if (!_connectionMap.TryGetByKey(carId.ToString(), out var carClientId))
        {
            Logger.LogDebug($"PingCar: Car {carId} not found in connection dictionary.");
            return null;
        }
        var sw = Stopwatch.StartNew();
        var carTimestamp = await Clients.Client(carClientId).Ping();
        sw.Stop();
        return new PingCarResult(carTimestamp, sw.Elapsed.TotalMilliseconds);
    }
}
