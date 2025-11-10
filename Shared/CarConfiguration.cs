using LteCar.Shared.Video;

namespace LteCar.Shared;

public class CarConfiguration : IConfigurationModel
{
    public int ServerAssignedCarId { get; set; }
    public JanusConfiguration? JanusConfiguration { get; set; }
    public VideoSettings? VideoSettings { get; set; }
    public bool RequiresChannelMapUpdate { get; set; }
}