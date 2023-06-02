using Azure;
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
        
        // for simplicity storing last locations
        private string contentLocation;
        private string deleteLocation;

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
        [HttpGet("api/call", Name = "Outbound_Call")]
        public async Task<IActionResult> OutboundCall([FromQuery] string targetPhoneNumber)
        {
            var callerId = new PhoneNumberIdentifier(_configuration["ACSAcquiredPhoneNumber"]);
            var target = new PhoneNumberIdentifier(targetPhoneNumber);
            var callInvite = new CallInvite(target, callerId);
            var createCallOption = new CreateCallOptions(callInvite, new Uri(_configuration["BaseUri"] + "/api/callbacks"));

            var response = await _client.CreateCallAsync(createCallOption).ConfigureAwait(false);

            return Ok($"CallConnectionId: {response.Value.CallConnection.CallConnectionId}");
        }
        
        #endregion

        /// <summary>
        /// Start Recording 
        /// </summary>
        /// <param name="serverCallId"></param>
        /// <returns></returns>
        [HttpPost("recordings", Name = "Start_Recording")]
        public async Task<IActionResult> StartRecordingAsync([FromQuery] string serverCallId)
        {
            try
            {
                StartRecordingOptions recordingOptions = new StartRecordingOptions(new ServerCallLocator(serverCallId))
                {
                    RecordingContent = RecordingContent.Audio,
                    RecordingFormat = RecordingFormat.Mp3,
                    RecordingChannel = RecordingChannel.Mixed,
                };

                var callRecording = _client.GetCallRecording();
                var response = await callRecording.StartAsync(recordingOptions).ConfigureAwait(false);
                
                return Ok($"RecordingId: {response.Value.RecordingId}");
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
        [HttpPost("recordings/{recordingId}:pause", Name = "Pause_Recording")]
        public async Task<IActionResult> PauseRecording([FromRoute] string recordingId)
        {
            var response =  await _client.GetCallRecording().PauseAsync(recordingId).ConfigureAwait(false);

            _logger.LogInformation($"Pause Recording response -- > {response}");
            return Ok();
        }

        /// <summary>
        /// Resume Recording
        /// </summary>
        /// <param name="recordingId"></param>
        /// <returns></returns>
        [HttpPost("recordings/{recordingId}:resume", Name = "Resume_Recording")]
        public async Task<IActionResult> ResumeRecordingAsync([FromRoute] string recordingId)
        {
            var response = await _client.GetCallRecording().ResumeAsync(recordingId).ConfigureAwait(false);
            
            _logger.LogInformation($"Resume Recording response -- > {response}");
            return Ok();
        }

        /// <summary>
        /// Stop Recording
        /// </summary>
        /// <param name="recordingId"></param>
        /// <returns></returns>
        [HttpDelete("recordings/{recordingId}", Name = "Stop_Recording")]
        public async Task<IActionResult> StopRecordingAsync([FromRoute] string recordingId)
        {
            var response = await _client.GetCallRecording().StopAsync(recordingId).ConfigureAwait(false);
            
            _logger.LogInformation($"StopRecordingAsync response -- > {response}");
            return Ok();
        }

        /// <summary>
        /// Get recording state
        /// </summary>
        /// <param name="recordingId"></param>
        /// <returns></returns>
        [HttpGet("getRecordingState/{recordingId}", Name = "GetRecording_State")]
        public async Task<IActionResult> GetRecordingStateAsync([FromRoute] string recordingId)
        {
            var response = await _client.GetCallRecording().GetStateAsync(recordingId).ConfigureAwait(false);
            
            _logger.LogInformation($"GetRecordingStateAsync response -- > {response}");
            return Ok();
        }

        /// <summary>
        /// Download Recording
        /// </summary>
        /// <returns></returns>
        [HttpGet("download", Name = "Download_Recording")]
        public IActionResult DownloadRecording()
        {
            var callRecording = _client.GetCallRecording();
            callRecording.DownloadTo(new Uri(contentLocation), "Recording_File.wav");
            return Ok();
        }

        /// <summary>
        /// Delete Recording
        /// </summary>
        /// <returns></returns>
        [HttpDelete("delete", Name = "Delete_Recording")]
        public IActionResult DeleteRecording()
        {
            _client.GetCallRecording().Delete(new Uri(deleteLocation));
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
                        contentLocation = statusUpdated.RecordingStorageInfo.RecordingChunks[0].ContentLocation;
                        deleteLocation = statusUpdated.RecordingStorageInfo.RecordingChunks[0].DeleteLocation;
                    }
                }
            }
            return Ok($"Recording Download Location : {contentLocation}, Recording Delete Location: {deleteLocation}");
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
