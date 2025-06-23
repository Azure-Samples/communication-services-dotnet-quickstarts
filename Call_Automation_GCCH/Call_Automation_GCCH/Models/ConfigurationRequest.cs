namespace Call_Automation_GCCH.Models
{
    public class ConfigurationRequest
    {
        public string? AcsConnectionString { get; set; } // no default
        public string? AcsPhoneNumber { get; set; }
        public string? pmaEndpoint { get; set; }
        // ACS GCCH Phase 2
        // public string CongnitiveServiceEndpoint { get; set; }
        public string? CallbackUriHost { get; set; }
        public string? StorageConnectionString { get; set; }
        public string? ContainerName { get; set; }
        public string? RecordingBlobStorageContainerName { get; set; }
    }
}
