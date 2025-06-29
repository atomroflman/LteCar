using System.Text.Json.Serialization;

namespace LteCar.Shared.Channels;

public class ChannelMap
{
    [JsonPropertyName("pinManagers")]
    public Dictionary<string, PinManagerMapItem> PinManagers { get; set; } = new();
    [JsonPropertyName("controlChannels")]
    public Dictionary<string, ControlChannelMapItem> ControlChannels { get; set; } = new();
    [JsonPropertyName("telemetryChannels")]
    public Dictionary<string, TelemetryChannelMapItem> TelemetryChannels { get; set; } = new();
}

public class PinManagerMapItem
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    [JsonPropertyName("options")]
    public Dictionary<string, object> Options { get; set; } = new();
}