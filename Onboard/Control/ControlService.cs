using CSharpVitamins;
using LteCar.Onboard.Telemetry;
using LteCar.Shared;
using LteCar.Shared.FileTransfer;
using LteCar.Server.Hubs;
using LteCar.Shared.HubClients;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TypedSignalR.Client;
using LteCar.Onboard;
using System.Diagnostics;

namespace LteCar.Onboard.Control;

public class ControlService : ICarControlClient, IHubConnectionObserver
{
    public IServiceProvider ServiceProvider { get; }
    public ILogger<ControlService> Logger { get; }
    public TelemetryService TelemetryService { get; }
    public ControlExecutionService Control { get; }
    public IConfiguration Configuration { get; }
    public ServerConnectionService ServerConnectionService { get; }
    public SshKeyService SshKeyService { get; }
    public ServerCarConfigurationService CarConfigurationService { get; }
    public Process BashProcess { get; } = new Process();

    private HubConnection _connection;
    private string? _sessionId;
    private DateTime _lastControlUpdate = DateTime.Now;
    private ICarControlServer _server;

    public ControlService(ILogger<ControlService> logger, TelemetryService telemetryService, ControlExecutionService control, IServiceProvider serviceProvider, IConfiguration configuration, ServerConnectionService serverConnectionService, SshKeyService sshKeyService, ServerCarConfigurationService carConfigurationService)
    {
        Logger = logger;
        TelemetryService = telemetryService;
        Control = control;
        ServiceProvider = serviceProvider;
        Configuration = configuration;
        ServerConnectionService = serverConnectionService;
        SshKeyService = sshKeyService;
        CarConfigurationService = carConfigurationService;
        BashProcess.StartInfo.FileName = "/bin/bash";
        BashProcess.StartInfo.RedirectStandardInput = true;
        BashProcess.StartInfo.RedirectStandardOutput = true;
        BashProcess.StartInfo.RedirectStandardError = true;
        BashProcess.StartInfo.UseShellExecute = false;
        BashProcess.StartInfo.CreateNoWindow = true;
        BashProcess.OutputDataReceived += (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                Logger.LogInformation($"[Bash Output] {args.Data}");
                if (_connection != null && _connection.State == HubConnectionState.Connected && this.CarConfigurationService.ServerAssignedCarId.HasValue)
                {
                    _server?.SendBashOutput(CarConfigurationService.ServerAssignedCarId!.Value, args.Data, false);
                }
            }
        };
        BashProcess.ErrorDataReceived += (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                Logger.LogError($"[Bash Error] {args.Data}");
                if (_connection != null && _connection.State == HubConnectionState.Connected && this.CarConfigurationService.ServerAssignedCarId.HasValue)
                {
                    _server?.SendBashOutput(CarConfigurationService.ServerAssignedCarId!.Value, args.Data, true);
                }
            }
        };
        BashProcess.Start();
    }

    public void Initialize()
    {
        Control.Initialize();
        Control.ReleaseControl();
    }

    public async Task ExecuteBashCommand(string sessionId, string command) 
    {
        if (_sessionId != sessionId)
            return;
        Logger.LogInformation($"Executing bash command from client: {command}");
        await BashProcess.StandardInput.WriteLineAsync(command);
        await BashProcess.StandardInput.FlushAsync();
    }

    public async Task ConnectToServer()
    {
        _connection = ServerConnectionService.ConnectToHub(HubPaths.CarControlHub);
        _connection.Register<ICarControlClient>(this);
        await _connection.StartAsync();
        _server = _connection.CreateHubProxy<ICarControlServer>();
        var carId = CarConfigurationService.ServerAssignedCarId;
        if (!carId.HasValue)
        {
            Logger.LogError("Cannot connect to control server: ServerAssignedCarId not available");
            return;
        }
        await _server.RegisterForControl(carId.Value);
        Logger.LogInformation($"Connected to control server with CarId: {carId}");
        await TelemetryService.UpdateTelemetry("Control Server", "Connected");
    }

    public async Task TestControlsAsync() {
        await Control.RunControlTestsAsync();
    }
    
    public async Task<string?> AquireCarControl(SshAuthenticationRequest authRequest)
    {
        if (_sessionId != null && _lastControlUpdate.AddSeconds(30) > DateTime.Now) {
            Logger.LogError("Cannot aquire control: Already connected to driver session.");
            return null;
        }

        // Verify SSH signature
        if (SshKeyService.VerifySignature(authRequest.Challenge, authRequest.Signature))
        {
            // Generate a new session ID using ShortGuid (22 chars instead of 36)
            var newSessionId = ShortGuid.NewGuid().ToString();
            _sessionId = newSessionId;
            Logger.LogInformation($"Acquired control for car using SSH key. SessionID: {_sessionId}.");
            await TelemetryService.UpdateTelemetry("Control Session", "Connected (SSH)");
            return newSessionId; // Return the newly generated session ID
        }
        else
        {
            Logger.LogError("Cannot acquire control: Invalid SSH signature.");
            return null;
        }
    }

    public Task<string?> GetChallenge()
    {
        var challenge = SshKeyService.GenerateChallenge();
        return Task.FromResult(challenge);
    }

    public async Task ReleaseCarControl(string sessionId)
    {
        if (_sessionId != sessionId)
            return;
        Logger.LogInformation($"Release control for session {sessionId}");
        Control.ReleaseControl();
        await TelemetryService.UpdateTelemetry("Control Session", "Ended");
        _sessionId = null;
    }

    public Task UpdateChannel(string sessionId, string channelId, decimal value)
    {
        if (_sessionId != sessionId)
            return Task.CompletedTask;
        Logger.LogDebug($"Update channel {channelId} to {value}");
        Control.SetControl(channelId, value);
        _lastControlUpdate = DateTime.Now;
        return Task.CompletedTask;
    }

    public async Task OnClosed(Exception? exception)
    {
        Control.ReleaseControl();
        await TelemetryService.UpdateTelemetry("Control Server", "Disconnected");
    }

    public async Task OnReconnected(string? connectionId)
    {
        var carId = CarConfigurationService.ServerAssignedCarId;
        if (carId.HasValue)
        {
            await _server.RegisterForControl(carId.Value);
        }
        await TelemetryService.UpdateTelemetry("Control Server", "Connected");
    }

    public async Task OnReconnecting(Exception? exception)
    {
        Control.ReleaseControl();
        await TelemetryService.UpdateTelemetry("Control Server", "Disconnected");
    }

    private string FileTransferBasePath =>
        Configuration.GetValue<string>("FileTransfer:BasePath") ?? "/var/data/ltecar/files";

    public Task<bool> ApproveFileUpload(string sessionId, string filePath)
    {
        if (_sessionId != sessionId)
            return Task.FromResult(false);

        Logger.LogInformation("File upload approved: {FilePath}", filePath);
        return Task.FromResult(true);
    }

    public Task<ListFilesResponse> ListFiles(string sessionId, string path)
    {
        var response = new ListFilesResponse { Path = path };

        if (_sessionId != sessionId)
        {
            response.Error = "Invalid session";
            return Task.FromResult(response);
        }

        var fullPath = Path.GetFullPath(Path.Combine(FileTransferBasePath, path.TrimStart('/')));
        if (!fullPath.StartsWith(FileTransferBasePath))
        {
            response.Error = "Access denied";
            return Task.FromResult(response);
        }

        if (!Directory.Exists(fullPath))
        {
            response.Error = "Directory not found";
            return Task.FromResult(response);
        }

        foreach (var dir in Directory.GetDirectories(fullPath))
        {
            var info = new DirectoryInfo(dir);
            response.Entries.Add(new FileListEntry
            {
                Name = info.Name,
                FullPath = Path.GetRelativePath(FileTransferBasePath, dir),
                IsDirectory = true,
                LastModifiedUtc = info.LastWriteTimeUtc
            });
        }

        foreach (var file in Directory.GetFiles(fullPath))
        {
            var info = new FileInfo(file);
            response.Entries.Add(new FileListEntry
            {
                Name = info.Name,
                FullPath = Path.GetRelativePath(FileTransferBasePath, file),
                IsDirectory = false,
                SizeBytes = info.Length,
                LastModifiedUtc = info.LastWriteTimeUtc
            });
        }

        return Task.FromResult(response);
    }

    public Task<bool> DeleteFile(string sessionId, string filePath)
    {
        if (_sessionId != sessionId)
            return Task.FromResult(false);

        var fullPath = Path.GetFullPath(Path.Combine(FileTransferBasePath, filePath.TrimStart('/')));
        if (!fullPath.StartsWith(FileTransferBasePath))
            return Task.FromResult(false);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            Logger.LogInformation("File deleted: {FilePath}", fullPath);
            return Task.FromResult(true);
        }

        if (Directory.Exists(fullPath))
        {
            Directory.Delete(fullPath, recursive: true);
            Logger.LogInformation("Directory deleted: {FilePath}", fullPath);
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public async Task FileReady(FileReadyNotification notification)
    {
        Logger.LogInformation("FileReady: token {Token}, file '{FileName}', {Size} bytes",
            notification.Token, notification.FileName, notification.FileSizeBytes);
        // TODO: download from /api/filetransfer/{token}/download, save to FileTransferBasePath, report status
        await Task.CompletedTask;
    }
}