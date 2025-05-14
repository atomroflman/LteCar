using LteCar.Shared.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace LteCar.Server.Hubs;

public class TelemetryHub : Hub<ITelemetryClient>, ITelemetryServer
{
    public Task UpdateTelemetry(string carId, string valueName, string value)
    {
        return Clients.Groups(carId).UpdateTelemetry(valueName, value);
    }
}