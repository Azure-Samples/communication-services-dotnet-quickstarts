using Azure.Communication;
using Azure.Communication.CallAutomation;

namespace CallAutomation.Playground.Interfaces;

public interface ICallingServices
{
    Task AddParticipant(CallConnectionProperties callConnectionProperties, Delegate? executeWhileWaiting,
        Func<AddParticipantSucceeded, Task>? success, Func<AddParticipantFailed, Task>? failed, CallInvite callInvite,
        CancellationToken cancellationToken = default);

    Task RemoveParticipant(CallConnectionProperties callConnectionProperties, Delegate? executeWhileWaiting,
        Func<RemoveParticipantSucceeded, Task>? success, Func<RemoveParticipantFailed, Task>? failed,
        CommunicationIdentifier target, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CallParticipant>> GetCallParticipantList(CallConnectionProperties callConnectionProperties,
        CancellationToken cancellationToken = default);

    Task RecognizeDtmfInput(CallConnectionProperties callConnectionProperties, Delegate? executeWhileWaiting,
        Func<CollectTonesResult, Task>? success, Func<RecognizeFailed, Task>? failed,
        CallMediaRecognizeDtmfOptions callMediaRecognizeDtmfOptions, CancellationToken cancellationToken = default);

    Task PlayAudio(CallConnectionProperties callConnectionProperties, Uri fileUri, CommunicationIdentifier? target,
        bool loop = default, CancellationToken cancellationToken = default);

    Task StartRecording(CallConnectionProperties callConnectionProperties,
        CancellationToken cancellationToken = default);

    Task TransferCallLeg(CallConnectionProperties callConnectionProperties, Delegate? executeWhileWaiting,
        Func<CallTransferAccepted, Task>? success, Func<CallTransferFailed, Task>? failed, CallInvite callInvite,
        CancellationToken cancellationToken = default);

    Task CancelMedia(CallConnectionProperties callConnectionProperties, CancellationToken cancellationToken = default);

    Task HangUp(CallConnectionProperties callConnectionProperties, bool terminateCall,
        CancellationToken cancellationToken = default);
}