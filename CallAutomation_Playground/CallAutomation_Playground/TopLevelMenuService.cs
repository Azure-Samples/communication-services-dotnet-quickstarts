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

                    if (pressedDTMF?.Tones.FirstOrDefault() == DtmfTone.One)
                    {
                        // Option 1: Add Participant
                        await AddParticipantBlock(target, callConnection);
                    }

                    if (pressedDTMF?.Tones.FirstOrDefault() == DtmfTone.Two)
                    {
                        // Option 2: Remove Participant
                        await RemoveParticipantBlock(target, callConnection);
                    }

                    if (pressedDTMF?.Tones.FirstOrDefault() == DtmfTone.Three)
                    {
                        // Option 3: Transfer Participant
                        await TransferParticipantBlock(target, callConnection);
                        return;
                    }

                    if (pressedDTMF?.Tones.FirstOrDefault() == DtmfTone.Four)
                    {
                        // Option 4: Terminate the call
                        await TerminateBlockAsync(callConnection);
                        return;
                    }

                    i = 0;
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

            return;
        }

        private async Task AddParticipantBlock(CommunicationIdentifier target, CallConnection callConnection)
        {
            // Add Participant Menu
            string phonenumberToAdd = await RecognizePhonenumberAsync(target, callConnection.GetCallMedia(), _playgroundConfig.AddParticipantPromptUri);

            // add participant option
            PhoneNumberIdentifier addTarget = new PhoneNumberIdentifier(phonenumberToAdd);
            PhoneNumberIdentifier source = new PhoneNumberIdentifier(_playgroundConfig.ACS_DirectOffer_Phonenumber);

            CallInvite callInvite = new CallInvite(addTarget, source);
            AddParticipantResult addParticipantResult = await callConnection.AddParticipantAsync(callInvite);

            // Play hold music
            await PlayHoldMusicLoopAsync(callConnection.GetCallMedia());

            AddParticipantEventResult addParticipantEventResult = await addParticipantResult.WaitForEventProcessorAsync();

            // Cancel media once the person is added or failed
            await callConnection.GetCallMedia().CancelAllMediaOperationsAsync();

            if (addParticipantEventResult.IsSuccessEvent)
            {
                AddParticipantSucceeded addParticipantSucceeded = addParticipantEventResult.SuccessEvent;

                // start recording
                CallRecording recording = _callAutomation.GetCallRecording();

                // create call recording options
                CallLocator callLocator = new ServerCallLocator(addParticipantSucceeded.ServerCallId);
                StartRecordingOptions startRecordingOptions = new StartRecordingOptions(callLocator);

                RecordingStateResult recordingStateResult = await recording.StartRecordingAsync(startRecordingOptions);

                // TODO: add a recording statechange callback uri
            }
            else
            {
                // TODO: unhappy path
            }

            return;
        }

        private async Task RemoveParticipantBlock(CommunicationIdentifier target, CallConnection callConnection)
        {
            // List participants
            // TODO: Look into details why this one doesn't unwrap
            IReadOnlyList<CallParticipant> participantsList = (await callConnection.GetParticipantsAsync()).Value;

            // go through the list and remove each one 
            // TODO: unhappy path
            foreach (var participant in participantsList)
            {
                // Remov all PSTN participants that is not the original target
                if (participant.Identifier is PhoneNumberIdentifier && participant.Identifier.RawId != target.RawId)
                {
                    await callConnection.RemoveParticipantAsync(participant.Identifier);
                }
            }

            return;
        }

        private async Task TransferParticipantBlock(CommunicationIdentifier target, CallConnection callConnection)
        {
            string phonenumberToTransfer = await RecognizePhonenumberAsync(target, callConnection.GetCallMedia(), _playgroundConfig.TransferParticipantApi);

            // transfer participant option
            PhoneNumberIdentifier addTarget = new PhoneNumberIdentifier(phonenumberToTransfer);
            PhoneNumberIdentifier source = new PhoneNumberIdentifier(_playgroundConfig.ACS_DirectOffer_Phonenumber);

            CallInvite callInvite = new CallInvite(addTarget, source);
            TransferCallToParticipantResult transferCallToParticipantResult = await callConnection.TransferCallToParticipantAsync(callInvite);

            // TODO: exception handling when media fails to play
            await PlayHoldMusicLoopAsync(callConnection.GetCallMedia());

            TransferCallToParticipantEventResult transferCallToParticipantEventResult = await transferCallToParticipantResult.WaitForEventProcessorAsync();

            // Cancel media once the person is added or failed
            await callConnection.GetCallMedia().CancelAllMediaOperationsAsync();

            if (transferCallToParticipantEventResult.IsSuccessEvent)
            {
                // TODO... something when add participant
            }
            else
            {
                // TODO: unhappy path
            }

            return;
        }

        private async Task TerminateBlockAsync(CallConnection callConnection)
        {
            await callConnection.HangUpAsync(true);
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

        private async Task<string> RecognizePhonenumberAsync(
            CommunicationIdentifier target, 
            CallMedia callMedia, 
            Uri askPrompt)
        {
            // todo: add repeat capability
            // todo: prompt what you have entered is invalid, try again

            CallMediaRecognizeDtmfOptions callMediaRecognizeDtmfOptions = new CallMediaRecognizeDtmfOptions(target, 20);
            callMediaRecognizeDtmfOptions.Prompt = new FileSource(askPrompt);
            callMediaRecognizeDtmfOptions.InterruptPrompt = true;
            callMediaRecognizeDtmfOptions.StopTones = new List<DtmfTone> { DtmfTone.Pound, DtmfTone.Asterisk };

            StartRecognizingEventResult startRecognizingEventResult = await RecognizeBlockAsync(callMedia, callMediaRecognizeDtmfOptions);

            string phoneNumber = string.Empty;
            if (startRecognizingEventResult.IsSuccessEvent)
            {
                RecognizeCompleted recognizeCompleted = startRecognizingEventResult.SuccessEvent;
                var collectToneResult = (CollectTonesResult)recognizeCompleted.RecognizeResult; 

                return "+1" + collectToneResult.ConvertToString();
            }
            else
            {
                RecognizeFailed recognizeFailed = startRecognizingEventResult.FailureEvent;

                if (recognizeFailed.ReasonCode.Equals(ReasonCode.RecognizeInitialSilenceTimedOut))
                {
                    // TODO: do something special for this specific error?
                }

                // TODO: fix when unhappy path happens
            }

            return string.Empty;
        }
    }
}
