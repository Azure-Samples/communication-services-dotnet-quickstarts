using Azure.Communication;
using Azure.Communication.CallAutomation;
using CallAutomation.Playground.Interfaces;
using CallAutomation.Playground.Services;

namespace CallAutomation.Playground.Choices;

public class PressThreeChoice : IvrChoice
{
    private readonly PlaygroundConfig _playgroundConfig;

    public PressThreeChoice(ICallingServices callingServices, PlaygroundConfig playgroundConfig)
        : base(callingServices)
    {
        _playgroundConfig = playgroundConfig;
    }

    public override async Task OnPress<TTOne>(TTOne tone, CallConnectionProperties callConnectionProperties, CommunicationIdentifier target,
        CancellationToken cancellationToken = default)
    {
        CallMediaRecognizeDtmfOptions callMediaRecognizeDtmfOptions = new(target, 20)
        {
            Prompt = new FileSource(_playgroundConfig.InitialPromptUri),
            InterruptPrompt = true,
            StopTones = new[] { DtmfTone.Pound, DtmfTone.Asterisk }
        };

        await CallingServices.RecognizeDtmfInput(callConnectionProperties, null,
            async collectTonesResult =>
            {
                var phoneNumber = "+1" + collectTonesResult?.ConvertToString();

                // transfer participant option
                PhoneNumberIdentifier addTarget = new(phoneNumber);
                PhoneNumberIdentifier source = new(_playgroundConfig.PhoneNumber);
                CallInvite callInvite = new(addTarget, source);

                await CallingServices.TransferCallLeg(callConnectionProperties, null, null, null, callInvite, cancellationToken);
                await CallingServices.CancelMedia(callConnectionProperties, cancellationToken);
            },
            failed => Task.CompletedTask, callMediaRecognizeDtmfOptions, cancellationToken);
    }
}