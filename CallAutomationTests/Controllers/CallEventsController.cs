using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using CallAutomation.Scenarios.Interfaces;
using CallAutomation.Scenarios.Handlers;

namespace CallAutomation.Scenarios.Controllers
{
    [Route("/callbacks")]
    [ApiController]
    public class CallEventsController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IEventCloudEventHandler<AddParticipantFailed> _addParticipantFailedEventHandler;
        private readonly IEventCloudEventHandler<AddParticipantSucceeded> _addParticipantSucceededEventHandler;
        private readonly IEventCloudEventHandler<CallConnected> _callConnectedEventHandler;
        private readonly IEventCloudEventHandler<CallDisconnected> _callDisconnectedEventHandler;
        private readonly IEventCloudEventHandler<CallTransferAccepted> _callTransferAcceptedEventHandler;
        private readonly IEventCloudEventHandler<CallTransferFailed> _callTransferFailedEventHandler;
        private readonly IEventCloudEventHandler<ParticipantsUpdated> _participantsUpdatedEventHandler;
        private readonly IEventCloudEventHandler<PlayCompleted> _playCompletedEventHandler;
        private readonly IEventCloudEventHandler<PlayFailed> _playFailedEventHandler;
        private readonly IEventCloudEventHandler<PlayCanceled> _playCanceledEventHandler;
        private readonly IEventCloudEventHandler<RecognizeCompleted> _recognizeCompletedEventHandler;
        private readonly IEventCloudEventHandler<RecognizeFailed> _recognizeFailedEventHandler;
        private readonly IEventCloudEventHandler<RecognizeCanceled> _recognizeCanceledEventHandler;
        private readonly IEventCloudEventHandler<RecordingStateChanged> _recordingStateChangedEventHandler;
        private readonly ICallContextService _callContextService;
        private readonly ICallAutomationService _callAutomationService;

        public CallEventsController(ILogger<EventsController> logger,
            IEventCloudEventHandler<AddParticipantFailed> addParticipantFailedEventHandler,
            IEventCloudEventHandler<AddParticipantSucceeded> addParticipantSucceededEventHandler,
            IEventCloudEventHandler<CallConnected> callConnectedEventHandler,
            IEventCloudEventHandler<CallDisconnected> callDisconnectedEventHandler,
            IEventCloudEventHandler<CallTransferAccepted> callTransferAcceptedEventHandler,
            IEventCloudEventHandler<CallTransferFailed> callTransferFailedEventHandler,
            IEventCloudEventHandler<ParticipantsUpdated> participantsUpdatedEventHandler,
            IEventCloudEventHandler<PlayCompleted> playCompletedEventHandler,
            IEventCloudEventHandler<PlayFailed> playFailedEventHandler,
            IEventCloudEventHandler<PlayCanceled> playCanceledEventHandler,
            IEventCloudEventHandler<RecognizeCompleted> recognizeCompletedEventHandler,
            IEventCloudEventHandler<RecognizeFailed> recognizeFailedEventHandler,
            IEventCloudEventHandler<RecognizeCanceled> recognizeCanceledEventHandler,
            IEventCloudEventHandler<RecordingStateChanged> recordingStateChangedEventHandler,
            ICallContextService callContextService,
            ICallAutomationService callAutomationService
            )
        {
            _logger = logger;
            _addParticipantFailedEventHandler = addParticipantFailedEventHandler;
            _addParticipantSucceededEventHandler = addParticipantSucceededEventHandler;
            _callConnectedEventHandler = callConnectedEventHandler;
            _callDisconnectedEventHandler = callDisconnectedEventHandler;
            _callTransferAcceptedEventHandler = callTransferAcceptedEventHandler;
            _callTransferFailedEventHandler = callTransferFailedEventHandler;
            _participantsUpdatedEventHandler = participantsUpdatedEventHandler;
            _playCompletedEventHandler = playCompletedEventHandler;
            _playFailedEventHandler = playFailedEventHandler;
            _playCanceledEventHandler = playCanceledEventHandler;
            _callConnectedEventHandler = callConnectedEventHandler;
            _callDisconnectedEventHandler = callDisconnectedEventHandler;
            _recognizeCanceledEventHandler = recognizeCanceledEventHandler;
            _recognizeCompletedEventHandler = recognizeCompletedEventHandler;
            _recognizeFailedEventHandler = recognizeFailedEventHandler;
            _recordingStateChangedEventHandler = recordingStateChangedEventHandler;
            _callContextService = callContextService;
            _callAutomationService = callAutomationService;
        }

        [HttpPost("/callbacks/{contextId}", Name = "CallBack_Events")]
        [Authorize(EventGridAuthHandler.EventGridAuthenticationScheme)]
        public async Task<ActionResult> Handle([FromBody] CloudEvent[] cloudEvents, [FromRoute] string contextId, [FromQuery(Name = "CallerId")] string callerId)
        {
            _logger.LogDebug($"Received {cloudEvents.Length} CloudEvents");

            cloudEvents = cloudEvents.Where(e => e.Type != "Microsoft.Communication.ContinuousDtmfRecognitionToneReceived").ToArray();
            if (!cloudEvents.Any()) return new OkResult();

            _callAutomationService.ProcessEvents(cloudEvents);

            foreach (var cloudEvent in cloudEvents)
            {
                try
                {
                    var parsedCloudEvent = CallAutomationEventParser.Parse(cloudEvent);
                    _logger.LogInformation($"Event received: {parsedCloudEvent.GetType()}");

                    switch (parsedCloudEvent)
                    {
                        case CallConnected callConnected:
                            if (callConnected.OperationContext == Constants.OperationContext.ScheduledCallbackDialout)
                            {
                                _callContextService.SetOriginatingJobId(callConnected.CallConnectionId, contextId);
                            }
                            await _callConnectedEventHandler.Handle(callConnected, callerId);
                            break;

                        case CallDisconnected callDisconnected:
                            await _callDisconnectedEventHandler.Handle(callDisconnected, callerId);
                            break;

                        case RecognizeCompleted recognizeCompleted:
                            await _recognizeCompletedEventHandler.Handle(recognizeCompleted, callerId);
                            break;

                        case RecognizeFailed recognizeFailed:
                            await _recognizeFailedEventHandler.Handle(recognizeFailed, callerId);
                            break;

                        case RecognizeCanceled recognizeCanceled:
                            await _recognizeCanceledEventHandler.Handle(recognizeCanceled, callerId);
                            break;

                        case PlayCompleted playCompleted:
                            await _playCompletedEventHandler.Handle(playCompleted, callerId);
                            break;

                        case AddParticipantSucceeded addParticipantSucceeded:
                            await _addParticipantSucceededEventHandler.Handle(addParticipantSucceeded, callerId);
                            break;

                        case AddParticipantFailed addParticipantFailed:
                            await _addParticipantFailedEventHandler.Handle(addParticipantFailed, callerId);
                            break;

                        case ParticipantsUpdated participantsUpdated:
                            await _participantsUpdatedEventHandler.Handle(participantsUpdated, callerId);
                            break;

                        case PlayCanceled playCanceled:
                            await _playCanceledEventHandler.Handle(playCanceled, callerId);
                            break;

                        case PlayFailed playFailed:
                            await _playFailedEventHandler.Handle(playFailed, callerId);
                            break;

                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to handle event {ex}");
                    return new BadRequestResult();
                }
            }
            return new OkResult();
        }
    }
}
