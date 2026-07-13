using System.ComponentModel.DataAnnotations;

namespace Call_Automation_GCCH.Presentation.DTOs;

/// <summary>
/// Request DTOs for Participants Controller (Clean Architecture)
/// </summary>

public class ParticipantAddRequest
{
    [Required] public string CallConnectionId { get; set; } = string.Empty;
    [Required] public string ParticipantId { get; set; } = string.Empty;
    public bool IsPstn { get; set; }
    public string? OperationContext { get; set; }
    public int? InvitationTimeout { get; set; }
}

public class ParticipantRemoveRequest
{
    [Required] public string CallConnectionId { get; set; } = string.Empty;
    [Required] public string ParticipantId { get; set; } = string.Empty;
    public bool IsPstn { get; set; }
    public string? OperationContext { get; set; }
}

public class ParticipantMuteRequest
{
    [Required] public string CallConnectionId { get; set; } = string.Empty;
    [Required] public string ParticipantId { get; set; } = string.Empty;
    public bool IsPstn { get; set; }
}

public class ParticipantCancelAddRequest
{
    [Required] public string CallConnectionId { get; set; } = string.Empty;
    [Required] public string InvitationId { get; set; } = string.Empty;
    public string? OperationContext { get; set; }
}
