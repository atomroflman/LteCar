using System.Text.Json.Serialization;

namespace LteCar.Shared.Channels;

public class ChannelMapItem
{
    [JsonPropertyName("pinManager")]
    public string PinManager { get; set; } = "default";
    [JsonPropertyName("address")]
    public int? Address { get; set; }
    [JsonPropertyName("options")]
    public Dictionary<string, object> Options { get; set; } = new();
}

public class ControlChannelMapItem : ChannelMapItem
{
    [JsonPropertyName("controlType")]
    public string ControlType { get; set; }
    [JsonPropertyName("testDisabled")]
    public bool TestDisabled { get; set; }
}

public class TelemetryChannelMapItem : ChannelMapItem
{
    [JsonPropertyName("readIntervalTicks")]
    public int ReadIntervalTicks { get; set; }
    [JsonPropertyName("telemetryType")]
    public string TelemetryType { get; set; }
}

