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
        { "CarIdentityKey", carIdentityKey }
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
// Configuration
serviceCollection.AddSingleton<ChannelMap>(channelMap);
serviceCollection.AddSingleton<IConfiguration>(configuration);

// Hub Connections
serviceCollection.AddSingleton<ServerConnectionService>();
serviceCollection.AddSingleton<VideoStreamService>();
serviceCollection.AddSingleton<ServerCarConfigurationService>();
serviceCollection.AddSingleton<ControlService>();
serviceCollection.AddSingleton<TelemetryService>();

serviceCollection.AddSingleton<SshKeyService>();
serviceCollection.AddSingleton<ControlExecutionService>();
serviceCollection.AddTransient<Bash>();
serviceCollection.AddSingleton<IModuleManagerFactory, ModuleManagerFactory>();
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

var configService = serviceProvider.GetRequiredService<ServerCarConfigurationService>();
configService.OnConfigurationChanged += () =>
{
    var config = configService.Configuration;
    logger.LogInformation($"Configuration changed to: {JsonSerializer.Serialize(config)}");
};
// Log key fingerprints at startup
try
{
    var sshLogService = serviceProvider.GetRequiredService<SshKeyService>();
    sshLogService.LogKeyFingerprints();
}
catch (Exception ex)
{
    logger.LogError(ex, "Failed to log key fingerprints at startup");
}
var connectionService = serviceProvider.GetRequiredService<ServerConnectionService>();
var carControlService = serviceProvider.GetRequiredService<ControlService>();

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
// Initialize Car incatance if needed
await carControlService.ConnectToServer();
logger.LogInformation($"Car Engine Started...");

// Initialize Car remote control
logger.LogInformation("Initializing car control...");
carControlService.Initialize();

// Initialize video streaming
var videoStreamService = serviceProvider.GetRequiredService<VideoStreamService>();
logger.LogInformation("Initializing video streaming...");
await videoStreamService.Connect();

// Start HTTP server for SSH key download only if private key still exists
var keyDownloaded = !File.Exists(sshKeyPath);
if (!keyDownloaded)
{
    var httpListener = new HttpListener();
    httpListener.Prefixes.Add("http://+:8080/");
    httpListener.Start();

    var keepPrivateKey = configuration.GetValue<bool>("KeepPrivateKey");
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
                        // Serve the private key (PKCS#8 DER binary) and mark as downloaded
                        var privateKeyBytes = File.ReadAllBytes(sshKeyPath);
                        var response = context.Response;

                        // Add CORS headers to allow browser access
                        response.Headers.Add("Access-Control-Allow-Origin", "*");
                        response.Headers.Add("Access-Control-Allow-Methods", "GET, OPTIONS");
                        response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

                        response.ContentType = "application/octet-stream";
                        response.Headers.Add("Content-Disposition", "attachment; filename=\"vehicle-ssh-key.der\"");
                        response.ContentLength64 = privateKeyBytes.Length;
                        await response.OutputStream.WriteAsync(privateKeyBytes, 0, privateKeyBytes.Length);
                        response.OutputStream.Close();

                        // Delete the private key for security (this marks it as downloaded)
                        if (!keepPrivateKey)
                        {
                            File.Delete(sshKeyPath);
                            logger.LogInformation("SSH private key downloaded and deleted for security.");

                            // Stop the HTTP server since key is no longer available
                            httpListener.Stop();
                            logger.LogInformation("SSH key download server stopped - key no longer available.");
                        }
                        else
                        {
                            logger.LogWarning("SSH private key downloaded but retained as per configuration.");
                        }
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
    using var rsa = RSA.Create(2048);
    var privatePkcs8 = rsa.ExportPkcs8PrivateKey(); // → PKCS#8 Binary (DER)
    var publicSpki = rsa.ExportSubjectPublicKeyInfo(); // → SPKI Binary (DER)
    
    File.WriteAllBytes(privateKeyPath, privatePkcs8);
    File.WriteAllBytes(publicKeyPath, publicSpki);
}