using Azure.Communication;
using Azure.Communication.CallAutomation;
using CallAutomation_Playground.Interfaces;

namespace CallAutomation_Playground;

public class PlaygroundMainMenu : IvrMenu
{
    private readonly ICallingService _callingService;
    private readonly PlaygroundConfig _playgroundConfig;

    public PlaygroundMainMenu(ICallingService callingService, PlaygroundConfig playgroundConfig)
    {
        _callingService = callingService;
        _playgroundConfig = playgroundConfig;
    }

    public override async Task OnPressOne(CallConnectionProperties callConnectionProperties, CommunicationIdentifier target)
    {
        CallMediaRecognizeDtmfOptions callMediaRecognizeDtmfOptions = new CallMediaRecognizeDtmfOptions(target, 20);
        callMediaRecognizeDtmfOptions.Prompt = new FileSource(_playgroundConfig.InitialPromptUri);
        callMediaRecognizeDtmfOptions.InterruptPrompt = true;
        callMediaRecognizeDtmfOptions.StopTones = new List<DtmfTone> { DtmfTone.Pound, DtmfTone.Asterisk };

        // Add PSTN Participant Menu
        await _callingService.RecognizeDtmfInput(callConnectionProperties,
            async collectTonesResult =>
            {
                var phoneNumber = "+1" + collectTonesResult.ConvertToString();

                // add participant option
                PhoneNumberIdentifier addTarget = new PhoneNumberIdentifier(phoneNumber);
                PhoneNumberIdentifier source = new PhoneNumberIdentifier(_playgroundConfig.ACS_DirectOffer_Phonenumber);
                CallInvite callInvite = new CallInvite(addTarget, source);
                await _callingService.AddParticipant(callConnectionProperties,
                    async success =>
                    {
                        await _callingService.CancelMedia(callConnectionProperties);
                        await _callingService.StartRecording(callConnectionProperties);
                    },
                    null, callInvite);

                await _callingService.PlayHoldMusic(callConnectionProperties, _playgroundConfig.HoldMusicPromptUri, null, true);
            },
            null, callMediaRecognizeDtmfOptions);
    }

    public override async Task OnPressTwo(CallConnectionProperties callConnectionProperties, CommunicationIdentifier target)
    {
        var participantsList = await _callingService.GetCallParticipantList(callConnectionProperties);

        // go through the list and remove each one
        // TODO: unhappy path
        foreach (var participant in participantsList)
        {
            // Remove all PSTN participants that is not the original target
            if (participant.Identifier is PhoneNumberIdentifier && participant.Identifier.RawId != target.RawId)
            {
                await _callingService.RemoveParticipant(callConnectionProperties, null, null, participant.Identifier);
            }
        }
    }

    public override async Task OnPressThree(CallConnectionProperties callConnectionProperties, CommunicationIdentifier target)
    {
        CallMediaRecognizeDtmfOptions callMediaRecognizeDtmfOptions = new CallMediaRecognizeDtmfOptions(target, 20);
        callMediaRecognizeDtmfOptions.Prompt = new FileSource(_playgroundConfig.InitialPromptUri);
        callMediaRecognizeDtmfOptions.InterruptPrompt = true;
        callMediaRecognizeDtmfOptions.StopTones = new List<DtmfTone> { DtmfTone.Pound, DtmfTone.Asterisk };

        await _callingService.RecognizeDtmfInput(callConnectionProperties,
            async collectTonesResult =>
            {
                var phoneNumber = "+1" + collectTonesResult.ConvertToString();

                // transfer participant option
                PhoneNumberIdentifier addTarget = new PhoneNumberIdentifier(phoneNumber);
                PhoneNumberIdentifier source = new PhoneNumberIdentifier(_playgroundConfig.ACS_DirectOffer_Phonenumber);
                CallInvite callInvite = new CallInvite(addTarget, source);
                
                await _callingService.TransferCallLeg(callConnectionProperties, null, null, callInvite);
                await _callingService.CancelMedia(callConnectionProperties);
            },
            async failed =>
            {

            }, callMediaRecognizeDtmfOptions);
    }
}