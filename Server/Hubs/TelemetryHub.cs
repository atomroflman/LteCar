using LteCar.Server.Data;
using LteCar.Shared.HubClients;
using LteCar.Shared.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LteCar.Server.Hubs;

public class TelemetryHub : Hub<ITelemetryClient>, ITelemetryServer
{
    public Task UpdateTelemetry(string carId, string valueName, string value)
    {
        return Clients.Groups(carId).UpdateTelemetry(valueName, value);
    }

    public Task SubscribeToCarTelemetry(string carId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, carId);
    }

    public Task UnsubscribeFromCarTelemetry(string carId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, carId);
    }

    public Task RegisterAsOnboard(string carId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, $"onboard-{carId}");
    }

    public async Task SubscribeToChannel(string carId, string channelName)
    {
        await Clients.Group($"onboard-{carId}").SubscribeToTelemetryChannel(channelName);
        await PersistSubscription(carId, channelName, add: true);
    }

    public async Task UnsubscribeFromChannel(string carId, string channelName)
    {
        await Clients.Group($"onboard-{carId}").UnsubscribeFromTelemetryChannel(channelName);
        await PersistSubscription(carId, channelName, add: false);
    }

    private async Task PersistSubscription(string carId, string channelName, bool add)
    {
        var db = Context.GetHttpContext()?.RequestServices.GetService<LteCarContext>();
        if (db == null) return;

        if (!int.TryParse(carId, out var carIdInt)) return;

        var user = await HubUserHelper.GetUserAsync(Context.GetHttpContext()!, db);
        if (user == null) return;

        var setup = await db.UserSetups
            .FirstOrDefaultAsync(s => s.UserId == user.Id && s.CarId == carIdInt);
        if (setup == null) return;

        var telemetry = await db.CarTelemetry
            .FirstOrDefaultAsync(t => t.CarId == carIdInt && t.ChannelName == channelName);
        if (telemetry == null) return;

        var existing = await db.UserSetupTelemetries
            .FirstOrDefaultAsync(t => t.UserSetupId == setup.Id && t.CarTelemetryId == telemetry.Id);

        if (add && existing == null)
        {
            db.UserSetupTelemetries.Add(new UserSetupTelemetry
            {
                UserSetupId = setup.Id,
                CarTelemetryId = telemetry.Id,
            });
            await db.SaveChangesAsync();
        }
        else if (!add && existing != null)
        {
            db.UserSetupTelemetries.Remove(existing);
            await db.SaveChangesAsync();
        }
    }
}