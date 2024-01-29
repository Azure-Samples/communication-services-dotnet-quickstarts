using Azure;
using Azure.Communication.Email;
using Azure.Core;
using Azure.Core.Pipeline;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SendEmailPlainText
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // This code demonstrates how to send email in bulk using Azure Communication Services and (optionally) including a custom policy to catch 429 (throttling) errors.
            var connectionString = "<ACS_CONNECTION_STRING>";

            //Input email information
            var sender = "<SENDER_EMAIL>";
            var recipient = new EmailAddress(
                address: "<RECIPIENT_EMAIL>",
                displayName: "<RECIPIENT_DISPLAY_NAME>");
            var subject = "Send bulk email sample with optional throttle policy";

            // Add optional throttling policy to handle any throttling errors
            EmailClientOptions emailClientOptions = new EmailClientOptions();
            emailClientOptions.AddPolicy(new Catch429Policy(), HttpPipelinePosition.PerRetry);
            EmailClient emailClient = new EmailClient(connectionString, emailClientOptions);

            // Create custom email content for each recipient.
            // This is an optional step, you could also use the same content for all recipients.
            var emailMessages = new List<EmailMessage>();
            for (int i = 0; i < 210; i++)
            {
                var emailContent = new EmailContent(subject)
                {
                    PlainText = "This is plain text mail send test body \n Best Wishes!!",
                    Html = $"<html><body><h1>Hello Recipient{i}, This is a quick send email test</h1><br/><h4>Communication email as a service mail send app working properly</h4><p>Happy Learning!!</p></body></html>"
                };

                var emailMessage = new EmailMessage(sender, recipient, emailContent);
                emailMessages.Add(emailMessage);
            }

            // Send all emails in parallel without waiting for the previous one to complete
            var sendTasks = emailMessages.Select(emailMessage => SendEmailAsync(emailClient, emailMessage)).ToList();
            await Task.WhenAll(sendTasks);

            // Poll for the status of each email in parallel without waiting for the previous one to complete
            var pollTasks = sendTasks.Select(task => PollStatusAsync(task.Result)).ToList();
            await Task.WhenAll(pollTasks);
        }

        public static async Task<EmailSendOperation> SendEmailAsync(EmailClient emailClient, EmailMessage emailMessage)
        {
            try
            {
                // Sending the email is a long running operation.
                // Do not wait for the long running operation to complete, especially if you are sending emails in bulk.
                var emailSendOperation = await emailClient.SendAsync(
                    wait: WaitUntil.Started,
                    message: emailMessage);

                Console.WriteLine($"Email send operation with id = {emailSendOperation.Id} started successfully.");

                // Return the operation so that it can be used to poll for status later.
                return emailSendOperation;
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Email send operation failed with Code = {ex.ErrorCode} and Message = {ex.Message}");
                return null;
            }
            catch (RequestThrottledException rex)
            {
                Console.WriteLine($"Email send operation was throttled with Message = {rex.Message}");
                return null;
            }
        }

        public static async Task PollStatusAsync(EmailSendOperation emailSendOperation)
        {
            if (emailSendOperation == null)
            {
                return;
            }

            var operationId = emailSendOperation.Id;

            try
            {
                // Poll for the status of the email operation
                while (true)
                {
                    // Polling for the status of email send which is a long running operation.
                    await emailSendOperation.UpdateStatusAsync();

                    // If the email send operation has completed, stop polling.
                    if (emailSendOperation.HasCompleted)
                    {
                        break;
                    }

                    // Add a delay before polling again to avoid getting throttled.
                    await Task.Delay(100);
                }

                if (emailSendOperation.HasValue)
                {
                    Console.WriteLine($"Email message with id = {operationId} is out for delivery. Status = {emailSendOperation.Value.Status}");
                }
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Polling for status of email message with id = {operationId} failed with Code = {ex.ErrorCode} and Message = {ex.Message}");
            }
            catch (RequestThrottledException rex)
            {
                Console.WriteLine($"Polling for status of email message with id = {operationId} was throttled with Message = {rex.Message}");
            }
        }
    }

    public class Catch429Policy : HttpPipelineSynchronousPolicy
    {
        // This method is called when the response is received by the http client.
        public override void OnReceivedResponse(HttpMessage message)
        {
            // Check if the response status is 429 (Too Many Requests)
            if (message.Response.Status == 429)
            {
                // Add your custom logic to handle throttling here.
                // For example, you can add a delay before retrying the request.
                // The delay time is usually specified in the Retry-After header that is included in the response.
                throw new RequestThrottledException(message.Response.ToString());
            }
            else
            {
                base.OnReceivedResponse(message);
            }
        }
    }
}
