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
        private readonly IEventActionEventHandler<OutboundCallEvent> _outboundCallActionHandler;
        private readonly IEventActionEventHandler<StartRecordingEvent> _startRecordingActionHandler;
        private readonly IEventActionEventHandler<StopRecordingEvent> _stopRecordingActionHandler;
        private readonly IEventActionEventHandler<RecordingStateEvent> _getRecordingStateActionHandler;
        private readonly IEventActionEventHandler<PauseRecordingEvent> _pauseRecordingActionHandler;
        private readonly IEventActionEventHandler<ResumeRecordingEvent> _resumeRecordingActionHandler;

        public ActionsController(ILogger<EventsController> logger,
            EventConverter eventConverter,
            IEventActionEventHandler<OutboundCallEvent> outboundCallActionHandler,
            IEventActionEventHandler<StartRecordingEvent> startRecordingActionHandler,
            IEventActionEventHandler<StopRecordingEvent> stopRecordingActionHandler,
            IEventActionEventHandler<RecordingStateEvent> getRecordingStateActionHandler,
            IEventActionEventHandler<PauseRecordingEvent> pauseRecordingActionHandler,
            IEventActionEventHandler<ResumeRecordingEvent> resumeRecordingActionHandler)

        {
            _logger = logger;
            _eventConverter = eventConverter;
            _outboundCallActionHandler = outboundCallActionHandler;
            _startRecordingActionHandler = startRecordingActionHandler;
            _stopRecordingActionHandler = stopRecordingActionHandler;
            _getRecordingStateActionHandler = getRecordingStateActionHandler;
            _pauseRecordingActionHandler = pauseRecordingActionHandler;
            _resumeRecordingActionHandler = resumeRecordingActionHandler;

        }

        [HttpPost("call", Name = "Outbound_Call")]
        public async Task<ActionResult> OutboundCall([FromBody] OutboundCallEvent outboundCallEvent)
        {
            var response = _outboundCallActionHandler.Handle(outboundCallEvent);
            return new OkResult();
        }

        [HttpPost("startrecording", Name = "Start_Recording")]
        public async Task<ActionResult> StartRecording([FromQuery] StartRecordingEvent startRecordingEvent)
        {
            var response = _startRecordingActionHandler.Handle(startRecordingEvent);
            return new OkResult();
        }
        [HttpPost("pauserecording", Name = "Pause_Recording")]
        public async Task<ActionResult> PauseRecording([FromQuery] PauseRecordingEvent pauseRecordingEvent)
        {
            var response = _pauseRecordingActionHandler.Handle(pauseRecordingEvent);
            return new OkResult();
        }
        [HttpPost("resumerecording", Name = "Resume_Recording")]
        public async Task<ActionResult> ResumeRecording([FromQuery] ResumeRecordingEvent resumeRecordingEvent)
        {
            var response = _resumeRecordingActionHandler.Handle(resumeRecordingEvent);
            return new OkResult();
        }
        [HttpPost("stoprecording", Name = "Stop_Recording")]
        public async Task<ActionResult> StopRecording([FromQuery] StopRecordingEvent stopRecordingEvent)
        {
            var response = _stopRecordingActionHandler.Handle(stopRecordingEvent);
            return new OkResult();
        }

        [HttpPost("getRecordingState", Name = "GetRecording_State")]
        public async Task<ActionResult> getRecordingFile([FromQuery] RecordingStateEvent recordingStateEvent)
        {
            var response = _getRecordingStateActionHandler.Handle(recordingStateEvent);
            return new OkResult();
        }


    }
}
