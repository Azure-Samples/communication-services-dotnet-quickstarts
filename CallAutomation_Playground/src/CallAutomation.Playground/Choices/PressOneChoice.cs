using Azure.Communication;
using Azure.Communication.CallAutomation;
using CallAutomation.Playground.Interfaces;
using CallAutomation.Playground.Services;

namespace CallAutomation.Playground.Choices;

public class PressOneChoice : IvrChoice
{
    private readonly PlaygroundConfig _playgroundConfig;

    public PressOneChoice(ICallingServices callingServices, PlaygroundConfig playgroundConfig)
        : base(callingServices)
    {
        _playgroundConfig = playgroundConfig;
    }

    public override async Task OnPress<TTone>(TTone one, CallConnectionProperties callConnectionProperties, CommunicationIdentifier target, CancellationToken cancellationToken = default)
    {
        CallMediaRecognizeDtmfOptions callMediaRecognizeDtmfOptions = new(target, 20)
        {
            Prompt = new FileSource(_playgroundConfig.InitialPromptUri),
            InterruptPrompt = true,
            StopTones = new[] { DtmfTone.Pound, DtmfTone.Asterisk }
        };

        // collect phone number from caller
        await CallingServices.RecognizeDtmfInput(callConnectionProperties, null,
            async collectTonesResult =>
            {
                var phoneNumber = "+1" + collectTonesResult?.ConvertToString();

                PhoneNumberIdentifier addTarget = new(phoneNumber);
                PhoneNumberIdentifier source = new(_playgroundConfig.PhoneNumber);
                CallInvite callInvite = new(addTarget, source);

                // add the participant and play hold music while waiting, then cancel the hold music and start recording
                await CallingServices.AddParticipant(callConnectionProperties,
                    () =>
                    {
                        CallingServices.PlayAudio(callConnectionProperties, _playgroundConfig.HoldMusicPromptUri, null, true, cancellationToken);
                    },
                    async success =>
                    {
                        await CallingServices.CancelMedia(callConnectionProperties, cancellationToken);
                        await CallingServices.StartRecording(callConnectionProperties, cancellationToken);
                    },
                    null, callInvite, cancellationToken);
            },
            null, callMediaRecognizeDtmfOptions, cancellationToken);
    }
}