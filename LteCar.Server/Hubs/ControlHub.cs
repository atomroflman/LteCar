using LteCar.Shared.HubClients;
using Microsoft.AspNetCore.SignalR;

namespace LteCar.Server.Hubs;

public class CarControlHub : Hub<ICarControlClient>, ICarControlServer
{
    public IConfiguration Configuration { get; }
    private readonly CarConnectionStore _carConnectionStore;

    public CarControlHub(CarConnectionStore carConnectionStore, IConfiguration configuration)
    {
        Configuration = configuration;
        _carConnectionStore = carConnectionStore;
    }
    
    public async Task<string> AquireCarControl(string carId, string carSecret)
    {
        var session = await Clients.Client(carId).AquireCarControl(carSecret);
        return session;
    }
    
    public async Task ReleaseCarControl(string carId, string sessionId)
    {
        await Clients.Client(carId).ReleaseCarControl(sessionId);
    }
    
    public async Task UpdateChannel(string carId, string sessionId, string channelId, decimal value)
    {
        await Clients.Client(carId).UpdateChannel(sessionId, channelId, value);
    }
}