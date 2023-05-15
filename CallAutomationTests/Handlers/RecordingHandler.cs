using Azure.Communication.CallAutomation;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using CallAutomation.Scenarios.Handlers;
using CallAutomation.Scenarios.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace CallAutomation.Scenarios
{
    public class RecordingHandler : 
        IEventActionsEventHandler<StartRecordingEvent>,IEventActionsEventHandler<StopRecordingEvent>
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<CallEventHandler> _logger;
        private readonly ICallAutomationService _callAutomationService;
        private readonly ICallContextService _callContextService;
        static Dictionary<string, string> recordingData = new Dictionary<string, string>();
        public static string recFileFormat;
        public RecordingHandler(
            IConfiguration configuration,
            ILogger<CallEventHandler> logger,
            ICallAutomationService callAutomationService,
            ICallContextService callContextService)

        {
            _configuration = configuration;
            _logger = logger;
            _callAutomationService = callAutomationService;
            _callContextService = callContextService;
            
        }

        public async Task Handle(StartRecordingEvent startRecordingEvent)
        {
            try
            {
                _logger.LogInformation("IncomingCallEvent received");
                string serverCallId = startRecordingEvent.serverCallId;
                //await callContextService.StartRecordingAsync();
                if (!string.IsNullOrEmpty(serverCallId))
                {  
                   var startRecordingResponse = await _callAutomationService.StartRecordingAsync(serverCallId);
                    Logger.LogInformation($"StartRecordingAsync response -- >  {startRecordingResponse.RecordingState}, Recording Id: {startRecordingResponse.RecordingId}");
                    var recordingId = startRecordingResponse.RecordingId;
                    if (!recordingData.ContainsKey(serverCallId))
                    {
                        recordingData.Add(serverCallId, string.Empty);
                    }
                    recordingData[serverCallId] = recordingId;

                    //return Json(recordingId);
                }
                else
                {
                    Logger.LogInformation($"serverCallId is invalid : {serverCallId}");
                    // return JsonException(new { Message = "serverCallId is invalid" });
                }


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Start Recording failed unexpectedly");
                throw;
            }
        }

        public async Task Handle(StopRecordingEvent StopRecordingEvent)
        {
            try
            {
                _logger.LogInformation("IncomingCallEvent received");
                string serverCallId = StopRecordingEvent.serverCallId;
                string recordingId = StopRecordingEvent.recordingId;
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

                    var stopRecording = await _callAutomationService.StopRecordingAsync(recordingId);                 
                    Logger.LogInformation($"StopRecordingAsync response -- > {stopRecording}");
                    if (recordingData.ContainsKey(serverCallId))
                    {
                        recordingData.Remove(serverCallId);
                    }
                   // return Ok();
                }
                else
                {
                   // return BadRequest(new { Message = "serverCallId is invalid" });
                }


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Start Recording failed unexpectedly");
                throw;
            }
        }

        public async Task<Object> Handle(GetRecordingFileEvent getRecordingFileEvent)
        {
            try
            {

                var httpContent = new BinaryData(getRecordingFileEvent.request.ToString()).ToStream();
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
                        //return Ok(responseData);
                    }
                }

                if (cloudEvent.EventType == SystemEventNames.AcsRecordingFileStatusUpdated)
                {
                    Logger.LogInformation($"Event type is -- > {cloudEvent.EventType}");

                    Logger.LogInformation("Microsoft.Communication.RecordingFileStatusUpdated response  -- >" + cloudEvent.Data);

                    var eventData = cloudEvent.Data.ToObjectFromJson<AcsRecordingFileStatusUpdatedEventData>();

                    Logger.LogInformation("Start processing metadata -- >");

                    await _callAutomationService.ProcessFile(eventData.RecordingStorageInfo.RecordingChunks[0].MetadataLocation,
                        eventData.RecordingStorageInfo.RecordingChunks[0].DocumentId,
                        FileFormat.Json,
                        FileDownloadType.Metadata);

                    Logger.LogInformation("Start processing recorded media -- >");

                    await _callAutomationService.ProcessFile(eventData.RecordingStorageInfo.RecordingChunks[0].ContentLocation,
                        eventData.RecordingStorageInfo.RecordingChunks[0].DocumentId,
                        string.IsNullOrWhiteSpace(recFileFormat) ? FileFormat.Mp4 : recFileFormat,
                        FileDownloadType.Recording);
                }

                return "OK";


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get Recording File failed unexpectedly");
                throw;
            }
        }


    }
}
