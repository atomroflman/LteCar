using LteCar.Shared;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles(new StaticFileOptions()
{
    ServeUnknownFileTypes = true,
    DefaultContentType = "application/octet-stream",
    FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"))
});
app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapHub<CarConnectionHub>("/carconnectionhub");
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