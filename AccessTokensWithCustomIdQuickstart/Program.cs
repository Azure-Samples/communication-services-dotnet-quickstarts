using System;
using Azure.Identity;
using Azure.Communication.Identity;
using Azure.Core;
using Azure.Communication;
using Azure;

namespace AccessTokensWithCustomIdQuickstart
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            Console.WriteLine("Azure Communication Services - Access Tokens for Identity with customId Quickstart");

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

            // Access Tokens with Custom Id feature demonstration
            string customId = "alice@contoso.com"; // Alphanumeric custom ID
            var userAndTokenResponse = await client.CreateUserAndTokenAsync(scopes: new[] { CommunicationTokenScope.Chat }, customId: customId);
            var user = userAndTokenResponse.Value.User;
            var token = userAndTokenResponse.Value.AccessToken;
            var userDetails = await client.GetUserDetailAsync(user);
            Console.WriteLine($"\nUser ID: {user.Id}");
            Console.WriteLine($"Custom ID: {userDetails.Value.CustomId}");
            Console.WriteLine($"Access Token: {token.Token}");

            // Create another token with the same customId and validate that it the same user is returned
            var userAndTokenResponse2 = await client.CreateUserAndTokenAsync(scopes: new[] { CommunicationTokenScope.Chat }, customId: customId);
            var user2 = userAndTokenResponse2.Value.User;
            var userDetails2 = await client.GetUserDetailAsync(user2);
            Console.WriteLine($"\nUser ID (second call): {user2.Id}");
            Console.WriteLine($"Custom ID (second call): {userDetails2.Value.CustomId}");


            if (user.Id == user2.Id)
            {
                Console.WriteLine("\nValidation successful: Both identities have the same ID as expected.");
            }
            else
            {
                Console.WriteLine("\nValidation failed: Identity IDs do not match!");
            }

            // Cleanup: Delete the identities created in this example
            Console.WriteLine("\nCleaning up: Deleting identities...");
            await client.DeleteUserAsync(user);
            Console.WriteLine($"\nDeleted identities.");
        }
    }
}