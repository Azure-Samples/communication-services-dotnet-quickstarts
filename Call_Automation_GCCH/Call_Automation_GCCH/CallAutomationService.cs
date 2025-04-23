using Azure.Communication.CallAutomation;
using Call_Automation_GCCH.Controllers;
using Microsoft.Extensions.Logging;

namespace Call_Automation_GCCH.Services
{
    public class CallAutomationService
    {
        private readonly CallAutomationClient _client;
        private readonly ILogger<CallAutomationService> _logger;
        private static string? _recordingLocation;
        private static string _recordingFileFormat = "mp4";

        public CallAutomationService(string connectionString, string pmaEndpoint, ILogger<CallAutomationService> logger)
        {
            _client = new CallAutomationClient(pmaEndpoint: new Uri(pmaEndpoint), connectionString);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

        //Need Azure Cognitive services for this so in phase 2
        //public List<RecognitionChoice> GetChoices()
        //{
        //    return new List<RecognitionChoice> {
        //    new RecognitionChoice("Confirm", new List<string> {
        //        "Confirm",
        //        "First",
        //        "One"
        //    }) {
        //        Tone = DtmfTone.One
        //    },
        //    new RecognitionChoice("Cancel", new List<string> {
        //        "Cancel",
        //        "Second",
        //        "Two"
        //    }) {
        //        Tone = DtmfTone.Two
        //    }
        //};
        //public List<RecognitionChoice> GetChoices() => new List<RecognitionChoice>
        //        {
        //            // Only DTMF tones, no speech phrases
        //            new RecognitionChoice("Confirm", new List<string>()) { Tone = DtmfTone.One },
        //            new RecognitionChoice("Cancel",  new List<string>()) { Tone = DtmfTone.Two }
        //        };

    }
}






