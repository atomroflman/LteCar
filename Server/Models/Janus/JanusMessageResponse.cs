using System.Text.Json.Serialization;

public class JanusMessageResponse<TBody> : JanusResponseBase
{
    [JsonPropertyName("body")]
    public TBody Body { get; set; }
}   

public class JanusPluginMessageResponse<TBody> : JanusResponseBase
{
    [JsonPropertyName("plugindata")]
    public TBody Body { get; set; }
}   

public class JanusPluginMessageListResponseBody
{
    [JsonPropertyName("plugin")]
    public string Plugin { get; set; }
    [JsonPropertyName("list")]
    public List<JanusStreamInfo> Streams { get; set; }
}

public class JanusStreamInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonPropertyName("description")]
    public string Description { get; set; }
    [JsonPropertyName("type")]
    public string Type { get; set; }
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }
    [JsonPropertyName("media")]
    public List<JanusStreamMediaInfo> Media { get; set; }
}

public class JanusStreamMediaInfo
{
    [JsonPropertyName("type")]
    public string Type { get; set; }
    [JsonPropertyName("label")]
    public string Label { get; set; }
    [JsonPropertyName("age_ms")]
    public int AgeMs { get; set; }
    public override string ToString()
    {
        return $"{Type} ({Label}), age {TimeSpan.FromMilliseconds(AgeMs)}";
    }
}