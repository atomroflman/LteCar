using System.Text.Json;
using LteCar.Server.Configuration;
using LteCar.Server.Data;
using LteCar.Server.Services;
using LteCar.Shared;
using LteCar.Shared.HubClients;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Sqids;

namespace LteCar.Server.Hubs;

public class CarControlHub : Hub<ICarControlClient>, ICarControlServer
{
    public ILogger<CarControlHub> Logger { get; }
    public SqidsEncoder<long> SqidsEncoder { get; }

    private readonly IConfigurationService _configService;
    private readonly LteCarContext _context;

    private static BiDictionary<string, string> _connectionMap = new BiDictionary<string, string>();

    public CarControlHub(IConfigurationService configService, ILogger<CarControlHub> logger, LteCarContext context, SqidsEncoder<long> sqidsEncoder)
    {
        _configService = configService;
        Logger = logger;
        _context = context;
        SqidsEncoder = sqidsEncoder;
    }

    public async Task RegisterForControl(int carId) 
    {
        Logger.LogDebug($"Invoked: RegisterForControl({carId}) => saving Connection Id: {Context.ConnectionId}");
        _connectionMap.Add(carId.ToString(), Context.ConnectionId);
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
        
        // If authentication was successful, ensure UserCarSetup exists
        if (!string.IsNullOrEmpty(session))
        {
            await EnsureUserCarSetupExists(carIdStr);
        }
        
        return session;
    }
    
    public async Task ReleaseCarControl(int carId, string sessionId)
    {
        Logger.LogDebug($"Invoked: ReleaseCarControl({carId}, {sessionId})");
        if (!_connectionMap.TryGetByKey(carId.ToString(), out var carClientId))
            return;
        await Clients.Client(carClientId).ReleaseCarControl(sessionId);
    }
    
    public async Task UpdateChannel(int carId, string sessionId, int channelId, decimal value)
    {
        Logger.LogDebug($"Invoked: UpdateChannel({carId}, {sessionId}, {channelId}, {value})");
        if (!_connectionMap.TryGetByKey(carId.ToString(), out var carClientId))
            return;
        // TODO: Cache einbauen
        var channelName = Context.GetHttpContext().RequestServices.GetRequiredService<LteCarContext>()
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

}
