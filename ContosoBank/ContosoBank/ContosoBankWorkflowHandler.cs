using Azure.Communication;
using Azure.Communication.CallAutomation;

namespace CogSvcIvrSamples
{
    public class ContosoBankWorkflowHandler : IWorkflowHandler
    {
        private readonly string playSourceBaseId;
        private readonly string playVoiceName;
        private readonly string playVoiceExpression;
        private readonly ILogger<ContosoBankWorkflowHandler> logger;

        public ContosoBankWorkflowHandler(string playSourceBaseId)
        {
            this.playSourceBaseId = playSourceBaseId;
            this.logger = new LoggerFactory().CreateLogger<ContosoBankWorkflowHandler>();
            this.playVoiceName = "en-US-GuyNeural";
            this.playVoiceExpression = "friendly";
        }

        public async Task HandleAsync(string callerId, CallAutomationEventBase @event, CallConnection callConnection, CallMedia callConnectionMedia)
        {
            if (@event is CallConnected)
            {
                // play greeting message
                var greetingPlaySource = "Welcome to Contoso Bank, I’m Dave. Please note that this call will be recorded for quality assurance."
                .ToSsmlPlaySource(voiceName: playVoiceName, expression: playVoiceExpression);
                await callConnectionMedia.PlayToAllAsync(new PlayToAllOptions(greetingPlaySource) { OperationContext = "GreetingMessage", Loop = false });
            }

            if (@event is PlayCompleted { OperationContext: "GreetingMessage" })
            {
                await HandlePinAuthAsync(callerId, callConnectionMedia, context: "IdentificationResult", prompt: "For identification purposes please key in or say the last 4 digits of your customer number.");
            }

            if (@event is RecognizeCompleted { OperationContext: "IdentificationResult" })
            {
                var identificationPin = GetPinFromSpeechOrDtmf((RecognizeCompleted)@event);
                if (ValidatePin(identificationPin, callerId))
                {
                    await HandleMainMenuAsync(callerId, callConnectionMedia);
                }
                else
                {
                    await HandlePinAuthAsync(callerId, callConnectionMedia, context: "IdentificationResult", prompt: "For identification purposes please key in or say the last 4 digits of your customer number.");
                }
            }

            if (@event is RecognizeCompleted { OperationContext: "OpenQuestionSpeech" } ||
                @event is RecognizeCompleted { OperationContext: "MainMenuReconfirmResult" })
            {
                var result = ((RecognizeCompleted)@event).RecognizeResult;
                if (result == null)
                {
                    logger.LogInformation($"Received null or empty result for open question prompt");
                    return;
                }

                var selection = DtmfTone.Nine;
                switch(result)
                {
                    case SpeechResult speechResult:
                        var speech = speechResult.Speech;
                        if (speech.Contains("account balance", StringComparison.OrdinalIgnoreCase) ||
                            speech.Contains("account", StringComparison.OrdinalIgnoreCase) ||
                            speech.Contains("balance", StringComparison.OrdinalIgnoreCase)) {
                            selection = DtmfTone.One;
                        }
                        else if (speech.Contains("credit card address", StringComparison.OrdinalIgnoreCase))
                        {
                            selection = DtmfTone.Three;
                        }
                        else if (speech.Contains("credit card", StringComparison.OrdinalIgnoreCase))
                        {
                            selection = DtmfTone.Two;
                        }
                        else if (speech.Contains("agent", StringComparison.OrdinalIgnoreCase) ||
                            speech.Contains("customer agent", StringComparison.OrdinalIgnoreCase) ||
                            speech.Contains("customer service", StringComparison.OrdinalIgnoreCase) ||
                            speech.Contains("customer service representative", StringComparison.OrdinalIgnoreCase))
                        {
                            selection = DtmfTone.Zero;
                        }
                        else if (speech.Contains("no", StringComparison.OrdinalIgnoreCase) ||
                            speech.Contains("i'm good", StringComparison.OrdinalIgnoreCase) ||
                            speech.Contains("naah", StringComparison.OrdinalIgnoreCase) ||
                            speech.Contains("nada", StringComparison.OrdinalIgnoreCase) ||
                            speech.Contains("i am good", StringComparison.OrdinalIgnoreCase) ||
                            speech.Contains("thank you", StringComparison.OrdinalIgnoreCase))
                        {
                            selection = DtmfTone.D;
                        }
                        break;
                    case DtmfResult dtmfResult:
                        selection = dtmfResult.Tones[0];
                        break;
                }

                if (selection == DtmfTone.Zero)
                {
                    var digits = callerId[^4..].ToCharArray();
                    var lastFour = String.Join(',', digits);
                    await HandleEndCallPromptAsync(callConnectionMedia, $"All our agents are busy right now. Our next available agent will call you back at the same number ending with {lastFour}. Thanks for calling, Good Bye!");
                    return;
                }
                
                if (selection == DtmfTone.One)
                {
                    await HandlePinAuthAsync(callerId, callConnectionMedia, context: "BalancePinAuth", prompt: "Please key in or say your 4 digit unique account pin");
                    return;
                }

                if (selection == DtmfTone.Two)
                {
                    await HandleChangeAddressChoiceReconfirmPromptAsync(callerId, callConnectionMedia);
                    return;
                }

                if (selection == DtmfTone.Three)
                {
                    await HandleChangeAddressChoiceReconfirmPromptAsync(callerId, callConnectionMedia);
                    return;
                }

                if (selection == DtmfTone.D)
                {
                    await HandleEndCallPromptAsync(callConnectionMedia, "Thank you for calling, good bye!");
                }

                await HandleMainMenuReconfirmAsync(callerId, callConnectionMedia);
            }

            if (@event is RecognizeFailed { OperationContext: "OpenQuestionSpeech" })
            {
                var result = (RecognizeFailed)@event;
                if (result.ResultInformation?.Code >= 400 &&  result.ResultInformation.SubCode == 8510)
                {
                    await HandleMainMenuReconfirmAsync(callerId, callConnectionMedia);
                }
            }

            if (@event is RecognizeCompleted { OperationContext: "BalancePinAuth" })
            {
                var authPin = GetPinFromSpeechOrDtmf((RecognizeCompleted)@event);
                if (ValidatePin(authPin, callerId))
                {
                    await HandleReadBalanceAsync(callerId, callConnectionMedia);
                }
                else
                {
                    await HandlePinAuthAsync(callerId, callConnectionMedia, context: "BalancePinAuth", prompt: "Please key in or say your 4 digit unique account pin");
                }

            }

            if (@event is RecognizeCompleted { OperationContext: "BalanceEndInputResult" })
            {
                var result = (DtmfResult)((RecognizeCompleted)@event).RecognizeResult;
                if (result.Tones[0] == DtmfTone.Zero)
                {
                    await HandleMainMenuAsync(callerId, callConnectionMedia);
                }
                else
                {
                    await HandleEndCallPromptAsync(callConnectionMedia);
                }
            }

            if (@event is RecognizeCompleted { OperationContext: "AddressChangeChoiceReconfirm" })
            {
                var result = (SpeechResult)((RecognizeCompleted)@event).RecognizeResult;
                var speech = result?.Speech ?? "";
                if ((speech.Contains("yes", StringComparison.OrdinalIgnoreCase) || speech.Contains("confirmed")) && !speech.Contains("no", StringComparison.OrdinalIgnoreCase))
                {
                    await HandleChangeAddressAsync(callerId, callConnectionMedia);
                }
                else
                {
                    await HandleMainMenuAsync (callerId, callConnectionMedia);
                }
            }

            if (@event is RecognizeCompleted { OperationContext: "AddressChangeResult" })
            {
                var address = ((SpeechResult)((RecognizeCompleted)@event).RecognizeResult).Speech;
                await HandleReconfirmNewAddressAsync(callerId, callConnectionMedia, address);
            }

            if (@event is RecognizeCompleted { OperationContext: "ReconfirmNewAddressResult" })
            {
                var confirmation = ((SpeechResult)((RecognizeCompleted)@event).RecognizeResult).Speech;
                if (confirmation.Contains("confirm", StringComparison.OrdinalIgnoreCase) ||
                    confirmation.Contains("yes", StringComparison.OrdinalIgnoreCase)||
                    confirmation.Contains("affirmative", StringComparison.OrdinalIgnoreCase)||
                    confirmation.Contains("sure", StringComparison.OrdinalIgnoreCase))
                {
                    await HandleMainMenuAsync(callerId, callConnectionMedia, prompt: "Your address has been updated. Is there anything else I can help you with today?");
                }
                else
                {
                    await HandleChangeAddressAsync(callerId, callConnectionMedia);
                }
            }

            if (@event is RecognizeFailed)
            {
                await HandleEndCallPromptAsync(callConnectionMedia, prompt: "No valid input received. This call will be ended. Good Bye!");
            }

            if (@event is PlayCompleted { OperationContext: "EndCallPrompt" } || @event is PlayFailed)
            {
                await callConnection.HangUpAsync(forEveryone: true);
            }
        }

        private async Task HandleMainMenuReconfirmAsync(string callerId, CallMedia callConnectionMedia)
        {
            var menuReconfirmPrompt = "Sorry I didn’t quite get that. Please say account balance or press 1 for checking your account balance. Say credit card or press 2 for information about Contoso credit cards. Press 0 for agent."
                .ToSsmlPlaySource(voiceName: playVoiceName, expression: playVoiceExpression, playSourceId: GetPlaySourceId("MainMenuReconfirmResult"));
            var recognizeOptions = new CallMediaRecognizeSpeechOrDtmfOptions(
                targetParticipant: CommunicationIdentifier.FromRawId(callerId),
                maxTonesToCollect: 1)
            {
                InterruptCallMediaOperation = true,
                InterruptPrompt = true,
                EndSilenceTimeoutInMs = TimeSpan.FromMilliseconds(3000),
                Prompt = menuReconfirmPrompt,
                OperationContext = "MainMenuReconfirmResult"
            };

            await callConnectionMedia.StartRecognizingAsync(recognizeOptions);
        }

        private async Task HandleChangeAddressAsync(string callerId, CallMedia callConnectionmedia)
        {
            var changeAddressPrompt = "Please provide me the new address you would like to be associated with your credit card."
                .ToSsmlPlaySource(voiceName: playVoiceName, expression: playVoiceExpression, playSourceId: GetPlaySourceId("AddressChangePrompt"));
            var recognizeOptions = new CallMediaRecognizeSpeechOptions(targetParticipant: CommunicationIdentifier.FromRawId(callerId))
            {
                InterruptPrompt = true,
                InterruptCallMediaOperation = true,
                EndSilenceTimeoutInMs = TimeSpan.FromMilliseconds(3000),
                Prompt = changeAddressPrompt,
                OperationContext = "AddressChangeResult"
            };

            await callConnectionmedia.StartRecognizingAsync(recognizeOptions);
        }

        private async Task HandleReconfirmNewAddressAsync(string callerId, CallMedia callConnectionmedia, string address)
        {
            var reconfirmNewAddressPrompt = $"You provided {address} please confirm by saying 'Confirm' or to try again say 'Try again'"
                .ToSsmlPlaySource(voiceName: playVoiceName, expression: playVoiceExpression);
            var recognizeOptions = new CallMediaRecognizeSpeechOptions(targetParticipant: CommunicationIdentifier.FromRawId(callerId))
            {
                InterruptCallMediaOperation = true,
                Prompt = reconfirmNewAddressPrompt,
                OperationContext = "ReconfirmNewAddressResult"
            };

            await callConnectionmedia.StartRecognizingAsync(recognizeOptions);
        }

        private async Task HandleChangeAddressChoiceReconfirmPromptAsync(string callerId, CallMedia callConnectionMedia)
        {
            var confirmAddressChangeChoice = "Do I understand correctly you would like to update the address associated with your credit card"
                .ToSsmlPlaySource(voiceName: playVoiceName, expression: playVoiceExpression, playSourceId: GetPlaySourceId("ConfirmAddressChangeChoicePrompt"));
            var recognizeOptions = new CallMediaRecognizeSpeechOptions(targetParticipant: CommunicationIdentifier.FromRawId(callerId))
            {
                InterruptCallMediaOperation = true,
                InterruptPrompt = true,
                Prompt = confirmAddressChangeChoice,
                OperationContext = "AddressChangeChoiceReconfirm"
            };

            await callConnectionMedia.StartRecognizingAsync(recognizeOptions);
        }

        private async Task HandleMainMenuAsync(string callerId, CallMedia callConnectionMedia, string? prompt = null)
        {
            prompt ??= $"Hi {GetCustomerName(callerId)}, how can I help you today?";
            var openQuestionPrompt = prompt.ToSsmlPlaySource(voiceName: playVoiceName, expression: playVoiceExpression);
            var recognizeOptions = new CallMediaRecognizeSpeechOptions(targetParticipant: CommunicationIdentifier.FromRawId(callerId))
            {
                InterruptCallMediaOperation = true,
                InterruptPrompt = false,
                Prompt = openQuestionPrompt,
                EndSilenceTimeoutInMs = TimeSpan.FromMilliseconds(1000),
                OperationContext = "OpenQuestionSpeech"
            };

            // ask "How can I help you today?
            await callConnectionMedia.StartRecognizingAsync(recognizeOptions);
        }

        private async Task HandlePinAuthAsync(string callerId, CallMedia callConnectionMedia, string context, string prompt = "", int digits = 4)
        {
            var pinAuthPrompt = prompt
                        .ToSsmlPlaySource(voiceName: playVoiceName, expression: playVoiceExpression, playSourceId: GetPlaySourceId("PinAuthPrompt"));
            var recognizeOptions = new CallMediaRecognizeSpeechOrDtmfOptions(
                targetParticipant: CommunicationIdentifier.FromRawId(callerId),
                maxTonesToCollect: digits)
            {
                InterruptCallMediaOperation = true,
                InterruptPrompt = true,
                Prompt = pinAuthPrompt,
                EndSilenceTimeoutInMs = TimeSpan.FromMilliseconds(1000),
                InterToneTimeout = TimeSpan.FromMilliseconds(1000),
                OperationContext = context
            };

            await callConnectionMedia.StartRecognizingAsync(recognizeOptions);
        }

        private async Task HandleReadBalanceAsync(string callerId, CallMedia callConnectionMedia)
        {
            var balancePrompt = "Your balance is $10. To return to the main menu press 0. Press any other button to hangup."
                    .ToSsmlPlaySource(voiceName: playVoiceName, expression: playVoiceExpression, playSourceId: GetPlaySourceId("BalancePrompt"));
            var recognizeOptions = new CallMediaRecognizeDtmfOptions(
                targetParticipant: CommunicationIdentifier.FromRawId(callerId),
                maxTonesToCollect: 1)
            {
                InterruptCallMediaOperation = true,
                InitialSilenceTimeout = TimeSpan.FromMilliseconds(2000),
                InterruptPrompt = true,
                Prompt = balancePrompt,
                OperationContext = "BalanceEndInputResult"
            };

            await callConnectionMedia.StartRecognizingAsync(recognizeOptions);
        }

        private async Task HandleEndCallPromptAsync(CallMedia callConnectionMedia, string? prompt = null)
        {
            prompt ??= "This call will be ended. Good Bye!";
            var endCallPrompt = prompt.ToSsmlPlaySource(voiceName: playVoiceName, expression: playVoiceExpression, playSourceId: GetPlaySourceId("EndCallPrompt"));
            await callConnectionMedia.PlayToAllAsync(new PlayToAllOptions (endCallPrompt) { OperationContext = "EndCallPrompt", Loop = false });
        }

        string GetCustomerName(string callerId)
        {
            return "Bob";
        }

        private string GetPlaySourceId(string name)
        {
            return playSourceBaseId + "2" + name;
        }

        private string GetPinFromSpeechOrDtmf(RecognizeCompleted eventData)
        {
            switch (eventData.RecognizeResult)
            {
                case SpeechResult speechResult:
                    return speechResult.Speech?.Trim()?.Replace(" ", "").Replace(".", "") ?? "";
                case DtmfResult dtmfResult:
                    return new string(dtmfResult.Tones.Select(tone => tone.ToChar()).ToArray());
                default:
                    return "";
            }
        }

        private bool ValidatePin(string pin, string callerId, int length = 4)
        {
            return pin.Equals(callerId[^length..]);
        }
    }
}
