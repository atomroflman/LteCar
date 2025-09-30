using System.Text.Json.Serialization;
using LteCar.Onboard;

namespace LteCar.Shared.Channels;

public class VideoStreamMapItem
{
    [JsonPropertyName("streamId")]
    public string StreamId { get; set; } = string.Empty;
    
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;
    
    [JsonPropertyName("videoSettings")]
    public VideoSettings VideoSettings { get; set; } = VideoSettings.Default;
    
    [JsonPropertyName("protocol")]
    public string Protocol { get; set; } = "UDP";
    
    [JsonPropertyName("purpose")]
    public string Purpose { get; set; } = "main_camera";
    
    [JsonPropertyName("codec")]
    public string Codec { get; set; } = "vp8";
    
    [JsonPropertyName("options")]
    public Dictionary<string, object> Options { get; set; } = new();
}