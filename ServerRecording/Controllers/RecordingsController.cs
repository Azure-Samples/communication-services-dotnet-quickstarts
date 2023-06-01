using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RecordingApi.Controllers
{
    [ApiController]
    public class RecordingsController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly CallAutomationClient _client;
        private readonly IConfiguration _configuration;
        private const string RecordingActiveCode = "8553";
        private const string RecordingActiveMessage = "Recording is already in progress, only one recording can be started.";
        private string _recordingFileFormat;


        public RecordingsController(IConfiguration configuration, ILogger<RecordingsController> logger)
        {
            _logger = logger;
            _configuration = configuration;
            _client = new CallAutomationClient(_configuration["ACSResourceConnectionString"]);

        }

        #region outbound call - an active call required for recording to start.

        [HttpGet("api/outbound_call", Name = "Outbound_Call")]
        public async Task<IActionResult> OutboundCall([FromQuery] string targetPstnPhoneNumber)
        {
            var CallerId = new PhoneNumberIdentifier(_configuration["ACSAcquiredPhoneNumber"]);
            var target = new PhoneNumberIdentifier(targetPstnPhoneNumber);
            var callInvite = new CallInvite(target, CallerId);

            var callbackUri = _configuration["BaseUri"] + "/api/callbacks";
            var createCallOption = new CreateCallOptions(callInvite, new Uri(callbackUri));

            var response = await _client.CreateCallAsync(createCallOption).ConfigureAwait(false);

            _logger.LogInformation($"Reponse from create call: {response.GetRawResponse()}" +
            $"CallConnection Id : {response.Value.CallConnection.CallConnectionId}" +
            $"Servercall id:{response.Value.CallConnectionProperties.ServerCallId}");
            return Ok();
        }

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


        [HttpPost("recordings", Name = "Start_Recording")]
        public async Task<IActionResult> StartRecordingAsync([FromQuery] string serverCallId)
        {
            try
            {
                if (!string.IsNullOrEmpty(serverCallId))
                {
                    //Passing RecordingContent initiates recording in specific format. audio/audiovideo
                    //RecordingChannel is used to pass the channel type. mixed/unmixed
                    //RecordingFormat is used to pass the format of the recording. mp4/mp3/wav
                    StartRecordingOptions recordingOptions = new StartRecordingOptions(new ServerCallLocator(serverCallId));

                    recordingOptions.RecordingContent = RecordingContent.Audio;
                    recordingOptions.RecordingFormat = RecordingFormat.Mp3;
                    recordingOptions.RecordingChannel = RecordingChannel.Mixed;

                    var startRecordingResponse = await _client.GetCallRecording()
                        .StartAsync(recordingOptions).ConfigureAwait(false);

                    _logger.LogInformation($"StartRecordingAsync response -- >  {startRecordingResponse.GetRawResponse()}, Recording Id: {startRecordingResponse.Value.RecordingId}");

                    var recordingId = startRecordingResponse.Value.RecordingId;
                    return Ok(recordingId);
                }
                else
                {
                    return BadRequest(new { Message = "serverCallId is invalid" });
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains(RecordingActiveCode))
                {
                    return BadRequest(new { Message = RecordingActiveMessage });
                }
                return Problem(ex.Message);
            }
        }

        [HttpPost("recordings/{recordingId}:pause", Name = "Pause_Recording")]
        public async Task<IActionResult> PauseRecording([FromRoute] string recordingId)
        {
            var response = await _client.GetCallRecording().PauseAsync(recordingId);
            _logger.LogInformation($"PauseRecordingAsync response -- > {response}");

            return Ok();
        }
        [HttpPost("recordings/{recordingId}:resume", Name = "Resume_Recording")]
        public async Task<IActionResult> ResumeRecordingAsync([FromRoute] string recordingId)
        {
            var response = await _client.GetCallRecording().ResumeAsync(recordingId);
            _logger.LogInformation($"ResumeRecordingAsync response -- > {response}");

            return Ok();
        }
        [HttpDelete("recordings/{recordingId}", Name = "Stop_Recording")]
        public async Task<IActionResult> StopRecordingAsync([FromRoute] string recordingId)
        {
            var response = await _client.GetCallRecording().StopAsync(recordingId);
            _logger.LogInformation($"StopRecordingAsync response -- > {response}");

            return Ok();
        }

        [HttpGet("getRecordingState/{recordingId}", Name = "GetRecording_State")]
        public async Task<IActionResult> GetRecordingStateAsync([FromRoute] string recordingId)
        {
            var response = await _client.GetCallRecording().GetStateAsync(recordingId);
            _logger.LogInformation($"GetRecordingStateAsync response -- > {response}");

            return Ok();
        }

        #region recording download file after stop recording

        /// <summary>
        /// Web hook to receive the recording file update status event
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("getRecordingFile")]
        public async Task<IActionResult> GetRecordingFile([FromBody] object request)
        {
            try
            {
                var httpContent = new BinaryData(request.ToString()).ToStream();
                EventGridEvent cloudEvent = EventGridEvent.ParseMany(BinaryData.FromStream(httpContent)).FirstOrDefault();

                if (cloudEvent.EventType == SystemEventNames.EventGridSubscriptionValidation)
                {
                    var eventData = cloudEvent.Data.ToObjectFromJson<SubscriptionValidationEventData>();

                    _logger.LogInformation("Microsoft.EventGrid.SubscriptionValidationEvent response  -- >" + cloudEvent.Data);

                    var responseData = new SubscriptionValidationResponse
                    {
                        ValidationResponse = eventData.ValidationCode
                    };

                    if (responseData.ValidationResponse != null)
                    {
                        return Ok(responseData);
                    }
                }

                if (cloudEvent.EventType == SystemEventNames.AcsRecordingFileStatusUpdated)
                {
                    _logger.LogInformation($"Event type is -- > {cloudEvent.EventType}");
                    _logger.LogInformation("Microsoft.Communication.RecordingFileStatusUpdated response  -- >" + cloudEvent.Data);

                    var eventData = cloudEvent.Data.ToObjectFromJson<AcsRecordingFileStatusUpdatedEventData>();
                    _logger.LogInformation("Start processing metadata -- >");

                    await ProcessFile(eventData.RecordingStorageInfo.RecordingChunks[0].MetadataLocation,
                        eventData.RecordingStorageInfo.RecordingChunks[0].DocumentId,
                        FileFormat.Json,
                        FileDownloadType.Metadata);

                    _logger.LogInformation("Start processing recorded media -- >");

                    await ProcessFile(eventData.RecordingStorageInfo.RecordingChunks[0].ContentLocation,
                        eventData.RecordingStorageInfo.RecordingChunks[0].DocumentId,
                        string.IsNullOrWhiteSpace(_recordingFileFormat) ? FileFormat.Mp4 : _recordingFileFormat,
                        FileDownloadType.Recording);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { Exception = ex });
            }
        }

        private async Task<bool> ProcessFile(string downloadLocation, string documentId, string fileFormat, string downloadType)
        {
            var recordingDownloadUri = new Uri(downloadLocation);
            var response = await _client.GetCallRecording().DownloadStreamingAsync(recordingDownloadUri);

            _logger.LogInformation($"Download {downloadType} response  -- >" + response.GetRawResponse());
            _logger.LogInformation($"Save downloaded {downloadType} -- >");

            string filePath = ".\\" + documentId + "." + fileFormat;
            using (Stream streamToReadFrom = response.Value)
            {
                using (Stream streamToWriteTo = System.IO.File.Open(filePath, FileMode.Create))
                {
                    await streamToReadFrom.CopyToAsync(streamToWriteTo);
                    await streamToWriteTo.FlushAsync();
                }
            }

            if (string.Equals(downloadType, FileDownloadType.Metadata, StringComparison.InvariantCultureIgnoreCase) && System.IO.File.Exists(filePath))
            {
                Root deserializedFilePath = JsonConvert.DeserializeObject<Root>(System.IO.File.ReadAllText(filePath));
                _recordingFileFormat = deserializedFilePath.recordingInfo.format;

                _logger.LogInformation($"Recording File Format is -- > {_recordingFileFormat}");
            }

            return true;
        }

        #endregion
    }
}
