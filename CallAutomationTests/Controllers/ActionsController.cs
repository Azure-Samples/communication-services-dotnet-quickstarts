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
        private readonly IEventActionsEventHandler<StopRecordingEvent> _StopRecordingActionHandler;

        public ActionsController(ILogger<EventsController> logger,
            EventConverter eventConverter,
            IEventActionsEventHandler<OutboundCallEvent> outboundCallActionHandler,
            IEventActionsEventHandler<StartRecordingEvent> startRecordingActionHandler,
            IEventActionsEventHandler<StopRecordingEvent> StopRecordingActionHandler )
            
        {
            _logger = logger;
            _eventConverter = eventConverter;
            _outboundCallActionHandler = outboundCallActionHandler;
            _startRecordingActionHandler = startRecordingActionHandler;
            _StopRecordingActionHandler = StopRecordingActionHandler;

        }

        [HttpPost("call", Name = "outbound_call")]
        public async Task<ActionResult> OutboundCall([FromBody] OutboundCallEvent outboundCallEvent)
        {
            var response = _outboundCallActionHandler.Handle(outboundCallEvent);
            return new OkResult();
        }

        [HttpGet("recording/start", Name = "Start_Recording")]
        public async Task<ActionResult> StartRecording([FromQuery] StartRecordingEvent startRecordingEvent)
        {
            var response = _startRecordingActionHandler.Handle(startRecordingEvent);
            return new OkResult();
        }
        [HttpPost("recording/stop", Name = "Stop_Recording")]
        public async Task<ActionResult> StopRecording([FromBody] StopRecordingEvent StopRecordingEvent)
        {
            var response = _StopRecordingActionHandler.Handle(StopRecordingEvent);
            return new OkResult();
        }


    }
}
