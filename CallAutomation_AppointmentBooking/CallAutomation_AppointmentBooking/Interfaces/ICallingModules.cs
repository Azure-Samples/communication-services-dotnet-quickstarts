using Azure.Communication.CallAutomation;
using Azure.Communication;

namespace CallAutomation_AppointmentBooking.Interfaces
{
    public interface ICallingModules
    {
        Task<string> RecognizeTonesAsync(CommunicationIdentifier targetToRecognize, int minDigitToCollect, int maxDigitToCollect, Uri askPrompt, Uri retryPrompt);

        Task PlayMessageThenWaitUntilItEndsAsync(Uri playPrompt);

        Task TerminateCallAsync();
    }
}
