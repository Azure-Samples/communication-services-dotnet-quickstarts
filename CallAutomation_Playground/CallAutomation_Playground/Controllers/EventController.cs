using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Microsoft.AspNetCore.Mvc;

namespace CallAutomation_Playground.Controllers
{
    /// <summary>
    /// This is controller where it will recieve interim events from Call automation service.
    /// We are utilizing event processor, this will handle events and relay to our business logic.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class EventController : ControllerBase
    {
        private readonly ILogger<EventController> _logger;
        private readonly CallAutomationEventProcessor _eventProcessor;

        public EventController(
            ILogger<EventController> logger, 
            CallAutomationEventProcessor eventProcessor)
        {
            _logger = logger;
            _eventProcessor = eventProcessor;
        }

        [HttpPost]
        public IActionResult CallbackEvent([FromBody] CloudEvent[] cloudEvents)
        {
            _logger.LogInformation($"Event Recieved. Type[{cloudEvents.FirstOrDefault()?.Type}]");

            // process event into processor, so events could be handled in CallingModule
            _eventProcessor.ProcessEvents(cloudEvents);
            return Ok();
        }
    }
}
