using LteCar.Shared.Channels;
using LteCar.Shared.FileTransfer;

namespace LteCar.Shared.HubClients;

public interface ICarControlClient
{
    Task<string> AquireCarControl(SshAuthenticationRequest authRequest);
    Task ReleaseCarControl(string sessionId);
    Task UpdateChannel(string sessionId, string channelId, decimal value);
    Task<string?> GetChallenge();
    Task ExecuteBashCommand(string sessionId, string command);

    Task<bool> ApproveFileUpload(string sessionId, string filePath);
    Task<ListFilesResponse> ListFiles(string sessionId, string path);
    Task<bool> DeleteFile(string sessionId, string filePath);
    Task FileReady(FileReadyNotification notification);
    Task<long> Ping();

    Task UpsertControlChannel(string dictKey, ControlChannelMapItem item);
    Task DeleteControlChannel(string dictKey);
    Task UpsertTelemetryChannel(string dictKey, TelemetryChannelMapItem item);
    Task DeleteTelemetryChannel(string dictKey);
    Task UpsertVideoStream(string dictKey, VideoStreamMapItem item);
    Task DeleteVideoStream(string dictKey);
}