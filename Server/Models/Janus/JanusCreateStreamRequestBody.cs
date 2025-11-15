using System.Text.Json.Serialization;

public class JanusCreateStreamRequestBody
{
    [JsonPropertyName("request")]
    public string Request { get; set; } = "create";
    [JsonPropertyName("type")]
    public string Type { get; set; } = "rtp";
    [JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonPropertyName("description")]
    public string Description { get; set; }
    [JsonPropertyName("audio")]
    public bool Audio { get; set; } = false;
    [JsonPropertyName("video")]
    public bool Video { get; set; } = true;
    [JsonPropertyName("rtp_port")]
    public int RtpPort { get; set; }
    [JsonPropertyName("rtp_pt")]
    public int RtpPt { get; set; } = 96;
}