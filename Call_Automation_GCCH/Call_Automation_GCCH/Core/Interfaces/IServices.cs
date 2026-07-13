using Call_Automation_GCCH.Core.Entities;

namespace Call_Automation_GCCH.Core.Interfaces;

/// <summary>
/// Core interface for call automation operations
/// Clean architecture: This belongs to the Core layer and defines the contract
/// </summary>
public interface ICallService
{
    /// <summary>
    /// Creates an outbound call with specified options
    /// </summary>
    Task<CallConnection> CreateCallAsync(CallOptions options, string callbackUri);

    /// <summary>
    /// Creates a group call with multiple participants
    /// </summary>
    Task<CallConnection> CreateGroupCallAsync(List<string> targets, string callbackUri, CallOptions? options = null);

    /// <summary>
    /// Transfers an active call to another participant
    /// </summary>
    Task<CallConnection> TransferCallAsync(string callConnectionId, string targetParticipant, string? transferee = null, bool isPstn = true);

    /// <summary>
    /// Terminates an active call
    /// </summary>
    Task<bool> HangupCallAsync(string callConnectionId, bool forEveryone = false);

    /// <summary>
    /// Gets call connection properties
    /// </summary>
    Task<CallConnection> GetCallPropertiesAsync(string callConnectionId);
}

/// <summary>
/// Interface for media operations
/// </summary>
public interface IMediaService
{
    Task<string> PlayAsync(string callConnectionId, string targetParticipant, bool isPstn, string? audioFileUrl = null, string? textToPlay = null, bool loop = false, bool interruptCallMediaOperation = false, string? operationContext = null);
    Task<string> PlayToAllAsync(string callConnectionId, string? audioFileUrl = null, string? textToPlay = null, bool loop = false, bool interruptCallMediaOperation = false, string? operationContext = null);
    Task<string> HoldAsync(string callConnectionId, string participantId, bool isPstn, string? playSourceId = null, string? operationContext = null);
    Task<string> UnholdAsync(string callConnectionId, string participantId, bool isPstn, string? operationContext = null);
    Task<string> StartMediaStreamingAsync(string callConnectionId, string? operationContext = null);
    Task<string> StopMediaStreamingAsync(string callConnectionId, string? operationContext = null);
    Task<string> CancelAllMediaOperationsAsync(string callConnectionId);
    Task<string> StartTranscriptionAsync(string callConnectionId, string locale = "en-US", string? operationContext = null);
    Task<string> StopTranscriptionAsync(string callConnectionId, string? operationContext = null);
    Task<string> UpdateTranscriptionAsync(string callConnectionId, string locale, string? operationContext = null);
    Task PlayAudioAsync(string callConnectionId, string audioUrl, bool loop = false);
    Task CancelMediaOperationsAsync(string callConnectionId);
}

/// <summary>
/// Interface for participant management
/// </summary>
public interface IParticipantService
{
    Task<string> AddParticipantAsync(string callConnectionId, string participantId, bool isPstn, string? operationContext = null, int? invitationTimeout = null);
    Task<string> RemoveParticipantAsync(string callConnectionId, string participantId, bool isPstn, string? operationContext = null);
    Task<ParticipantInfo> GetParticipantAsync(string callConnectionId, string participantId, bool isPstn);
    Task<IEnumerable<ParticipantInfo>> GetAllParticipantsAsync(string callConnectionId);
    Task<string> MuteParticipantAsync(string callConnectionId, string participantId, bool isPstn);
    Task<string> CancelAddParticipantAsync(string callConnectionId, string invitationId, string? operationContext = null);
    Task AddParticipantAsync(string callConnectionId, string participantId);
    Task RemoveParticipantAsync(string callConnectionId, string participantId);
    Task MuteParticipantAsync(string callConnectionId, string participantId);
}

public class ParticipantInfo
{
    public string RawId { get; set; } = string.Empty;
    public bool IsOnHold { get; set; }
    public bool IsMuted { get; set; }
}

/// <summary>
/// Interface for recording operations
/// </summary>
public interface IRecordingService
{
    Task<RecordingInfo> StartRecordingAsync(string callConnectionId, RecordingConfiguration? options = null);
    Task<string> PauseRecordingAsync(string recordingId);
    Task<string> ResumeRecordingAsync(string recordingId);
    Task<string> StopRecordingAsync(string recordingId);
    Task<RecordingStateInfo> GetRecordingStateAsync(string recordingId);
    Task<string> DeleteRecordingAsync(string recordingLocation);
    Task<byte[]> DownloadRecordingAsync(string downloadLocation);
    Task<string> StartRecordingAsync(string callConnectionId);
    Task StopRecordingAsync(string callConnectionId, string recordingId);
    Task PauseRecordingAsync(string callConnectionId, string recordingId);
    Task ResumeRecordingAsync(string callConnectionId, string recordingId);
}

public class RecordingInfo
{
    public string RecordingId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
}

public class RecordingStateInfo
{
    public string RecordingId { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}

public class RecordingConfiguration
{
    public bool IsAudioVideo { get; set; }
    public bool IsMixed { get; set; } = true;
    public string RecordingFormat { get; set; } = "Mp3";
    public bool PauseOnStart { get; set; }
    public string? ExternalStorageUri { get; set; }
    public List<ChannelAffinity>? ChannelAffinity { get; set; }
    public string? CallLocatorType { get; set; }
    public string? ServerCallId { get; set; }
    public string? GroupCallId { get; set; }
}

public class ChannelAffinity
{
    public string Participant { get; set; } = string.Empty;
    public int Channel { get; set; }
}

/// <summary>
/// Interface for configuration management
/// </summary>
public interface IConfigurationService
{
    Task<ApplicationConfiguration> GetConfigurationAsync();
    Task<ApplicationConfiguration> UpdateConfigurationAsync(ConfigurationUpdate update);
    Task<bool> SetConnectionStringAsync(string connectionString);
    Task<string> GetCallbackUriAsync();
}

public class ApplicationConfiguration
{
    public string? AcsConnectionString { get; set; }
    public string? PmaEndpoint { get; set; }
    public string? CallbackUriHost { get; set; }
    public string? AcsPhoneNumber { get; set; }
    public string? AudioFileUrl { get; set; }
}

public class ConfigurationUpdate
{
    public string? AcsConnectionString { get; set; }
    public string? PmaEndpoint { get; set; }
    public string? CallbackUriHost { get; set; }
    public string? AcsPhoneNumber { get; set; }
    public string? AudioFileUrl { get; set; }
}
