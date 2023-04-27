using Azure.Communication;
using Azure.Communication.CallAutomation;

namespace CallAutomation.Playground;

public class PlaygroundMainMenu : IvrMenu
{
    private readonly PlaygroundConfig _playgroundConfig;

    public PlaygroundMainMenu(CallAutomationClient callAutomationClient, PlaygroundConfig playgroundConfig)
        :base(callAutomationClient, playgroundConfig)
    {
        _playgroundConfig = playgroundConfig;
    }

    public override async Task OnPressOne(CallConnectionProperties callConnectionProperties, CommunicationIdentifier target, CancellationToken cancellationToken = default)
    {
        CallMediaRecognizeDtmfOptions callMediaRecognizeDtmfOptions = new (target, 20)
        {
            Prompt = new FileSource(_playgroundConfig.InitialPromptUri),
            InterruptPrompt = true,
            StopTones = new [] { DtmfTone.Pound, DtmfTone.Asterisk }
        };

        // collect phone number from caller
        await RecognizeDtmfInput(callConnectionProperties, null,
            async collectTonesResult =>
            {
                var phoneNumber = "+1" + collectTonesResult?.ConvertToString();

                PhoneNumberIdentifier addTarget = new (phoneNumber);
                PhoneNumberIdentifier source = new (_playgroundConfig.PhoneNumber);
                CallInvite callInvite = new (addTarget, source);

                // add the participant and play hold music while waiting, then cancel the hold music and start recording
                await AddParticipant(callConnectionProperties,
                    async () =>
                    {
                        await PlayAudio(callConnectionProperties, _playgroundConfig.HoldMusicPromptUri, null, true, cancellationToken);
                    },
                    async success =>
                    {
                        await CancelMedia(callConnectionProperties, cancellationToken);
                        await StartRecording(callConnectionProperties, cancellationToken);
                    },
                    null, callInvite, cancellationToken);
            },
            null, callMediaRecognizeDtmfOptions, cancellationToken);
    }

    public override async Task OnPressTwo(CallConnectionProperties callConnectionProperties, CommunicationIdentifier target, CancellationToken cancellationToken = default)
    {
        var participantsList = await GetCallParticipantList(callConnectionProperties, cancellationToken);

        // go through the list and remove each one
        // TODO: unhappy path
        foreach (var participant in participantsList)
        {
            // Remove all PSTN participants that is not the original target
            if (participant.Identifier is PhoneNumberIdentifier && participant.Identifier.RawId != target.RawId)
            {
                await RemoveParticipant(callConnectionProperties, null, null, null, participant.Identifier, cancellationToken);
            }
        }
    }

    public override async Task OnPressThree(CallConnectionProperties callConnectionProperties, CommunicationIdentifier target, CancellationToken cancellationToken = default)
    {
        CallMediaRecognizeDtmfOptions callMediaRecognizeDtmfOptions = new (target, 20)
        {
            Prompt = new FileSource(_playgroundConfig.InitialPromptUri),
            InterruptPrompt = true,
            StopTones = new [] { DtmfTone.Pound, DtmfTone.Asterisk }
        };

        await RecognizeDtmfInput(callConnectionProperties, null,
            async collectTonesResult =>
            {
                var phoneNumber = "+1" + collectTonesResult?.ConvertToString();

                // transfer participant option
                PhoneNumberIdentifier addTarget = new (phoneNumber);
                PhoneNumberIdentifier source = new (_playgroundConfig.PhoneNumber);
                CallInvite callInvite = new (addTarget, source);
                
                await TransferCallLeg(callConnectionProperties, null, null, null, callInvite, cancellationToken);
                await CancelMedia(callConnectionProperties, cancellationToken);
            },
            failed => Task.CompletedTask, callMediaRecognizeDtmfOptions, cancellationToken);
    }

    public override async Task OnPressFour(CallConnectionProperties callConnectionProperties, CommunicationIdentifier target, CancellationToken cancellationToken = default)
    {
        await HangUp(callConnectionProperties, true, cancellationToken);
    }
}