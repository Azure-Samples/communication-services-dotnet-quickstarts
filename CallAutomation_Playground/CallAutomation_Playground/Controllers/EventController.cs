using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CallAutomation_Playground.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventController : ControllerBase
    {
        private readonly CallAutomationEventProcessor _eventProcessor;

        public EventController(CallAutomationEventProcessor eventProcessor)
        {
            _eventProcessor = eventProcessor;
        }

        [HttpPost]
        public async Task<IActionResult> CallbackEvent([FromBody] CloudEvent[] cloudEvents)
        {
            _eventProcessor.ProcessEvents(cloudEvents);
            return Ok();
        }
    }
}
