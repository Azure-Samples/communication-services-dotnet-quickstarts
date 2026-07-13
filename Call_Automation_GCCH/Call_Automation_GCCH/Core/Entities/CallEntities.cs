namespace Call_Automation_GCCH.Core.Entities;

/// <summary>
/// Represents a call connection in the system
/// </summary>
public class CallConnection
{
    public string CallConnectionId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents options for creating a call
/// </summary>
public class CallOptions
{
    public string Target { get; set; } = string.Empty;
    public bool IsPstn { get; set; }
    public string? OperationContext { get; set; }
    public TranscriptionConfiguration? Transcription { get; set; }
    public MediaStreamingConfiguration? MediaStreaming { get; set; }
    public CallIntelligenceConfiguration? CallIntelligence { get; set; }
}

/// <summary>
/// Transcription configuration
/// </summary>
public class TranscriptionConfiguration
{
    public string Locale { get; set; } = "en-US";
    public bool StartTranscription { get; set; }
    public bool EnableIntermediateResults { get; set; }
}

/// <summary>
/// Media streaming configuration
/// </summary>
public class MediaStreamingConfiguration
{
    public bool StartMediaStreaming { get; set; }
    public string MediaStreamingAudioChannel { get; set; } = "Mixed";
    public bool EnableBidirectional { get; set; }
    public string AudioFormat { get; set; } = "Pcm16KMono";
}

/// <summary>
/// Call intelligence configuration
/// </summary>
public class CallIntelligenceConfiguration
{
    public string CognitiveServicesEndpoint { get; set; } = string.Empty;
}
