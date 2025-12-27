using LteCar.Shared;
using LteCar.Shared.Channels;

public interface ICarConnectionServer
{
    Task<CarConfiguration> OpenCarConnection(string carIdentityKey, string channelMapHash);
    Task UpdateChannelMap(int carId, ChannelMap channelMap);
    Task<ChannelMapSyncResponse> SyncChannelMap(ChannelMapSyncRequest request);
}