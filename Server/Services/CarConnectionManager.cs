using System.Collections.Concurrent;
using LteCar.Shared;

namespace LteCar.Server;

public class CarConnectionStore : ConcurrentDictionary<string, CarConnectionInfo>
{
}

public class CarConnectionInfo
{
    public CarConfiguration CarConfiguration { get; set; } = new CarConfiguration();
    public string? DriverId { get; set; }
    public string? DriverName { get; set; }
}