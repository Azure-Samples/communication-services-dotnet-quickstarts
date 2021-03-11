using System;
using Azure.Communication;
using Azure.Communication.Sms;

namespace SendSMS
{
    internal class Program
    {
        private static void Main()
        {
            var connectionString = "<connection-string>"; // Find your Communication Services resource in the Azure portal
            SmsClient smsClient = new SmsClient(connectionString);

            SmsSendResult sendResult = smsClient.Send(
                    from: "<from-phone-number>", // Your E.164 formatted from phone number used to send SMS
                    to: "<to-phone-number>", // E.164 formatted recipient phone number
                    message: "Hello 👋🏻");
            Console.WriteLine($"Message id {sendResult.MessageId}");
        }
    }
}
