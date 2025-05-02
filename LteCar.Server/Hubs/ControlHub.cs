using LteCar.Server.Services;
using LteCar.Shared.HubClients;
using Microsoft.AspNetCore.SignalR;

namespace LteCar.Server.Hubs;

public class CarControlHub : Hub<ICarControlClient>, ICarControlServer
{
    public IConfiguration Configuration { get; }
    private BiDictionary<string, string> _connectionMap = new BiDictionary<string, string>();

    public CarControlHub(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public async Task RegisterForControl(string carId) 
    {
        _connectionMap.Add(carId, Context.ConnectionId);
    }
    
    public async Task<string> AquireCarControl(string carId, string carSecret)
    {
        if (!_connectionMap.TryGetByKey(carId, out var carClientId))
            return null;

        var session = await Clients.Client(carClientId).AquireCarControl(carSecret);
        return session;
    }
    
    public async Task ReleaseCarControl(string carId, string sessionId)
    {
        if (!_connectionMap.TryGetByKey(carId, out var carClientId))
            return;
        await Clients.Client(carClientId).ReleaseCarControl(sessionId);
    }
    
    public async Task UpdateChannel(string carId, string sessionId, string channelId, decimal value)
    {
        if (!_connectionMap.TryGetByKey(carId, out var carClientId))
            return;
        await Clients.Client(carClientId).UpdateChannel(sessionId, channelId, value);
    }
}