using LteCar.Shared;
using LteCar.Shared.Channels;
using LteCar.Shared.FileTransfer;

public interface IConnectionHubServer
{
    Task<CarConfiguration> OpenCarConnection(string carIdentityKey, string channelMapHash);
    Task ReportOnboardVersion(string branch, string? commit);
    Task UpdateChannelMap(int carId, ChannelMap channelMap);
    Task<ChannelMapSyncResponse> SyncChannelMap(ChannelMapSyncRequest request);
    Task ReportFileTransferStatus(FileTransferStatusUpdate update);
    Task<CarStateModel[]> UiClientConnected();
    Task SendBashOutput(int carId, string output, bool isError);

    Task RegisterForControl(int carId);
    Task<string?> AquireCarControl(int carId, SshAuthenticationRequest authRequest);
    Task ReleaseCarControl(int carId, string sessionId);
    Task UpdateChannel(int carId, string sessionId, int channelId, decimal value);
    Task<string?> GetChallenge(int carId);
    Task<FileUploadApproval?> RequestFileUpload(int carId, string sessionId, string filePath);
    Task<ListFilesResponse?> ListFilesOnDevice(int carId, string sessionId, string path);
    Task<bool> DeleteFileOnDevice(int carId, string sessionId, string filePath);
    Task<PingCarResult?> PingCar(int carId);
}