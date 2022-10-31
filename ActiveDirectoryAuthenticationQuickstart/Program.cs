using Azure.Identity;
using Azure.Communication.Identity;
using Azure.Communication.Sms;
using Azure.Core;
using Azure;
using System.Data;

class Program
     {
          private DefaultAzureCredential credential = new DefaultAzureCredential();
          static void Main(string[] args)
          {
               // You can find your endpoint and access key from your resource in the Azure portal
               // e.g. "https://<RESOURCE_NAME>.communication.azure.com";
               Uri endpoint = new("https://verizann-media.communication.azure.com/");
        string conString = "endpoint=https://verizann-media.communication.azure.com/;accesskey=EQHWAYFO9E0NNcj8OZEFHcVtFWUa1EBEV4tsgX1ej53kJjv4v9ZgBLVotnwhKRtjTxdIf2UEq4xoJ5n/on5IYA==";
               // We need an instance of the program class to use within this method.
               Program instance = new();

               Console.WriteLine("Retrieving new Access Token, using Service Principals");
               Response<AccessToken> response = instance.CreateIdentityAndGetTokenAsync(conString);
               Console.WriteLine($"Retrieved Access Token: {response.Value.Token}");

               Console.WriteLine("Sending SMS using Service Principals");

               // You will need a phone number from your resource to send an SMS.
               SmsSendResult result = instance.SendSms(endpoint, "+18772178780", "+14048386995", "Hello from Service Principals");
               Console.WriteLine($"Sms id: {result.MessageId}");
               Console.WriteLine($"Send Result Successful: {result.Successful}");
          }
          public Response<AccessToken> CreateIdentityAndGetTokenAsync(string conString)
    {
        var client = new CommunicationIdentityClient(conString);
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