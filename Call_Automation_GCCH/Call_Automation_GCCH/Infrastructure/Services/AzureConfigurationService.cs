using Call_Automation_GCCH.Core.Interfaces;
using Call_Automation_GCCH.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Call_Automation_GCCH.Infrastructure.Services;

/// <summary>
/// Infrastructure layer implementation of IConfigurationService
/// Manages runtime configuration for the application
/// </summary>
public class AzureConfigurationService : IConfigurationService
{
    private readonly ILogger<AzureConfigurationService> _logger;
    private readonly IOptionsMonitor<AcsCommunicationSettings> _configMonitor;
    private AcsCommunicationSettings _currentConfig;

    public AzureConfigurationService(
        ILogger<AzureConfigurationService> logger,
        IOptionsMonitor<AcsCommunicationSettings> configMonitor)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configMonitor = configMonitor ?? throw new ArgumentNullException(nameof(configMonitor));
        _currentConfig = _configMonitor.CurrentValue;
    }

    public Task<ApplicationConfiguration> GetConfigurationAsync()
    {
        _logger.LogInformation("Getting current application configuration");

        var config = new ApplicationConfiguration
        {
            AcsConnectionString = MaskConnectionString(_currentConfig.AcsConnectionString),
            PmaEndpoint = _currentConfig.PmaEndpoint,
            CallbackUriHost = _currentConfig.CallbackUriHost,
            AcsPhoneNumber = _currentConfig.AcsPhoneNumber,
            AudioFileUrl = _currentConfig.AudioFileUrl
        };

        return Task.FromResult(config);
    }

    public Task<ApplicationConfiguration> UpdateConfigurationAsync(ConfigurationUpdate update)
    {
        _logger.LogInformation("Updating application configuration");

        if (!string.IsNullOrWhiteSpace(update.AcsConnectionString))
        {
            _currentConfig.AcsConnectionString = update.AcsConnectionString;
        }

        if (!string.IsNullOrWhiteSpace(update.PmaEndpoint))
        {
            _currentConfig.PmaEndpoint = update.PmaEndpoint;
        }

        if (!string.IsNullOrWhiteSpace(update.CallbackUriHost))
        {
            _currentConfig.CallbackUriHost = update.CallbackUriHost;
        }

        if (!string.IsNullOrWhiteSpace(update.AcsPhoneNumber))
        {
            _currentConfig.AcsPhoneNumber = update.AcsPhoneNumber;
        }

        if (!string.IsNullOrWhiteSpace(update.AudioFileUrl))
        {
            _currentConfig.AudioFileUrl = update.AudioFileUrl;
        }

        return GetConfigurationAsync();
    }

    public Task<bool> SetConnectionStringAsync(string connectionString)
    {
        _logger.LogInformation("Setting ACS connection string");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger.LogWarning("Connection string is null or empty");
            return Task.FromResult(false);
        }

        _currentConfig.AcsConnectionString = connectionString;
        return Task.FromResult(true);
    }

    public Task<string> GetCallbackUriAsync()
    {
        _logger.LogInformation("Getting callback URI");
        return Task.FromResult(_currentConfig.CallbackUriHost ?? string.Empty);
    }

    private static string MaskConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return "Not configured";
        }

        // Mask the access key portion of the connection string
        var parts = connectionString.Split(';');
        var maskedParts = parts.Select(part =>
        {
            if (part.StartsWith("accesskey=", StringComparison.OrdinalIgnoreCase))
            {
                return "accesskey=***MASKED***";
            }
            return part;
        });

        return string.Join(";", maskedParts);
    }
}
