namespace Call_Automation_GCCH.Models
{
    public class ConfigurationRequest
    {
        public string? AcsConnectionString { get; set; } // no default
        public string? AcsPhoneNumber { get; set; }
        public string? pmaEndpoint { get; set; }
        public string? CognitiveServiceEndpoint { get; set; }
        public string? CallbackUriHost { get; set; }
        public string? StorageConnectionString { get; set; }
        public string? ContainerName { get; set; }
        public string? RecordingBlobStorageContainerName { get; set; }
    }
}
