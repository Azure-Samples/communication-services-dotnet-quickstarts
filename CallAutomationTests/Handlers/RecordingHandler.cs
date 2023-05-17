using Azure.Messaging.EventGrid;
using CallAutomation.Scenarios.Handlers;
using CallAutomation.Scenarios.Interfaces;

namespace CallAutomation.Scenarios
{
    public class RecordingHandler :
        IEventActionEventHandler<StartRecordingEvent>,
        IEventActionEventHandler<StopRecordingEvent>,
        IEventActionEventHandler<RecordingStateEvent>,
        IEventActionEventHandler<PauseRecordingEvent>,
        IEventActionEventHandler<ResumeRecordingEvent>
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

                _callContextService.SetRecordingContext(serverCallId, new RecordingContext() { StartTime = DateTime.UtcNow, RecordingId = recordingId });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Start Recording failed unexpectedly");
                throw;
            }
        }

        public async Task Handle(StopRecordingEvent stopRecordingEvent)
        {
            try
            {
                _logger.LogInformation("StopRecordingAsync received");               
                string recordingId = stopRecordingEvent.recordingId;           
                    
                        var stopRecording = await _callAutomationService.StopRecordingAsync(recordingId);
                        Logger.LogInformation($"StopRecordingAsync response -- > {stopRecording}");                   
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
                string recordingId = pauseRecordingEvent.recordingId;
                
                        var PauseRecording = await _callAutomationService.PauseRecordingAsync(recordingId);
                        Logger.LogInformation($"PauseRecordingAsync response -- > {PauseRecording}");
                     
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
                string recordingId = resumeRecordingEvent.recordingId;
                        var ResumeRecording = await _callAutomationService.ResumeRecordingAsync(recordingId);
                        Logger.LogInformation($"ResumeRecordingAsync response -- > {ResumeRecording}");
                    
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Resume Recording failed unexpectedly");
                throw;
            }
        }

        public async Task Handle(RecordingStateEvent recordingStateEvent)
        {
            try
            {
                _logger.LogInformation("GetRecordingStateEvent received");               
                string recordingId = recordingStateEvent.recordingId; 
                        var PauseRecording = await _callAutomationService.GetRecordingStateAsync(recordingId);
                        Logger.LogInformation($"PauseRecordingAsync response -- > {PauseRecording}");
                                    
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get Recording state event failed unexpectedly");
                throw;
            }
        }


    }
}
