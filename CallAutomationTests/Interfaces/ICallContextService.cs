namespace CallAutomation.Scenarios.Interfaces
{
    public interface ICallContextService
    {
        // TODO: collapse these into a POCO
        string? GetCustomerId(string callConnectionId);
        bool SetCustomerId(string callConnectionId, string accountId);
        bool RemoveCustomerId(string callConnectionId);
        string? GetCustomerAcsId(string callConnectionId);
        bool SetCustomerAcsId(string callConnectionId, string acsId);
        double SetEstimatedWaitTime(string callConnectionId, double estimatedWaitTime);
        double? GetEstimatedWaitTime(string callConnectionId);
        bool RemoveEstimatedWaitTime(string callConnectionId);
        string SetClassification(string callConnectionId, string classification);
        string? GetClassification(string callConnectionId);
        bool RemoveClassification(string callConnectionId);
        string SetOriginatingJobId(string callConnectionId, string originatingJobId);
        string? GetOriginatingJobId(string callConnectionId);
        bool RemoveOriginatingJobId(string callConnectionId);
        bool RemoveCustomerAcsId(string callConnectionId);
        string[] GetAgentAcsIds(string callConnectionId);
        bool AddAgentAcsId(string callConnectionId, string acsId);
        bool RemoveAgentAcsAcsIds(string callConnectionId);
        bool RemoveAudioStream(string callMediaSubscriptionId);
        bool SetAccountIdSpeechRecognizerCancellationTokenSource(string callConnectionId, CancellationTokenSource cancellationTokenSource);
        CancellationTokenSource? GetAccountIdSpeechRecognizerCancellationTokenSource(string callConnectionId);
        bool RemoveAccountIdSpeechRecognizerCancellationTokenSource(string callConnectionId);
        bool SetPairingSpeechRecognizerCancellationTokenSource(string callConnectionId, CancellationTokenSource cancellationTokenSource);
        CancellationTokenSource? GetPairingSpeechRecognizerCancellationTokenSource(string callConnectionId);
        bool RemovePairingSpeechRecognizerCancellationTokenSource(string callConnectionId);
        string? GetMediaSubscriptionId(string callConnectionId);
        bool SetMediaSubscriptionId(string callConnectionId, string mediaSubscriptionId);
        bool RemoveMediaSubscriptionId(string callConnectionId);
        string? GetCallSummary(string callConnectionId);
        bool SetCallSummary(string callConnectionId, string summary);
        bool RemoveCallSummary(string callConnectionId);
        bool SetMainMenuSpeechRecognizerCancellationTokenSource(string callConnectionId, CancellationTokenSource cancellationTokenSource);
        CancellationTokenSource? GetMainMenuSpeechRecognizerCancellationTokenSource(string callConnectionId);
        bool RemoveMainMenuSpeechRecognizerCancellationTokenSource(string callConnectionId);
    }
}
