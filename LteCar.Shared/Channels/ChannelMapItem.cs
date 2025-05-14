using System.Text.Json.Serialization;

namespace LteCar.Shared.Channels;

public class ChannelMapItem
{
    [JsonPropertyName("physicalGpio")]
    public int? PhysicalGpio { get; set; }
    [JsonPropertyName("options")]
    public Dictionary<string, object> Options { get; set; } = new();
}

public class ControlChannelMapItem : ChannelMapItem
{
    [JsonPropertyName("controlType")]
    public string ControlType { get; set; }
    [JsonPropertyName("ignoreTest")]
    public bool IgnoreTest { get; set; }
}

public class TelemetryChannelMapItem : ChannelMapItem
{
    [JsonPropertyName("readIntervalTicks")]
    public int ReadIntervalTicks { get; set; }
    [JsonPropertyName("telemetryType")]
    public string TelemetryType { get; set; }
}

