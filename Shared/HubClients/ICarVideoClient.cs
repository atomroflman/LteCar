using LteCar.Shared.Video;

namespace LteCar.Server.Hubs;

public interface ICarVideoClient
{
    /// <summary>
    /// Used to start a video stream on the car with specified settings or change the settings of a running Stream.
    /// </summary>
    Task StartVideoStream(string streamId, VideoSettings settings);
    /// <summary>
    /// Used to stop a running video stream on the car.
    /// </summary>
    Task StopVideoStream(string streamId);
}
