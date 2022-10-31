using System;
using System.Collections.Generic;
using Azure;
using Azure.Communication;
using Azure.Communication.Sms;

// This code retrieves your connection string
// from an environment variable.
//string connectionString = Environment.GetEnvironmentVariable("COMMUNICATION_SERVICES_CONNECTION_STRING");

//SmsClient smsClient = new SmsClient(connectionString);

namespace SendSMS
{
    internal class Program
    {
        private static void Main()
        {
            var connectionString = "endpoint=https://verizann-media.communication.azure.com/;accesskey=EQHWAYFO9E0NNcj8OZEFHcVtFWUa1EBEV4tsgX1ej53kJjv4v9ZgBLVotnwhKRtjTxdIf2UEq4xoJ5n/on5IYA=="; // Find your Communication Services resource in the Azure portal
            SmsClient smsClient = new SmsClient(connectionString);

            SmsSendResult sendResult = smsClient.Send(
                from: "+18772178780", // Your E.164 formatted from phone number used to send SMS
                to: "+14048386995", // E.164 formatted recipient phone number
                message: "Hello 👋🏻");
            Console.WriteLine($"Message id {sendResult.MessageId}");

            Response<IReadOnlyList<SmsSendResult>> response = smsClient.Send(
            from: "+18772178780",
            to: new string[] { "+14048386995", "+14045477873" }, // E.164 formatted recipient phone numbers
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
