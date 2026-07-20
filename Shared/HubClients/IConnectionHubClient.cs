namespace LteCar.Shared;

public interface IConnectionHubClient
{
    Task CarStateUpdated(CarStateModel state);
    Task SendBashOutput(int carId, string output, bool isError);
}
