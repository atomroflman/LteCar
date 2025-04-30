using System.Text.Json.Serialization;

namespace LteCar.Onboard.Control;

public class ChannelMapItem
{
    [JsonPropertyName("physicalGpio")]
    public int PhysicalGpio { get; set; }
    [JsonPropertyName("controlType")]
    public string ControlType { get; set; }
    [JsonPropertyName("ignoreTest")]
    public bool IgnoreTest {get;set;}
    [JsonPropertyName("options")]
    public Dictionary<string, object> Options { get; set; } = new();
}

