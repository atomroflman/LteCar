using LteCar.Shared.Channels;
using LteCar.Shared.Video;

namespace LteCar.Shared;

public class CarConfiguration : IConfigurationModel
{
    public int ServerAssignedCarId { get; set; }
    public JanusConfiguration? JanusConfiguration { get; set; }
    public VideoSettings? VideoSettings { get; set; }

    /// <summary>
    /// Legacy flag: server wants the client to upload its channel map via SyncChannelMap.
    /// </summary>
    public bool RequiresChannelMapUpdate { get; set; }

    /// <summary>
    /// Server has no channel map for this car and requests a one-time upload from the client.
    /// </summary>
    public bool RequiresChannelMapUpload { get; set; }

    /// <summary>
    /// Server-pushed channel map (SPOT). Only set when the server is the source of truth.
    /// </summary>
    public ChannelMap? ChannelMap { get; set; }

    /// <summary>
    /// Hash of the server-pushed channel map. The client should compare this with its local hash.
    /// </summary>
    public string ChannelMapHash { get; set; } = string.Empty;
}