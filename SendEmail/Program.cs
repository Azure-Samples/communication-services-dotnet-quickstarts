using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Communication.Email;
using Azure.Communication.Email.Models;

namespace SendEmail
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // This code demonstrates how to send email using Azure Communication Services.
            string connectionString = "endpoint=https://verizann-media.communication.azure.com/;accesskey=EQHWAYFO9E0NNcj8OZEFHcVtFWUa1EBEV4tsgX1ej53kJjv4v9ZgBLVotnwhKRtjTxdIf2UEq4xoJ5n/on5IYA==";
            ;
            var emailClient = new EmailClient(connectionString);

            var subject = "Send email quick start - dotnet";
            var emailContent = new EmailContent(subject)
            {
                PlainText = "This is plain mail send test body \n Best Wishes!!",
                Html = "<html><body><h1>Quick send email test</h1><br/><h4>Communication email as a service mail send app working properly</h4><p>Happy Learning!!</p></body></html>"
            };
            var sender = "DoNotReply@402eaf99-ad29-431c-94fb-389dd5ab257a.azurecomm.net";

            var emailRecipients = new EmailRecipients(new List<EmailAddress> {
                new EmailAddress("pandeysun.342@gmail.com") { DisplayName = "Sunil Pandey" }
            });

            var emailMessage = new EmailMessage(sender, emailContent, emailRecipients);

            try
            {
                SendEmailResult sendEmailResult = emailClient.Send(emailMessage);

                string messageId = sendEmailResult.MessageId;
                if (!string.IsNullOrEmpty(messageId))
                {
                    Console.WriteLine($"Email sent, MessageId = {messageId}");
                }
                else
                {
                    Console.WriteLine($"Failed to send email.");
                    return;
                }

                // wait max 2 minutes to check the send status for mail.
                var cancellationToken = new CancellationTokenSource(TimeSpan.FromMinutes(2));
                do
                {
                    SendStatusResult sendStatus = emailClient.GetSendStatus(messageId);
                    Console.WriteLine($"Send mail status for MessageId : <{messageId}>, Status: [{sendStatus.Status}]");

                    if (sendStatus.Status != SendStatus.Queued)
                    {
                        break;
                    }
                    await Task.Delay(TimeSpan.FromSeconds(10));
                    
                } while (!cancellationToken.IsCancellationRequested);

                if (cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine($"Looks like we timed out for email");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in sending email, {ex}");
            }
        }
    }
}
