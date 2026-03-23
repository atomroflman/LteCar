using LteCar.Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LteCar.Server.Services;

public class UserCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UserCleanupService> _logger;
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(24);
    private static readonly TimeSpan InactiveThreshold = TimeSpan.FromDays(7);

    public UserCleanupService(IServiceProvider serviceProvider, ILogger<UserCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("UserCleanupService started. Cleaning up users inactive since {Threshold} hours", InactiveThreshold.TotalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupInactiveUsersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user cleanup");
            }

            await Task.Delay(CleanupInterval, stoppingToken);
        }
    }

    private async Task CleanupInactiveUsersAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LteCarContext>();

        var cutoffDate = DateTime.UtcNow.Subtract(InactiveThreshold);

        var anonymousInactiveUsers = await dbContext.Users
            .Where(u => u.LoginName == null && u.LastSeen < cutoffDate && !u.HasControlledCar)
            .ToListAsync(stoppingToken);

        var loggedInInactiveUsers = await dbContext.Users
            .Where(u => u.LoginName != null && u.LastLogin < cutoffDate && !u.HasControlledCar)
            .ToListAsync(stoppingToken);

        var inactiveUsers = anonymousInactiveUsers.Concat(loggedInInactiveUsers).ToList();

        if (inactiveUsers.Count == 0)
        {
            _logger.LogDebug("No inactive users to clean up");
            return;
        }

        _logger.LogInformation("Cleaning up {Count} inactive users who haven't controlled a car", inactiveUsers.Count);

        foreach (var user in inactiveUsers)
        {
            var lastActive = user.LoginName != null ? user.LastLogin : user.LastSeen;
            var userType = user.LoginName != null ? "logged-in" : "anonymous";
            _logger.LogInformation("Removing {UserType} inactive user: {UserId} ({Name}) - LastActive: {LastActive}", 
                userType, user.Id, user.Name ?? user.LoginName ?? "unknown", lastActive);
        }

        dbContext.Users.RemoveRange(inactiveUsers);
        await dbContext.SaveChangesAsync(stoppingToken);

        _logger.LogInformation("Successfully cleaned up {Count} inactive users", inactiveUsers.Count);
    }
}

public static class UserCleanupServiceExtensions
{
    public static IServiceCollection AddUserCleanupService(this IServiceCollection services)
    {
        services.AddHostedService<UserCleanupService>();
        return services;
    }
}
