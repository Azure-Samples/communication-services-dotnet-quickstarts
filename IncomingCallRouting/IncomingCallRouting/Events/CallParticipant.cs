using System.ComponentModel.DataAnnotations;
using Azure.Communication;

namespace IncomingCallRouting.Events
{
    /// <summary>
    /// A participant in a call.
    /// </summary>
    public class CallParticipant
    {
        /// <summary>
        /// Communication identifier of the participant
        /// </summary>
        [Required]
        public CommunicationIdentifier Identifier { get; set; }

        /// <summary>
        /// Participant id
        /// </summary>
        public string ParticipantId { get; set; }

        /// <summary>
        /// Is participant muted
        /// </summary>
        [Required]
        public bool IsMuted { get; set; }
        
    }
}
