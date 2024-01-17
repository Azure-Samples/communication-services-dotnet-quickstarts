using System.Net;
using System.Net.Mail;


namespace SendEmailSMTP
{
    class Program
    {
        static void Main(String[] args)
        {
            string smtpAuthUsername = "<Azure Communication Services Resource name>|<Entra Application Id>|<Entra Application Tenant Id>";
            string smtpAuthPassword = "<Entra Application Client Secret>";
            string sender = "donotreply@xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx.azurecomm.net";
            string recipient = "emailalias@contoso.com";
            string subject = "Welcome to Azure Communication Service Email SMTP";
            string body = "This email message is sent from Azure Communication Service Email using SMTP.";

            string smtpHostUrl = "smtp.azurecomm.net";
            var client = new SmtpClient(smtpHostUrl)
            {
                Port = 587,
                Credentials = new NetworkCredential(smtpAuthUsername, smtpAuthPassword),
                EnableSsl = true
            };

            var message = new MailMessage(sender, recipient, subject, body);

            try
            {
                client.Send(message);
                Console.WriteLine("The email was successfully sent using Smtp.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Smtp failed the the exception: {ex.Message}.");
            }
        }
    }
}