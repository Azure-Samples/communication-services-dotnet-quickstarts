using Azure.Communication.CallingServer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace QuickStartApi.Controllers
{
    [Route("/recording")]
    public class CallRecordingController : Controller
    {
        private readonly string blobStorageConnectionString;
        private readonly string callbackUri;
        private readonly string containerName;
        private readonly CallingServerClient callingServerClient;
        private const string CallRecodingActiveErrorCode = "8553";
        private const string CallRecodingActiveError = "Recording is already in progress, one recording can be active at one time.";
        public ILogger<CallRecordingController> Logger { get; }
        static Dictionary<string, string> recordingData = new Dictionary<string, string>();

        public CallRecordingController(IConfiguration configuration, ILogger<CallRecordingController> logger)
        {
            blobStorageConnectionString = configuration["BlobStorageConnectionString"];
            callbackUri = configuration["CallbackUri"];
            containerName = configuration["ContainerName"];
            callingServerClient = new CallingServerClient(configuration["ResourceConnectionString"]);
            Logger = logger;
        }

        /// <summary>
        /// Method to start call recording
        /// </summary>
        /// <param name="serverCallId">Conversation id of the call</param>
        [HttpGet]
        [Route("startRecording")]
        public async Task<IActionResult> StartRecordingAsync(string serverCallId)
        {
            try
            {
                if (!string.IsNullOrEmpty(serverCallId))
                {
                    var uri = new Uri(callbackUri);
                    var startRecordingResponse = await callingServerClient.InitializeServerCall(serverCallId).StartRecordingAsync(uri).ConfigureAwait(false);

                    Logger.LogInformation($"StartRecordingAsync response -- >  {startRecordingResponse.GetRawResponse()}, Recording Id: {startRecordingResponse.Value.RecordingId}");

                    var recordingId = startRecordingResponse.Value.RecordingId;
                    if (!recordingData.ContainsKey(serverCallId))
                    {
                        recordingData.Add(serverCallId, string.Empty);
                    }
                    recordingData[serverCallId] = recordingId;

                    return Json(recordingId);
                }
                else
                {
                    return BadRequest(new { Message = "serverCallId is invalid" });
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains(CallRecodingActiveErrorCode))
                {
                    return BadRequest(new { Message = CallRecodingActiveError });
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
                        recordingId = recordingData[serverCallId];
                    }
                    else
                    {
                        if (!recordingData.ContainsKey(serverCallId))
                        {
                            recordingData[serverCallId] = recordingId;
                        }
                    }
                    var pauseRecording = await callingServerClient.InitializeServerCall(serverCallId).PauseRecordingAsync(recordingId);
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
                        recordingId = recordingData[serverCallId];
                    }
                    else
                    {
                        if (!recordingData.ContainsKey(serverCallId))
                        {
                            recordingData[serverCallId] = recordingId;
                        }
                    }
                    var resumeRecording = await callingServerClient.InitializeServerCall(serverCallId).ResumeRecordingAsync(recordingId);
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
                        recordingId = recordingData[serverCallId];
                    }
                    else
                    {
                        if (!recordingData.ContainsKey(serverCallId))
                        {
                            recordingData[serverCallId] = recordingId;
                        }
                    }

                    var stopRecording = await callingServerClient.InitializeServerCall(serverCallId).StopRecordingAsync(recordingId).ConfigureAwait(false);
                    Logger.LogInformation($"StopRecordingAsync response -- > {stopRecording}");

                    if (recordingData.ContainsKey(serverCallId))
                    {
                        recordingData.Remove(serverCallId);
                    }
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
                //Deserializing the request 
                var eventGridEvent = JsonConvert.DeserializeObject<EventGridEvent[]>(request.ToString()).FirstOrDefault();

                if (string.Equals(eventGridEvent.EventType, EventTypes.EventGridSubscriptionValidationEvent, StringComparison.OrdinalIgnoreCase))
                {
                    var data = eventGridEvent.Data as JObject;
                    var eventData = data.ToObject<SubscriptionValidationEventData>();

                    Logger.LogInformation("Microsoft.EventGrid.SubscriptionValidationEvent response  -- >" + eventGridEvent.Data);

                    var responseData = new SubscriptionValidationResponse
                    {
                        ValidationResponse = eventData.ValidationCode
                    };

                    if (responseData.ValidationResponse != null)
                    {
                        return Ok(responseData);
                    }
                }

                if (string.Equals(eventGridEvent.EventType, "Microsoft.Communication.RecordingFileStatusUpdated", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.LogInformation($"Event type is -- > {eventGridEvent.EventType}");

                    Logger.LogInformation("Microsoft.Communication.RecordingFileStatusUpdated response  -- >" + eventGridEvent.Data);

                    RecordingFileData data = EventSerializer.DeserializeObject<RecordingFileData>(eventGridEvent.Data.ToString());

                    Logger.LogInformation("Start processing recorded media -- >");

                    await ProcessFile(data.recordingStorageInfo.recordingChunks[0].contentLocation,
                        data.recordingStorageInfo.recordingChunks[0].documentId,
                        "mp4",
                        "recording");

                    Logger.LogInformation("Start processing metadata -- >");

                    await ProcessFile(data.recordingStorageInfo.recordingChunks[0].metadataLocation,
                        data.recordingStorageInfo.recordingChunks[0].documentId,
                        "json",
                        "metadata");
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return Json(new { Exception = ex });
            }
        }

        private async Task<bool> ProcessFile(string downloadLocation, string documentId, string fileFormat, string downloadType)
        {
            var recordingDownloadUri = new Uri(downloadLocation);
            var response = DownloadExtentions.DownloadStreamingAsync(callingServerClient, recordingDownloadUri);

            Logger.LogInformation($"Download {downloadType} response  -- >" + response.Result.GetRawResponse());
            Logger.LogInformation($"Save downloaded {downloadType} -- >");

            string filePath = ".\\" + documentId + "." + fileFormat;
            using (Stream streamToReadFrom = response.Result.Value)
            {
                using (Stream streamToWriteTo = System.IO.File.Open(filePath, FileMode.Create))
                {
                    await streamToReadFrom.CopyToAsync(streamToWriteTo);
                    await streamToWriteTo.FlushAsync();
                }
            }

            Logger.LogInformation($"Starting to upload {downloadType} to BlobStorage into container -- > {containerName}");

            var blobStorageHelperInfo = await BlobStorageHelper.UploadFileAsync(blobStorageConnectionString, containerName, filePath, filePath);
            if (blobStorageHelperInfo.Status)
            {
                Logger.LogInformation(blobStorageHelperInfo.Message);
                Logger.LogInformation($"Deleting temporary {downloadType} file being created");

                System.IO.File.Delete(filePath);
            }
            else
            {
                Logger.LogError($"{downloadType} file was not uploaded,{blobStorageHelperInfo.Message}");
            }

            return true;
        }
    }
}