using CallAutomation.Scenarios.Handlers;
using CallAutomation.Scenarios.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CallAutomation.Scenarios.Controllers
{
    [ApiController]
    public class ActionsController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly EventConverter _eventConverter;
        private readonly IEventActionEventHandler<OutboundCallContext> _outboundCallActionHandler;
        private readonly IEventActionEventHandler<RecordingContext> _recordingActionHandler;

        public ActionsController(ILogger<EventsController> logger,
            EventConverter eventConverter,
            IEventActionEventHandler<OutboundCallContext> outboundCallActionHandler,
            IEventActionEventHandler<RecordingContext> recordingActionHandler)

        {
            _logger = logger;
            _eventConverter = eventConverter;
            _outboundCallActionHandler = outboundCallActionHandler;
            _recordingActionHandler = recordingActionHandler;

        }

        [HttpPost("call", Name = "Outbound_Call")]
        public async Task<ActionResult> OutboundCall([FromBody] OutboundCallContext outboundCallContext)
        {
            await _outboundCallActionHandler.Handle(outboundCallContext);
            return new OkResult();
        }

        [HttpPost("recordings", Name = "Start_Recording")]
        public async Task<ActionResult> StartRecordingAsync([FromBody] RecordingContext recordingContext)
        {
            await _recordingActionHandler.Handle(recordingContext);
            return new OkResult();
        }
        [HttpPost("recordings{recordingId}:pause", Name = "Pause_Recording")]
        public async Task<ActionResult> PauseRecording([FromRoute] string recordingId)
        {
            await _recordingActionHandler.Handle("PauseRecording", recordingId);
            return new OkResult();
        }
        [HttpPost("recordings{recordingId}:resume", Name = "Resume_Recording")]
        public async Task<ActionResult> ResumeRecordingAsync([FromRoute] string recordingId)
        {
            await _recordingActionHandler.Handle("ResumeRecording", recordingId);
            return new OkResult();
        }
        [HttpDelete("recordings/{recordingId}", Name = "Stop_Recording")]
        public async Task<ActionResult> StopRecordingAsync([FromRoute] string recordingId)
        {
            await _recordingActionHandler.Handle("StopRecording", recordingId);
            return new OkResult();
        }

        [HttpPost("getRecordingState/{recordingId}", Name = "GetRecording_State")]
        public async Task<ActionResult> GetRecordingStateAsync([FromRoute] string recordingId)
        {
            await _recordingActionHandler.Handle("GetRecordingState", recordingId);
            return new OkResult();
        }
    }
}
