using LteCar.Shared.HubClients;
using Microsoft.AspNetCore.SignalR;

namespace LteCar.Server.Hubs;

public class CarUiHub : Hub<ICarUiClient>, ICarUiServer
{
    public CarUiHub(CarConnectionStore connectionStore)
    {
        ConnectionStore = connectionStore;
    }

    public CarConnectionStore ConnectionStore { get; }

    public CarStateModel[] UiClientConnected()
    {
        return ConnectionStore.Select(e => new CarStateModel() {
            Id = e.Key,
            DriverId = e.Value?.DriverId,
            DriverName = e.Value?.DriverName
        }).ToArray();
    }
}

public interface ICarUiClient
{
    Task CarStateUpdated(CarStateModel stateModel);
}

public interface ICarUiServer
{
    CarStateModel[] UiClientConnected();
}

public class CarStateModel
{
    public string Id { get; internal set; }
    public string? DriverId { get; internal set; }
    public string? DriverName { get; internal set; }
}