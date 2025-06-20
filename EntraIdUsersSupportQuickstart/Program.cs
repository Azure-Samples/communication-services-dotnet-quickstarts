using Azure.Identity;
using Azure.Communication;

namespace EntraIdUsersSupportQuickstart
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Azure Communication Services - Obtain Access Token for Entra ID User Quickstart");
            string clientId = Environment.GetEnvironmentVariable("ENTRA_CLIENT_ID");
            string tenantId = Environment.GetEnvironmentVariable("ENTRA_TENANT_ID");
            string resourceEndpoint = Environment.GetEnvironmentVariable("COMMUNICATION_SERVICES_RESOURCE_ENDPOINT");

            //Initialize InteractiveBrowserCredential for use with CommunicationTokenCredential.
            var entraTokenCredential = new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
            {
                TenantId = tenantId,
                ClientId = clientId,
                RedirectUri = new Uri("<REDIRECT_URI>") // Ensure this URI is registered in your Azure AD app
            });

            // Set up CommunicationTokenCredential to request a Communication Services access token for a Microsoft Entra ID user.
            var entraTokenCredentialOptions = new EntraCommunicationTokenCredentialOptions(
                resourceEndpoint: resourceEndpoint,
                entraTokenCredential: entraTokenCredential);

            var credential = new CommunicationTokenCredential(entraTokenCredentialOptions);
            
            // To obtain a Communication Services access token for Microsoft Entra ID call GetTokenAsync() method.
            var accessToken = await credential.GetTokenAsync();
            Console.WriteLine($"Token: {accessToken.Token}");
        }
    }
}