using System.Text.Json.Serialization;

namespace Call_Automation_GCCH.Models
{
    public class CommunicationConfiguration
    {
        [JsonPropertyName("acsConnectionString")]
        public string AcsConnectionString { get; set; }
        [JsonPropertyName("acsPhoneNumber")]
        public string AcsPhoneNumber { get; set; }
        [JsonPropertyName("callbackUriHost")]
        public string CallbackUriHost { get; set; }
        [JsonPropertyName("pmaEndpoint")]
        public string PmaEndpoint { get; set; }
        // ACS GCCH Phase 2
        // [JsonPropertyName("cognitiveServiceEndpoint")]
        // public string CongnitiveServiceEndpoint { get; set; }
    }
}
