using Azure.Communication.Email;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SendEmailToMultipleRecipients
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // This code demonstrates how to send email using Azure Communication Services.
            var connectionString = "<ACS_CONNECTION_STRING>";
            var emailClient = new EmailClient(connectionString);

            var subject = "Send email sample - Multiple recipients";
            var emailContent = new EmailContent(subject)
            {
                PlainText = "This is plain mail send test body \n Best Wishes!!",
                Html = "<html><body><h1>Quick send email test</h1><br/><h4>Communication email as a service mail send app working properly</h4><p>Happy Learning!!</p></body></html>"
            };
            var sender = "<SENDER_EMAIL>";

            var emailRecipients = new EmailRecipients(new List<EmailAddress> {
                new EmailAddress("<RECIPIENT_EMAIL_1>", "Alice"),
                new EmailAddress("<RECIPIENT_EMAIL_2>", "Bob"),
            });

            var emailMessage = new EmailMessage(sender, emailRecipients, emailContent)
            {
                // Header name is "x-priority" or "x-msmail-priority"
                // Header value is a number from 1 to 5. 1 or 2 = High, 3 = Normal, 4 or 5 = Low
                // Not all email clients recognize this header directly (outlook client does recognize)
                Headers =
                {
                    // Set Email Importance to High
                    { "x-priority", "1" },
                    { "EmailTrackingHeader", "MyCustomEmailTrackingID" }
                }
            };

            try
            {
                Console.WriteLine("Sending email to multiple recipients...");
                EmailSendOperation emailSendOperation = await emailClient.SendAsync(Azure.WaitUntil.Completed, emailMessage);
                EmailSendResult statusMonitor = emailSendOperation.Value;

                string operationId = emailSendOperation.Id;
                var emailSendStatus = statusMonitor.Status;

                if (emailSendStatus == EmailSendStatus.Succeeded)
                {
                    Console.WriteLine($"Email send operation succeeded with OperationId = {operationId}.\nEmail is out for delivery.");
                }
                else
                {
                    Console.WriteLine($"Failed to send email.\n OperationId = {operationId}.\n Status = {emailSendStatus}.");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in sending email, {ex}");
            }
        }
    }
}
