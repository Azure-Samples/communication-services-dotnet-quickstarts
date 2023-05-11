using CallAutomation.Scenarios.Handlers;
using CallAutomation.Scenarios.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CallAutomation.Scenarios.Controllers
{
    [ApiController]
    public class ActionsController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly EventConverter _eventConverter;
        private readonly IEventActionsEventHandler<OutboundCallEvent> _outboundCallActionHandler;
        private readonly IEventActionsEventHandler<StartRecordingEvent> _startRecordingActionHandler;

        public ActionsController(ILogger<EventsController> logger,
            EventConverter eventConverter,
            IEventActionsEventHandler<OutboundCallEvent> outboundCallActionHandler)
        {
            _logger = logger;
            _eventConverter = eventConverter;
            _outboundCallActionHandler = outboundCallActionHandler;
        }

        [HttpPost("call", Name = "outbound_call")]
        public async Task<ActionResult> OutboundCall([FromBody] OutboundCallEvent outboundCallEvent)
        {
            var response = _outboundCallActionHandler.Handle(outboundCallEvent);
            return new OkResult();
        }

        [HttpPost("recording/start", Name = "outbound_call")]
        public async Task<ActionResult> StartRecording([FromBody] StartRecordingEvent startRecordingEvent)
        {
            var response = _startRecordingActionHandler.Handle(startRecordingEvent);
            return new OkResult();
        }

    }
}
