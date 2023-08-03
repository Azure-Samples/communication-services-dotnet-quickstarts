using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using CallAutomation_Playground.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CallAutomation_Playground.Controllers
{
    /// <summary>
    /// This is the controller for recieving an inbound call.
    /// See README files how to setup incoming call and its incoming call event
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class IncomingCallController : ControllerBase
    {
        private readonly ILogger<IncomingCallController> _logger;
        private readonly CallAutomationClient _callAutomationClient;
        private readonly PlaygroundConfigs _playgroundConfig;
        private readonly ITopLevelMenuService _topLevelMenuService;
        private readonly IOngoingEventHandler _ongoingEventHandler;

        public IncomingCallController(
            ILogger<IncomingCallController> logger,
            CallAutomationClient callAutomationClient,
            PlaygroundConfigs playgroundConfig,
            ITopLevelMenuService topLevelMenuService,
            IOngoingEventHandler ongoingEventHandler)
        {
            _logger = logger;
            _callAutomationClient = callAutomationClient;
            _playgroundConfig = playgroundConfig;
            _topLevelMenuService = topLevelMenuService;
            _ongoingEventHandler = ongoingEventHandler;
        }

        [HttpPost]
        public async Task<IActionResult> IncomingCall([FromBody] object request)
        {
            string callConnectionId = string.Empty;
            try
            {
                // Parse incoming call event using eventgrid parser
                var httpContent = new BinaryData(request.ToString());
                EventGridEvent cloudEvent = EventGridEvent.ParseMany(httpContent).First();

                if (cloudEvent.EventType == SystemEventNames.EventGridSubscriptionValidation)
                {
                    // this section is for handling initial handshaking with Event webhook registration
                    var eventData = cloudEvent.Data.ToObjectFromJson<SubscriptionValidationEventData>();
                    var responseData = new SubscriptionValidationResponse
                    {
                        ValidationResponse = eventData.ValidationCode
                    };

                    if (responseData.ValidationResponse != null)
                    {
                        _logger.LogInformation($"Incoming EventGrid event: Handshake Successful.");
                        return Ok(responseData);
                    }
                }
                else if (cloudEvent.EventType == SystemEventNames.AcsIncomingCall)
                {
                    // parse again the data into ACS incomingCall event
                    AcsIncomingCallEventData incomingCallEventData = cloudEvent.Data.ToObjectFromJson<AcsIncomingCallEventData>();

                    // Answer Incoming call with incoming call event data
                    // IncomingCallContext can be used to answer the call
                    var mediaStreamingConfiguration = new MediaStreamingOptions(
                        new Uri("wss://c8f0-20-125-145-72.ngrok-free.app/"),
                        MediaStreamingTransport.Websocket,
                        MediaStreamingContent.Audio,
                        MediaStreamingAudioChannel.Unmixed);

                    var answerCallOption = new AnswerCallOptions(incomingCallEventData.IncomingCallContext, _playgroundConfig.CallbackUri)
                    {
                        MediaStreamingOptions = mediaStreamingConfiguration,
                    };

                    AnswerCallResult answerCallResult = await _callAutomationClient.AnswerCallAsync(answerCallOption);
                    callConnectionId = answerCallResult.CallConnectionProperties.CallConnectionId;

                    _ = Task.Run(async () =>
                    {
                        // attaching ongoing event handler for specific events
                        // This is useful for handling unexpected events could happen anytime (such as participants leaves the call and cal is disconnected)
                        _ongoingEventHandler.AttachCountParticipantsInTheCall(callConnectionId);
                        _ongoingEventHandler.AttachDisconnectedWrapup(callConnectionId);

                        // Wait for call to be connected.
                        // Wait for 40 seconds before throwing timeout error.
                        var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(40));
                        AnswerCallEventResult eventResult = await answerCallResult.WaitForEventProcessorAsync(tokenSource.Token);

                        if (eventResult.IsSuccess)
                        {
                            // call connected returned! Call is now established.
                            // invoke top level menu now the call is connected;
                            await _topLevelMenuService.InvokeTopLevelMenu(
                                CommunicationIdentifier.FromRawId(incomingCallEventData.FromCommunicationIdentifier.RawId),
                                answerCallResult.CallConnection,
                                eventResult.SuccessResult.ServerCallId,
                                eventResult.SuccessResult.CorrelationId);
                        }
                    });
                }
            }
            catch (Exception e)
            {
                // Exception! Failed to answer the call.
                _logger.LogError($"Exception while answer the call. CallConnectionId[{callConnectionId}], Exception[{e}]");
            }

            return Ok();
        }
    }
}
