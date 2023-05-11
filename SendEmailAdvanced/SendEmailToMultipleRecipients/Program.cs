using Azure;
using Azure.Communication.Email;
using System;
using System.Collections.Generic;
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

            // Create the To list
            var toRecipients = new List<EmailAddress>
            {
                new EmailAddress(
                    address: "<RECIPIENT_EMAIL_1>",
                    displayName: "<RECIPIENT_DISPLAY_NAME_1>"),
                new EmailAddress(
                    address: "<RECIPIENT_EMAIL_2>",
                    displayName: "<RECIPIENT_DISPLAY_NAME_2>")
            };

            // Create the CC list
            var ccRecipients = new List<EmailAddress>
            {
                new EmailAddress(
                    address: "<RECIPIENT_EMAIL_1>",
                    displayName: "<RECIPIENT_DISPLAY_NAME_1>"),
                new EmailAddress(
                    address: "<RECIPIENT_EMAIL_2>",
                    displayName: "<RECIPIENT_DISPLAY_NAME_2>")
            };

            // Create the BCC list
            var bccRecipients = new List<EmailAddress>
            {
                new EmailAddress(
                    address: "<RECIPIENT_EMAIL_1>",
                    displayName: "<RECIPIENT_DISPLAY_NAME_1>"),
                new EmailAddress(
                    address: "<RECIPIENT_EMAIL_2>",
                    displayName: "<RECIPIENT_DISPLAY_NAME_2>")
            };

            var emailRecipients = new EmailRecipients(toRecipients, ccRecipients, bccRecipients);
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

            // Add optional ReplyTo address which is where any replies to the email will go to.
            emailMessage.ReplyTo.Add(new EmailAddress("<REPLY_TO_EMAIL>", "<REPLY_TO_DISPLAY_NAME>"));

            try
            {
                EmailSendOperation emailSendOperation = await emailClient.SendAsync(WaitUntil.Completed, emailMessage);
                Console.WriteLine($"Email Sent. Status = {emailSendOperation.Value.Status}");

                /// Get the OperationId so that it can be used for tracking the message for troubleshooting
                string operationId = emailSendOperation.Id;
                Console.WriteLine($"Email operation id = {operationId}");
            }
            catch (RequestFailedException ex)
            {
                /// OperationID is contained in the exception message and can be used for troubleshooting purposes
                Console.WriteLine($"Email send operation failed with error code: {ex.ErrorCode}, message: {ex.Message}");
            }
        }
    }
}
