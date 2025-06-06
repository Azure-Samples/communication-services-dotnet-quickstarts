using Azure.Identity;
using Azure.Communication;

namespace EntraIdUsersSupportQuickstart
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Azure Communication Services - Obtain Access Token for Entra ID User Quickstart");

            // This code demonstrates how to fetch your Microsoft Entra client ID and tenant ID from environment variables.
            string clientId = Environment.GetEnvironmentVariable("ENTRA_CLIENT_ID");
            string tenantId = Environment.GetEnvironmentVariable("ENTRA_TENANT_ID");

            //Initialize InteractiveBrowserCredential for use with AzureCommunicationTokenCredential.
            var options = new InteractiveBrowserCredentialOptions
            {
                TenantId = tenantId,
                ClientId = clientId,
            };
            var entraTokenCredential = new InteractiveBrowserCredential(options);

            // This code demonstrates how to fetch your Azure Communication Services resource endpoint URI
            // from an environment variable.
            string resourceEndpoint = Environment.GetEnvironmentVariable("COMMUNICATION_SERVICES_RESOURCE_ENDPOINT");

            // Set up CommunicationTokenCredential to request a Communication Services access token for a Microsoft Entra ID user.
            var entraTokenCredentialOptions = new EntraCommunicationTokenCredentialOptions(
                resourceEndpoint: resourceEndpoint,
                entraTokenCredential: entraTokenCredential)
            {
                Scopes = new[] { "https://communication.azure.com/clients/VoIP" }
            };

            var credential = new CommunicationTokenCredential(entraTokenCredentialOptions);
            
            // To obtain a Communication Services access token for Microsoft Entra ID call GetTokenAsync() method.
            var accessToken = await credential.GetTokenAsync();
            Console.WriteLine($"Token: {accessToken.Token}");
        }
    }
}