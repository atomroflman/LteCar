using System.Text.Json.Serialization;

namespace LteCar.Shared.Channels;

public class ChannelMap
{
    [JsonPropertyName("controlChannels")]
    public Dictionary<string, ControlChannelMapItem> ControlChannels { get; set; } = new();
    [JsonPropertyName("telemetryChannels")]
    public Dictionary<string, TelemetryChannelMapItem> TelemetryChannels { get; set; } = new();
}

