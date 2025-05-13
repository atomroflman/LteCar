using LteCar.Onboard;

namespace LteCar.Shared;

public class CarConfiguration : IConfigurationModel
{
    public JanusConfiguration JanusConfiguration { get; set; }
    public VideoSettings VideoSettings { get; set; }
    public bool RequiresChannelMapUpdate { get; set; }
}