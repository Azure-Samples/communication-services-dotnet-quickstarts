using Azure;
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
                new EmailAddress("<RECIPIENT_EMAIL>", "<RECIPIENT_DISPLAY_NAME>")
            });

            var emailMessage = new EmailMessage(sender, emailRecipients, emailContent);

            // Add pdf attachment
            byte[] pdfBytes = File.ReadAllBytes("attachment.pdf");
            var pdfContentBinaryData = new BinaryData(pdfBytes);
            var pdfEmailAttachment = new EmailAttachment("attachment.pdf", MediaTypeNames.Application.Pdf, pdfContentBinaryData);
            emailMessage.Attachments.Add(pdfEmailAttachment);

            // Add txt attachment
            byte[] txtBytes = File.ReadAllBytes("attachment.txt");
            var txtContentBinaryData = new BinaryData(txtBytes);
            var txtEmailAttachment = new EmailAttachment("attachment.txt", MediaTypeNames.Application.Pdf, txtContentBinaryData);
            emailMessage.Attachments.Add(txtEmailAttachment);

            try
            {
                Console.WriteLine("Sending email with attachments...");
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
