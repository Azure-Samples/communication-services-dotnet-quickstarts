using Azure.Communication;
using Azure.Communication.CallAutomation;
using CallAutomation_Playground.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CallAutomation_Playground.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CallController : ControllerBase
    {
        private readonly CallAutomationClient _callAutomationClient;
        private readonly PlaygroundConfig _playgroundConfig;
        private readonly ITopLevelMenuService _topLevelMenuService;


        public CallController(
            CallAutomationClient callAutomationClient,
            PlaygroundConfig playgroundConfig,
            ITopLevelMenuService topLevelMenuService)
        {
            _callAutomationClient = callAutomationClient;
            _playgroundConfig = playgroundConfig;
            _topLevelMenuService = topLevelMenuService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateCall([FromQuery] string pstnTarget)
        {
            // prepare the target and caller in CallInvite
            PhoneNumberIdentifier target = new PhoneNumberIdentifier(pstnTarget);
            PhoneNumberIdentifier caller = new PhoneNumberIdentifier(_playgroundConfig.ACS_DirectOffer_Phonenumber);
            CallInvite callInvite = new CallInvite(target, caller);

            // create an outbound call to target using caller number
            CreateCallResult createCallResult = await _callAutomationClient.CreateCallAsync(callInvite, _playgroundConfig.CallbackUri);

            CallConnection callConnection = createCallResult.CallConnection;
            CallMedia callMedia = createCallResult.CallConnection.GetCallMedia();

            // Waiting for event related to createCallResult, which is CallConnected
            // TODO: implement cancellation token with timeout
            CreateCallEventResult eventResult = await createCallResult.WaitForEventProcessorAsync();
            CallConnected callConnected = eventResult.SuccessEvent;

            // invoke top level menu
            await _topLevelMenuService.InvokeTopLevelMenu(target, callConnection.CallConnectionId);

            return Ok();
        }
    }
}
