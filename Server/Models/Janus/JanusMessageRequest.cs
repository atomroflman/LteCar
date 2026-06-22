using System.Text.Json.Serialization;

public class JanusMessageRequest<TBody> : JanusRequestBase
{
    [JsonPropertyName("body")]
    public TBody Body { get; set; }
}

public class JanusMessageBody
{
    [JsonPropertyName("request")]
    public string Request { get; set; }
}

public class JanusMountpointActionBody
{
    [JsonPropertyName("request")]
    public string Request { get; set; }
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public uint? Id { get; set; }
}