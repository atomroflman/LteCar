using LteCar.Onboard;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

var serviceCollection = new ServiceCollection();
serviceCollection.AddSingleton<ServerConnectionService>();
serviceCollection.AddLogging(c => c.AddConsole());

var serviceProvider = serviceCollection.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

var connectionService = serviceProvider.GetRequiredService<ServerConnectionService>();
connectionService.ConnectToServer(carId);



