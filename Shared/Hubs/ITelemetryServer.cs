namespace LteCar.Shared.Hubs;

public interface ITelemetryServer
{
    Task UpdateTelemetry(string carId, string valueName, string value);
    Task SubscribeToCarTelemetry(string carId);
    Task UnsubscribeFromCarTelemetry(string carId);
}