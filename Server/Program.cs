using LteCar.Server;
using LteCar.Server.Configuration;
using LteCar.Server.Data;
using LteCar.Server.Extensions;
using LteCar.Server.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;

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
var idSalt = builder.Configuration.GetValue<string>("IdSalt") ?? Guid.NewGuid().ToString();
var idAlphabet = builder.Configuration.GetValue<string>("IdAlphabet") ?? "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_";
var transferAlphabet = builder.Configuration.GetValue<string>("SessionTransferAlphabet") ?? "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
while (idSalt.Length < idAlphabet.Length || idSalt.Length < transferAlphabet.Length)
{
    idSalt += idSalt;
}
var resolvedAlphabet = new string(idAlphabet
    .Distinct()
    .Zip(idSalt)
    .OrderBy(x => x.Second)
    .Select(x => x.First)
    .ToArray());
var transferResolvedAlphabet = new string(transferAlphabet
    .Distinct()
    .Zip(idSalt)
    .OrderBy(x => x.Second)
    .Select(x => x.First)
    .ToArray());
var sqids = new Sqids.SqidsEncoder<long>(new Sqids.SqidsOptions
{
    Alphabet = resolvedAlphabet,
    MinLength = 16
});
var transferSqids = new Sqids.SqidsEncoder<long>(new Sqids.SqidsOptions
{
    Alphabet = transferResolvedAlphabet,
    MinLength = 8
});
builder.Services.AddSingleton(sqids);
builder.Services.AddKeyedSingleton("transfer", transferSqids);

builder.Services.AddSingleton<VideoStreamReceiverService>();
builder.Services.AddSingleton<CarConnectionStore>();
builder.Services.AddDbContext<LteCarContext>((serviceProvider, options) =>
{
    var configService = serviceProvider.GetRequiredService<IConfigurationService>();
    options.UseSqlServer(configService.DefaultConnectionString);
});

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});
builder.Services.AddSignalR()
    .AddMessagePackProtocol()
    .AddJsonProtocol();

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "DataProtectionKeys")))
    .SetApplicationName("LteCar.Server");
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
    try
    {
        logger.LogDebug($"{ctx.Request.Method} {ctx.Request.Path}");
        logger.LogTrace($"Request: {string.Join(", ", ctx.Request.Headers.Select(h => $"{h.Key}: {h.Value}"))}");
        await next();
        logger.LogDebug($"{ctx.Response.StatusCode}");
        logger.LogTrace($"Response: {string.Join(", ", ctx.Response.Headers.Select(h => $"{h.Key}: {h.Value}"))}");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "GLOBAL EXCEPTION HANDLER: Unhandled exception in request pipeline for {Method} {Path}", ctx.Request.Method, ctx.Request.Path);
        throw;
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

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