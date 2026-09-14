using Azure.Communication.CallAutomation;
using Azure.Communication.CallAutomation;

namespace Call_Automation_GCCH.Services
{
    /// <summary>
    /// Abstraction over CallAutomationClient for testability and extensibility.
    /// </summary>
    public interface ICallAutomationService
    {
        CallAutomationClient GetCallAutomationClient();

        /// <summary>
        /// Returns a CallAutomationClient built from the ACS connection string WITHOUT a PMA endpoint.
        /// Recording download/delete operations must use this client because the AMS storage endpoint
        /// (e.g. storage.ams.infra.gov.teams.microsoft.us) rejects requests signed by a PMA-scoped client
        /// with 401 Unauthorized in sovereign clouds (GCCH/DoD).
        /// </summary>
        CallAutomationClient GetRecordingDownloadClient();

        CallConnection GetCallConnection(string callConnectionId);
        CallMedia GetCallMedia(string callConnectionId);
        CallConnectionProperties GetCallConnectionProperties(string callConnectionId);
        void UpdateClient(string connectionString, string pmaEndpoint);
        string GetCurrentPmaEndpoint();

        string? RecordingLocation { get; set; }
        string RecordingFileFormat { get; set; }
    }
}
