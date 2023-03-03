using Azure.Communication.Email;
using System;
using System.Threading.Tasks;

namespace SendEmail
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // This code demonstrates how to send email using Azure Communication Services.
            var connectionString = "<ACS_CONNECTION_STRING>";
            var emailClient = new EmailClient(connectionString);

            var subject = "Send email quick start - dotnet";
            var htmlContent = "<html><body><h1>Quick send email test</h1><br/><h4>Communication email as a service mail send app working properly</h4><p>Happy Learning!!</p></body></html>";
            var sender = "<SENDER_EMAIL>";
            var recipient = "<RECIPIENT_EMAIL>";

            try
            {
                Console.WriteLine("Sending email...");
                EmailSendOperation emailSendOperation = await emailClient.SendAsync(Azure.WaitUntil.Completed, sender, recipient, subject, htmlContent);
                EmailSendResult statusMonitor = emailSendOperation.Value;

                string operationId = emailSendOperation.Id;
                var emailSendStatus = statusMonitor.Status;

                if (emailSendStatus == EmailSendStatus.Succeeded)
                {
                    Console.WriteLine($"Email send operation succeeded with OperationId = {operationId}.\nEmail is out for delivery.");
                }
                else
                {
                    var error = statusMonitor.Error;
                    Console.WriteLine($"Failed to send email.\n OperationId = {operationId}.\n Status = {emailSendStatus}.");
                    Console.WriteLine($"Error Code = {error.Code}, Message = {error.Message}");
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
