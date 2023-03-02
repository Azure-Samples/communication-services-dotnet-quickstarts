using Azure.Communication.Email;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;

namespace SendEmailWithAttachments
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // This code demonstrates how to send email using Azure Communication Services.
            var connectionString = "<ACS_CONNECTION_STRING>";
            var emailClient = new EmailClient(connectionString);

            var subject = "Send email sample - With attachments";
            var emailContent = new EmailContent(subject)
            {
                PlainText = "This is plain mail send test body \n Best Wishes!!",
                Html = "<html><body><h1>Quick send email test</h1><br/><h4>Communication email as a service mail send app working properly</h4><p>Happy Learning!!</p></body></html>"
            };
            var sender = "<SENDER_EMAIL>";

            var emailRecipients = new EmailRecipients(new List<EmailAddress> {
                new EmailAddress("<RECIPIENT_EMAIL>", "<RECIPINENT_DISPLAY_NAME>")
            });

            var emailMessage = new EmailMessage(sender, emailRecipients, emailContent);

            // Add Email Pdf Attchament
            byte[] bytes = File.ReadAllBytes("attachment.pdf");
            var contentBinaryData = new BinaryData(bytes);
            var emailAttachment = new EmailAttachment("attachment.pdf", MediaTypeNames.Application.Pdf, contentBinaryData);
            emailMessage.Attachments.Add(emailAttachment);

            try
            {
                Console.WriteLine("Sending email with attachments...");
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
