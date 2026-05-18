namespace Call_Automation_GCCH.Models
{
    /// <summary>
    /// Application settings for Azure Communication Services.
    /// Can be pre-configured in appsettings.json or set at runtime via the
    /// /api/configuration endpoints in Swagger.
    /// </summary>
    public class AcsCommunicationSettings
    {
        public string? AcsConnectionString { get; set; }
        public string? AcsPhoneNumber { get; set; }
        public string? PmaEndpoint { get; set; }
        public string? CallbackUriHost { get; set; }

        /// <summary>
        /// Optional external URL for the audio/prompt file (e.g. Azure Blob Storage with a trusted cert).
        /// When set, this is used instead of the self-hosted /audio/prompt.wav endpoint,
        /// avoiding SSL certificate validation failures from ACS fetching hold music.
        /// </summary>
        public string? AudioFileUrl { get; set; }
    }
}
