namespace MoveParticipantsSample
{
    public class ConfigurationRequest
    {
        public string AcsConnectionString { get; set; } = string.Empty;
        public string AcsPhoneNumber { get; set; } = string.Empty;
        public string CallbackUriHost { get; set; } = string.Empty;
        public string AcsInboundSender { get; set; } = string.Empty;
    }
}
