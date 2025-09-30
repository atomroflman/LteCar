using System.Text.Json.Serialization;
using MessagePack;
using LteCar.Onboard;

namespace LteCar.Shared.Channels;

[MessagePackObject]
public class VideoStreamMapItem
{
    [Key(0)][JsonPropertyName("name")] public string? Name { get; set; }
    [Key(1)][JsonPropertyName("location")] public string? Location { get; set; }
    [Key(2)][JsonPropertyName("type")] public string? Type { get; set; }

    [Key(3)][JsonPropertyName("streamId")] public string StreamId { get; set; } = string.Empty;
    
    [Key(4)][JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    [Key(5)][JsonPropertyName("serverId")] public int? ServerId { get; set; }
}