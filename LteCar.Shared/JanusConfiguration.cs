namespace LteCar.Shared;

public class JanusConfiguration : IConfigurationModelS
{
    public string JanusServerHost { get; set; } = string.Empty;
    public int JanusUdpPort { get; set; } 
}