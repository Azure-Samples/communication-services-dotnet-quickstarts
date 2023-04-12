// © Microsoft Corporation. All rights reserved.

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

    // TODO: investigate why we can't parse using the SDK CommunicationUserIdentifier, out of date?

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
