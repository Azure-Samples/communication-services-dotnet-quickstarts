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
       // private readonly IEventActionsEventHandler<GetRecordingFileEvent> _GetRecordingFileActionHandler;
        private readonly IEventActionsEventHandler<PauseRecordingEvent> _PauseRecordingActionHandler;
        private readonly IEventActionsEventHandler<ResumeRecordingEvent> _ResumeRecordingActionHandler;

        public ActionsController(ILogger<EventsController> logger,
            EventConverter eventConverter,
            IEventActionsEventHandler<OutboundCallEvent> outboundCallActionHandler,
            IEventActionsEventHandler<StartRecordingEvent> startRecordingActionHandler,
            IEventActionsEventHandler<StopRecordingEvent> StopRecordingActionHandler,
             //IEventActionsEventHandler<GetRecordingFileEvent> GetRecordingFileActionHandler,
             IEventActionsEventHandler<PauseRecordingEvent> PauseRecordingActionHandler,
             IEventActionsEventHandler<ResumeRecordingEvent> ResumeRecordingActionHandler)
            
        {
            _logger = logger;
            _eventConverter = eventConverter;
            _outboundCallActionHandler = outboundCallActionHandler;
            _startRecordingActionHandler = startRecordingActionHandler;
            _StopRecordingActionHandler = StopRecordingActionHandler;
           // _GetRecordingFileActionHandler = GetRecordingFileActionHandler;
            _PauseRecordingActionHandler = PauseRecordingActionHandler;
            _ResumeRecordingActionHandler = ResumeRecordingActionHandler;

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
        [HttpGet("recording/pause", Name = "pause_Recording")]
        public async Task<ActionResult> PauseRecording([FromQuery] PauseRecordingEvent pauseRecordingEvent)
        {
            var response = _PauseRecordingActionHandler.Handle(pauseRecordingEvent);
            return new OkResult();
        }
        [HttpGet("recording/Resume", Name = "Resume_Recording")]
        public async Task<ActionResult> ResumeRecording([FromQuery] ResumeRecordingEvent resumeRecordingEvent)
        {
            var response = _ResumeRecordingActionHandler.Handle(resumeRecordingEvent);
            return new OkResult();
        }
        [HttpGet("recording/stop", Name = "Stop_Recording")]
        public async Task<ActionResult> StopRecording([FromQuery] StopRecordingEvent StopRecordingEvent)
        {
            var response = _StopRecordingActionHandler.Handle(StopRecordingEvent);
            return new OkResult();
        }       

        //[HttpPost("getRecording/File", Name = "getRecording_File")]
        //public async Task<ActionResult> getRecordingFile([FromBody] GetRecordingFileEvent getRecordingFileEvent)
        //{
        //    var response = _GetRecordingFileActionHandler.Handle(getRecordingFileEvent);
        //    return new OkResult();
        //}


    }
}
