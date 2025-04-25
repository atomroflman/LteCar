using System.Text.Json;
using LteCar.Onboard;
using LteCar.Onboard.Control;
using LteCar.Onboard.Control.ControlTypes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.Console;

var carId = Guid.NewGuid().ToString();

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
var serviceCollection = new ServiceCollection();
serviceCollection.AddSingleton<IConfiguration>(configuration);
serviceCollection.AddSingleton<ServerConnectionService>();
serviceCollection.AddSingleton<VideoStreamService>();
serviceCollection.AddSingleton<CarConfigurationService>();
serviceCollection.AddSingleton<ControlService>();
serviceCollection.AddSingleton<ControlExecutionService>();
serviceCollection.AddAllTransient(typeof(ControlTypeBase));
serviceCollection.AddLogging(c =>  {
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
await connectionService.ConnectToServer(carId);
await carControlService.ConnectToServer();

logger.LogInformation($"Car Engine Started...");
await Task.Delay(Timeout.Infinite);