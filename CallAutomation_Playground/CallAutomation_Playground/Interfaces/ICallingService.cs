using Azure.Communication;
using Azure.Communication.CallAutomation;

namespace CallAutomation_Playground.Interfaces;

public interface ICallingService
{
    Task<CallConnectionProperties> GetCallConnectionProperties(string callConnectionId);

    Task AddParticipant(CallConnectionProperties callConnectionProperties, Func<AddParticipantSucceeded, Task>? success, Func<AddParticipantFailed, Task>? failed, CallInvite callInvite);

    Task RemoveParticipant(CallConnectionProperties callConnectionProperties, Func<RemoveParticipantSucceeded, Task>? success, Func<RemoveParticipantFailed, Task>? failed, CommunicationIdentifier target);

    Task<IReadOnlyList<CallParticipant>> GetCallParticipantList(CallConnectionProperties callConnectionProperties);

    Task RecognizeDtmfInput(CallConnectionProperties callConnectionProperties, Func<CollectTonesResult?, Task>? success, Func<RecognizeFailed, Task>? failed, CallMediaRecognizeDtmfOptions callMediaRecognizeDtmfOptions);

    Task PlayHoldMusic(CallConnectionProperties callConnectionProperties, Uri fileUri, CommunicationIdentifier? target, bool loop = false);

    Task StartRecording(CallConnectionProperties callConnectionProperties);

    Task TransferCallLeg(CallConnectionProperties callConnectionProperties, Func<CallTransferAccepted, Task>? success, Func<CallTransferFailed, Task>? failed, CallInvite callInvite);

    Task CancelMedia(CallConnectionProperties callConnectionProperties);

    Task HangUp(CallConnectionProperties callConnectionProperties, bool terminateCall);
}