using Azure.Communication;
using Azure.Communication.CallAutomation;
using CallAutomation_AppointmentBooking.Interfaces; 

namespace CallAutomation_AppointmentBooking
{
    /// <summary>
    /// Reusuable common calling actions for business needs
    /// </summary>
    public class CallingModules : ICallingModules
    {
        private readonly CallConnection _callConnection;
        private readonly AppointmentBookingConfig _appointmentBookingConfig;

        public CallingModules(
            CallConnection callConnection,
            AppointmentBookingConfig appointmentBookingConfig)
        {
            _callConnection = callConnection;
            _appointmentBookingConfig = appointmentBookingConfig;
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
