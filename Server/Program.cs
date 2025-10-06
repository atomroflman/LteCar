using LteCar.Server;
using LteCar.Server.Configuration;
using LteCar.Server.Data;
using LteCar.Server.Extensions;
using LteCar.Server.Hubs;
using LteCar.Server.Services;
using LteCar.Shared;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Authentication.Cookies;


var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .AddJsonFile("appSettings.json")
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .AddUserSecrets<Program>();

builder.Logging.AddConsole()
    .AddConfiguration(builder.Configuration.GetSection("Logging"));

// Configure application configuration
builder.Services.AddApplicationConfiguration(builder.Configuration);

builder.Services.AddSingleton<VideoStreamReceiverService>();
builder.Services.AddSingleton<CarConnectionStore>();
builder.Services.AddDbContext<LteCarContext>((serviceProvider, options) =>
{
    var configService = serviceProvider.GetRequiredService<IConfigurationService>();
    options.UseSqlite(configService.DefaultConnectionString);
});

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});
builder.Services.AddSignalR()
    .AddMessagePackProtocol()
    .AddJsonProtocol();

builder.Services.AddAuthentication("cookie")
    .AddCookie("cookie", options =>
    {
        options.Cookie.Name = "LteCarAuth";
        options.LoginPath = "/";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.MaxAge = TimeSpan.MaxValue;
        options.Events.OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
    });

var app = builder.Build();
var configuration = app.Configuration;
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<LteCarContext>();
    dbContext.Database.Migrate();
}

var logger = app.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation("Database migrations applied successfully.");
var vss = app.Services.GetRequiredService<VideoStreamReceiverService>();

var configService = app.Services.GetRequiredService<IConfigurationService>();
if (configService.Application.RunJanusServer)
{
    app.Services.GetRequiredService<VideoStreamReceiverService>().RunVideoStreamServer();
}
else
{
    logger.LogWarning("Running Janus server is disabled.");
}

app.Use(async(ctx, next) => {
    logger.LogDebug($"{ctx.Request.Method} {ctx.Request.Path}");
    logger.LogTrace($"Request: {string.Join(", ", ctx.Request.Headers.Select(h => $"{h.Key}: {h.Value}"))}");
    await next();
    logger.LogDebug($"{ctx.Response.StatusCode}");
    logger.LogTrace($"Response: {string.Join(", ", ctx.Response.Headers.Select(h => $"{h.Key}: {h.Value}"))}");
});

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}


var staticFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
if (!Directory.Exists(staticFilePath))
{
    Directory.CreateDirectory(staticFilePath);
}
if (!Directory.GetFiles(staticFilePath).Any()) 
{    
    logger.LogWarning($"No static files in path: '{staticFilePath}'. Server will run without client.");
}
app.UseStaticFiles(new StaticFileOptions()
{
    ServeUnknownFileTypes = true,
    DefaultContentType = "application/octet-stream",
    FileProvider = new PhysicalFileProvider(staticFilePath)
});
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<CarConnectionHub>(HubPaths.CarConnectionHub);
app.MapHub<CarControlHub>(HubPaths.CarControlHub);
app.MapHub<TelemetryHub>(HubPaths.TelemetryHub);
app.MapHub<CarUiHub>(HubPaths.CarUiHub);
app.MapHub<CarVideoHub>(HubPaths.CarVideoHub);
app.MapHub<UserChannelHub>(HubPaths.UserChannelHub);

// Validate configuration during startup
app.Services.ValidateConfiguration();

app.Run();