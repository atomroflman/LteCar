using System.Text.Json;
using LteCar.Server.Configuration;
using LteCar.Server.Data;
using LteCar.Server.Services;
using LteCar.Shared.HubClients;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LteCar.Server.Hubs;

public class CarControlHub : Hub<ICarControlClient>, ICarControlServer
{
    public ILogger<CarControlHub> Logger { get; }
    private readonly IConfigurationService _configService;
    private readonly LteCarContext _context;

    private static BiDictionary<string, string> _connectionMap = new BiDictionary<string, string>();

    public CarControlHub(IConfigurationService configService, ILogger<CarControlHub> logger, LteCarContext context)
    {
        _configService = configService;
        Logger = logger;
        _context = context;
    }

    public async Task RegisterForControl(string carId) 
    {
        Logger.LogDebug($"Invoked: RegisterForControl({carId}) => saving Connection Id: {Context.ConnectionId}");
        _connectionMap.Add(carId, Context.ConnectionId);
    }
    
    public async Task<string?> AquireCarControl(string carId, string? carSecret)
    {
        Logger.LogDebug($"Invoked: AquireCarControl({carId}, {carSecret}) as {this.Context?.User?.Identity?.Name}");
        if (!_connectionMap.TryGetByKey(carId, out var carClientId))
        {
            Logger.LogDebug($"Car not found in connection dictionary. {JsonSerializer.Serialize(_connectionMap)}");
            return null;
        }
        var session = await Clients.Client(carClientId).AquireCarControl(carSecret);
        Logger.LogDebug($"Session returned: {session}");
        
        // If authentication was successful, ensure UserCarSetup exists
        if (!string.IsNullOrEmpty(session))
        {
            await EnsureUserCarSetupExists(carId);
        }
        
        return session;
    }
    
    public async Task ReleaseCarControl(string carId, string sessionId)
    {
        Logger.LogDebug($"Invoked: ReleaseCarControl({carId}, {sessionId})");
        if (!_connectionMap.TryGetByKey(carId, out var carClientId))
            return;
        await Clients.Client(carClientId).ReleaseCarControl(sessionId);
    }
    
    public async Task UpdateChannel(string carId, string sessionId, int channelId, decimal value)
    {
        Logger.LogDebug($"Invoked: UpdateChannel({carId}, {sessionId}, {channelId}, {value})");
        if (!_connectionMap.TryGetByKey(carId, out var carClientId))
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

    public async Task<string?> GetChallenge(string carId)
    {
        Logger.LogDebug($"Invoked: GetChallenge({carId})");
        if (!_connectionMap.TryGetByKey(carId, out var carClientId))
        {
            Logger.LogDebug($"Car not found in connection dictionary. {JsonSerializer.Serialize(_connectionMap)}");
            return null;
        }
        var challenge = await Clients.Client(carClientId).GetChallenge();
        Logger.LogDebug($"Challenge returned: {challenge?[..Math.Min(20, challenge?.Length ?? 0)]}...");
        return challenge;
    }

    private async Task EnsureUserCarSetupExists(string carId)
    {
        try
        {
            // Get current user from context
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                Logger.LogWarning($"No authenticated user found for car {carId}");
                return;
            }

            // Get or create car
            var car = await _context.Cars.FirstOrDefaultAsync(c => c.CarId == carId);
            if (car == null)
            {
                Logger.LogInformation($"Creating new car entry for {carId}");
                car = new Car { CarId = carId };
                _context.Cars.Add(car);
                await _context.SaveChangesAsync();
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
            Logger.LogError(ex, $"Error ensuring UserCarSetup exists for car {carId}");
        }
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        if (Context.User?.Identity?.IsAuthenticated != true)
            return null;

        var sessionToken = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(sessionToken))
            return null;

        return await _context.Users.FirstOrDefaultAsync(u => u.SessionToken == sessionToken);
    }

}
