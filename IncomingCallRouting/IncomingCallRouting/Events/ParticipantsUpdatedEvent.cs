using System.Collections.Generic;
using Azure.Communication.CallingServer;

namespace IncomingCallRouting.Events
{
    /// <summary>
    /// The participant update event
    /// </summary>
    public class ParticipantsUpdatedEvent : CallingServerEventBase
    {
        /// <summary>
        /// The call connection id.
        /// </summary>
        public string CallConnectionId { get; set; }

        /// <summary>
        /// The server call locator.
        /// </summary>
        public CallLocator CallLocator { get; set; }

        /// <summary>
        /// The list of participants.
        /// </summary>
        public IEnumerable<CallParticipant> Participants { get; set; }
    }
}
