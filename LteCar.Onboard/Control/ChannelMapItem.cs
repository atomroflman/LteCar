namespace LteCar.Onboard.Control;

public class ChannelMapItem
{
    public int PhysicalGpio { get; set; }
    public string ControlType { get; set; }
    public Dictionary<string, object> Options { get; set; } = new();
}

