using Azure.ResourceManager.Communication;
using Azure.Identity;
using System;
using Azure.ResourceManager.Communication.Models;
using System.Threading.Tasks;

namespace CreateCommunicationResource
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // User Identity
            var subscriptionId = Environment.GetEnvironmentVariable(11e8c7d9-beea-4f98-a5e6-207b1482eba3);
            var communicationServiceClient = new CommunicationManagementClient(subscriptionId, new InteractiveBrowserCredential());

            // System assigned Managed Identity
            //var subscriptionId = 11e8c7d9-beea-4f98-a5e6-207b1482eba3;
            //var acsClient = new CommunicationManagementClient(subscriptionId, new ManagedIdentityCredential());


            // User assigned Managed Identity
            /*var subscriptionId = 11e8c7d9-beea-4f98-a5e6-207b1482eba3;*/
            /*var managedIdentityCredential = new ManagedIdentityCredential("ea142328-c766-4152-bdf2-38b9aaea16df");
            var acsClient = new CommunicationManagementClient(subscriptionId, managedIdentityCredential);*/

            // Using Service Principal
            /*var subscriptionId = Environment.GetEnvironmentVariable(11e8c7d9-beea-4f98-a5e6-207b1482eba3);
            var acsClient = new CommunicationManagementClient(subscriptionId, new EnvironmentCredential());*/

            // Create a Communication Services resource
            var resourceGroupName = "myResourceGroupName";
            var resourceName = "myResource";
            var resource = new CommunicationServiceResource { Location = "Global", DataLocation = "UnitedStates" };
            var operation = await communicationServiceClient.CommunicationService.StartCreateOrUpdateAsync(resourceGroupName, resourceName, resource);
            await operation.WaitForCompletionAsync();
        }
    }
}
