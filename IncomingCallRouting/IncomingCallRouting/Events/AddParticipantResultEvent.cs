using Azure.Communication;

namespace IncomingCallRouting.Events
{
    /// <summary>
    /// The add participant result event.
    /// </summary>
    public class AddParticipantResultEvent : OperationResultEvent
    {
        public CommunicationIdentifier Participant { get; set; }
    }
}