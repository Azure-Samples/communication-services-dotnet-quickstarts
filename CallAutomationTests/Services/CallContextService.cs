using CallAutomation.Scenarios.Handlers;
using CallAutomation.Scenarios.Interfaces;
using System.Collections.Concurrent;

namespace CallAutomation.Scenarios.Services
{
    public class CallContextService : ICallContextService
    {
        private ConcurrentDictionary<string, double> _callConnectionIdToEstimatedWaitTime;
        private ConcurrentDictionary<string, string> _callConnectionIdToClassification;
        private ConcurrentDictionary<string, string> _callConnectionIdToOriginatingJobId;
        private ConcurrentDictionary<string, string> _callConnectionIdToAccountId;
        private ConcurrentDictionary<string, string> _callConnectionIdToCustomerAcsId;
        private ConcurrentDictionary<string, ConcurrentBag<string>> _callConnectionIdToAgentAcsId;
        private ConcurrentDictionary<string, CancellationTokenSource> _callConnectionIdToAccountIdSpeechRecognizerCancellationTokenSource;
        private ConcurrentDictionary<string, CancellationTokenSource> _callConnectionIdToPairingSpeechRecognizerCancellationTokenSource;
        private ConcurrentDictionary<string, CancellationTokenSource> _callConnectionIdToMainMenuSpeechRecognizerCancellationTokenSource;
        private ConcurrentDictionary<string, string> _callConnectionIdToMediaSubscriptionId;
        private ConcurrentDictionary<string, string> _callConnectionIdToCallSummary;
        private ConcurrentDictionary<string, RecordingContext> _serverCallIdToRecordingContext;

        public CallContextService()
        {
            _callConnectionIdToEstimatedWaitTime = new ConcurrentDictionary<string, double>();
            _callConnectionIdToClassification = new ConcurrentDictionary<string, string>();
            _callConnectionIdToOriginatingJobId = new ConcurrentDictionary<string, string>();
            _callConnectionIdToAccountId = new ConcurrentDictionary<string, string>();
            _callConnectionIdToCustomerAcsId = new ConcurrentDictionary<string, string>();
            _callConnectionIdToAgentAcsId = new ConcurrentDictionary<string, ConcurrentBag<string>>();
            //_callMediaSubscriptionIdToAudioStream = new ConcurrentDictionary<string, AudioStream>();
            _callConnectionIdToAccountIdSpeechRecognizerCancellationTokenSource = new ConcurrentDictionary<string, CancellationTokenSource>();
            _callConnectionIdToPairingSpeechRecognizerCancellationTokenSource = new ConcurrentDictionary<string, CancellationTokenSource>();
            _callConnectionIdToMediaSubscriptionId = new ConcurrentDictionary<string, string>();
            _callConnectionIdToCallSummary = new ConcurrentDictionary<string, string>();
            _callConnectionIdToMainMenuSpeechRecognizerCancellationTokenSource = new ConcurrentDictionary<string, CancellationTokenSource>();
            _serverCallIdToRecordingContext = new ConcurrentDictionary<string, RecordingContext>();
        }

        public RecordingContext? GetRecordingContext(string serverCallId)
        {
            if (_serverCallIdToRecordingContext.TryGetValue(serverCallId, out var recordingContext))
            {
                return recordingContext;
            }

            return null;
        }

        public void SetRecordingContext(string serverCallId, RecordingContext recordingContext)
        {
            _serverCallIdToRecordingContext.AddOrUpdate(serverCallId, recordingContext, (_, _) => recordingContext);
        }

        public string? GetCustomerId(string callConnectionId)
        {
            if (_callConnectionIdToAccountId.TryGetValue(callConnectionId, out var accountId))
            {
                return accountId;
            }

            return null;
        }

        public bool SetCustomerId(string callConnectionId, string accountId)
        {
            return _callConnectionIdToAccountId.TryAdd(callConnectionId, accountId);
        }

        public bool RemoveCustomerId(string callConnectionId)
        {
            return _callConnectionIdToAccountId.TryRemove(callConnectionId, out _);
        }

        public string? GetCustomerAcsId(string callConnectionId)
        {
            if (_callConnectionIdToCustomerAcsId.TryGetValue(callConnectionId, out var acsId))
            {
                return acsId;
            }

            return null;
        }

        public bool SetCustomerAcsId(string callConnectionId, string acsId)
        {
            return _callConnectionIdToCustomerAcsId.TryAdd(callConnectionId, acsId);
        }

        public bool RemoveCustomerAcsId(string callConnectionId)
        {
            return _callConnectionIdToCustomerAcsId.TryRemove(callConnectionId, out _);
        }

        public double SetEstimatedWaitTime(string callConnectionId, double estimatedWaitTime)
        {
            return _callConnectionIdToEstimatedWaitTime.AddOrUpdate(callConnectionId, estimatedWaitTime, (_, _) => estimatedWaitTime);
        }

        public double? GetEstimatedWaitTime(string callConnectionId)
        {
            if (_callConnectionIdToEstimatedWaitTime.TryGetValue(callConnectionId, out var estimatedWaitTime))
            {
                return estimatedWaitTime;
            }

            return null;
        }

        public bool RemoveOriginatingJobId(string callConnectionId)
        {
            return _callConnectionIdToOriginatingJobId.TryRemove(callConnectionId, out _);
        }

        public bool RemoveClassification(string callConnectionId)
        {
            return _callConnectionIdToClassification.TryRemove(callConnectionId, out _);
        }

        public string SetOriginatingJobId(string callConnectionId, string originatingJobId)
        {
            return _callConnectionIdToOriginatingJobId.AddOrUpdate(callConnectionId, originatingJobId, (_, _) => originatingJobId);
        }

        public string? GetOriginatingJobId(string callConnectionId)
        {
            if (_callConnectionIdToOriginatingJobId.TryGetValue(callConnectionId, out var originatingJobId))
            {
                return originatingJobId;
            }

            return null;
        }

        public bool RemoveEstimatedWaitTime(string callConnectionId)
        {
            return _callConnectionIdToClassification.TryRemove(callConnectionId, out _);
        }

        public string SetClassification(string callConnectionId, string classification)
        {
            return _callConnectionIdToClassification.AddOrUpdate(callConnectionId, classification, (_, _) => classification);
        }

        public string? GetClassification(string callConnectionId)
        {
            if (_callConnectionIdToClassification.TryGetValue(callConnectionId, out var classification))
            {
                return classification;
            }

            return null;
        }

        public string[] GetAgentAcsIds(string callConnectionId)
        {
            if (_callConnectionIdToAgentAcsId.TryGetValue(callConnectionId, out var agentAcsIds))
            {
                return agentAcsIds.ToArray();
            }

            return Array.Empty<string>();
        }

        public bool AddAgentAcsId(string callConnectionId, string acsId)
        {
            if (_callConnectionIdToAgentAcsId.TryGetValue(callConnectionId, out var agents))
            {
                if (!agents.Contains(acsId))
                {
                    agents.Add(acsId);
                }

                return true;
            }

            return _callConnectionIdToAgentAcsId.TryAdd(callConnectionId, new ConcurrentBag<string> { acsId });
        }

        public bool RemoveAgentAcsAcsIds(string callConnectionId)
        {
            return _callConnectionIdToAgentAcsId.TryRemove(callConnectionId, out _);
        }


        public bool SetAccountIdSpeechRecognizerCancellationTokenSource(string callConnectionId, CancellationTokenSource cancellationTokenSource)
        {
            return _callConnectionIdToAccountIdSpeechRecognizerCancellationTokenSource.TryAdd(callConnectionId, cancellationTokenSource);
        }

        public CancellationTokenSource? GetAccountIdSpeechRecognizerCancellationTokenSource(string callConnectionId)
        {
            if (_callConnectionIdToAccountIdSpeechRecognizerCancellationTokenSource.TryGetValue(callConnectionId, out var cancellationTokenSource))
            {
                return cancellationTokenSource;
            }

            return null;
        }

        public bool RemoveAccountIdSpeechRecognizerCancellationTokenSource(string callConnectionId)
        {
            return _callConnectionIdToAccountIdSpeechRecognizerCancellationTokenSource.TryRemove(callConnectionId, out _);
        }

        public bool SetPairingSpeechRecognizerCancellationTokenSource(string callConnectionId, CancellationTokenSource cancellationTokenSource)
        {
            return _callConnectionIdToPairingSpeechRecognizerCancellationTokenSource.TryAdd(callConnectionId, cancellationTokenSource);
        }

        public CancellationTokenSource? GetPairingSpeechRecognizerCancellationTokenSource(string callConnectionId)
        {
            if (_callConnectionIdToPairingSpeechRecognizerCancellationTokenSource.TryGetValue(callConnectionId, out var cancellationTokenSource))
            {
                return cancellationTokenSource;
            }

            return null;
        }

        public bool RemovePairingSpeechRecognizerCancellationTokenSource(string callConnectionId)
        {
            return _callConnectionIdToPairingSpeechRecognizerCancellationTokenSource.TryRemove(callConnectionId, out _);
        }

        public string? GetMediaSubscriptionId(string callConnectionId)
        {
            if (_callConnectionIdToMediaSubscriptionId.TryGetValue(callConnectionId, out var accountId))
            {
                return accountId;
            }

            return null;
        }

        public bool SetMediaSubscriptionId(string callConnectionId, string mediaSubscriptionId)
        {
            return _callConnectionIdToMediaSubscriptionId.TryAdd(callConnectionId, mediaSubscriptionId);
        }

        public bool RemoveMediaSubscriptionId(string callConnectionId)
        {
            return _callConnectionIdToMediaSubscriptionId.TryRemove(callConnectionId, out _);
        }

        public string? GetCallSummary(string callConnectionId)
        {
            if (_callConnectionIdToCallSummary.TryGetValue(callConnectionId, out var summary))
            {
                return summary;
            }

            return null;
        }

        public bool SetCallSummary(string callConnectionId, string summary)
        {
            return _callConnectionIdToCallSummary.TryAdd(callConnectionId, summary);
        }

        public bool RemoveCallSummary(string callConnectionId)
        {
            return _callConnectionIdToCallSummary.TryRemove(callConnectionId, out _);
        }

        public bool SetMainMenuSpeechRecognizerCancellationTokenSource(string callConnectionId, CancellationTokenSource cancellationTokenSource)
        {
            return _callConnectionIdToMainMenuSpeechRecognizerCancellationTokenSource.TryAdd(callConnectionId, cancellationTokenSource);
        }

        public CancellationTokenSource? GetMainMenuSpeechRecognizerCancellationTokenSource(string callConnectionId)
        {
            if (_callConnectionIdToMainMenuSpeechRecognizerCancellationTokenSource.TryGetValue(callConnectionId, out var cancellationTokenSource))
            {
                return cancellationTokenSource;
            }

            return null;
        }

        public bool RemoveMainMenuSpeechRecognizerCancellationTokenSource(string callConnectionId)
        {
            return _callConnectionIdToMainMenuSpeechRecognizerCancellationTokenSource.TryRemove(callConnectionId, out _);
        }

        public bool RemoveAudioStream(string callMediaSubscriptionId)
        {
            throw new NotImplementedException();
        }
    }
}
