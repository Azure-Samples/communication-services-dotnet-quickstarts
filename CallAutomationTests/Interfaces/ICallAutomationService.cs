// © Microsoft Corporation. All rights reserved.

using Azure;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using CallAutomation.Scenarios.Handlers;

namespace CallAutomation.Scenarios.Interfaces
{
    public interface ICallAutomationService
    {
        Task<AnswerCallResult> AnswerCallAsync(IncomingCallEvent incomingCallEvent);
        Task<CreateCallResult> CreateCallAsync(string callerId);
        CallConnection GetCallConnection(string callConnectionId);
        Task PlayMenuChoiceAsync(DtmfTone choiceOrTone, CallMedia callMedia, string textToSpeechLocale, string? prerollText = null);
        Task PlayMenuOptionsAsync(string callerId, CallMedia callMedia, string textToSpeechLocale, string? prerollText = null, CancellationToken cancellationToken = default);
        Task PlayCallbackOfferOptionsAsync(string callerId, CallMedia callMedia, string textToSpeechLocale, string? prerollText = null);
        Task<bool> PlayCallbackOfferChoiceAsync(string callerId, DtmfTone choiceOrTone, CallMedia callMedia, string textToSpeechLocale);
        Task PlayCallbackTimeSelectionOptionsAsync(string callerId, CallMedia callMedia, string textToSpeechLocale, string? prerollText = null);
        Task<TimeSpan?> PlayCallbackTimeSelectionChoiceAsync(string callerId, DtmfTone choiceOrTone, CallMedia callMedia, string textToSpeechLocale, double estimatedWaitTime);
        Task PlayCallbackDialoutOptionsAsync(string callerId, CallMedia callMedia, string textToSpeechLocale, string? prerollText = null);
        Task StartRecognizingDtmfAsync(string callerId, string operationContext, CallMedia callMedia, string textToSpeechLocale, int initialSilenceTimeout, string? prerollText = null, CancellationToken cancellationToken = default);
        Task PlayHoldMusicAsync(string operationContext, CallMedia callMedia, bool loop = true);
        Task PlayPairingCompletedAsync(string department, string operationContext, CallMedia callMedia, string textToSpeechLocale);
        Task PlayEstimatedWaitTimeAsync(double estimatedWaitTime, string operationContext, CallMedia callMedia, string textToSpeechLocale);
        Task PlaceCallOnHoldAsync(string callConnectionId, string operationContext, string[] agentAcsIds, bool playMusic = true);
        Task AddParticipantAsync(string callConnectionId, string operationContext, string? acsId, int? invitationTimeoutInSeconds = 120);
        Task RemoveParticipantAsync(string callConnectionId, string operationContext, string acsId);
        Task PlayTextToSpeechToAllAsync(CallMedia callMedia, string audioText, string operationContext, string audioTextLocale = "en-US", bool loop = false);
        Task EndCallAsync(string callConnectionId, string operationContext);
        Task CancelAllMediaOperationsAsync(CallMedia callMedia, string callConnectionId, string operationContext);
        void ProcessEvents(CloudEvent[] cloudEvents);
        IConfigurationSection GetIvrText();
        IConfigurationSection GetIvrConfig();
        IConfigurationSection GetQueuesConfig();
        string[] GetAllowedIncomingIdentitiesList();
        IDictionary<string, DtmfTone> GetAllRecognizedPhrasesAsDtmfTones();

        Task<RecordingStateResult> StartRecordingAsync(string serverCallId);
        Task<Response> StopRecordingAsync(string recordingId);
        //Task<Response> GetRecordingFileEvent(Object request);
        Task ProcessFile(string downloadLocation, string documentId, string fileFormat, string downloadType);
    }
}
