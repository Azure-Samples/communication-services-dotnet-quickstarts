using Azure;
using Azure.Communication.Email;
using Azure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace SendEmailWithManagedIdentity
{
    // This code demonstrates how to authenticate to your Communication Service resource using Managed Identities.
    // This function app should be given a managed identity with "Contributor" access to the Azure Communication
    // Service resouce or a custom role with both the "Microsoft.Communication/CommunicationServices/Read" and
    // "Microsoft.Communication/CommunicationServices/Write" can also be used.
    // This example uses DefaultAzureCredential, but ManagedIdentityCredential could have also been used.
    // DefaultAzureCredential allows the same code to be used during local development and in the deployed environment
    // because DefaultAzureCredential supports many authentication types.
    public static class Function1
    {
        [FunctionName("SendEmailWithManagedIdentity")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string resourceEndpoint = "<ACS_RESOURCE_ENDPOINT>";
            EmailClient emailClient = new EmailClient(new Uri(resourceEndpoint), new DefaultAzureCredential());

            var subject = "Welcome to Azure Communication Service Email APIs.";
            var htmlContent = "<html><body><h1>Quick send email managed identity test</h1><br/><h4>This email message is sent from Azure Communication Service Email using a function app.</h4><p>This mail was sent using .NET SDK!!</p></body></html>";
            var sender = "donotreply@xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx.azurecomm.net";
            var recipient = "emailalias@contoso.com";

            try
            {
                log.LogInformation("Sending email...");
                EmailSendOperation emailSendOperation = await emailClient.SendAsync(
                    Azure.WaitUntil.Completed,
                    sender,
                    recipient,
                    subject,
                    htmlContent);
                EmailSendResult statusMonitor = emailSendOperation.Value;

                log.LogInformation($"Email Sent. Status = {emailSendOperation.Value.Status}");

                /// Get the OperationId so that it can be used for tracking the message for troubleshooting
                string operationId = emailSendOperation.Id;
                log.LogInformation($"Email operation id = {operationId}");

                return new OkObjectResult(operationId);
            }
            catch (RequestFailedException ex)
            {
                return new ObjectResult($"Email send operation failed with error code: {ex.ErrorCode}, message: {ex.Message}")
                {
                    StatusCode = 500,
                };
            }
        }
    }
}
