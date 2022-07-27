using System.Collections.Generic;
using Azure.Communication;
using IncomingCallRouting.Enums;

namespace IncomingCallRouting.Events
{
    public class ParticipantsUpdatedEvent : AcsCallbackEvent
    {
        /// <summary>
        /// List of current participants in the call.
        /// </summary>
        /// <value></value>
        public IEnumerable<CommunicationIdentifier> Participants { get; set; }

        public override AcsEventType Type { get; set; } = AcsEventType.ParticipantsUpdated;
    }
}
