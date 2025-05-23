using System.Text.Json;
using System.Text;
using Call_Automation_GCCH.Models;
using Microsoft.Extensions.Options;

namespace Call_Automation_GCCH.Services
{
    public interface ICommunicationConfigurationService
    {
        CommunicationConfiguration communicationConfiguration { get; }
    }

    public class CommunicationConfigurationService : ICommunicationConfigurationService
    {
        public CommunicationConfiguration communicationConfiguration { get; }

        public CommunicationConfigurationService(IHttpContextAccessor httpContextAccessor, IOptions<CommunicationConfiguration> defaultCommunicationConfiguration)
        {
            var context = httpContextAccessor.HttpContext;
            if (context != null && context.Request.Headers.TryGetValue("X-Communication-Config", out var headerValue) && !string.IsNullOrWhiteSpace(headerValue))
            {
                byte[] jsonBytes = Convert.FromBase64String(headerValue.ToString());
                string jsonString = Encoding.UTF8.GetString(jsonBytes);
                var config = JsonSerializer.Deserialize<CommunicationConfiguration>(jsonString);

                if (config != null)
                {
                    communicationConfiguration = new CommunicationConfiguration
                    {
                        AcsConnectionString = string.IsNullOrEmpty(config.AcsConnectionString) ? defaultCommunicationConfiguration.Value.AcsConnectionString : config.AcsConnectionString,
                        AcsPhoneNumber = string.IsNullOrEmpty(config.AcsPhoneNumber) ? defaultCommunicationConfiguration.Value.AcsPhoneNumber : config.AcsPhoneNumber,
                        CallbackUriHost = string.IsNullOrEmpty(config.CallbackUriHost) ? defaultCommunicationConfiguration.Value.CallbackUriHost : config.CallbackUriHost,
                        PmaEndpoint = string.IsNullOrEmpty(config.PmaEndpoint) ? defaultCommunicationConfiguration.Value.PmaEndpoint : config.PmaEndpoint,
                        // ACS GCCH Phase 2
                        // CongnitiveServiceEndpoint = string.IsNullOrEmpty(config.CongnitiveServiceEndpoint) ? defaultCommunicationConfiguration.Value.CongnitiveServiceEndpoint: config.CongnitiveServiceEndpoint,
                    };

                    return;
                }

            }

            communicationConfiguration = defaultCommunicationConfiguration.Value;
        }
    }
}