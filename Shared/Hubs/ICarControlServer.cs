using LteCar.Shared;
using LteCar.Shared.FileTransfer;

namespace LteCar.Server.Hubs;

public interface ICarControlServer
{
    Task<string?> AquireCarControl(int carId, SshAuthenticationRequest authRequest);
    Task ReleaseCarControl(int carId, string sessionId);
    Task UpdateChannel(int carId, string sessionId, int channelId, decimal value);
    Task RegisterForControl(int carId);
    Task<string?> GetChallenge(int carId);
    Task ExecuteBashCommand(int carId, string sessionId, string command);
    Task SendBashOutput(int carId, string output, bool isError);

    Task<FileUploadApproval?> RequestFileUpload(int carId, string sessionId, string filePath);
    Task<ListFilesResponse?> ListFilesOnDevice(int carId, string sessionId, string path);
    Task<bool> DeleteFileOnDevice(int carId, string sessionId, string filePath);
}