using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace LteCar.Server.Configuration;

public interface IApplicationConfigurationService
{
    bool RunJanusServer { get; }
    ConnectionStrings ConnectionStrings { get; }
    string DefaultConnectionString { get; }
}

public class ApplicationConfigurationService : IApplicationConfigurationService
{
    private readonly ApplicationConfiguration _config;
    private readonly ILogger<ApplicationConfigurationService> _logger;

    public ApplicationConfigurationService(
        IOptions<ApplicationConfiguration> config,
        ILogger<ApplicationConfigurationService> logger)
    {
        _config = config.Value;
        _logger = logger;
        
        ValidateConfiguration();
        LogConfiguration();
    }

    public bool RunJanusServer => _config.RunJanusServer;
    public ConnectionStrings ConnectionStrings => _config.ConnectionStrings;
    public string DefaultConnectionString => _config.ConnectionStrings.DefaultConnection;

    private void ValidateConfiguration()
    {
        try
        {
            var validationContext = new ValidationContext(_config);
            var validationResults = new List<ValidationResult>();
            
            if (!Validator.TryValidateObject(_config, validationContext, validationResults, true))
            {
                var errors = string.Join(", ", validationResults.Select(r => r.ErrorMessage));
                throw new InvalidOperationException($"Application configuration validation failed: {errors}");
            }

            _logger.LogDebug("Application configuration validation successful");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Application configuration validation failed");
            throw;
        }
    }

    private void LogConfiguration()
    {
        _logger.LogInformation("Application Configuration:");
        _logger.LogInformation("- RunJanusServer: {RunJanusServer}", RunJanusServer);
        _logger.LogInformation("- DefaultConnection: [CONFIGURED]");
    }
}