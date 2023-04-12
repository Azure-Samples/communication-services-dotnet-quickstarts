using Azure.Communication;
using Azure.Communication.CallAutomation;
using CallAutomation_Playground.Interfaces;

namespace CallAutomation_Playground
{
    public class TopLevelMenuService : ITopLevelMenuService
    {
        private readonly CallAutomationClient _callAutomation;
        private readonly PlaygroundConfig _playgroundConfig;

        public TopLevelMenuService(CallAutomationClient callAutomation, PlaygroundConfig playgroundConfig)
        {
            _callAutomation = callAutomation;
            _playgroundConfig = playgroundConfig;
        }

        public async Task InvokeTopLevelMenu(CommunicationIdentifier target, string callConnectionId)
        {
            CallConnection callConnection = _callAutomation.GetCallConnection(callConnectionId);

            // Top Level DTMF Menu
            CallMediaRecognizeDtmfOptions callMediaRecognizeDtmfOptions = new CallMediaRecognizeDtmfOptions(target, 1);
            callMediaRecognizeDtmfOptions.Prompt = new FileSource(_playgroundConfig.InitialPromptUri);
            callMediaRecognizeDtmfOptions.InterruptPrompt = true;

            // TODO: add retry logic
            CollectTonesResult pressedDTMF = null;
            for (int i = 0; i < 3; i++)
            {
                StartRecognizingEventResult startRecognizingEventResult = await RecognizeBlockAsync(callConnection.GetCallMedia(), callMediaRecognizeDtmfOptions);

                if (startRecognizingEventResult.IsSuccessEvent)
                {
                    RecognizeCompleted recognizeCompleted = startRecognizingEventResult.SuccessEvent;
                    pressedDTMF = (CollectTonesResult)recognizeCompleted.RecognizeResult;
                    break;
                }
                else
                {
                    RecognizeFailed recognizeFailed = startRecognizingEventResult.FailureEvent;

                    if (recognizeFailed.ReasonCode.Equals(ReasonCode.RecognizeInitialSilenceTimedOut))
                    {
                        // TODO: do something special for this specific error?
                    }

                    continue;
                }
            }

            if (pressedDTMF?.Tones.FirstOrDefault() == DtmfTone.One)
            {
                // Option 1: Add Participant
                await AddParticipantBlock(target, callConnection);
            }

            if (pressedDTMF?.Tones.FirstOrDefault() == DtmfTone.Two)
            {
                // Option 2: Remove Participant
            }

            if (pressedDTMF?.Tones.FirstOrDefault() == DtmfTone.Three)
            {
                // Option 3: Transfer Participant
            }

            if (pressedDTMF?.Tones.FirstOrDefault() == DtmfTone.Four)
            {
                // Option 4: Terminate the call
            }

            return;
        }

        private async Task AddParticipantBlock(CommunicationIdentifier target, CallConnection callConnection)
        {
            // Add Participant Menu
            CallMediaRecognizeDtmfOptions callMediaRecognizeDtmfOptions = new CallMediaRecognizeDtmfOptions(target, 20);
            callMediaRecognizeDtmfOptions.Prompt = new FileSource(_playgroundConfig.AddParticipantPromptUri);
            callMediaRecognizeDtmfOptions.InterruptPrompt = true;
            callMediaRecognizeDtmfOptions.StopTones = new List<DtmfTone> { DtmfTone.Pound, DtmfTone.Asterisk };

            StartRecognizingEventResult startRecognizingEventResult = await RecognizeBlockAsync(callConnection.GetCallMedia(), callMediaRecognizeDtmfOptions);

            string phoneNumber = string.Empty;
            if (startRecognizingEventResult.IsSuccessEvent)
            {
                RecognizeCompleted recognizeCompleted = startRecognizingEventResult.SuccessEvent;
                var collectToneResult = (CollectTonesResult)recognizeCompleted.RecognizeResult;
                phoneNumber = "+1" + collectToneResult.ConvertToString();

                await PlayHoldMusicLoopAsync(callConnection.GetCallMedia());
            }
            else
            {
                RecognizeFailed recognizeFailed = startRecognizingEventResult.FailureEvent;

                if (recognizeFailed.ReasonCode.Equals(ReasonCode.RecognizeInitialSilenceTimedOut))
                {
                    // TODO: do something special for this specific error?
                }
            }

            // add participant option
            PhoneNumberIdentifier addTarget = new PhoneNumberIdentifier(phoneNumber);
            PhoneNumberIdentifier source = new PhoneNumberIdentifier(_playgroundConfig.ACS_DirectOffer_Phonenumber);

            CallInvite callInvite = new CallInvite(addTarget, source);
            AddParticipantResult addParticipantResult = await callConnection.AddParticipantAsync(callInvite);

            AddParticipantEventResult addParticipantEventResult = await addParticipantResult.WaitForEventProcessorAsync();

            // Cancel media once the person is added or failed
            await callConnection.GetCallMedia().CancelAllMediaOperationsAsync();

            if (addParticipantEventResult.IsSuccessEvent)
            {
                AddParticipantSucceeded addParticipantSucceeded = addParticipantEventResult.SuccessEvent;
                // TODO... something when add participant
            }
            else
            {
            }

            return;
        }

        private async Task<StartRecognizingEventResult> RecognizeBlockAsync(CallMedia callMedia, CallMediaRecognizeDtmfOptions callMediaRecognizeDtmfOptions)
        {
            StartRecognizingResult startRecognizingResult = await callMedia.StartRecognizingAsync(callMediaRecognizeDtmfOptions);

            // wait for recognition completion event
            return await startRecognizingResult.WaitForEventProcessorAsync();
        }

        private async Task PlayHoldMusicLoopAsync(CallMedia callMedia)
        {
            // Play Hold Music...
            FileSource fileSource = new FileSource(_playgroundConfig.HoldMusicPromptUri);
            PlayOptions playOptions = new PlayOptions();
            playOptions.Loop = true;
            PlayResult playResult = await callMedia.PlayToAllAsync(fileSource, playOptions);
        }
    }
}
