using CallAutomation.Scenarios.Handlers;
using CallAutomation.Scenarios.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CallAutomation.Scenarios.Controllers
{
    [ApiController]
    public class ContextsController : ControllerBase
    {
        private readonly ICallContextService _callContextService;
        public ContextsController(ICallContextService callContextService)
        {
            _callContextService = callContextService;
        }

        [HttpGet("recordings/{serverCallId}", Name = "GetRecording_Context")]
        public IActionResult GetRecordingContext([FromRoute] string serverCallId)
        {
            var recordingContext = _callContextService.GetRecordingContext(serverCallId);
            return new JsonResult(recordingContext);
        }

        [HttpPatch("recordings/{serverCallId}", Name = "SetRecording_Context")]
        public IActionResult SetRecordingContext([FromBody] RecordingContext recordingContext, [FromRoute] string serverCallId)
        {
            _callContextService.SetRecordingContext(serverCallId, recordingContext);
            return new OkResult();
        }
    }
}
