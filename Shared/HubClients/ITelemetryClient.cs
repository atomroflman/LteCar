namespace LteCar.Shared.HubClients;

public interface ITelemetryClient
{
    Task<IEnumerable<string>> GetAvailableTelemetryChannels();
    Task SubscribeToTelemetryChannel(string channelName);
    Task UnsubscribeFromTelemetryChannel(string channelName);
    Task UpdateTelemetry(string channelName, string value);
}
