using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace RecordingApi.Controllers
{
    /// <summary>
    /// Recording APIs
    /// </summary>
    [ApiController]
    public class RecordingsController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly CallAutomationClient _client;
        private readonly IConfiguration _configuration;

        // for simplicity using static values
        private static string _serverCallId = "";
        private static string _callConnectionId = "";
        private static string _recordingId = "";
        private static string _contentLocation = "";
        private static string _deleteLocation = "";

        /// <summary>
        /// Initilize Recording
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        public RecordingsController(IConfiguration configuration, ILogger<RecordingsController> logger)
        {
            _logger = logger;
            _configuration = configuration;
            _client = new CallAutomationClient(_configuration["ACSResourceConnectionString"]);
        }

        #region outbound call - an active call required for recording to start.

        /// <summary>
        /// Start outbound call, Run before start recording
        /// </summary>
        /// <param name="targetPhoneNumber"></param>
        /// <returns></returns>
        [HttpGet("OutboundCall")]
        public async Task<IActionResult> OutboundCall([FromQuery] string targetPhoneNumber)
        {
            if(!targetPhoneNumber.Contains("+"))
            {
                targetPhoneNumber = targetPhoneNumber.Replace(" ", "+");
            }
            var callerId = new PhoneNumberIdentifier(_configuration["ACSAcquiredPhoneNumber"]);
            var target = new PhoneNumberIdentifier(targetPhoneNumber);
            var callInvite = new CallInvite(target, callerId);
            var createCallOption = new CreateCallOptions(callInvite, new Uri(_configuration["BaseUri"] + "/api/callbacks"));

            var response = await _client.CreateCallAsync(createCallOption).ConfigureAwait(false);
            _callConnectionId = response.Value.CallConnection.CallConnectionId;

            return Ok($"CallConnectionId: {_callConnectionId}");
        }

        #endregion

        /// <summary>
        /// Start Recording 
        /// </summary>
        /// <param name="serverCallId"></param>
        /// <returns></returns>
        [HttpGet("StartRecording")]
        public async Task<IActionResult> StartRecordingAsync([FromQuery] string serverCallId)
        {
            try
            {
                _serverCallId = serverCallId ?? _client.GetCallConnection(_callConnectionId).GetCallConnectionProperties().Value.ServerCallId;
                StartRecordingOptions recordingOptions = new StartRecordingOptions(new ServerCallLocator(_serverCallId));
                var callRecording = _client.GetCallRecording();
                var response = await callRecording.StartAsync(recordingOptions).ConfigureAwait(false);
                _recordingId = response.Value.RecordingId;
                return Ok($"RecordingId: {_recordingId}");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Pause Recording
        /// </summary>
        /// <param name="recordingId"></param>
        /// <returns></returns>
        [HttpPost("PauseRecording")]
        public async Task<IActionResult> PauseRecording([FromQuery] string recordingId)
        {
            _recordingId = recordingId ?? _recordingId;
            var response = await _client.GetCallRecording().PauseAsync(_recordingId).ConfigureAwait(false);

            _logger.LogInformation($"Pause Recording response -- > {response}");
            return Ok();
        }

        /// <summary>
        /// Resume Recording
        /// </summary>
        /// <param name="recordingId"></param>
        /// <returns></returns>
        [HttpPost("ResumeRecording")]
        public async Task<IActionResult> ResumeRecordingAsync([FromQuery] string recordingId)
        {
            _recordingId = recordingId ?? _recordingId;
            var response = await _client.GetCallRecording().ResumeAsync(_recordingId).ConfigureAwait(false);

            _logger.LogInformation($"Resume Recording response -- > {response}");
            return Ok();
        }

        /// <summary>
        /// Stop Recording
        /// </summary>
        /// <param name="recordingId"></param>
        /// <returns></returns>
        [HttpDelete("StopRecording")]
        public async Task<IActionResult> StopRecordingAsync([FromQuery] string recordingId)
        {
            _recordingId = recordingId ?? _recordingId;
            var response = await _client.GetCallRecording().StopAsync(_recordingId).ConfigureAwait(false);

            _logger.LogInformation($"StopRecordingAsync response -- > {response}");
            return Ok();
        }

        /// <summary>
        /// Get recording state
        /// </summary>
        /// <param name="recordingId"></param>
        /// <returns></returns>
        [HttpGet("GetRecordingState")]
        public async Task<IActionResult> GetRecordingStateAsync([FromQuery] string recordingId)
        {
            _recordingId = recordingId ?? _recordingId;
            var response = await _client.GetCallRecording().GetStateAsync(_recordingId).ConfigureAwait(false);

            _logger.LogInformation($"GetRecordingStateAsync response -- > {response}");
            return Ok($"{response.Value.RecordingState}");
        }

        /// <summary>
        /// Download Recording
        /// </summary>
        /// <returns></returns>
        [HttpGet("DownloadRecording")]
        public IActionResult DownloadRecording()
        {
            var callRecording = _client.GetCallRecording();
            callRecording.DownloadTo(new Uri(_contentLocation), "Recording_File.wav");
            return Ok();
        }

        /// <summary>
        /// Delete Recording
        /// </summary>
        /// <returns></returns>
        [HttpDelete("DeleteRecording")]
        public IActionResult DeleteRecording()
        {
            _client.GetCallRecording().Delete(new Uri(_deleteLocation));
            return Ok();
        }

        #region call backs apis

        /// <summary>
        /// Web hook to receive the recording file update status event, [Do not call directly from Swagger]
        /// </summary>
        /// <param name="eventGridEvents"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("recordingFileStatus")]
        public IActionResult RecordingFileStatus([FromBody] EventGridEvent[] eventGridEvents)
        {
            foreach (var eventGridEvent in eventGridEvents)
            {
                if (eventGridEvent.TryGetSystemEventData(out object eventData))
                {
                    // Handle the webhook subscription validation event.
                    if (eventData is Azure.Messaging.EventGrid.SystemEvents.SubscriptionValidationEventData subscriptionValidationEventData)
                    {
                        var responseData = new Azure.Messaging.EventGrid.SystemEvents.SubscriptionValidationResponse
                        {
                            ValidationResponse = subscriptionValidationEventData.ValidationCode
                        };
                        return Ok(responseData);
                    }

                    if (eventData is Azure.Messaging.EventGrid.SystemEvents.AcsRecordingFileStatusUpdatedEventData statusUpdated)
                    {
                        _contentLocation = statusUpdated.RecordingStorageInfo.RecordingChunks[0].ContentLocation;
                        _deleteLocation = statusUpdated.RecordingStorageInfo.RecordingChunks[0].DeleteLocation;
                    }
                }
            }
            return Ok($"Recording Download Location : {_contentLocation}, Recording Delete Location: {_deleteLocation}");
        }

        /// <summary>
        /// Call backs for signalling events, [Do not call directly from swagger]
        /// </summary>
        /// <param name="cloudEvents"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("/api/callbacks")]
        public IActionResult Callbacks([FromBody] CloudEvent[] cloudEvents)
        {
            try
            {
                foreach (var cloudEvent in cloudEvents)
                {
                    _logger.LogInformation($"Event received: {JsonConvert.SerializeObject(cloudEvent)}");
                    CallAutomationEventBase @event = CallAutomationEventParser.Parse(cloudEvent);

                    // for start recording we required server call id, so capture it when call connected.
                    if (@event is CallConnected)
                    {
                        _logger.LogInformation($"Server Call Id: {@event.ServerCallId}");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { Exception = ex });
            }
            return Ok();
        }

        #endregion
    }
}
