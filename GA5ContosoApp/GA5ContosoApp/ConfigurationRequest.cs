namespace Call_Automation_GCCH
{
    public class ConfigurationRequest
    {
        public string AcsConnectionString { get; set; } = string.Empty;
        public string AcsPhoneNumber { get; set; } = string.Empty;
        public string CongnitiveServiceEndpoint { get; set; } = string.Empty;
        public string CallbackUriHost { get; set; } = string.Empty;
    }
}
