using Azure.Communication.CallAutomation;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using CallAutomation.Scenarios.Handlers;
using CallAutomation.Scenarios.Interfaces;
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
                   // var stopRecording = await callAutomationClient.GetCallRecording().StopRecordingAsync(recordingId).ConfigureAwait(false);
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
                _logger.LogError(ex, "Start Recording failed unexpectedly");
                throw;
            }
        }
    }
}
