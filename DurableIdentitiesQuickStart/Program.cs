using System;
using Azure.Identity;
using Azure.Communication.Identity;
using Azure.Core;
using Azure.Communication;
using Azure;

namespace AccessTokensQuickstart
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            Console.WriteLine("Azure Communication Services - Access Tokens Quickstart");

            //  Authenticate the client
            // This code demonstrates how to fetch your connection string
            // from an environment variable.
            string connectionString = Environment.GetEnvironmentVariable("COMMUNICATION_SERVICES_CONNECTION_STRING");
            var client = new CommunicationIdentityClient(connectionString);

            // This code demonstrates how to fetch your endpoint and access key
            // from an environment variable.
            /*string endpoint = Environment.GetEnvironmentVariable("COMMUNICATION_SERVICES_ENDPOINT");
            string accessKey = Environment.GetEnvironmentVariable("COMMUNICATION_SERVICES_ACCESSKEY");
            var client = new CommunicationIdentityClient(new Uri(endpoint), new AzureKeyCredential(accessKey));*/

            // Update documentation with URI details
            /*TokenCredential tokenCredential = new DefaultAzureCredential();
            string endPoint = Environment.GetEnvironmentVariable("COMMUNICATION_SERVICES_ENDPOINT");
            var client = new CommunicationIdentityClient(new Uri(endPoint), tokenCredential);*/

            // Create an identity
            var identityResponse = await client.CreateUserAsync();
            var identity = identityResponse.Value;
            Console.WriteLine($"\nCreated an identity with ID: {identity.Id}");

            // Bring Your Own Identity (BYOI) feature demonstration using latest API
            string customId = "alice@contoso.com"; // Alphanumeric custom ID
            Response<CommunicationUserIdentifier> user = await client.CreateUserAsync(customId: customId);
            var userDetails = client.GetUserDetail(user);
            Console.WriteLine($"\nUser ID: {userDetails.Id}");
            Console.WriteLine($"Custom ID: {userDetails.CustomId}");
            Console.WriteLine($"Last token issued at: {userDetails.LastTokenIssuedAt}");

            // Create another identity with the same customId and validate
            Response<CommunicationUserIdentifier> user2 = await client.CreateUserAsync(customId: customId);
            var userDetails2 = client.GetUserDetail(user2);
            Console.WriteLine($"\nUser ID (second call): {userDetails2.Id}");
            Console.WriteLine($"Custom ID (second call): {userDetails2.CustomId}");
            Console.WriteLine($"Last token issued at (second call): {userDetails2.LastTokenIssuedAt}");

            if (userDetails.Id == userDetails2.Id)
            {
                Console.WriteLine("\nValidation successful: Both identities have the same ID as expected.");
            }
            else
            {
                Console.WriteLine("\nValidation failed: Identity IDs do not match!");
            }
        }
    }
}