using Azure.Communication.CallAutomation;
using Microsoft.AspNetCore.Mvc;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace QuickStartApi.Controllers
{
    [Route("/recording")]
    public class CallRecordingController : Controller
    {
        private readonly string blobStorageConnectionString;
        private readonly string containerName;
        private readonly CallAutomationClient callAutomationClient;
        private const string CallRecodingActiveErrorCode = "8553";
        private const string CallRecodingActiveError = "Recording is already in progress, one recording can be active at one time.";
        public ILogger<CallRecordingController> Logger { get; }
        static Dictionary<string, string> recordingData = new Dictionary<string, string>();
        public static string recFileFormat;

        public CallRecordingController(IConfiguration configuration, ILogger<CallRecordingController> logger)
        {
            blobStorageConnectionString = configuration["BlobStorageConnectionString"];
            containerName = configuration["BlobContainerName"];
            callAutomationClient = new CallAutomationClient(configuration["ACSResourceConnectionString"]);
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
                    //Passing RecordingContent initiates recording in specific format. audio/audiovideo
                    //RecordingChannel is used to pass the channel type. mixed/unmixed
                    //RecordingFormat is used to pass the format of the recording. mp4/mp3/wav
                    StartRecordingOptions recordingOptions = new StartRecordingOptions(new ServerCallLocator(serverCallId));
                    var startRecordingResponse = await callAutomationClient.GetCallRecording()
                        .StartRecordingAsync(recordingOptions).ConfigureAwait(false);

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


//********** Replace above API with this if you want to start recording with additional arguments. *************

        /// <summary>
        /// Method to start call recording using given parameters
        /// </summary>
        /// <param name="serverCallId">Conversation id of the call</param>
        /// <param name="recordingContent">Recording content type. audiovideo/audio</param>
        /// <param name="recordingChannel">Recording channel type. mixed/unmixed</param>
        /// <param name="recordingFormat">Recording format type. mp3/mp4/wav</param>
        //[HttpGet]
        //[Route("startRecording")]
        public async Task<IActionResult> StartRecordingAsync(string serverCallId, string recordingContent, string recordingChannel, string recordingFormat)
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
                    StartRecordingOptions recordingOptions = new StartRecordingOptions(new ServerCallLocator(serverCallId));
                    recordingOptions.RecordingChannel = Mapper.RecordingChannelMap.TryGetValue(recordingChannel, out recChannelVal) ? recChannelVal : RecordingChannel.Mixed;
                    recordingOptions.RecordingContent = Mapper.RecordingContentMap.TryGetValue(recordingContent, out recContentVal) ? recContentVal : RecordingContent.AudioVideo;
                    recordingOptions.RecordingFormat = Mapper.RecordingFormatMap.TryGetValue(recordingFormat, out recFormatVal) ? recFormatVal : RecordingFormat.Mp4;
                  
                    var startRecordingResponse = await callAutomationClient.GetCallRecording()
                        .StartRecordingAsync(recordingOptions).ConfigureAwait(false);

                    Logger.LogInformation($"StartRecordingAudioAsync response -- >  {startRecordingResponse.GetRawResponse()}, Recording Id: {startRecordingResponse.Value.RecordingId}");

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
                        recordingId = recordingData[serverCallId];
                    }
                    else
                    {
                        if (!recordingData.ContainsKey(serverCallId))
                        {
                            recordingData[serverCallId] = recordingId;
                        }
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
                        recordingId = recordingData[serverCallId];
                    }
                    else
                    {
                        if (!recordingData.ContainsKey(serverCallId))
                        {
                            recordingData[serverCallId] = recordingId;
                        }
                    }

                    var stopRecording = await callAutomationClient.GetCallRecording().StopRecordingAsync(recordingId).ConfigureAwait(false);
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
        /// Method to get recording state
        /// </summary>
        /// <param name="serverCallId">Conversation id of the call</param>
        /// <param name="recordingId">Recording id of the call</param>
        /// <returns>
        /// CallRecordingProperties
        ///     RecordingState is {active}, in case of active recording
        ///     RecordingState is {inactive}, in case if recording is paused
        /// 404:Recording not found, if recording was stopped or recording id is invalid.
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
                        recordingId = recordingData[serverCallId];
                    }
                    else
                    {
                        if (!recordingData.ContainsKey(serverCallId))
                        {
                            recordingData[serverCallId] = recordingId;
                        }
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

                    Logger.LogInformation("Microsoft.EventGrid.SubscriptionValidationEvent response  -- >" + cloudEvent.Data);

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
                    Logger.LogInformation($"Event type is -- > {cloudEvent.EventType}");

                    Logger.LogInformation("Microsoft.Communication.RecordingFileStatusUpdated response  -- >" + cloudEvent.Data);

                    var eventData = cloudEvent.Data.ToObjectFromJson<AcsRecordingFileStatusUpdatedEventData>();

                    Logger.LogInformation("Start processing metadata -- >");

                    await ProcessFile(eventData.RecordingStorageInfo.RecordingChunks[0].MetadataLocation,
                        eventData.RecordingStorageInfo.RecordingChunks[0].DocumentId,
                        FileFormat.Json,
                        FileDownloadType.Metadata);

                    Logger.LogInformation("Start processing recorded media -- >");

                    await ProcessFile(eventData.RecordingStorageInfo.RecordingChunks[0].ContentLocation,
                        eventData.RecordingStorageInfo.RecordingChunks[0].DocumentId,
                        string.IsNullOrWhiteSpace(recFileFormat) ? FileFormat.Mp4 : recFileFormat,
                        FileDownloadType.Recording);
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
            var response = await callAutomationClient.GetCallRecording().DownloadStreamingAsync(recordingDownloadUri);

            Logger.LogInformation($"Download {downloadType} response  -- >" + response.GetRawResponse());
            Logger.LogInformation($"Save downloaded {downloadType} -- >");

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
                recFileFormat = deserializedFilePath.recordingInfo.format;

                Logger.LogInformation($"Recording File Format is -- > {recFileFormat}");
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