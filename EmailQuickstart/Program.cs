using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Azure;
using Azure.Communication.Email;
using Azure.Communication.Email.Models;

namespace SendEmail
{
  internal class Program
  {
      static async Task Main(string[] args)
      {
          
      }
  }
}

// This code demonstrates how to fetch your connection string
// from an environment variable.
string connectionString = Environment.GetEnvironmentVariable("endpoint=https://verizann-media.communication.azure.com/;accesskey=EQHWAYFO9E0NNcj8OZEFHcVtFWUa1EBEV4tsgX1ej53kJjv4v9ZgBLVotnwhKRtjTxdIf2UEq4xoJ5n/on5IYA==
");
EmailClient emailClient = new EmailClient(connectionString);

// This code demonstrates how to authenticate to your Communication Service resource using
// DefaultAzureCredential and the environment variables AZURE_CLIENT_ID, AZURE_TENANT_ID,
// and AZURE_CLIENT_SECRET.
string resourceEndpoint = "https://verizann-media.communication.azure.com/";
EmailClient emailClient = new EmailClient(new Uri(resourceEndpoint), new DefaultAzureCredential());

//Replace with your domain and modify the content, recipient details as required

EmailContent emailContent = new EmailContent("Welcome to Azure Communication Service Email APIs.");
emailContent.PlainText = "This email message is sent from Azure Communication Service Email using .NET SDK.";
List<EmailAddress> emailAddresses = new List<EmailAddress> { new EmailAddress("anthony.robinson@lookinnovative.com") { DisplayName = "Anthony Robinson" }};
EmailRecipients emailRecipients = new EmailRecipients(emailAddresses);
EmailMessage emailMessage = new EmailMessage("DoNotReply@402eaf99-ad29-431c-94fb-389dd5ab257a.azurecomm.net", emailContent, emailRecipients);
SendEmailResult emailResult = emailClient.Send(emailMessage,CancellationToken.None);

Console.WriteLine($"MessageId = {emailResult.MessageId}");

Response<SendStatusResult> messageStatus = null;
messageStatus = emailClient.GetSendStatus(emailResult.MessageId);
Console.WriteLine($"MessageStatus = {messageStatus.Value.Status}");
TimeSpan duration = TimeSpan.FromMinutes(3);
long start = DateTime.Now.Ticks;
do
{
    messageStatus = emailClient.GetSendStatus(emailResult.MessageId);
    if (messageStatus.Value.Status != SendStatus.Queued)
    {
        Console.WriteLine($"MessageStatus = {messageStatus.Value.Status}");
        break;
    }
    Thread.Sleep(10000);
    Console.WriteLine($"...");

} while (DateTime.Now.Ticks - start < duration.Ticks);