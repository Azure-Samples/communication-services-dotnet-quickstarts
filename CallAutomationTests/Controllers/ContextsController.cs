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

        [HttpGet("recordings/{recordingId}", Name = "GetRecording_Context")]
        public IActionResult GetRecordingContext([FromRoute] string recordingId)
        {
            var recordingContext = _callContextService.GetRecordingContext(recordingId);
            return new JsonResult(recordingContext);
        }

        [HttpPatch("recordings/{recordingId}", Name = "SetRecording_Context")]
        public IActionResult SetRecordingContext([FromBody] RecordingContext recordingContext, [FromRoute] string recordingId)
        {
            _callContextService.SetRecordingContext(recordingId, recordingContext);
            return new OkResult();
        }
    }
}
