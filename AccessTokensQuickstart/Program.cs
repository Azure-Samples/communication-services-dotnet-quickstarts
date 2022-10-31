using System;
using Azure.Communication.Identity;


namespace AccessTokensQuickstart
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            Console.WriteLine("Azure Communication Services - Access Tokens Quickstart");
            // This code demonstrates how to retrieve your connection string
            // from an environment variable.
            //string connectionString = Environment.GetEnvironmentVariable("https://verizann-media.communication.azure.com/;accesskey=EQHWAYFO9E0NNcj8OZEFHcVtFWUa1EBEV4tsgX1ej53kJjv4v9ZgBLVotnwhKRtjTxdIf2UEq4xoJ5n/on5IYA==");
            string connectionString = "endpoint=https://verizann-media.communication.azure.com/;accesskey=EQHWAYFO9E0NNcj8OZEFHcVtFWUa1EBEV4tsgX1ej53kJjv4v9ZgBLVotnwhKRtjTxdIf2UEq4xoJ5n/on5IYA==";

            var client = new CommunicationIdentityClient(connectionString);

            var identityResponse = await client.CreateUserAsync();
            var identity = identityResponse.Value;
            Console.WriteLine($"\nCreated an identity with ID: {identity.Id}");

            // Issue an access token with the "voip" scope for an identity
            var tokenResponse = await client.GetTokenAsync(identity, scopes: new[] { CommunicationTokenScope.VoIP });

            // Get the token from the response
            var token = tokenResponse.Value.Token;
            var expiresOn = tokenResponse.Value.ExpiresOn;

            // Write the token details to the screen
            Console.WriteLine($"\nIssued an access token with 'voip' scope that expires at {expiresOn}:");
            Console.WriteLine(token);


        }
    }
}