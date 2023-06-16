using Azure.Communication;
using Azure.Communication.Identity;

namespace CallAutomation_CogSvcIvr
{
    public static class WebApplicationExtension
    {
        public async static Task<string> ProvisionAzureCommunicationServicesIdentity(this WebApplication app, string connectionString)
        {
            var client = new CommunicationIdentityClient(connectionString);
            var user = await client.CreateUserAsync().ConfigureAwait(false);
            return user.Value.Id;
        }
    }
}
