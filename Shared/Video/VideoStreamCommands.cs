using MessagePack;
using System.Text.Json.Serialization;
using LteCar.Onboard;

namespace LteCar.Shared.Video;

[MessagePackObject]
public class VideoStreamStartCommand
{
    [Key(0)]
    [JsonPropertyName("carId")] 
    public string CarId { get; set; } = string.Empty;
    [Key(1)]
    [JsonPropertyName("streamId")] 
    public string StreamId { get; set; } = "main";
    [Key(2)]
    [JsonPropertyName("settings")] 
    public VideoSettings? Settings { get; set; } = null; // null -> server default
}

[MessagePackObject]
public class VideoStreamStartedEvent
{
    [Key(0)]
    [JsonPropertyName("carId")] 
    public string CarId { get; set; } = string.Empty;
    [Key(1)]
    [JsonPropertyName("streamId")] 
    public string StreamId { get; set; } = string.Empty;
    [Key(2)]
    [JsonPropertyName("settings")] 
    public VideoSettings? Settings { get; set; }
    [Key(3)]
    [JsonPropertyName("timestampUtc")] 
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}