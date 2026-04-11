using LteCar.Shared.HubClients;
using LteCar.Server.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LteCar.Server.Hubs;

public class CarUiHub : Hub<ICarUiClient>, ICarUiServer
{
    public CarUiHub(CarConnectionStore connectionStore, LteCarContext dbContext)
    {
        ConnectionStore = connectionStore;
        DataContext = dbContext;
    }

    public CarConnectionStore ConnectionStore { get; }
    public LteCarContext DataContext { get; }

    public CarStateModel[] UiClientConnected()
    {
        var cars = DataContext.Cars
            .AsNoTracking()
            .ToList()
            .Select(car => {
                var hasConnectionInfo = ConnectionStore.TryGetValue(car.Id.ToString(), out var connectionInfo);
                return new CarStateModel
                {
                Id = car.Id.ToString(),
                IsConnected = hasConnectionInfo,
                DriverId = hasConnectionInfo ? connectionInfo?.DriverId : null,
                DriverName = hasConnectionInfo ? connectionInfo?.DriverName : null
                };
            })
            .ToArray();

        return cars;
    }
}

public interface ICarUiClient
{
    Task CarStateUpdated(CarStateModel stateModel);
    Task SendBashOutput(int carId, string output, bool isError);
}

public interface ICarUiServer
{
    CarStateModel[] UiClientConnected();
}

public class CarStateModel
{
    public string Id { get; internal set; } = string.Empty;
    public bool IsConnected { get; internal set; }
    public string? DriverId { get; internal set; }
    public string? DriverName { get; internal set; }
}