using Azure.Communication.Email;
using System;
using System.Threading.Tasks;

namespace SendEmailPlainText
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // This code demonstrates how to send email using Azure Communication Services.
            var connectionString = "<ACS_CONNECTION_STRING>";
            var emailClient = new EmailClient(connectionString);

            var subject = "Send email plain text sample";
            var emailContent = new EmailContent(subject)
            {
                PlainText = "This is plain text mail send test body \n Best Wishes!!",
            };
            var sender = "<SENDER_EMAIL>";
            var recipient = "<RECIPIENT_EMAIL>";

            var emailMessage = new EmailMessage(sender, recipient, emailContent);

            try
            {
                Console.WriteLine("Sending email with plain text content...");
                EmailSendOperation emailSendOperation = await emailClient.SendAsync(Azure.WaitUntil.Completed, emailMessage);
                EmailSendResult statusMonitor = emailSendOperation.Value;

                string operationId = emailSendOperation.Id;
                var emailSendStatus = statusMonitor.Status;

                if (emailSendStatus == EmailSendStatus.Succeeded)
                {
                    Console.WriteLine($"Email sent. \n OperationId = {operationId}. \n Status = {emailSendStatus}");
                }
                else
                {
                    Console.WriteLine($"Failed to send email. \n OperationId = {operationId}. \n Status = {emailSendStatus}");
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
