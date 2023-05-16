using Azure.Communication;
using Azure.Communication.CallAutomation;
using CallAutomation_Playground.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CallAutomation_Playground.Controllers
{
    /// <summary>
    /// This is the controller for making an outbound call.
    /// Pass on PSTN target number here to make an outbound call to target.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class OutboundCallController : ControllerBase
    {
        private readonly ILogger<OutboundCallController> _logger;
        private readonly CallAutomationClient _callAutomationClient;
        private readonly PlaygroundConfigs _playgroundConfig;
        private readonly ITopLevelMenuService _topLevelMenuService;
        private readonly IOngoingEventHandler _ongoingEventHandler;

        public OutboundCallController(
            ILogger<OutboundCallController> logger,
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
        public async Task<IActionResult> CreateCall([FromQuery] string pstnTarget)
        {
            string callConnectionId = string.Empty;
            try
            {
                // prepare the target and caller in CallInvite
                PhoneNumberIdentifier target = new PhoneNumberIdentifier(Tools.FormatPhoneNumbers(pstnTarget));
                PhoneNumberIdentifier caller = new PhoneNumberIdentifier(_playgroundConfig.DirectOfferedPhonenumber);
                CallInvite callInvite = new CallInvite(target, caller);

                _logger.LogInformation($"Calling[{target.PhoneNumber}] from DirectOfferNumber[{_playgroundConfig.DirectOfferedPhonenumber}]");

                // create an outbound call to target using caller number
                CreateCallResult createCallResult = await _callAutomationClient.CreateCallAsync(callInvite, _playgroundConfig.CallbackUri);
                callConnectionId = createCallResult.CallConnectionProperties.CallConnectionId;

                _ = Task.Run(async () =>
                {
                    // attaching ongoing event handler for specific events
                    // This is useful for handling unexpected events could happen anytime (such as participants leaves the call and cal is disconnected)
                    _ongoingEventHandler.AttachCountParticipantsInTheCall(callConnectionId);
                    _ongoingEventHandler.AttachDisconnectedWrapup(callConnectionId);

                    // Waiting for event related to createCallResult, which is CallConnected
                    // Wait for 40 seconds before throwing timeout error.
                    var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(40));
                    CreateCallEventResult eventResult = await createCallResult.WaitForEventProcessorAsync(tokenSource.Token);

                    if (eventResult.IsSuccess)
                    {
                        // call connected returned! Call is now established.
                        // invoke top level menu now the call is connected;
                        await _topLevelMenuService.InvokeTopLevelMenu(
                            target, 
                            createCallResult.CallConnection,
                            eventResult.SuccessResult.ServerCallId);
                    }
                });
            }
            catch (Exception e)
            {
                // Exception! likely the call was never established due to other party not answering.
                _logger.LogError($"Exception while doing outbound call. CallConnectionId[{callConnectionId}], Exception[{e}]");
            }

            return Ok(callConnectionId);
        }
    }
}
