// © Microsoft Corporation. All rights reserved.

using Azure.Communication.CallAutomation;
using Azure.Messaging.EventGrid;
using CallAutomation.Scenarios.Handlers;
using CallAutomation.Scenarios.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CallAutomation.Scenarios.Controllers
{
    [Route("/events")]
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

        [HttpPost(Name = "Receive_ACS_Events")]
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

                    if (eventData.GetType() == typeof(IncomingCallEvent))
                    {
                        await _incomingCallEventHandler.Handle(eventData as IncomingCallEvent);
                    }
                    if (eventData.GetType() == typeof(RecordingFileStatusUpdatedEvent))
                    {
                        await _recordingFileStatusUpdatedEventHandler.Handle(eventData as RecordingFileStatusUpdatedEvent);
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
