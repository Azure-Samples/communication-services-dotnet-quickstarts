using Newtonsoft.Json;

namespace IncomingCallRouting.Models
{
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
}
