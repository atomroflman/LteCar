using Microsoft.AspNetCore.SignalR;

namespace LteCar.Server.Hubs;

public class CarControlHub : Hub<ICarControlClient>, ICarControlServer
{
    public void UpdateChannel(string conrtolId, string channelId, decimal value)    
    {
        
    }
}

public interface ICarControlServer
{
}

public interface ICarControlClient
{
}