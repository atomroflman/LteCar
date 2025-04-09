using LteCar.Shared;

public interface ICarConnectionServer
{
    Task<CarConfiguration> OpenCarConnection(string carId);
}