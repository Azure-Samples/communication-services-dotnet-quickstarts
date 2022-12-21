using Azure.Communication.CallAutomation;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CallAutomation.Controllers
{
    [Route("/recording")]
    public class CallRecordingController : Controller
    {
        private readonly CallAutomationClient callAutomationClient;
        public ILogger<CallRecordingController> Logger { get; }
        private IConfiguration Config { get; }
        private ILocalRecordingInfoCache localRecordingInfoCache;

        public CallRecordingController(IConfiguration configuration, ILogger<CallRecordingController> logger)
        {
            callAutomationClient = new CallAutomationClient(configuration["ResourceConnectionString"]);
            Config = configuration;
            Logger = logger;
            localRecordingInfoCache = LocalRecordingInfoCache.Instance;
        }

        [Route("/recordingSettings")]
        [HttpGet]
        public IActionResult GetRecording()
        {
            var clientResponse = new
            {
                isRecordingEnabled = Config["IsRecordingEnabled"],
            };

            return this.Ok(clientResponse);
        }

        /// <summary>
        /// Method to start call recording
        /// </summary>
        /// <param name="serverCallId">Conversation id of the call</param>
        [HttpGet]
        [Route("startRecording")]
        public async Task<IActionResult> StartRecordingAsync(string serverCallId, string recordingContent = "", string recordingChannel = "", string recordingFormat = "")
        {
            try
            {
                if (!string.IsNullOrEmpty(serverCallId))
                {
                    RecordingContent recContentVal;
                    RecordingChannel recChannelVal;
                    RecordingFormat recFormatVal;

                    //Passing RecordingContent initiates recording in specific format. audio/audiovideo
                    //RecordingChannel is used to pass the channel type. mixed/unmixed
                    //RecordingFormat is used to pass the format of the recording. mp4/mp3/wav

                    StartRecordingOptions startRecordingOptions = new StartRecordingOptions(new ServerCallLocator(serverCallId));

                    var startRecordingResponse = await callAutomationClient
                        .GetCallRecording()
                        .StartRecordingAsync(startRecordingOptions).ConfigureAwait(false);

                    Logger.LogInformation($"StartRecordingAsync response -- >  {startRecordingResponse.GetRawResponse()}, Recording Id: {startRecordingResponse.Value.RecordingId}");

                    var recordingId = startRecordingResponse.Value.RecordingId;
                    localRecordingInfoCache.AddOrUpdate(serverCallId, recordingId);

                    return Json(recordingId);
                }
                else
                {
                    return BadRequest(new { Message = "serverCallId is invalid" });
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains(Constants.CallRecodingActiveErrorCode))
                {
                    return BadRequest(new { Message = Constants.CallRecodingActiveError });
                }
                return Json(new { Exception = ex });
            }
        }

        /// <summary>
        /// Method to pause call recording
        /// </summary>
        /// <param name="serverCallId">Conversation id of the call</param>
        /// <param name="recordingId">Recording id of the call</param>
        [HttpGet]
        [Route("pauseRecording")]
        public async Task<IActionResult> PauseRecordingAsync(string serverCallId, string recordingId)
        {
            try
            {
                if (!string.IsNullOrEmpty(serverCallId))
                {
                    if (string.IsNullOrEmpty(recordingId))
                    {
                        LocalRecordingInfo recInfo = localRecordingInfoCache.Get(serverCallId);
                        if (recInfo != null)
                        {
                            recordingId = recInfo.recordingId;
                        }
                    }
                    else
                    {
                        localRecordingInfoCache.AddOrUpdate(serverCallId, recordingId);
                    }

                    var pauseRecording = await callAutomationClient.GetCallRecording().PauseRecordingAsync(recordingId);
                    Logger.LogInformation($"PauseRecordingAsync response -- > {pauseRecording}");

                    return Ok();
                }
                else
                {
                    return BadRequest(new { Message = "serverCallId is invalid" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { Exception = ex });
            }
        }

        /// <summary>
        /// Method to resume call recording
        /// </summary>
        /// <param name="serverCallId">Conversation id of the call</param>
        /// <param name="recordingId">Recording id of the call</param>
        [HttpGet]
        [Route("resumeRecording")]
        public async Task<IActionResult> ResumeRecordingAsync(string serverCallId, string recordingId)
        {
            try
            {
                if (!string.IsNullOrEmpty(serverCallId))
                {
                    if (string.IsNullOrEmpty(recordingId))
                    {
                        LocalRecordingInfo recInfo = localRecordingInfoCache.Get(serverCallId);
                        if (recInfo != null)
                        {
                            recordingId = recInfo.recordingId;
                        }
                    }
                    else
                    {
                        localRecordingInfoCache.AddOrUpdate(serverCallId, recordingId);
                    }

                    var resumeRecording = await callAutomationClient.GetCallRecording().ResumeRecordingAsync(recordingId);
                    Logger.LogInformation($"ResumeRecordingAsync response -- > {resumeRecording}");

                    return Ok();
                }
                else
                {
                    return BadRequest(new { Message = "serverCallId is invalid" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { Exception = ex });
            }
        }

        /// <summary>
        /// Method to stop call recording
        /// </summary>
        /// <param name="serverCallId">Conversation id of the call</param>
        /// <param name="recordingId">Recording id of the call</param>
        /// <returns></returns>
        [HttpGet]
        [Route("stopRecording")]
        public async Task<IActionResult> StopRecordingAsync(string serverCallId, string recordingId)
        {
            try
            {
                if (!string.IsNullOrEmpty(serverCallId))
                {
                    if (string.IsNullOrEmpty(recordingId))
                    {
                        LocalRecordingInfo recInfo = localRecordingInfoCache.Get(serverCallId);
                        if (recInfo != null)
                        {
                            recordingId = recInfo.recordingId;
                        }
                    }
                    else
                    {
                        localRecordingInfoCache.AddOrUpdate(serverCallId, recordingId);
                    }

                    var stopRecording = await callAutomationClient.GetCallRecording().StopRecordingAsync(recordingId).ConfigureAwait(false);
                    Logger.LogInformation($"StopRecordingAsync response -- > {stopRecording}");

                    return Ok();
                }
                else
                {
                    return BadRequest(new { Message = "serverCallId is invalid" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { Exception = ex });
            }
        }

        /// <summary>
        /// Method to get recording Link
        /// </summary>
        /// <param name="serverCallId">Conversation id of the call</param>
        /// <param name="recordingId">Recording id of the call</param>
        /// <returns></returns>
        [HttpGet]
        [Route("getRecordingLink")]
        public IActionResult GetRecordingLink(string serverCallId)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(serverCallId))
                {
                    string downloadRecordingURL = null;

                    var recInfo = localRecordingInfoCache.Get(serverCallId);
                    if (recInfo != null)
                        downloadRecordingURL = recInfo.recordingUri;

                    if (!string.IsNullOrWhiteSpace(downloadRecordingURL))
                    {
                        Logger.LogInformation($"Recording download link -- > {downloadRecordingURL}");

                        var response = new
                        {
                            uri = downloadRecordingURL,
                        };

                        return Ok(response);
                    }
                    else
                    {
                        return NotFound(new { Message = "Recording SAS link is unavailable for download at this moment. Please try again later." });
                    }
                }
                else
                {
                    return BadRequest(new { Message = "Invalid server call id." });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Method to get recording state
        /// </summary>
        /// <param name="serverCallId">Conversation id of the call</param>
        /// <param name="recordingId">Recording id of the call</param>
        /// <returns>
        /// CallRecordingProperties
        ///     RecordingState is {active}, in case of active recording
        ///     RecordingState is {inactive}, in case if recording is paused
        /// GetRecordingStateAsync returns 404:Recording not found, in if recording was stopped or recording id is invalid.
        /// </returns>
        [HttpGet]
        [Route("getRecordingState")]
        public async Task<IActionResult> GetRecordingState(string serverCallId, string recordingId)
        {
            try
            {
                if (!string.IsNullOrEmpty(serverCallId))
                {
                    if (string.IsNullOrEmpty(recordingId))
                    {
                        LocalRecordingInfo recInfo = localRecordingInfoCache.Get(serverCallId);
                        if (recInfo != null)
                        {
                            recordingId = recInfo.recordingId;
                        }
                    }
                    else
                    {
                        localRecordingInfoCache.AddOrUpdate(serverCallId, recordingId);
                    }

                    var recordingState = await callAutomationClient.GetCallRecording().GetRecordingStateAsync(recordingId).ConfigureAwait(false);

                    Logger.LogInformation($"GetRecordingStateAsync response -- > {recordingState}");

                    return Json(recordingState.Value.RecordingState);
                }
                else
                {
                    return BadRequest(new { Message = "serverCallId is invalid" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { Exception = ex });
            }
        }

        /// <summary>
        /// Web hook to receive the recording file update status event
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("getRecordingFile")]
        public async Task<ActionResult> GetRecordingFile([FromBody] object request)
        {
            try
            {
                var httpContent = new BinaryData(request.ToString()).ToStream();
                EventGridEvent cloudEvent = EventGridEvent.ParseMany(BinaryData.FromStream(httpContent)).FirstOrDefault();

                if (cloudEvent.EventType == SystemEventNames.EventGridSubscriptionValidation)
                {
                    var eventData = cloudEvent.Data.ToObjectFromJson<SubscriptionValidationEventData>();

                    Logger.LogInformation($"{Constants.SubValidationEvent} response  -- > {cloudEvent.Data}");

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
                    var processRecordingFiles = new ProcessRecordingFiles(Logger, Config, callAutomationClient);
                    await processRecordingFiles.Process(cloudEvent.EventType, cloudEvent.Data, cloudEvent.Subject);
                }
            }
            catch (Exception ex)
            {
                Logger.LogInformation($"Exception Message -- > {ex.Message}");
                Logger.LogInformation($"Exception Stack Trace -- > {ex.StackTrace}");
            }

            return Ok();
        }
    }
}