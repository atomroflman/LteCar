using LteCar.Shared;
using LteCar.Shared.Channels;

public interface ICarConnectionServer
{
    Task<CarConfiguration> OpenCarConnection(string carId, string channelMapHash);
    Task UpdateChannelMap(string carId, ChannelMap channelMap);
}