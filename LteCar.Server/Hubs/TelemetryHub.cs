using Microsoft.AspNetCore.SignalR;

namespace LteCar.Server.Hubs;

public class TelemetryHub : Hub<ITelemetryClient>, ITelemetryServer
{
    public Task UpdateTelemetry(string carId, string valueName, string value)
    {
        return Clients.Client(carId).UpdateTelemetry(valueName, value);
    }
}

public interface ITelemetryClient
{
    Task UpdateTelemetry(string valueName, string value);
}

public interface ITelemetryServer
{
    Task UpdateTelemetry(string carId, string valueName, string value);
}