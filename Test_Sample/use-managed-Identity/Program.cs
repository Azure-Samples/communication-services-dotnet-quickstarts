using System;
using Azure.Identity;
using Azure.Communication.Identity;
using Azure.Communication.Sms;
using Azure.Core;
using Azure;

namespace ManagedIdentitiesQuickstart
{
    class Program
    {
        private DefaultAzureCredential credential = new DefaultAzureCredential();
        static void Main(string[] args)
        {
            // You can find your endpoint and access key from your resource in the Azure portal
            // e.g. "https://<RESOURCE_NAME>.communication.azure.com";
            Uri endpoint = new("https://<RESOURCENAME>.communication.azure.com/");

            // We need an instance of the program class to use within this method.
            Program instance = new();

            Console.WriteLine("Retrieving new Access Token, using Managed Identities");
            Response<AccessToken> response = instance.CreateIdentityAndGetTokenAsync(endpoint);
            Console.WriteLine($"Retrieved Access Token: {response.Value.Token}");

            Console.WriteLine("Sending SMS using Managed Identities");

            // You will need a phone number from your resource to send an SMS.
            SmsSendResult result = instance.SendSms(endpoint, "<Your ACS Phone Number>", "<The Phone Number you'd like to send the SMS to.>", "Hello from Managed Identities");
            Console.WriteLine($"Sms id: {result.MessageId}");
            Console.WriteLine($"Send Result Successful: {result.Successful}");
        }
        public Response<AccessToken> CreateIdentityAndGetTokenAsync(Uri resourceEndpoint)
        {
            var client = new CommunicationIdentityClient(resourceEndpoint, this.credential);
            var identityResponse = client.CreateUser();
            var identity = identityResponse.Value;

            var tokenResponse = client.GetToken(identity, scopes: new[] { CommunicationTokenScope.VoIP });

            return tokenResponse;
        }
        public SmsSendResult SendSms(Uri resourceEndpoint, string from, string to, string message)
        {
            SmsClient smsClient = new SmsClient(resourceEndpoint, this.credential);
            SmsSendResult sendResult = smsClient.Send(
                 from: from,
                 to: to,
                 message: message,
                 new SmsSendOptions(enableDeliveryReport: true) // optional
            );

            return sendResult;
        }
    }
}
