using Azure.Communication.CallAutomation;
using Azure.Communication;

namespace CogSvcIvrSamples
{
    public class ContosoElectricityWorkflowHandler : IWorkflowHandler
    {
        private readonly string playSourceBaseId;
        private readonly ILogger<ContosoElectricityWorkflowHandler> logger;

        public ContosoElectricityWorkflowHandler(string playSourceBaseId)
        {
            this.playSourceBaseId = playSourceBaseId;
            this.logger = new LoggerFactory().CreateLogger<ContosoElectricityWorkflowHandler>();
        }

        public async Task HandleAsync(
            string callerId,
            CallAutomationEventBase @event,
            CallConnection callConnection,
            CallMedia callConnectionMedia)
        {
            if (@event is CallAutomationEventBase)
            {
                await HandleWelcomMessageAsync(callConnectionMedia, callerId);
            }

            if (@event is RecognizeCompleted)
            {
                var recognizeCompletedEvent = (RecognizeCompleted)@event;
                switch (recognizeCompletedEvent.RecognizeResult)
                {
                    case ChoiceResult choiceResult:
                        var labelDetected = choiceResult.Label;

                        if (labelDetected.Equals(ContosoElectricitySelections.ConfirmAddress, StringComparison.OrdinalIgnoreCase))
                        {
                            await HandleNoOutageReportedPromptAsync(callConnectionMedia);
                        }

                        if (labelDetected.Equals(ContosoElectricitySelections.UpdateAddress, StringComparison.OrdinalIgnoreCase))
                        {
                            await HandleAddressUpdatedMessageAsync(callConnectionMedia);
                        }

                        if (labelDetected.Equals(ContosoElectricitySelections.ReportOutage, StringComparison.OrdinalIgnoreCase))
                        {
                            await HandleOutageReportRecordedMessageAsync(callConnectionMedia);
                        }

                        if (labelDetected.Equals(ContosoElectricitySelections.MainMenu, StringComparison.OrdinalIgnoreCase))
                        {
                            await HandleMainMenuAsync(callConnectionMedia, callerId);
                        }

                        if (labelDetected.Equals(ContosoElectricitySelections.SpeakToAgent, StringComparison.OrdinalIgnoreCase))
                        {
                            await HandleConnectingToAgentAsync(callConnectionMedia);
                        }
                        break;
                    case SpeechResult speechResult:
                        var speech = speechResult.Speech;
                        break;
                    default:
                        logger.LogError($"Unexpected recognize event result identified for connection id: {@event.CallConnectionId}");
                        break;
                }
            }

            if (@event is RecognizeFailed)
            {
                var recognizeFailedEvent = (RecognizeFailed)@event;

                // Check for time out, and then play audio message
                if (recognizeFailedEvent.ReasonCode.Equals(MediaEventReasonCode.RecognizeInitialSilenceTimedOut))
                {
                    logger.LogInformation($"Recognition timed out for call connection id: {@event.CallConnectionId}");
                    var playSource = "No input recieved and recognition timed out, Disconnecting the call. Thank you!"
                    .ToTextPlaySource(playSourceId: GetPlaySourceId("SilenceResponseToChoice"));

                    //Play audio for time out
                    await callConnectionMedia.PlayToAllAsync(new PlayToAllOptions (playSource) { OperationContext = "SilenceResponseToChoice", Loop = false });
                }

                //Check for invalid speech option or invalid tone detection
                //TODO: Add incorrect tone detected check 
                if (recognizeFailedEvent.ReasonCode.Equals(MediaEventReasonCode.RecognizeSpeechOptionNotMatched))
                {
                    logger.LogInformation($"Recognition failed for invalid speech detected, connection id: {@event.CallConnectionId}");
                    var playSource = "Invalid speech phrase detected, Disconnecting the call. Thank you!"
                    .ToTextPlaySource(playSourceId: GetPlaySourceId("ResponseToChoiceNotMatched"));

                    //Play text prompt for speech option not matched
                    await callConnectionMedia.PlayToAllAsync(new PlayToAllOptions (playSource) { OperationContext = "ResponseToChoiceNotMatched", Loop = false });
                }

                //await callConnection.HangUpAsync(forEveryone: true);
            }
            if (@event is PlayCompleted { OperationContext: "GreetingMessage" })
            {
                await HandleAddressConfirmationAsync(callConnectionMedia, callerId);
            }

            if (@event is PlayCompleted { OperationContext: "NoOutageReportedMessage" })
            {
                await HandleReportOutageMenuAsync(callConnectionMedia, callerId);
            }

            // hang up call when these prompts complete
            if (@event is PlayCompleted { OperationContext: "ReportRecordedMessage" } ||
                @event is PlayCompleted { OperationContext: "SilenceResponseToChoice" } ||
                @event is PlayCompleted { OperationContext: "ResponseToChoiceNotMatched" } ||
                @event is PlayCompleted { OperationContext: "UpdateAddressMessage" } ||
                @event is PlayCompleted { OperationContext: "AgentUnavailableMessage" })
            {
                logger.LogInformation($"PlayCompleted event received for call connection id: {@event.CallConnectionId}");
                await callConnection.HangUpAsync(forEveryone: true);
            }

            if (@event is PlayFailed)
            {
                logger.LogInformation($"PlayFailed event received for call connection id: {@event.CallConnectionId}");
                await callConnection.HangUpAsync(forEveryone: true);
            }
        }

        async Task HandleConnectingToAgentAsync(CallMedia callConnectionMedia)
        {
            var connectingToAgentMessage = "Please wait while we connect you to a customer agent."
                .ToTextPlaySource(playSourceId: GetPlaySourceId("ConnectingToAgentMessage"));
            await callConnectionMedia.PlayToAllAsync(new PlayToAllOptions(connectingToAgentMessage) { OperationContext = "ConnectingToAgentMessage", Loop = false });

            var agentUnavailableMessage = "To help reduce your wait time, we have added you to our call back queue. One of our agents will call you back as soon as possible. Thank you for calling Contoso Electricity"
                .ToTextPlaySource(playSourceId: GetPlaySourceId("AgentUnavailableMessage"));
            await callConnectionMedia.PlayToAllAsync(new PlayToAllOptions(agentUnavailableMessage) { OperationContext = "AgentUnavailableMessage", Loop = false });
        }

        async Task HandleWelcomMessageAsync(CallMedia callConnectionMedia, string callerId)
        {
            var greetingPlaySource = $"Hello {GetCustomerName(callerId)}, welcome to Contoso Electricity. I’m Sam, I can help provide you with information about planned and unplanned outages in your area."
                .ToSsmlPlaySource();
            await callConnectionMedia.PlayToAllAsync(new PlayToAllOptions(greetingPlaySource) { OperationContext = "GreetingMessage", Loop = false });
        }

        async Task HandleOutageReportRecordedMessageAsync(CallMedia callConnectionMedia)
        {
            var reportOutageConfirmation = "Your power outage report have been recorded. A member of our technical staff will get in touch with you soon. This call will be disconnected."
                .ToTextPlaySource(playSourceId: GetPlaySourceId("ReportRecordedMessage"));
            await callConnectionMedia.PlayToAllAsync(new PlayToAllOptions(reportOutageConfirmation) { OperationContext = "ReportRecordedMessage", Loop = false });
        }

        async Task HandleAddressConfirmationAsync(CallMedia callConnectionMedia, string callerId)
        {
            var playAddressMessage = $"Based on our records against your phone number, your address is {GetCustomerAddress(callerId)}."
                .ToTextPlaySource();

            await callConnectionMedia.PlayToAllAsync(new PlayToAllOptions(playAddressMessage) { OperationContext = "PlayAddressMessage", Loop = false });

            // Confirm address
            var choices = new List<RecognizeChoice>
            {
                new RecognizeChoice(ContosoElectricitySelections.ConfirmAddress, new List<string> { "Yes", "Confirm", "Confirm Address", "First", "One"})
                {
                    Tone = DtmfTone.One
                },
                new RecognizeChoice(ContosoElectricitySelections.UpdateAddress, new List<string> { "Update", "Update address", "Second", "Two"})
                {
                    Tone = DtmfTone.Two
                },
                new RecognizeChoice(ContosoElectricitySelections.SpeakToAgent, new List<string> { "Speak to an agent", "Agent", "Customer Agent", "Talk to customer agent"})
                {
                    Tone = DtmfTone.Zero
                }
            };

            var addressConfirmationPlaySource = $"if this is correct, please press 1 or say Confirm Address, else, press 2 or say update address, press 0 to speak to an agent."
                .ToTextPlaySource(playSourceId: GetPlaySourceId("AddressConfirmationMenuPrompt"));

            var recognizeOptions =
                new CallMediaRecognizeChoiceOptions(
                    targetParticipant: CommunicationIdentifier.FromRawId(callerId),
                    recognizeChoices: choices)
                {
                    InterruptPrompt = false,
                    InitialSilenceTimeout = TimeSpan.FromSeconds(10),
                    Prompt = addressConfirmationPlaySource,
                    OperationContext = "AddressConfirmationMenu"
                };

            //Start recognition 
            await callConnectionMedia.StartRecognizingAsync(recognizeOptions);
        }

        async Task HandleMainMenuAsync(CallMedia callConnectionMedia, string callerId)
        {
            // Main menu choices
            var choices = new List<RecognizeChoice>
            {
                new RecognizeChoice(ContosoElectricitySelections.ReportOutage, new List<string> { "report outage", "Outage", "One" })
                {
                    Tone = DtmfTone.One
                },
                new RecognizeChoice(ContosoElectricitySelections.UpdateAddress, new List<string> { "Update", "Second", "Two"})
                {
                    Tone = DtmfTone.Two
                },
                new RecognizeChoice(ContosoElectricitySelections.SpeakToAgent, new List<string> { "Speak to an agent", "Agent", "Customer Agent", "Talk to customer agent"})
                {
                    Tone = DtmfTone.Zero
                }
            };

            var addressConfirmationPlaySource = $"to report a power outage please press 1 or say report outage, to update your address, press 2 or say update address, press 0 to speak to an agent."
                .ToTextPlaySource(playSourceId: GetPlaySourceId("MainMenuPrompt"));

            var recognizeOptions =
                new CallMediaRecognizeChoiceOptions(
                    targetParticipant: CommunicationIdentifier.FromRawId(callerId),
                    recognizeChoices: choices)
                {
                    InterruptPrompt = true,
                    InitialSilenceTimeout = TimeSpan.FromSeconds(20),
                    Prompt = addressConfirmationPlaySource,
                    OperationContext = "MainMenu"
                };

            //Start recognition 
            await callConnectionMedia.StartRecognizingAsync(recognizeOptions);
        }

        async Task HandleNoOutageReportedPromptAsync(CallMedia callConnectionMedia)
        {
            var noOutageReportedMessage = "Thank you for confirming your address. We are not aware of any outages for your address.".ToTextPlaySource(playSourceId: GetPlaySourceId("NoOutageReportedMessage"));

            await callConnectionMedia.PlayToAllAsync(new PlayToAllOptions(noOutageReportedMessage) { OperationContext = "NoOutageReportedMessage", Loop = false });
        }

        async Task HandleReportOutageMenuAsync(CallMedia callConnectionMedia, string callerId)
        {
            var reportOutageMesgSource = "If you’re experiencing a power outage please press 1 or say report outage, or if you would like to speak to an agent say speak to an agent, to go back to the main menu, press pound or say go back."
                .ToTextPlaySource(playSourceId: GetPlaySourceId("ReportOutageMenuPrompt"));
            var reportOutageChoices = new List<RecognizeChoice>
        {
            new RecognizeChoice(ContosoElectricitySelections.ReportOutage, new List<string> { "report outage", "Outage", "One" })
            {
                Tone = DtmfTone.One
            },
            new RecognizeChoice(ContosoElectricitySelections.SpeakToAgent, new List<string> { "Speak to an agent", "Two" })
            {
                Tone = DtmfTone.Two
            },
            new RecognizeChoice(ContosoElectricitySelections.MainMenu, new List<string>() { "Main Menu", "Go back", "pound" })
            {
                Tone = DtmfTone.Pound
            }
        };
            var reportOutageRecoOptions = new CallMediaRecognizeChoiceOptions(
                targetParticipant: CommunicationIdentifier.FromRawId(callerId),
                recognizeChoices: reportOutageChoices)
            {
                InterruptPrompt = false,
                InitialSilenceTimeout = TimeSpan.FromSeconds(10),
                Prompt = reportOutageMesgSource,
                OperationContext = "ReportOutageMenu"
            };
            await callConnectionMedia.StartRecognizingAsync(reportOutageRecoOptions);
        }

        async Task HandleAddressUpdatedMessageAsync(CallMedia callConnectionMedia)
        {
            var updateAddressMessage = "I understand you would like to update your address, to do this please visit contoso.com/account. Thanks for calling contoso electricity. This call will be disconnected."
                .ToTextPlaySource(playSourceId: GetPlaySourceId("UpdateAddressMessage"));
            await callConnectionMedia.PlayToAllAsync(new PlayToAllOptions(updateAddressMessage) { OperationContext = "UpdateAddressMessage", Loop = false });
        }

        string GetCustomerName(string callerId)
        {
            return "Bob";
        }

        string GetCustomerAddress(string callerId)
        {
            return "3910 163rd Avenue North East, Redmond, Washington 98052";
        }

        string GetPlaySourceId(string name)
        {
            return playSourceBaseId + name;
        }
    }
}
