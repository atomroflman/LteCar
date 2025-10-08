using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using Spectre.Console;
using LteCar.Onboard;
using LteCar.Onboard.Control;
using LteCar.Onboard.Control.ControlTypes;
using LteCar.Onboard.Hardware;
using LteCar.Onboard.Setup;
using LteCar.Onboard.Telemetry;
using LteCar.Onboard.Video;
using LteCar.Shared.Channels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.FileProviders;


// Setup-Modus prüfen
if (args.Length > 0 && args[0].Equals("setup", StringComparison.OrdinalIgnoreCase))
{
    LteCar.Onboard.Setup.VehicleSetupTool.Run();
    return;
}

var carIdentityKey = Guid.NewGuid().ToString();
var startupTime = DateTime.Now;
if (File.Exists("carIdentityKey.txt"))
{
    carIdentityKey = File.ReadAllText("carIdentityKey.txt");
}
else
{
    Console.WriteLine($"New Car Identity Key created: {carIdentityKey}");
    File.WriteAllText("carIdentityKey.txt", carIdentityKey);
}

// Generate SSH key pair only if no public key exists
var sshKeyPath = "ssh_key";
var sshPublicKeyPath = "ssh_key.pub";
if (!File.Exists(sshPublicKeyPath))
{
    Console.WriteLine("Generating SSH key pair for vehicle authentication...");
    GenerateSshKeyPair(sshKeyPath, sshPublicKeyPath);
    Console.WriteLine($"SSH key pair generated. Public key: {File.ReadAllText(sshPublicKeyPath)}");
}
else
{
    Console.WriteLine("SSH public key already exists, skipping generation.");
}

Console.WriteLine($"Car Identity Key: {carIdentityKey}");
var configuration = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>() {
        { "carIdentityKey", carIdentityKey }
    })
    .AddJsonFile("appSettings.json")
    .AddJsonFile("appSettings.development.json", true)
    .Build();

var channelMapFile = new FileInfo("channelMap.json");
if (!channelMapFile.Exists)
    throw new FileNotFoundException("channelMap.json could not be found");
var channelMap = JsonSerializer.Deserialize<ChannelMap>(channelMapFile.OpenRead());
if (channelMap == null)
    throw new Exception("channelMap.json could not be deserialized");

var serviceCollection = new ServiceCollection();
serviceCollection.AddSingleton<ChannelMap>(channelMap);
serviceCollection.AddSingleton<IConfiguration>(configuration);
serviceCollection.AddSingleton<ServerConnectionService>();
serviceCollection.AddSingleton<VideoStreamService>();
serviceCollection.AddSingleton<VideoStreamManager>();
serviceCollection.AddSingleton<CarConfigurationService>();
serviceCollection.AddSingleton<SshKeyService>();
serviceCollection.AddSingleton<ControlService>();
serviceCollection.AddSingleton<ControlExecutionService>();
serviceCollection.AddSingleton<TelemetryService>();
serviceCollection.AddSingleton<Bash>();
serviceCollection.AddSingleton<IModuleManagerFactory, ModuleManagerFactory>();
serviceCollection.AddSingleton<CameraProcessParameterBuilder>();
serviceCollection.AddAllTransient(typeof(ControlTypeBase));
serviceCollection.AddAllTransient(typeof(IPwmModule));
serviceCollection.AddAllTransient(typeof(IGpioModule));
serviceCollection.AddLogging(c => {
    c.AddConsole(); 
    c.AddConfiguration(configuration.GetSection("Logging"));
});

var serviceProvider = serviceCollection.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
logger.LogDebug("Debug log enabled...");

var configService = serviceProvider.GetRequiredService<CarConfigurationService>();
configService.OnConfigurationChanged += () =>
{
    var config = configService.Configuration;
    logger.LogInformation($"Configuration changed to: {JsonSerializer.Serialize(config)}");
};
var videoStreamService = serviceProvider.GetRequiredService<VideoStreamService>();
var videoStreamManager = serviceProvider.GetRequiredService<VideoStreamManager>();
var connectionService = serviceProvider.GetRequiredService<ServerConnectionService>();
var carControlService = serviceProvider.GetRequiredService<ControlService>();

logger.LogInformation("Initializing car control...");
carControlService.Initialize();

if (configuration.GetValue<bool>("EnableChannelTest")) 
{
    logger.LogInformation("Running channel test...");
    await carControlService.TestControlsAsync();
}

await connectionService.ConnectToServer(carIdentityKey);
// Try load previous sync (contains server IDs) before optional sync
var hadPreviousSync = connectionService.TryLoadPreviousSync();
if (!hadPreviousSync)
{
    await connectionService.SyncChannelMapAsync();
}
// Connect control (will trigger sync if server hash mismatch)
await carControlService.ConnectToServer();

// Start video streams from channel map
logger.LogInformation("Starting video stream manager...");
videoStreamManager.StartAllStreams();

logger.LogInformation($"Car Engine Started...");

// Start HTTP server for SSH key download only if private key still exists
var keyDownloaded = !File.Exists(sshKeyPath);

if (!keyDownloaded)
{
    var httpListener = new HttpListener();
    httpListener.Prefixes.Add("http://+:8080/");
    httpListener.Start();

    // Handle SSH key download requests
    _ = Task.Run(async () =>
    {
        while (httpListener.IsListening)
        {
            try
            {
                var context = await httpListener.GetContextAsync();
                
                // Handle CORS preflight requests
                if (context.Request.HttpMethod == "OPTIONS")
                {
                    context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                    context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, OPTIONS");
                    context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
                    context.Response.StatusCode = 200;
                    context.Response.Close();
                    continue;
                }
                
                if (context.Request.Url?.AbsolutePath == "/ssh-key")
                {
                    // Verify identity hash from query parameter
                    var expectedHash = context.Request.QueryString.Get("hash");
                    if (string.IsNullOrEmpty(expectedHash))
                    {
                        context.Response.StatusCode = 400;
                        context.Response.ContentType = "text/plain";
                        context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                        var message = "Missing identity hash parameter";
                        var buffer = Encoding.UTF8.GetBytes(message);
                        context.Response.ContentLength64 = buffer.Length;
                        await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                        context.Response.OutputStream.Close();
                        continue;
                    }

                    // Compute hash of our carIdentityKey
                    var actualHash = LteCar.Shared.HashUtility.GenerateSha256Hash(carIdentityKey);

                    // Verify the hash matches
                    if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
                    {
                        context.Response.StatusCode = 403;
                        context.Response.ContentType = "text/plain";
                        context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                        var message = "Identity verification failed - this is not the selected vehicle";
                        var buffer = Encoding.UTF8.GetBytes(message);
                        context.Response.ContentLength64 = buffer.Length;
                        await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                        context.Response.OutputStream.Close();
                        logger.LogWarning($"SSH key download attempt with wrong identity hash. Expected: {actualHash}, Got: {expectedHash}");
                        continue;
                    }

                    if (File.Exists(sshKeyPath))
                    {
                        // Serve the private key and mark as downloaded
                        var privateKey = File.ReadAllText(sshKeyPath);
                        var response = context.Response;
                        
                        // Add CORS headers to allow browser access
                        response.Headers.Add("Access-Control-Allow-Origin", "*");
                        response.Headers.Add("Access-Control-Allow-Methods", "GET, OPTIONS");
                        response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
                        
                        response.ContentType = "text/plain";
                        response.Headers.Add("Content-Disposition", "attachment; filename=\"vehicle-ssh-key.pem\"");
                        var buffer = Encoding.UTF8.GetBytes(privateKey);
                        response.ContentLength64 = buffer.Length;
                        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                        response.OutputStream.Close();
                        
                        // Delete the private key for security (this marks it as downloaded)
                        File.Delete(sshKeyPath);
                        logger.LogInformation("SSH private key downloaded and deleted for security.");
                        
                        // Stop the HTTP server since key is no longer available
                        httpListener.Stop();
                        logger.LogInformation("SSH key download server stopped - key no longer available.");
                    }
                    else
                    {
                        // No private key available
                        context.Response.StatusCode = 404;
                        context.Response.ContentType = "text/plain";
                        context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                        var message = "SSH private key not available.";
                        var buffer = Encoding.UTF8.GetBytes(message);
                        context.Response.ContentLength64 = buffer.Length;
                        await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                        context.Response.OutputStream.Close();
                    }
                }
                else
                {
                    context.Response.StatusCode = 404;
                    context.Response.Close();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in HTTP listener");
            }
        }
    });

    logger.LogInformation("SSH key download server started on port 8080");
}
else
{
    logger.LogInformation("SSH key already downloaded - HTTP server not started");
}

// Application loop
await Task.Run(async () =>
{
    var telemetryService = serviceProvider.GetRequiredService<TelemetryService>();
    while (true)
    {
        await Task.WhenAll(telemetryService.Tick(), Task.Delay(100));
    }
});

static void GenerateSshKeyPair(string privateKeyPath, string publicKeyPath)
{
    // Use OpenSSL to generate RSA key pair (PKCS#8 format for browser compatibility)
    // Generate private key
    var generateKeyProcess = new System.Diagnostics.Process
    {
        StartInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "openssl",
            Arguments = $"genpkey -algorithm RSA -out {privateKeyPath} -pkeyopt rsa_keygen_bits:2048",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        }
    };
    
    generateKeyProcess.Start();
    generateKeyProcess.WaitForExit();
    
    if (generateKeyProcess.ExitCode != 0)
    {
        var error = generateKeyProcess.StandardError.ReadToEnd();
        throw new Exception($"Failed to generate private key with OpenSSL: {error}");
    }
    
    // Extract public key in PEM format
    var extractPublicKeyProcess = new System.Diagnostics.Process
    {
        StartInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "openssl",
            Arguments = $"rsa -pubout -in {privateKeyPath} -out {publicKeyPath}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        }
    };
    
    extractPublicKeyProcess.Start();
    extractPublicKeyProcess.WaitForExit();
    
    if (extractPublicKeyProcess.ExitCode != 0)
    {
        var error = extractPublicKeyProcess.StandardError.ReadToEnd();
        throw new Exception($"Failed to extract public key with OpenSSL: {error}");
    }
    
    // Set appropriate file permissions (Unix only)
    if (Environment.OSVersion.Platform == PlatformID.Unix)
    {
        File.SetUnixFileMode(privateKeyPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }
}