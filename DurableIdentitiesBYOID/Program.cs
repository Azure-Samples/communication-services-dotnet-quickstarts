using System;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Communication.Identity;
using Azure.Core;
using Azure.Communication;
using Azure;

namespace DurableIdentitiesBYOID
{
    class Program
    {        static async Task Main(string[] args)
        {
            Console.WriteLine("Azure Communication Services - Identity Management Quickstart");

            try
            {
                // Authenticate the client
                // This code demonstrates how to fetch your connection string
                // from an environment variable.
                string? connectionString = Environment.GetEnvironmentVariable("COMMUNICATION_SERVICES_CONNECTION_STRING");
                
                if (string.IsNullOrEmpty(connectionString))
                {
                    Console.WriteLine("Error: COMMUNICATION_SERVICES_CONNECTION_STRING environment variable is not set.");
                    Console.WriteLine("Please set it using: $env:COMMUNICATION_SERVICES_CONNECTION_STRING=\"your-connection-string\"");
                    return;
                }

                var client = new CommunicationIdentityClient(connectionString);// Create multiple identities to demonstrate the standard workflow
            var identityResponse1 = await client.CreateUserAsync();
            var identity1 = identityResponse1.Value;
            Console.WriteLine($"\nCreated first identity with ID: {identity1.Id}");

            var identityResponse2 = await client.CreateUserAsync();
            var identity2 = identityResponse2.Value;
            Console.WriteLine($"Created second identity with ID: {identity2.Id}");

            // Issue access tokens for different scopes
            var tokenResponseChat = await client.GetTokenAsync(identity1, scopes: new[] { CommunicationTokenScope.Chat });
            Console.WriteLine($"\nIssued Chat token for first identity:");
            Console.WriteLine($"Token: {tokenResponseChat.Value.Token}");
            Console.WriteLine($"Expires on: {tokenResponseChat.Value.ExpiresOn}");

            var tokenResponseVoip = await client.GetTokenAsync(identity2, scopes: new[] { CommunicationTokenScope.VoIP });
            Console.WriteLine($"\nIssued VoIP token for second identity:");
            Console.WriteLine($"Token: {tokenResponseVoip.Value.Token}");
            Console.WriteLine($"Expires on: {tokenResponseVoip.Value.ExpiresOn}");

            // Issue token with multiple scopes
            var tokenResponseMultiple = await client.GetTokenAsync(identity1, scopes: new[] { CommunicationTokenScope.Chat, CommunicationTokenScope.VoIP });
            Console.WriteLine($"\nIssued token with multiple scopes for first identity:");
            Console.WriteLine($"Token: {tokenResponseMultiple.Value.Token}");
            Console.WriteLine($"Expires on: {tokenResponseMultiple.Value.ExpiresOn}");

            // Demonstrate that different identities have unique IDs
            if (identity1.Id != identity2.Id)
            {
                Console.WriteLine("\n✓ Validation successful: Each CreateUserAsync() call creates a unique identity.");
            }
            else
            {
                Console.WriteLine("\n✗ Validation failed: Identity IDs should be unique!");
            }            // Clean up resources (optional - identities can be left to expire)
            Console.WriteLine("\nCleaning up resources...");
            await client.DeleteUserAsync(identity1);
            await client.DeleteUserAsync(identity2);
            Console.WriteLine("✓ Identities deleted successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                Console.WriteLine("\nPlease check:");
                Console.WriteLine("1. Your connection string is valid");
                Console.WriteLine("2. Your Azure Communication Services resource is active");
                Console.WriteLine("3. You have proper network connectivity");
            }
        }
    }
}