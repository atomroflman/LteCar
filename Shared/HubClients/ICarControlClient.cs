namespace LteCar.Shared.HubClients;

public interface ICarControlClient
{
    Task<string> AquireCarControl(string carSecret);
    Task ReleaseCarControl(string sessionId);
    Task UpdateChannel(string sessionId, string channelId, decimal value);
}