using LteCar.Shared;

namespace LteCar.Server;

public class CarConnectionStore : Dictionary<string, CarConnectionInfo>
{
}

public class CarConnectionInfo
{
    public CarConfiguration CarConfiguration { get; set; } = new CarConfiguration();
    public string? DriverId { get; set; }
    public string? DriverName { get; set; }
}