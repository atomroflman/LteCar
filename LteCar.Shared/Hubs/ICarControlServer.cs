namespace LteCar.Server.Hubs;

public interface ICarControlServer
{
    Task<string?> AquireCarControl(string carId, string carSecret);
    Task ReleaseCarControl(string carId, string sessionId);
    Task UpdateChannel(string carId, string sessionId, string channelId, decimal value);
    Task RegisterForControl(string carId);
}