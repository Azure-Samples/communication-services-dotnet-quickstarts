using Azure.Communication.CallAutomation;

namespace Call_Automation_GCCH.Services
{
    public class CallAutomationService
    {
        private CallAutomationClient _client;
        private ILogger<CallAutomationService> _logger;
        private static string? _recordingLocation;
        private static string _recordingFileFormat = "mp4";
        private string _currentPmaEndpoint = string.Empty;

        public CallAutomationService(string connectionString, ILogger<CallAutomationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _client = new CallAutomationClient(connectionString: connectionString);
        }

        /// <summary>
        /// Gets the call automation client for direct operations
        /// </summary>
        /// <returns>CallAutomationClient instance</returns>
        public CallAutomationClient GetCallAutomationClient()
        {
            return _client;
        }

        /// <summary>
        /// Gets the recording location
        /// </summary>
        /// <returns>Recording location string</returns>
        public static string GetRecordingLocation()
        {
            return _recordingLocation;
        }

        /// <summary>
        /// Sets the recording location
        /// </summary>
        /// <param name="location">The recording location to set</param>
        public static void SetRecordingLocation(string location)
        {
            _recordingLocation = location;
        }

        /// <summary>
        /// Gets the recording file format
        /// </summary>
        /// <returns>Recording file format string</returns>
        public static string GetRecordingFileFormat()
        {
            return _recordingFileFormat;
        }

        /// <summary>
        /// Sets the recording file format
        /// </summary>
        /// <param name="format">The recording file format to set</param>
        public static void SetRecordingFileFormat(string format)
        {
            _recordingFileFormat = format;
        }

        public CallConnection GetCallConnection(string callConnectionId)
        {
            try
            {
                return _client.GetCallConnection(callConnectionId);
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error in GetCallConnection: {ex.Message}. CallConnectionId: {callConnectionId}";
                _logger.LogError(errorMessage);
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
                string errorMessage = $"Error in GetCallMedia: {ex.Message}. CallConnectionId: {callConnectionId}";
                _logger.LogError(errorMessage);
                throw; // Rethrow so the caller can handle or return an error response
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
                string errorMessage = $"Error in GetCallConnectionProperties: {ex.Message}. CallConnectionId: {callConnectionId}";
                _logger.LogError(errorMessage);
                throw;
            }
        }

        ///// <summary>
        ///// Updates the CallAutomationClient with new connection settings
        ///// </summary>
        ///// <param name="connectionString">The ACS connection string</param>
        ///// <param name="pmaEndpoint">The PMA endpoint to use</param>
        //public void UpdateClient(string connectionString, string pmaEndpoint)
        //{
        //  _logger = _logger ?? throw new InvalidOperationException("Logger is not initialized");
        //  _currentPmaEndpoint = pmaEndpoint;

        //  if (!string.IsNullOrEmpty(pmaEndpoint))
        //  {
        //    _client = new CallAutomationClient(pmaEndpoint: new Uri(pmaEndpoint), connectionString: connectionString);
        //    _logger.LogInformation($"CallAutomationClient recreated with PMA endpoint: {pmaEndpoint}");
        //  }
        //  else
        //  {
        //    _logger.LogWarning("PmaEndpoint is empty. Creating CallAutomationClient without PmaEndpoint parameter.");
        //    _client = new CallAutomationClient(connectionString: connectionString);
        //  }
        //}

        //public string GetCurrentPmaEndpoint()
        //{
        //  return _currentPmaEndpoint;
        //}

    }
}






