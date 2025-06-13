using System.Text.Json;
using LteCar.Server.Data;
using LteCar.Server.Services;
using LteCar.Shared.HubClients;
using Microsoft.AspNetCore.SignalR;

namespace LteCar.Server.Hubs;

public class CarControlHub : Hub<ICarControlClient>, ICarControlServer
{
    public IConfiguration Configuration { get; }
    public ILogger<CarControlHub> Logger { get; }

    private static BiDictionary<string, string> _connectionMap = new BiDictionary<string, string>();

    public CarControlHub(IConfiguration configuration, ILogger<CarControlHub> logger)
    {
        Configuration = configuration;
        Logger = logger;
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
        Logger.LogDebug($"Seesion returned: {session}");
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
}
