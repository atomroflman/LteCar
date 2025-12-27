using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace LteCar.Server.Configuration;

public interface IConfigurationService
{
    ApplicationConfiguration Application { get; }
    JanusConfiguration Janus { get; }
    string DefaultConnectionString { get; }
}

public class ConfigurationService : IConfigurationService
{
    private readonly ILogger<ConfigurationService> _logger;

    public ConfigurationService(
        IOptions<ApplicationConfiguration> appConfig,
        IOptions<JanusConfiguration> janusConfig,
        ILogger<ConfigurationService> logger)
    {
        _logger = logger;
        
        Application = appConfig.Value;
        Janus = janusConfig.Value;
        
        ValidateConfiguration();
        LogConfiguration();
    }

    public ApplicationConfiguration Application { get; }
    public JanusConfiguration Janus { get; }
    public string DefaultConnectionString => Application.ConnectionStrings.DefaultConnection;

    private void ValidateConfiguration()
    {
        try
        {
            // Validate Application Configuration
            var appValidationContext = new ValidationContext(Application);
            var appValidationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(Application, appValidationContext, appValidationResults, true))
            {
                var errors = string.Join(", ", appValidationResults.Select(r => r.ErrorMessage));
                throw new InvalidOperationException($"Application configuration validation failed: {errors}");
            }

            // Validate Janus Configuration
            var janusValidationContext = new ValidationContext(Janus);
            var janusValidationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(Janus, janusValidationContext, janusValidationResults, true))
            {
                var errors = string.Join(", ", janusValidationResults.Select(r => r.ErrorMessage));
                throw new InvalidOperationException($"Janus configuration validation failed: {errors}");
            }

            // Custom validation
            Janus.Validate();

            _logger.LogInformation("Configuration validation successful");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Configuration validation failed - application cannot start");
            throw;
        }
    }

    private void LogConfiguration()
    {
        _logger.LogInformation("Configuration loaded:");
        _logger.LogInformation("- RunJanusServer: {RunJanusServer}", Application.RunJanusServer);
        _logger.LogInformation("- JanusHostName: {HostName}", Janus.HostName);
        _logger.LogInformation("- Video Port Range: {Start}-{End} ({Total} Streams possible)", Janus.PortRangeStart, Janus.PortRangeEnd, (Janus.PortRangeEnd - Janus.PortRangeStart) / 2);
        _logger.LogInformation("- DefaultConnection: [CONFIGURED]");
    }
}