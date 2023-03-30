using Azure.Communication.Email;
using System;
using System.Net;
using System.Threading;
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

            var subject = "Send email with manual status polling";
            var emailContent = new EmailContent(subject)
            {
                PlainText = "This is plain text mail send test body \n Best Wishes!!",
                Html = "<html><body><h1>Quick send email test</h1><br/><h4>Communication email as a service mail send app working properly</h4><p>Happy Learning!!</p></body></html>"
            };
            var sender = "<SENDER_EMAIL>";
            var recipient = "<RECIPIENT_EMAIL>";

            var emailMessage = new EmailMessage(sender, recipient, emailContent);

            try
            {
                Console.WriteLine("Sending email with manual polling for status...");
                EmailSendOperation emailSendOperation = await emailClient.SendAsync(Azure.WaitUntil.Started, emailMessage);

                var cancellationToken = new CancellationTokenSource(TimeSpan.FromMinutes(2));

                // Poll for email send status manually
                while (!cancellationToken.IsCancellationRequested)
                {
                    await emailSendOperation.UpdateStatusAsync();
                    if (emailSendOperation.HasCompleted)
                    {
                        break;
                    }
                    Console.WriteLine("Email send operation is still running. Rechecking after 1 second...");
                    await Task.Delay(1000);
                }

                if (emailSendOperation.HasValue)
                {
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
                else if (cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine($"Looks like we timed out while manually polling for email status");
                }
            }
            catch (RequestFailedException ex)
            {
                /// OperationID is contained in the exception message and can be used for troubleshooting purposes
                Console.WriteLine($"Email send operation failed with error code: {ex.ErrorCode}, message: {ex.Message}");
            }
        }
    }
}
