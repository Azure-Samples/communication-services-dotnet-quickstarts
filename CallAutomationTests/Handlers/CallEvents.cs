// © Microsoft Corporation. All rights reserved.

using Azure.Messaging.EventGrid.SystemEvents;

namespace CallAutomation.Scenarios.Handlers
{
    public class IncomingCallEvent
    {
        public CommunicationIdentifierModel? To { get; set; }
        public CommunicationIdentifierModel? From { get; set; }
        public string? CallerDisplayName { get; set; }
        public string? ServerCallId { get; set; }
        public string? IncomingCallContext { get; set; }
    }

    public class OutboundCallEvent
    {
        public string? TargetId { get; set; }
    }
    
    public class StartRecordingEvent
    {
        public string? serverCallId { get; set; }
    }
    public class StopRecordingEvent
    {
        public string? serverCallId { get; set; }
        public string? recordingId { get; set; }
    }

    public class RecordingFileStatusUpdatedEvent
    {
        public AcsRecordingStorageInfoProperties RecordingStorageInfo { get; }
        /// <summary> The time at which the recording started. </summary>
        public DateTimeOffset? RecordingStartTime { get; }
        /// <summary> The recording duration in milliseconds. </summary>
        public long? RecordingDurationMs { get; }
        /// <summary> The recording content type- AudioVideo, or Audio. </summary>
        public AcsRecordingContentType? ContentType { get; }
        /// <summary> The recording  channel type - Mixed, Unmixed. </summary>
        public AcsRecordingChannelType? ChannelType { get; }
        /// <summary> The recording format type - Mp4, Mp3, Wav. </summary>
        public AcsRecordingFormatType? FormatType { get; }
        /// <summary> The reason for ending recording session. </summary>
        public string SessionEndReason { get; }
    }

    public class CommunicationIdentifierModel
    {
        /// <summary> Raw Id of the identifier. Optional in requests, required in responses. </summary>
        public string RawId { get; set; }

        /// <summary> The communication user. </summary>
        public CommunicationUserIdentifierModel CommunicationUser { get; set; }

        /// <summary> The phone number. </summary>
        public PhoneNumberIdentifierModel PhoneNumber { get; set; }

        /// <summary> The Microsoft Teams user. </summary>
        public MicrosoftTeamsUserIdentifierModel MicrosoftTeamsUser { get; set; }
    }

    public class CommunicationUserIdentifierModel
    {
        /// <summary> The Id of the communication user. </summary>
        public string Id { get; set; }
    }

    public class PhoneNumberIdentifierModel
    {
        /// <summary> The phone number in E.164 format. </summary>
        public string Value { get; set; }
    }

    public class MicrosoftTeamsUserIdentifierModel
    {
        /// <summary> The Id of the Microsoft Teams user. If not anonymous, this is the AAD object Id of the user. </summary>
        public string UserId { get; set; }
    }
}
