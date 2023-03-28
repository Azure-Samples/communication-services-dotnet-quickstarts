using System;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Azure;
using Azure.Communication;
using Azure.Communication.Sms;
using System.Text;

namespace Company.Function
{
    public class ShortenedUrl
    {
        public string ShortUrl { get; set; }
    }

    public static class SendSMSUrlShortener
    {
        const string connectionString = "INSERT AZURE COMMUNICATION SERVICES CONNECTION STRING";
        const string phoneNumberFrom = "INSERT AZURE COMMUNICATION SERVICES PHONE NUMBER"; // Ex. +15555555555
        const string urlShortener = "INSERT AZURE URL SHORTENER ENDPOINT"; // Ex. https://<Azure Function URL>/api/UrlCreate
        [FunctionName("SendSMSUrlShortener")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            SmsClient smsClient = new SmsClient(connectionString);

            string phoneNumberTo = req.Query["phoneNumber"];
            string urlToShorten = req.Query["url"];

            using var client = new HttpClient();
            
            var requestData = new
            {
                Url = "https://microsoft.com"
            };

            var requestBody = JsonSerializer.Serialize(requestData);

            var httpContent = new StringContent(requestBody, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(urlShortener, httpContent);

            var content = await response.Content.ReadAsStringAsync();

            var data = System.Text.Json.JsonSerializer.Deserialize<ShortenedUrl>(content);

            var url = data.ShortUrl;

            SmsSendResult sendResult = smsClient.Send(
                from: phoneNumberFrom,
                to: phoneNumberTo,
                message: "Here is your shortened URL: " + url
            );

            return new OkObjectResult(sendResult);
        }
    }
}
