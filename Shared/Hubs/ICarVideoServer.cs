using LteCar.Shared.Video;

namespace LteCar.Server.Hubs;

public interface ICarVideoServer
{
    Task ConnectCar(string carIdentityKey);
    Task<Dictionary<int, VideoSettingsModel>> GetVideoStreamsForCar(int carId);
    Task StartVideoStream(int streamId);
    Task ChangeVideoStreamSettings(int streamId, VideoSettingsModel settings);
    Task StopVideoStream(int streamId);
}
