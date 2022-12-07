using Azure.ResourceManager.Communication;
using Azure.Identity;
using System;
using Azure.ResourceManager.Communication.Models;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace CreateCommunicationResource
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // User Identity
            var subscriptionId = "11e8c7d9-beea-4f98-a5e6-207b1482eba3";
            var communicationServiceClient = new CommunicationManagementClient(subscriptionId, new InteractiveBrowserCredential());

            // System assigned Managed Identity11e8c7d9-beea-4f98-a5e6-207b1482eba3
            //var subscriptionId = 11e8c7d9-beea-4f98-a5e6-207b1482eba3;529c7b72-7c34-4ddb-9e78-1318bebc1e4d  529c7b72-7c34-4ddb-9e78-1318bebc1e4d
            //var acsClient = new CommunicationManagementClient(subscriptionId, new ManagedIdentityCredential());


            // User assigned Managed Identity
            //var subscriptionId = "11e8c7d9-beea-4f98-a5e6-207b1482eba3";
            var managedIdentityCredential = new ManagedIdentityCredential("ea142328-c766-4152-bdf2-38b9aaea16df");
            //var communicationServiceClient = new CommunicationManagementClient(subscriptionId, managedIdentityCredential);

            // Using Service Principal
            /*var subscriptionId = Environment.GetEnvironmentVariable(11e8c7d9-beea-4f98-a5e6-207b1482eba3);
            var acsClient = new CommunicationManagementClient(subscriptionId, new EnvironmentCredential());*/

            // Create a Communication Services resource


            var resourceGroupName = "Verizann_Media_Services";
            var resourceName = "lookRes";
            var resource = new CommunicationServiceResource { Location = "Global", DataLocation = "UnitedStates" };
            var operation = await communicationServiceClient.CommunicationService.StartCreateOrUpdateAsync(resourceGroupName, resourceName, resource);
            await operation.WaitForCompletionAsync();
            var resources = communicationServiceClient.CommunicationService.ListBySubscription();
            foreach (var resource1 in resources)
            {
                Console.WriteLine(resource1.Name);
            }
        }
    }
}