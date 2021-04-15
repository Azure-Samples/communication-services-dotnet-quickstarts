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
            var subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
            var communicationServiceClient = new CommunicationManagementClient(subscriptionId, new InteractiveBrowserCredential());

            // System assigned Managed Identity
            //var subscriptionId = "AZURE_SUBSCRIPTION_ID";
            //var acsClient = new CommunicationManagementClient(subscriptionId, new ManagedIdentityCredential());


            // User assigned Managed Identity
            /*var subscriptionId = "AZURE_SUBSCRIPTION_ID";*/
            /*var managedIdentityCredential = new ManagedIdentityCredential("AZURE_CLIENT_ID");
            var acsClient = new CommunicationManagementClient(subscriptionId, managedIdentityCredential);*/

            // Using Service Principal
            /*var subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
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
