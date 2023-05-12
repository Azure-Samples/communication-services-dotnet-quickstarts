using Azure.Communication.CallAutomation;
using Azure.Communication;

namespace CallAutomation_Playground.Interfaces
{
    public interface ICallingModules
    {
        Task<string> RecognizeTonesAsync(CommunicationIdentifier targetToRecognize, int howManyDigitsToRecognize, Uri askPrompt, Uri retryPrompt);

        Task AddParticipantAsync(PhoneNumberIdentifier targetToAdd, Uri successPrompt, Uri failurePrompt, Uri musicPrompt);

        Task RemoveAllParticipantExceptCallerAsync(CommunicationIdentifier originalCaller);

        Task<bool> TransferCallAsync(PhoneNumberIdentifier transferTo, Uri failurePrompt);

        Task PlayHoldMusicInLoopAsync(Uri musicPrompt);

        Task PlayMessageThenWaitUntilItEndsAsync(Uri playPrompt);

        Task TerminateCallAsync();
    }
}
