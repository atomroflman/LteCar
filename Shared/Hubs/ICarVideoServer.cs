using LteCar.Shared.Video;

namespace LteCar.Server.Hubs;

public interface ICarVideoServer
{
    Task ConnectCar(string carIdentityKey);
    Task<IReadOnlyList<VideoStreamInfoModel>> GetVideoStreamsForCar(int carId);
    Task ActivateStream(int streamId);
    Task DeactivateStream(int streamId);
    Task StartVideoStream(int streamId);
    Task ChangeVideoStreamSettings(int streamId, VideoSettingsModel settings);
    Task StopVideoStream(int streamId);
    Task SetVideoStreamEnabled(int carId, int streamId, bool enabled);
}
