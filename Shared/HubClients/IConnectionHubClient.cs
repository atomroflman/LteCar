using LteCar.Shared.Channels;
using LteCar.Shared.FileTransfer;

namespace LteCar.Shared;

// ponytail: the Onboard registers one client interface on its single hub
// connection (CarConnectionHub). All server→onboard calls (control, telemetry
// config pushes, file transfer, version sync, channel CRUD) route through here.
public interface IConnectionHubClient
{
    // Connection-lifecycle broadcasts
    Task CarStateUpdated(CarStateModel state);
    Task SendBashOutput(int carId, string output, bool isError);

    // Control session
    Task<string> AquireCarControl(SshAuthenticationRequest authRequest);
    Task ReleaseCarControl(string sessionId);
    Task UpdateChannel(string sessionId, string channelId, decimal value);
    Task<string?> GetChallenge();
    Task ExecuteBashCommand(string sessionId, string command);

    // File transfer
    Task<bool> ApproveFileUpload(string sessionId, string filePath);
    Task<ListFilesResponse> ListFiles(string sessionId, string path);
    Task<bool> DeleteFile(string sessionId, string filePath);
    Task FileReady(FileReadyNotification notification);

    Task<long> Ping();

    // Bidirectional channel sync (web UI edits push live to the Onboard)
    Task UpsertControlChannel(string dictKey, ControlChannelMapItem item);
    Task DeleteControlChannel(string dictKey);
    Task UpsertTelemetryChannel(string dictKey, TelemetryChannelMapItem item);
    Task DeleteTelemetryChannel(string dictKey);
    Task UpsertVideoStream(string dictKey, VideoStreamMapItem item);
    Task DeleteVideoStream(string dictKey);
}