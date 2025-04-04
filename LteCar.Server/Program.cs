using LteCar.Shared;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapHub<CarConnectionHub>("/videostreamhub");
});

app.Run();

public class CarConnectionHub : Hub<IConnectionHubClient>
{
    public async Task UpdateCarAddress(string carId)
    {
        var remoteIp = Context.GetHttpContext().Connection.RemoteIpAddress;
        await Clients.Groups(carId).UpdateCarAddress(remoteIp.ToString());
    }
}