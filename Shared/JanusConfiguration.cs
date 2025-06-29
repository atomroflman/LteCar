namespace LteCar.Shared;

public class JanusConfiguration : IConfigurationModel
{
    public string JanusServerHost { get; set; } = string.Empty;
    public int JanusUdpPort { get; set; } 
}