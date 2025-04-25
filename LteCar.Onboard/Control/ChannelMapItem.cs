using System.Text.Json.Serialization;

namespace LteCar.Onboard.Control;

public class ChannelMapItem
{
    [JsonPropertyName("physicalGpio")]
    public int PhysicalGpio { get; set; }
    [JsonPropertyName("controlType")]
    public string ControlType { get; set; }
    public Dictionary<string, object> Options { get; set; } = new();
}

