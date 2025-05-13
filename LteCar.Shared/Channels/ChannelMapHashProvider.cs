using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LteCar.Shared.Channels;

public static class ChannelMapHashProvider
{
    public static string GenerateHash(ChannelMap channelMap)
    {
        // Serialize the ChannelMap to JSON
        var json = JsonSerializer.Serialize(channelMap, new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        // Compute the hash using SHA256
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));

        // Convert the hash to a hexadecimal string
        var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

        return hashString;
    }
}