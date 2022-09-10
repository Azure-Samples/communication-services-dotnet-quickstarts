using Newtonsoft.Json;

namespace IncomingCallRouting.Models
{
    public class MicrosoftTeamsUserIdentifierModel
    {
        /// <summary> The Id of the Microsoft Teams user. If not anonymous, this is the AAD object Id of the user. </summary>
        public string UserId { get; set; }
    }
}
