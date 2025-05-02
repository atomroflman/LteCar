using LteCar.Server;
using LteCar.Server.Hubs;
using LteCar.Shared;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();

builder.Services.AddSingleton<VideoStreamRecieverService>();
builder.Services.AddSingleton<CarConnectionStore>();

builder.Services.AddControllers();
builder.Services.AddSignalR();

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation("Starting Janus service...");
var vss = app.Services.GetRequiredService<VideoStreamRecieverService>();
vss.RunVideoStreamServer();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
app.Use(async(ctx, next) => {
    logger.LogDebug($"{ctx.Request.Method} {ctx.Request.Path}");
    await next();
    logger.LogDebug($"{ctx.Response.StatusCode}");
});

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
    endpoints.MapHub<CarControlHub>("/control");
    endpoints.MapHub<TelemetryHub>("/telemetry");
});

app.Run();