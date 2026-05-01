using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Call_Automation_GCCH.Models
{
    /// <summary>
    /// Request body for adding a participant to a call.
    /// 
    /// Example: { "callConnectionId": "...", "participantId": "+18001234567", "isPstn": true }
    /// </summary>
    public class AddParticipantRequest
    {
        /// <summary>The active call connection ID.</summary>
        [Required] public string CallConnectionId { get; set; } = default!;

        /// <summary>ACS user ID (8:...) or phone number (+...).</summary>
        [Required] public string ParticipantId { get; set; } = default!;

        /// <summary>True if participant is a PSTN phone number.</summary>
        public bool IsPstn { get; set; } = false;

        /// <summary>Seconds to wait before the invitation times out.</summary>
        public int InvitationTimeoutInSeconds { get; set; } = 30;

        /// <summary>Custom context for correlating events.</summary>
        public string? OperationContext { get; set; }
    }

    /// <summary>
    /// Request body for removing a participant from a call.
    /// 
    /// Example: { "callConnectionId": "...", "participantId": "+18001234567", "isPstn": true }
    /// </summary>
    public class RemoveParticipantRequest
    {
        [Required] public string CallConnectionId { get; set; } = default!;

        /// <summary>ACS user ID (8:...) or phone number (+...).</summary>
        [Required] public string ParticipantId { get; set; } = default!;

        /// <summary>True if participant is a PSTN phone number.</summary>
        public bool IsPstn { get; set; } = false;
        public string? OperationContext { get; set; }
    }

    /// <summary>
    /// Request body for cancelling a pending add-participant invitation.
    /// 
    /// Example: { "callConnectionId": "...", "invitationId": "..." }
    /// </summary>
    public class CancelAddParticipantRequest
    {
        [Required] public string CallConnectionId { get; set; } = default!;
        [Required] public string InvitationId { get; set; } = default!;
        public string? OperationContext { get; set; }
    }

    /// <summary>
    /// Request body for getting or muting a participant.
    /// 
    /// Example: { "callConnectionId": "...", "participantId": "+18001234567", "isPstn": true }
    /// </summary>
    public class ParticipantIdentifierRequest
    {
        [Required] public string CallConnectionId { get; set; } = default!;

        /// <summary>ACS user ID (8:...) or phone number (+...).</summary>
        [Required] public string ParticipantId { get; set; } = default!;

        /// <summary>True if participant is a PSTN phone number.</summary>
        public bool IsPstn { get; set; } = false;
    }
}
