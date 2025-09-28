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
    public string ControlType { get; set; } = string.Empty;
    
    [JsonPropertyName("testDisabled")]
    public bool TestDisabled { get; set; }
}

public class TelemetryChannelMapItem : ChannelMapItem
{
    [JsonPropertyName("readIntervalTicks")]
    public int ReadIntervalTicks { get; set; }
    
    [JsonPropertyName("telemetryType")]
    public string TelemetryType { get; set; } = string.Empty;
}

public class VideoStreamMapItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = "camera";
    
    [JsonPropertyName("location")]
    public string? Location { get; set; }
    
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }
    
    [JsonPropertyName("priority")]
    public int Priority { get; set; } = 1;
}



