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
            CallAutomationClient callAutomationClient)
        {
            _logger = logger;
            _eventProcessor = callAutomationClient.GetEventProcessor();
        }

        [HttpPost]
        public IActionResult CallbackEvent([FromBody] CloudEvent[] cloudEvents)
        {
            // Prase incoming event into solid base class of CallAutomationEvent.
            // This is useful when we want to access the properties of the event easily, such as CallConnectionId.
            // We are using this parsed event to log CallconnectionId of the event here.
            CallAutomationEventBase? parsedBaseEvent = CallAutomationEventParser.ParseMany(cloudEvents).FirstOrDefault();            
            _logger.LogInformation($"Event Recieved. CallConnectionId[{parsedBaseEvent?.CallConnectionId}], Type Name[{parsedBaseEvent?.GetType().Name}]");

            // Utilizing evnetProcessor here to easily handle mid-call call automation events.
            // process event into processor, so events could be handled in CallingModule.
            _eventProcessor.ProcessEvents(cloudEvents);
            return Ok();
        }
    }
}
