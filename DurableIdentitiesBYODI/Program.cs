using System;
using Azure.Identity;
using Azure.Communication.Identity;
using Azure.Core;
using Azure.Communication;
using Azure;

namespace DurableIdentitiesBYODI
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            Console.WriteLine("Azure Communication Services - Durable Identities BYOID Quickstart");

            // Authenticate the client
            // This code demonstrates how to fetch your connection string
            // from an environment variable.
            string? connectionString = Environment.GetEnvironmentVariable("COMMUNICATION_SERVICES_CONNECTION_STRING");

            // This code demonstrates how to fetch your endpoint and access key
            // from an environment variable.
            /*string endpoint = Environment.GetEnvironmentVariable("COMMUNICATION_SERVICES_ENDPOINT");
            string accessKey = Environment.GetEnvironmentVariable("COMMUNICATION_SERVICES_ACCESSKEY");
            var client = new CommunicationIdentityClient(new Uri(endpoint), new AzureKeyCredential(accessKey));*/

            // Update documentation with URI details
            /*TokenCredential tokenCredential = new DefaultAzureCredential();
            string endPoint = Environment.GetEnvironmentVariable("COMMUNICATION_SERVICES_ENDPOINT");
            var client = new CommunicationIdentityClient(new Uri(endPoint), tokenCredential);*/

            if (string.IsNullOrEmpty(connectionString))
            {
                Console.WriteLine("Error: COMMUNICATION_SERVICES_CONNECTION_STRING environment variable is not set.");
                Console.WriteLine("Please set it using: setx COMMUNICATION_SERVICES_CONNECTION_STRING \"your-connection-string\"");
                return;
            }

            var client = new CommunicationIdentityClient(connectionString);

            // Create a standard identity
            var identityResponse = await client.CreateUserAsync();
            var identity = identityResponse.Value;
            Console.WriteLine($"\nCreated a standard identity with ID: {identity.Id}");

            // Bring Your Own Identity (BYOID) feature demonstration
            string customId = "alice@contoso.com"; // Alphanumeric custom ID
            Response<CommunicationUserIdentifier> user = await client.CreateUserAsync(customId: customId);
            var userDetails = await client.GetUserDetailAsync(user.Value);
            Console.WriteLine($"\nUser ID: {user.Value.Id}");
            Console.WriteLine($"Custom ID: {userDetails.Value.CustomId}");

            // Create ano ther identity with the same customId and validate
            Response<CommunicationUserIdentifier> user2 = await client.CreateUserAsync(customId: customId);
            var userDetails2 = await client.GetUserDetailAsync(user2.Value);
            Console.WriteLine($"\nUser ID (second call): {user2.Value.Id}");
            Console.WriteLine($"Custom ID (second call): {userDetails2.Value.CustomId}");

            if (user.Value.Id == user2.Value.Id)
            {
                Console.WriteLine("\nValidation successful: Both identities have the same ID as expected.");
            }
            else
            {
                Console.WriteLine("\nValidation failed: Identity IDs do not match!");
            }

            // Issue access tokens for the BYOID identity
            var tokenResponse = await client.GetTokenAsync(user.Value, scopes: new[] { CommunicationTokenScope.Chat });
            Console.WriteLine($"\nIssued access token with 'chat' scope:");
            Console.WriteLine($"Token expires at: {tokenResponse.Value.ExpiresOn}");
            

            // Cleanup: Delete the identities created in this example
            Console.WriteLine("\nCleaning up: Deleting identities...");
            await client.DeleteUserAsync(identity);
            await client.DeleteUserAsync(user.Value);
            Console.WriteLine($"\nDeleted identities.");
        }
    }
}