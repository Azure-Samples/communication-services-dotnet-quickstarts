using Azure.Communication.CallAutomation;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using CallAutomation.Scenarios.Handlers;
using CallAutomation.Scenarios.Interfaces;
using Newtonsoft.Json;

namespace CallAutomation.Scenarios
{
    public class RecordingHandler : 
        IEventActionsEventHandler<StartRecordingEvent>
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<CallEventHandler> _logger;
        private readonly ICallAutomationService _callAutomationService;
        private readonly ICallContextService _callContextService;

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

                //await callContextService.StartRecordingAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Start Recording failed unexpectedly");
                throw;
            }
        }
    }
}
