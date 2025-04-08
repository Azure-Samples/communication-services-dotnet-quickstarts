using static System.Net.WebRequestMethods;

namespace Call_Automation_GCCH
{
    public class ConfigurationRequest
    {
        public string AcsConnectionString { get; set; } = string.Empty;
        public string AcsPhoneNumber { get; set; } = string.Empty;
        public string pmaEndpoint { get; set; } = "https://govaz-01.pma.gov.teams.microsoft.us";
        // ACS GCCH Phase 2
        //  public string CongnitiveServiceEndpoint { get; set; } = string.Empty;
        public string CallbackUriHost { get; set; } = "https://gcch-test-app.azurewebsites.us/";
    }
}
