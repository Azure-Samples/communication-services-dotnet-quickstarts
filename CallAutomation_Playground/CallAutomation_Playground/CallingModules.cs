using Azure.Communication;
using Azure.Communication.CallAutomation;
using CallAutomation_Playground.Interfaces;

namespace CallAutomation_Playground
{
    /// <summary>
    /// Reusuable common calling actions for business needs
    /// </summary>
    public class CallingModules : ICallingModules
    {
        private readonly CallConnection _callConnection;
        private readonly PlaygroundConfigs _playgroundConfig;

        public CallingModules(
            CallConnection callConnection,
            PlaygroundConfigs playgroundConfig)
        {
            _callConnection = callConnection;
            _playgroundConfig = playgroundConfig;
        }

        public async Task<string> RecognizeTonesAsync(
            CommunicationIdentifier targetToRecognize,
            int minDigitToCollect,
            int maxDigitToCollect,
            Uri askPrompt,
            Uri retryPrompt)
        {
            for (int i = 0; i < 3; i++)
            {
                // prepare recognize tones
                CallMediaRecognizeDtmfOptions callMediaRecognizeDtmfOptions = new CallMediaRecognizeDtmfOptions(targetToRecognize, maxDigitToCollect);
                callMediaRecognizeDtmfOptions.Prompt = new FileSource(askPrompt);
                callMediaRecognizeDtmfOptions.InterruptPrompt = true;
                callMediaRecognizeDtmfOptions.InitialSilenceTimeout = TimeSpan.FromSeconds(10);
                callMediaRecognizeDtmfOptions.InterToneTimeout = TimeSpan.FromSeconds(10);
                callMediaRecognizeDtmfOptions.StopTones = new List<DtmfTone> { DtmfTone.Pound, DtmfTone.Asterisk };

                // Send request to recognize tones
                StartRecognizingCallMediaResult startRecognizingResult = await _callConnection.GetCallMedia().StartRecognizingAsync(callMediaRecognizeDtmfOptions);

                // Wait for recognize related event...
                StartRecognizingEventResult recognizeEventResult = await startRecognizingResult.WaitForEventProcessorAsync();

                if (recognizeEventResult.IsSuccess)
                {
                    // success recognition - return the tones detected.
                    RecognizeCompleted recognizeCompleted = recognizeEventResult.SuccessResult;
                    string dtmfTones = ((DtmfResult)recognizeCompleted.RecognizeResult).ConvertToString();

                    // check if it collected the minimum digit it collected
                    if (dtmfTones.Length >= minDigitToCollect)
                    {
                        return dtmfTones;
                    }
                }
                else
                {
                    // failed recognition - likely timeout
                    _ = recognizeEventResult.FailureResult;
                }

                // play retry prompt and retry again
                await PlayMessageThenWaitUntilItEndsAsync(retryPrompt);
            }

            throw new Exception("Retried 3 times, Failed to get tones.");
        }

        public async Task AddParticipantAsync(
            PhoneNumberIdentifier targetToAdd,
            Uri successPrompt,
            Uri failurePrompt,
            Uri musicPrompt)
        {
            // Send add participant request
            PhoneNumberIdentifier source = new PhoneNumberIdentifier(_playgroundConfig.DirectOfferedPhonenumber);
            CallInvite callInvite = new CallInvite(targetToAdd, source);
            AddParticipantResult addParticipantResult = await _callConnection.AddParticipantAsync(callInvite);

            // Play hold music while the participant is joining
            await PlayHoldMusicInLoopAsync(musicPrompt);

            // give maximum 40 seconds timeout for other party to answer the call,
            var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(40));
            AddParticipantEventResult addParticipantEventResult = await addParticipantResult.WaitForEventProcessorAsync(tokenSource.Token);

            // As soon as event comesback (or the timeout happens)
            // stop paying the music above
            await _callConnection.GetCallMedia().CancelAllMediaOperationsAsync();

            if (addParticipantEventResult.IsSuccess)
            {
                // Success joining - play message and return
                _ = addParticipantEventResult.SuccessResult;
                await PlayMessageThenWaitUntilItEndsAsync(successPrompt);
            }
            else
            {
                // failed joining - play message and return
                _ = addParticipantEventResult.FailureResult;
                await PlayMessageThenWaitUntilItEndsAsync(failurePrompt);
            }

        }

        public async Task RemoveAllParticipantExceptCallerAsync(CommunicationIdentifier originalCaller)
        {
            // List all participants in the call
            IReadOnlyList<CallParticipant> participantsList = (await _callConnection.GetParticipantsAsync()).Value;

            // go through the list and remove each one 
            foreach (var participant in participantsList)
            {
                // Remov all PSTN participants that is not the original caller
                if (participant.Identifier is PhoneNumberIdentifier && participant.Identifier.RawId != originalCaller.RawId)
                {
                    await _callConnection.RemoveParticipantAsync(participant.Identifier);
                }
            }

            return;
        }

        public async Task<bool> TransferCallAsync(
            PhoneNumberIdentifier transferTo,
            Uri failurePrompt)
        {
            // send transfer request
            TransferCallToParticipantResult transferCallToParticipantResult = await _callConnection.TransferCallToParticipantAsync(transferTo);

            TransferCallToParticipantEventResult transferCallToParticipantEventResult = await transferCallToParticipantResult.WaitForEventProcessorAsync();

            if (transferCallToParticipantEventResult.IsSuccess)
            {
                // successful transfer
                return true;
            }
            else
            {
                // failed transfer - play message and return
                _ = transferCallToParticipantEventResult.FailureResult;
                await PlayMessageThenWaitUntilItEndsAsync(failurePrompt);

                return false;
            }
        }

        public async Task PlayHoldMusicInLoopAsync(Uri musicPrompt)
        {
            // Play Hold Music in Loop, until cancelled with CancelAllMediaOperation
            FileSource fileSource = new FileSource(musicPrompt);
            PlayToAllOptions playOptions = new PlayToAllOptions(fileSource);
            playOptions.Loop = true;
            await _callConnection.GetCallMedia().PlayToAllAsync(playOptions);
        }

        public async Task PlayMessageThenWaitUntilItEndsAsync(Uri playPrompt)
        {
            // Play failure prompt and retry.
            FileSource fileSource = new FileSource(playPrompt);
            PlayResult playResult = await _callConnection.GetCallMedia().PlayToAllAsync(fileSource);

            // ... wait for play to complete, then return
            await playResult.WaitForEventProcessorAsync();
        }

        public async Task TerminateCallAsync()
        {
            // Terminate the call
            await _callConnection.HangUpAsync(true);
        }
    }
}
