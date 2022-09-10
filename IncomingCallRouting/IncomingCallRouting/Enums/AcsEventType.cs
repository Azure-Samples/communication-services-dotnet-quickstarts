namespace IncomingCallRouting.Enums
{
    public enum AcsEventType
    {
        Unknown,
        CallConnected,
        CallDisconnected,
        CallTransferAccepted,
        CallTransferFailed,
        PlayCompleted,
        PlayFailed,
        RecognizeCompleted,
        RecognizeFailed,
        CallRecordingStateChanged,
        AddParticipantsSucceeded,
        AddParticipantsFailed,
        ParticipantsUpdated,
    }
}
