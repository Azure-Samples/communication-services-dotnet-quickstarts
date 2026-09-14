using Azure.Communication.CallAutomation;
using Microsoft.Extensions.Logging;

namespace Call_Automation_GCCH.Services
{
    /// <summary>
    /// Wraps the Azure <see cref="CallAutomationClient"/> and provides
    /// convenience methods for call connection, media, and recording operations.
    /// Registered as a singleton via <see cref="ICallAutomationService"/>.
    /// </summary>
    public class CallAutomationService : ICallAutomationService
    {
        private CallAutomationClient? _client;
        private CallAutomationClient? _downloadClient;
        private readonly ILogger<CallAutomationService> _logger;
        private string _currentPmaEndpoint = string.Empty;
        private string _currentConnectionString = string.Empty;

        public string? RecordingLocation { get; set; }
        public string RecordingFileFormat { get; set; } = "mp4";

        public CallAutomationService(string connectionString, string pmaEndpoint, ILogger<CallAutomationService> logger)
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
                _logger.LogWarning("No ACS connection string provided. Client not initialized. Call UpdateClient() or use /api/configuration/setConnectionString.");
            }
        }

        public CallAutomationClient GetCallAutomationClient()
        {
            if (_client == null)
                throw new InvalidOperationException("CallAutomationClient is not initialized. Set the ACS connection string first via POST /api/configuration/setConnectionString.");
            return _client;
        }

        public CallAutomationClient GetRecordingDownloadClient()
        {
            if (string.IsNullOrEmpty(_currentConnectionString))
                throw new InvalidOperationException("CallAutomationClient is not initialized. Set the ACS connection string first via POST /api/configuration/setConnectionString.");

            // Recording content is served from the AMS storage endpoint (not PMA).
            // A client constructed with a PMA endpoint signs against the PMA host and gets
            // 401 Unauthorized from AMS. Use a connection-string-only client for downloads.
            if (_downloadClient == null)
            {
                _downloadClient = new CallAutomationClient(connectionString: _currentConnectionString);
                _logger.LogInformation("Recording download CallAutomationClient created (no PMA endpoint).");
            }
            return _downloadClient;
        }

        public CallConnection GetCallConnection(string callConnectionId)
        {
            try
            {
                return _client.GetCallConnection(callConnectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCallConnection. CallConnectionId: {CallConnectionId}", callConnectionId);
                throw;
            }
        }

        public CallMedia GetCallMedia(string callConnectionId)
        {
            try
            {
                return _client.GetCallConnection(callConnectionId).GetCallMedia();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCallMedia. CallConnectionId: {CallConnectionId}", callConnectionId);
                throw;
            }
        }

        public CallConnectionProperties GetCallConnectionProperties(string callConnectionId)
        {
            try
            {
                return _client.GetCallConnection(callConnectionId).GetCallConnectionProperties();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCallConnectionProperties. CallConnectionId: {CallConnectionId}", callConnectionId);
                throw;
            }
        }

        public void UpdateClient(string connectionString, string pmaEndpoint)
        {
            _currentConnectionString = connectionString ?? string.Empty;
            _currentPmaEndpoint = pmaEndpoint ?? string.Empty;
            _downloadClient = null;
            CreateClient(connectionString, pmaEndpoint);
        }

        public string GetCurrentPmaEndpoint() => _currentPmaEndpoint;

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
}
