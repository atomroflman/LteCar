using System.Text.Json;
using Spectre.Console;
using LteCar.Onboard;
using LteCar.Onboard.Control;
using LteCar.Onboard.Control.ControlTypes;
using LteCar.Onboard.Hardware;
using LteCar.Onboard.Telemetry;
using LteCar.Onboard.Vehicle;
using LteCar.Shared.Channels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;


// Setup-Modus prüfen
if (args.Length > 0 && args[0].Equals("setup", StringComparison.OrdinalIgnoreCase))
{
    Setup.ConfigTool.Run();
    return;
}

var carId = Guid.NewGuid().ToString();
var startupTime = DateTime.Now;
if (File.Exists("carId.txt"))
{
    carId = File.ReadAllText("carId.txt");
}
else
{
    Console.WriteLine($"New Car ID created: {carId}");
    File.WriteAllText("carId.txt", carId);
}

Console.WriteLine($"Car ID: {carId}");
var configuration = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>() {
        { "carId", carId }
    })
    .AddJsonFile("appSettings.json")
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
serviceCollection.AddSingleton<CarConfigurationService>();
serviceCollection.AddSingleton<ControlService>();
serviceCollection.AddSingleton<ControlExecutionService>();
serviceCollection.AddSingleton<IGearbox, VirtualAutomaticGearbox>();
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
var connectionService = serviceProvider.GetRequiredService<ServerConnectionService>();
var carControlService = serviceProvider.GetRequiredService<ControlService>();

logger.LogInformation("Initializing car control...");
carControlService.Initialize();

if (configuration.GetValue<bool>("EnableChannelTest")) 
{
    logger.LogInformation("Running channel test...");
    await carControlService.TestControlsAsync();
}

await connectionService.ConnectToServer(carId);
await carControlService.ConnectToServer();

logger.LogInformation($"Car Engine Started...");

// Application loop
await Task.Run(async () =>
{
    var telemetryService = serviceProvider.GetRequiredService<TelemetryService>();
    while (true)
    {
        await Task.WhenAll(telemetryService.Tick(), Task.Delay(100));
    }
});