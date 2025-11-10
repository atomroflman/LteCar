using System.Text.Json.Serialization;
using MessagePack;

namespace LteCar.Shared.Channels;

/// <summary>
/// ConfigMap item for a video stream.
/// </summary>
[MessagePackObject]
public class VideoStreamMapItem
{
    [Key(0)]
    [JsonPropertyName("name")] 
    public string? Name { get; set; }
    [Key(1)]
    [JsonPropertyName("location")] 
    public string? Location { get; set; }
    [Key(2)]
    [JsonPropertyName("type")] 
    public string? Type { get; set; }
    [Key(3)]
    [JsonPropertyName("streamId")] 
    public required string StreamId { get; set; }
    [Key(4)]
    [JsonPropertyName("enabled")] 
    public bool Enabled { get; set; } = true;
    [Key(5)]
    [JsonPropertyName("serverId")] 
    public int? ServerId { get; set; }
}