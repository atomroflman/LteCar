using LteCar.Server;
using LteCar.Server.Data;
using LteCar.Server.Hubs;
using LteCar.Shared;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appSettings.json");

builder.Logging.AddConsole()
    .AddConfiguration(builder.Configuration.GetSection("Logging"));

builder.Services.AddSingleton<VideoStreamRecieverService>();
builder.Services.AddSingleton<CarConnectionStore>();
builder.Services.AddDbContext<LteCarContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddControllers();
builder.Services.AddSignalR()
    .AddMessagePackProtocol()
    .AddJsonProtocol();

var app = builder.Build();
var dbContext = app.Services.GetRequiredService<LteCarContext>();
dbContext.Database.EnsureCreated();

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

var staticFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
if (!Directory.Exists(staticFilePath))
{
    logger.LogWarning($"Static file path {staticFilePath} does not exist. Creating it. Server will run without client.");
    Directory.CreateDirectory(staticFilePath);
}
app.UseStaticFiles(new StaticFileOptions()
{
    ServeUnknownFileTypes = true,
    DefaultContentType = "application/octet-stream",
    FileProvider = new PhysicalFileProvider(staticFilePath)
});
app.UseRouting();

app.MapControllers();
app.MapHub<CarConnectionHub>(HubPaths.CarConnectionHub);
app.MapHub<CarControlHub>(HubPaths.CarControlHub);
app.MapHub<TelemetryHub>(HubPaths.TelemetryHub);
app.MapHub<CarUiHub>(HubPaths.CarUiHub);

app.Run();