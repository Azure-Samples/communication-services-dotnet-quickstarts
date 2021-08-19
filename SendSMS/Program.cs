using System;
using System.Collections.Generic;
using Azure;
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

            Response<IReadOnlyList<SmsSendResult>> response = smsClient.Send(
            from: "<from-phone-number>",
            to: new string[] { "<to-phone-number-1>", "<to-phone-number-2>" },
            message: "Hello 👋🏻",
            options: new SmsSendOptions(enableDeliveryReport: true) // OPTIONAL
            {
                Tag = "greeting", // custom tags
            });
  
            IEnumerable<SmsSendResult> results = response.Value;
            foreach (SmsSendResult result in results)
            {
                Console.WriteLine($"Sms id: {result.MessageId}");
                Console.WriteLine($"Send Result Successful: {result.Successful}");
            }
        }
    }
}
