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

    [Key(6)]
    [JsonPropertyName("cameraDevice")]
    public string? CameraDevice { get; set; }

    [Key(7)]
    [JsonPropertyName("rpiCamId")]
    public int? RpiCamId { get; set; }

    [Key(8)]
    [JsonPropertyName("width")]
    public int? Width { get; set; }

    [Key(9)]
    [JsonPropertyName("height")]
    public int? Height { get; set; }

    [Key(10)]
    [JsonPropertyName("framerate")]
    public int? Framerate { get; set; }

    [Key(11)]
    [JsonPropertyName("bitrate")]
    public int? Bitrate { get; set; }
}
