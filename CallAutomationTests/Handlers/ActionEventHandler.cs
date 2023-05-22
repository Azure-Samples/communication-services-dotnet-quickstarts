using Azure.Communication.CallAutomation;
using Azure.Messaging.EventGrid;
using CallAutomation.Scenarios.Handlers;
using CallAutomation.Scenarios.Interfaces;
using System.Runtime.Versioning;

namespace CallAutomation.Scenarios
{
    public class ActionEventHandler :
        IEventActionEventHandler<RecordingContext>,
        IEventActionEventHandler<OutboundCallContext>
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<CallEventHandler> _logger;
        private readonly ICallAutomationService _callAutomationService;
        private readonly ICallContextService _callContextService;
        public ActionEventHandler(
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

        public async Task Handle(OutboundCallContext outboundCallContext)
        {
            _logger.LogInformation("Outbound call received");

            try
            {
                var targetId = (outboundCallContext.TargetId ?? _configuration["targetId"]) ?? throw new ArgumentNullException($"Target Id is null : {outboundCallContext}");
                var createCallResult = await _callAutomationService.CreateCallAsync(targetId);
                var callConnectionId = createCallResult.CallConnectionProperties.CallConnectionId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbound call failed unexpectedly");
                throw;
            }
        }


        public async Task Handle(RecordingContext recordingContext)
        {
            try
            {
                var serverCallId = recordingContext.ServerCallId ?? throw new ArgumentNullException($"ServerCallId is null: {recordingContext}");
                var startRecordingResponse = await _callAutomationService.StartRecordingAsync(serverCallId);
                _callContextService.SetRecordingContext(startRecordingResponse.RecordingId,
                    new RecordingContext()
                    {
                        StartTime = DateTime.UtcNow,
                        RecordingId = startRecordingResponse.RecordingId,
                        ServerCallId = serverCallId
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Start Recording failed unexpectedly");
                throw;
            }
        }

        public RecordingContext Handle (string recordingId)
        {
            RecordingContext context = _callContextService.GetRecordingContext(recordingId);
            return context;
        }

        public async Task Handle(string actionName, string recordingId)
        {
            switch (actionName)
            {
                case "PauseRecording":
                    await _callAutomationService.PauseRecordingAsync(recordingId);
                    break;
                case "ResumeRecording":
                    await _callAutomationService.ResumeRecordingAsync(recordingId);
                    break;
                case "GetRecordingState":
                    await _callAutomationService.GetRecordingStateAsync(recordingId);
                    break;
                case "StopRecording":
                    await _callAutomationService.StopRecordingAsync(recordingId);
                    break;
            }
        }
    }
}
