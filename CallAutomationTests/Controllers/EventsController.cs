// © Microsoft Corporation. All rights reserved.

using Azure.Messaging.EventGrid;
using CallAutomation.Scenarios.Handlers;
using CallAutomation.Scenarios.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CallAutomation.Scenarios.Controllers
{
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly EventConverter _eventConverter;
        private readonly IEventGridEventHandler<IncomingCallEvent> _incomingCallEventHandler;
        private readonly IEventGridEventHandler<RecordingFileStatusUpdatedEvent> _recordingFileStatusUpdatedEventHandler;

        public EventsController(ILogger<EventsController> logger,
            EventConverter eventConverter,
            IEventGridEventHandler<IncomingCallEvent> incomingCallEventHandler,
            IEventGridEventHandler<RecordingFileStatusUpdatedEvent> recordingFileStatusUpdatedEventHandler)

        {
            _logger = logger;
            _eventConverter = eventConverter;
            _incomingCallEventHandler = incomingCallEventHandler;
            _recordingFileStatusUpdatedEventHandler = recordingFileStatusUpdatedEventHandler;

        }

        [HttpPost("/events", Name = "Receive_ACS_Events")]
        //[Authorize(EventGridAuthHandler.EventGridAuthenticationScheme)]
        public async Task<ActionResult> Handle([FromBody] EventGridEvent[] eventGridEvents)
        {
            Request.Headers.TryGetValue("Aeg-Event-Type", out var eventType);

            if (eventType.ToString().Equals("SubscriptionValidation"))
            {
                var response = _eventConverter.Convert(eventGridEvents[0], true);
                return new OkObjectResult(response);
            }

            _logger.LogDebug($"Received {eventGridEvents.Length} EventGridEvents");
            foreach (var eventGridEvent in eventGridEvents)
            {
                try
                {
                    var eventData = _eventConverter.Convert(eventGridEvent);
                    if (eventData == null) continue;

                    switch (eventData)
                    {
                        case IncomingCallEvent incomingCallEvent:
                            await _incomingCallEventHandler.Handle(incomingCallEvent);
                            break;

                        case RecordingFileStatusUpdatedEvent recordingFileStatusUpdatedEvent:
                            await _recordingFileStatusUpdatedEventHandler.Handle(recordingFileStatusUpdatedEvent);
                            break;

                        default: throw new ArgumentException($"{eventData.GetType().ToString()} : not handled.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to send event {ex}");
                }
            }

            return new OkResult();
        }
    }
}
