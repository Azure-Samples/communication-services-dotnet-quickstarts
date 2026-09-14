using Azure.Communication.CallAutomation;
using Microsoft.Extensions.Logging;

namespace Call_Automation_GCCH.Infrastructure.Services;

/// <summary>
/// Factory that provides a CallAutomationClient instance that can be updated at runtime.
/// This allows changing the PMA endpoint without restarting the application.
/// </summary>
public interface ICallAutomationClientFactory
{
    /// <summary>
    /// Gets the current CallAutomationClient instance.
    /// </summary>
    CallAutomationClient GetClient();

    /// <summary>
    /// Updates the client with new connection settings.
    /// </summary>
    void UpdateClient(string connectionString, string? pmaEndpoint);

    /// <summary>
    /// Gets the current PMA endpoint.
    /// </summary>
    string GetCurrentPmaEndpoint();

    /// <summary>
    /// Gets the current connection string (masked for security).
    /// </summary>
    string GetCurrentConnectionStringMasked();
}

public class CallAutomationClientFactory : ICallAutomationClientFactory
{
    private readonly ILogger<CallAutomationClientFactory> _logger;
    private readonly object _lock = new();
    private CallAutomationClient? _client;
    private string _currentConnectionString = string.Empty;
    private string _currentPmaEndpoint = string.Empty;

    public CallAutomationClientFactory(
        string connectionString,
        string pmaEndpoint,
        ILogger<CallAutomationClientFactory> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _currentConnectionString = connectionString ?? string.Empty;
        _currentPmaEndpoint = pmaEndpoint ?? string.Empty;

        if (!string.IsNullOrEmpty(connectionString))
        {
            CreateClient(connectionString, pmaEndpoint);
        }
        else
        {
            _logger.LogWarning("No ACS connection string provided. Client not initialized.");
        }
    }

    public CallAutomationClient GetClient()
    {
        lock (_lock)
        {
            if (_client == null)
            {
                throw new InvalidOperationException(
                    "CallAutomationClient is not initialized. Set the ACS connection string first via POST /api/configuration/set.");
            }
            return _client;
        }
    }

    public void UpdateClient(string connectionString, string? pmaEndpoint)
    {
        lock (_lock)
        {
            _currentConnectionString = connectionString ?? string.Empty;
            _currentPmaEndpoint = pmaEndpoint ?? string.Empty;
            CreateClient(connectionString, pmaEndpoint);
            _logger.LogInformation("CallAutomationClient updated. PMA endpoint: {PmaEndpoint}", 
                string.IsNullOrEmpty(pmaEndpoint) ? "(none)" : pmaEndpoint);
        }
    }

    public string GetCurrentPmaEndpoint() => _currentPmaEndpoint;

    public string GetCurrentConnectionStringMasked()
    {
        if (string.IsNullOrEmpty(_currentConnectionString))
            return "(not set)";

        // Mask the connection string for security
        var parts = _currentConnectionString.Split(';');
        var masked = parts.Select(p =>
        {
            if (p.StartsWith("accesskey=", StringComparison.OrdinalIgnoreCase))
                return "accesskey=***";
            return p;
        });
        return string.Join(";", masked);
    }

    private void CreateClient(string connectionString, string? pmaEndpoint)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            _logger.LogWarning("Cannot create client: connection string is empty.");
            _client = null;
            return;
        }

        if (!string.IsNullOrEmpty(pmaEndpoint))
        {
            _client = new CallAutomationClient(pmaEndpoint: new Uri(pmaEndpoint), connectionString: connectionString);
            _logger.LogInformation("CallAutomationClient created with PMA endpoint: {PmaEndpoint}", pmaEndpoint);
        }
        else
        {
            _client = new CallAutomationClient(connectionString: connectionString);
            _logger.LogInformation("CallAutomationClient created without PMA endpoint.");
        }
    }
}
