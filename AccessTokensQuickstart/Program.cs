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

            // Issue an access token with validity of 24 hours and the "voip" scope for an identity
            var tokenResponse = await client.GetTokenAsync(identity, scopes: new[] { CommunicationTokenScope.VoIP });
            var token = tokenResponse.Value.Token;
            var expiresOn = tokenResponse.Value.ExpiresOn;
            Console.WriteLine($"\nIssued an access token with 'voip' scope that expires at {expiresOn}:");
            Console.WriteLine(token);

            // Issue an access token with validity of an hour and the "voip" scope for an identity 
            TimeSpan tokenExpiresIn = TimeSpan.FromHours(1);
            tokenResponse = await client.GetTokenAsync(identity, scopes: new [] { CommunicationTokenScope.VoIP }, tokenExpiresIn);
            token =  tokenResponse.Value.Token;
            expiresOn = tokenResponse.Value.ExpiresOn;
            Console.WriteLine($"\nIssued an access token with 'voip' scope and custom expiration time that expires at {expiresOn}:");
            Console.WriteLine(token);

            // Issue an identity and an access token with validity of 24 hours and the "voip" scope for the new identity
            var identityAndTokenResponse = await client.CreateUserAndTokenAsync(scopes: new[] { CommunicationTokenScope.VoIP });
            identity = identityAndTokenResponse.Value.User;
            token = identityAndTokenResponse.Value.AccessToken.Token;
            expiresOn = identityAndTokenResponse.Value.AccessToken.ExpiresOn;
            Console.WriteLine($"\nCreated an identity with ID: {identity.Id}");
            Console.WriteLine($"\nIssued an access token with 'voip' scope that expires at {expiresOn}:");
            Console.WriteLine(token);

            // Issue an identity and an access token with validity of an hour and the "voip" scope for the new identity
            identityAndTokenResponse = await client.CreateUserAndTokenAsync(scopes: new[] { CommunicationTokenScope.VoIP }, tokenExpiresIn);
            identity = identityAndTokenResponse.Value.User;
            token = identityAndTokenResponse.Value.AccessToken.Token;
            expiresOn = identityAndTokenResponse.Value.AccessToken.ExpiresOn;
            Console.WriteLine($"\nCreated an identity with ID: {identity.Id}");
            Console.WriteLine($"\nIssued an access token with 'voip' scope and custom expiration time that expires at {expiresOn}:");
            Console.WriteLine(token);

            // Refresh access tokens
            var identityToRefresh = new CommunicationUserIdentifier(identity.Id);
            var refreshTokenResponse = await client.GetTokenAsync(identityToRefresh, scopes: new[] { CommunicationTokenScope.VoIP });

            // Revoke access tokens
            await client.RevokeTokensAsync(identity);
            Console.WriteLine($"\nSuccessfully revoked all access tokens for identity with ID: {identity.Id}");

            // Delete an identity
            await client.DeleteUserAsync(identity);
            Console.WriteLine($"\nDeleted the identity with ID: {identity.Id}");
        }
    }
}