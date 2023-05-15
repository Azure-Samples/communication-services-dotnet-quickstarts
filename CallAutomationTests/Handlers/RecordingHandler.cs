using Azure.Messaging.EventGrid;
using CallAutomation.Scenarios.Handlers;
using CallAutomation.Scenarios.Interfaces;

namespace CallAutomation.Scenarios
{
    public class RecordingHandler :
        IEventActionEventHandler<StartRecordingEvent>, IEventActionEventHandler<StopRecordingEvent>,
        IEventActionEventHandler<RecordingFileStatusUpdatedEvent>, IEventActionEventHandler<PauseRecordingEvent>, IEventActionEventHandler<ResumeRecordingEvent>
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
                var serverCallId = startRecordingEvent.serverCallId ?? throw new ArgumentNullException($"ServerCallId is null: {startRecordingEvent}");
                
                var startRecordingResponse = await _callAutomationService.StartRecordingAsync(serverCallId);
                Logger.LogInformation($"StartRecordingAsync response -- >  {startRecordingResponse.RecordingState}, Recording Id: {startRecordingResponse.RecordingId}");
                var recordingId = startRecordingResponse.RecordingId;
                if (!recordingData.ContainsKey(serverCallId))
                {
                    recordingData.Add(serverCallId, string.Empty);
                }
                recordingData[serverCallId] = recordingId;

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
                _logger.LogError(ex, "Stop Recording failed unexpectedly");
                throw;
            }
        }

        public async Task Handle(PauseRecordingEvent pauseRecordingEvent)
        {
            try
            {
                _logger.LogInformation("pauseRecordingEvent received");
                string serverCallId = pauseRecordingEvent.serverCallId;
                string recordingId = pauseRecordingEvent.recordingId;
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

                    var PauseRecording = await _callAutomationService.PauseRecordingAsync(recordingId);
                    Logger.LogInformation($"PauseRecordingAsync response -- > {PauseRecording}");

                    // return Ok();
                }
                else
                {
                    // return BadRequest(new { Message = "serverCallId is invalid" });
                }


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Pause Recording failed unexpectedly");
                throw;
            }
        }
        public async Task Handle(ResumeRecordingEvent resumeRecordingEvent)
        {
            try
            {
                _logger.LogInformation("ResumeRecordingEvent received");
                string serverCallId = resumeRecordingEvent.serverCallId;
                string recordingId = resumeRecordingEvent.recordingId;
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

                    var ResumeRecording = await _callAutomationService.ResumeRecordingAsync(recordingId);
                    Logger.LogInformation($"ResumeRecordingAsync response -- > {ResumeRecording}");

                    // return Ok();
                }
                else
                {
                    // return BadRequest(new { Message = "serverCallId is invalid" });
                }


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Resume Recording failed unexpectedly");
                throw;
            }
        }

        public async Task Handle(RecordingFileStatusUpdatedEvent recordingFileStatusUpdatedEvent)
        {
            try
            {
                var eventData = recordingFileStatusUpdatedEvent;

                Logger.LogInformation("Microsoft.Communication.RecordingFileStatusUpdated response  -- >" + eventData);

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get Recording File failed unexpectedly");
                throw;
            }
        }


    }
}
