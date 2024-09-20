using Azure;
using Azure.Communication.Email;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;

namespace SendEmailWithInlineAttachments
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // This code demonstrates how to send email using Azure Communication Services.
            var connectionString = "<ACS_CONNECTION_STRING>";
            var emailClient = new EmailClient(connectionString);

            var subject = "Send email sample - With inline attachments";
            var emailContent = new EmailContent(subject)
            {
                PlainText = "This is plain mail send test body \n Best Wishes!!",
                Html = "<html><h1>HTML body inline images:</h1><img src=\"cid:kittens-1\" /><img src=\"cid:kittens-2\" /></html>"
            };
            var sender = "<SENDER_EMAIL>";

            var emailRecipients = new EmailRecipients(new List<EmailAddress> {
                new EmailAddress("<RECIPIENT_EMAIL>", "<RECIPIENT_DISPLAY_NAME>")
            });

            var emailMessage = new EmailMessage(sender, emailRecipients, emailContent);

            // Add jpg attachment
            byte[] jpgBytes = File.ReadAllBytes("inline-attachment.jpg");
            var jpgContentBinaryData = new BinaryData(jpgBytes);
            var jpgEmailAttachment = new EmailAttachment("inline-attachment.jpg", "image/jpeg", jpgContentBinaryData);
            jpgEmailAttachment.ContentId = "kittens-1";
            emailMessage.Attachments.Add(jpgEmailAttachment);

            // Add png attachment
            byte[] pngBytes = File.ReadAllBytes("inline-attachment.png");
            var pngContentBinaryData = new BinaryData(pngBytes);
            var pngEmailAttachment = new EmailAttachment("inline-attachment.png", "image/png", pngContentBinaryData);
            pngEmailAttachment.ContentId = "kittens-2";
            emailMessage.Attachments.Add(pngEmailAttachment);

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
