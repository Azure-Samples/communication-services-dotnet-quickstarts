using System;
using System.IO;
using System.Collections.Generic;
using Azure;
using Azure.Communication.Email;
using System.Threading;
using System.Threading.Tasks;
using Azure.Communication.Email.Models;

namespace EmailSample
{
  class EmailSender
  {
    private string resourceConnectionString;
    private EmailClient emailClient;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="connectionString"></param>
    public EmailSender(string connectionString)
    {
      this.resourceConnectionString = connectionString;
    }

    /// <summary>
    /// Sending mail to all the recipients
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public async Task SendEmailToRecipients(EmailTemplate email)
    {
      Console.WriteLine($"Sending Email --> { email.TemplateName}");
      EmailContent emailContent = null;

      try
      {
        //Setting email Importance
        EmailImportance emailImportance = EmailImportance.Normal;
        if (!string.IsNullOrWhiteSpace(email.Importance))
        {
          emailImportance = new EmailImportance(email.Importance.ToLower());
        }

        // Setting Email content as PlainText/ HTML
        if (email.HTMLText != "")
        {
          emailContent = new EmailContent(email.Subject,
              new EmailBody { Html = email.HTMLText });
        }
        else
        {
          emailContent = new EmailContent(email.Subject,
              new EmailBody { PlainText = email.PlainText });
        }
        emailContent.Importance = emailImportance;

        var emailRecipients = ProcessEmailRecipients(email.Recipients);
        var emailMessage = new EmailMessage(email.Sender, emailContent, emailRecipients);

        if (!string.IsNullOrWhiteSpace(email.Attachments))
        {
          var attachements = email.Attachments.Split(new char[] { ',', ';' });
          foreach (var attachment in attachements)
          {
            emailMessage.Attachments.Add(GetEmailAttachment(attachment.Trim()));
          }
        }

        this.emailClient = new EmailClient(this.resourceConnectionString);

        Response<SendEmailResult> response = emailClient.Send(emailMessage);

        await CheckEmailStatus(response.GetRawResponse(), email.TemplateName);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Failed to send email: {email.TemplateName}--> {ex.Message}");
        throw;
      }
    }


    /// <summary>
    /// Process list of email recipients
    /// </summary>
    /// <param name="emailRecipients"></param>
    /// <returns> EmailRecipients object </returns>
    public EmailRecipients ProcessEmailRecipients(string emailRecipients)
    {
      List<EmailAddress> emailAddresses = new List<EmailAddress>();
      var recipients = emailRecipients.Split(";");

      try
      {
        foreach (var recipient in recipients)
        {
          var details = recipient.Split(",");
          if (details.Length == 2 && !string.IsNullOrWhiteSpace(details[0]) && !string.IsNullOrWhiteSpace(details[1]))
          {
            emailAddresses.Add(new EmailAddress(details[0]) { DisplayName = details[1] });
          }
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Failed to get Email Recipients --> {ex.Message}");
        throw;
      }

      return new EmailRecipients(emailAddresses);
    }

    /// <summary>
    /// Processing Attachment File
    /// </summary>
    /// <param name="attachmentPath"></param>
    /// <param name="emailMessage"></param>
    /// <returns>None</returns>
    private static EmailAttachment GetEmailAttachment(string attachmentPath)
    {
      EmailAttachment emailAttachment = null;

      try
      {
        byte[] bytes = File.ReadAllBytes(attachmentPath);
        string attachmentFileInBytes = Convert.ToBase64String(bytes);
        var fileName = Path.GetFileName(attachmentPath);
        EmailAttachmentType attachmentType = fileName.Split(".")[1];
        emailAttachment = new EmailAttachment(fileName, attachmentType, attachmentFileInBytes);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Failed to get the attachment --> {ex.Message}");
      }

      return emailAttachment;
    }

    /// <summary>
    /// Checking Email Status
    /// </summary>
    /// <param name="response"></param>
    /// <param name="emailId"></param>
    /// <returns>None</returns>
    public async Task CheckEmailStatus(Response response, string emailName)
    {
      try
      {
        string messageId = string.Empty;
        if (!response.IsError)
        {
          if (!response.Headers.TryGetValue("x-ms-request-id", out messageId))
          {
            Console.WriteLine($"MessageId of Email: {emailName} not found");
            return;
          }
          else
          {
            Console.WriteLine($"Email: {emailName} Sent, MessageId = {messageId}");
          }
        }
        else
        {
          Console.WriteLine($"Failed to send Email: {emailName}, response: {response.ToString()}");
          return;
        }

        await Task.Delay(TimeSpan.FromSeconds(5));

        Console.WriteLine($"Waiting for Email: {emailName} to go past Queued");
        var cancellationToken = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        Response<StatusFoundResponse> messageStatus = null;

        DateTime checkStatusStartTime = DateTime.Now.ToLocalTime();

        do
        {
          messageStatus = this.emailClient.GetSendStatus(messageId);

          Console.WriteLine($"[{DateTime.Now.ToLocalTime()}] Email send status for {emailName} :: [{messageStatus.Value.Status}],  waitTime = {DateTime.Now.ToLocalTime() - checkStatusStartTime} secs");

          if (messageStatus.Value.Status == MessageStatus.Queued)
          {
            await Task.Delay(TimeSpan.FromSeconds(10));
          }
          else
          {
            break;
          }
        } while (!cancellationToken.IsCancellationRequested);

        if (cancellationToken.IsCancellationRequested)
        {
          Console.WriteLine($"Looks like we timed out for email-{emailName} ");
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Failed while checking the Status of Mail {emailName} --> {ex.Message}");
        throw;
      }
    }
  }
}
