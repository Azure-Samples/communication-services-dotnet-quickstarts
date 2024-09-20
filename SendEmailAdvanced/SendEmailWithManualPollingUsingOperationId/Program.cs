﻿using Azure;
using Azure.Communication.Email;
using System;
using System.Threading.Tasks;

namespace SendEmailWithManualPollingUsingOperationId
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // This code demonstrates how to send email using Azure Communication Services.
            var connectionString = "<ACS_CONNECTION_STRING>";
            var emailClient = new EmailClient(connectionString);

            var sender = "<SENDER_EMAIL>";
            var recipient = "<RECIPIENT_EMAIL>";
            var subject = "Send email with manual status polling using operationID";

            var emailContent = new EmailContent(subject)
            {
                PlainText = "This is plain text mail send test body \n Best Wishes!!",
                Html = "<html><body><h1>Quick send email test</h1><br/><h4>Communication email as a service mail send app working properly</h4><p>Happy Learning!!</p></body></html>"
            };

            var emailMessage = new EmailMessage(sender, recipient, emailContent);

            var emailSendOperation = await emailClient.SendAsync(
                wait: WaitUntil.Started,
                message: emailMessage);

            /// Get the OperationId so that it can be used for rehydrating an EmailSendOperation object
            /// and use that object to poll for the status of the email send operation.
            var operationId = emailSendOperation.Id;
            Console.WriteLine($"Email operation id = {operationId}");

            /// Do a bunch of other things here...

            /// Poll for the status of the email send operation using the previous operationId
            await PollForEmailSendOperationStatusWithExistingOperationId(emailClient, operationId);
        }

        private static async Task PollForEmailSendOperationStatusWithExistingOperationId(EmailClient emailClient, string operationId)
        {
            /// Rehydrate a new EmailSendOperation object using the given operationId
            /// Rehydration refers to the process of creating a new EmailSendOperation object using the operation ID from a previous EmailSendOperation.
            /// This is necessary in case you want to continue monitoring the status of the email manually, when you don't have 
            /// the original EmailSendOperation object from the initial request.
            EmailSendOperation rehydratedEmailSendOperation = new EmailSendOperation(operationId, emailClient);

            /// Call UpdateStatus on the rehydrated email send operation to poll for the status manually.
            try
            {
                while (true)
                {
                    await rehydratedEmailSendOperation.UpdateStatusAsync();
                    if (rehydratedEmailSendOperation.HasCompleted)
                    {
                        break;
                    }
                    await Task.Delay(100);
                }

                if (rehydratedEmailSendOperation.HasValue)
                {
                    Console.WriteLine($"Email queued for delivery. Status = {rehydratedEmailSendOperation.Value.Status}");
                }
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Email send failed with Code = {ex.ErrorCode} and Message = {ex.Message}");
            }
        }
    }
}
