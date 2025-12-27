namespace LteCar.Shared.HubClients;

public interface ICarControlClient
{
    Task<string> AquireCarControl(SshAuthenticationRequest authRequest);
    Task ReleaseCarControl(string sessionId);
    Task UpdateChannel(string sessionId, string channelId, decimal value);
    Task<string?> GetChallenge();
    Task ExecuteBashCommand(string sessionId, string command);
}