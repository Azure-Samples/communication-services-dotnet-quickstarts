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
        [FunctionName("SendSMSUrlShortener")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            
            //Parse Query Parameters
            string phoneNumberTo = req.Query["phoneNumber"];
            string urlToShorten = req.Query["url"];

            //Get short URL from Azure URL Shortener
            using var client = new HttpClient();
            var requestData = new
            {
                Url = urlToShorten
            };
            var requestBody = JsonSerializer.Serialize(requestData);
            var httpContent = new StringContent(requestBody, Encoding.UTF8, "application/json");
            string urlShortener = Environment.GetEnvironmentVariable("URL_SHORTENER", EnvironmentVariableTarget.Process); // Ex. https://<Azure Function URL>/api/UrlCreate
            var response = await client.PostAsync(urlShortener, httpContent);
            var content = await response.Content.ReadAsStringAsync();
            var data = System.Text.Json.JsonSerializer.Deserialize<ShortenedUrl>(content);
            var url = data.ShortUrl;

            //Send SMS with Azure Communication Services
            string connectionString = Environment.GetEnvironmentVariable("ACS_CONNECTIONSTRING", EnvironmentVariableTarget.Process);
            string phoneNumberFrom = Environment.GetEnvironmentVariable("ACS_PHONE_NUMBER", EnvironmentVariableTarget.Process); // Ex. +15555555555
            SmsClient smsClient = new SmsClient(connectionString);
            SmsSendResult sendResult = smsClient.Send(
                from: phoneNumberFrom,
                to: phoneNumberTo,
                message: "Here is your shortened URL: " + url
            );

            return new OkObjectResult(sendResult);
        }
    }
}
