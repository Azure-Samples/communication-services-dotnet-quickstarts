using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Communication.Identity;
using Microsoft.Identity.Client;

namespace ManageTeamsIdentityMobileAndDesktop
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Azure Communication Services - Teams Access Tokens Quickstart");
            // This code demonstrates how to fetch an AAD client ID and tenant ID 
            // from an environment variable.
            string appId = Environment.GetEnvironmentVariable("AAD_CLIENT_ID");
            string tenantId = Environment.GetEnvironmentVariable("AAD_TENANT_ID");
            string authority = $"https://login.microsoftonline.com/{tenantId}";
            string redirectUri = "http://localhost";

            // Create an instance of PublicClientApplication
            var aadClient = PublicClientApplicationBuilder
                            .Create(appId)
                            .WithAuthority(authority)
                            .WithRedirectUri(redirectUri)
                            .Build();

            List<string> scopes = new() {
                "https://auth.msft.communication.azure.com/Teams.ManageCalls",
                "https://auth.msft.communication.azure.com/Teams.ManageChats"
            };

            // Retrieve the AAD token and object ID of a Teams user
            var result = await aadClient
                                    .AcquireTokenInteractive(scopes)
                                    .ExecuteAsync();
            string teamsUserAadToken = result.AccessToken;
            string userObjectId = result.UniqueId;

            // This code demonstrates how to fetch your connection string
            // from an environment variable.
            string connectionString = Environment.GetEnvironmentVariable("COMMUNICATION_SERVICES_CONNECTION_STRING");
            // Instantiate the identity client
            var client = new CommunicationIdentityClient(connectionString);

            // Exchange the Azure AD access token of the Teams User for a Communication Identity access token
            var options = new GetTokenForTeamsUserOptions(teamsUserAadToken, appId, userObjectId);
            var accessToken = await client.GetTokenForTeamsUserAsync(options);
            Console.WriteLine($"Token: {accessToken.Value.Token}");
        }
    }
}