using System.Collections.Generic;
using Azure.Communication;
using IncomingCallRouting.Enums;

namespace IncomingCallRouting.Events
{
    public class AddParticipantsFailedEvent : AcsCallbackEvent
    {
        /// <summary>
        /// Operation context
        /// </summary>
        public string OperationContext { get; set; }
        
        public ResultInformation ResultInfo { get; set; }

        /// <summary>
        /// Participants failed to be added
        /// </summary>
        public List<CommunicationIdentifier> Participants { get; set; }

        public override AcsEventType Type { get; set; } = AcsEventType.AddParticipantsFailed;
    }
}
