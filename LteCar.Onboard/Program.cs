

using LteCar.Onboard;
using Microsoft.Extensions.DependencyInjection;

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
serviceCollection.AddLogging(configure => {
    configure.AddConsole();
    configure.AddFile("logs/log.txt", append: true);
});

var serviceProvider = serviceCollection.BuildServiceProvider();

var connectionService = serviceProvider.GetRequiredService<ServerConnectionService>();
connectionService.ConnectToServer(carId);



