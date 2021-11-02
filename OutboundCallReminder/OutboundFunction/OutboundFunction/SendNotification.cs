// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace OutboundFunction
{
    public static class SendNotification
    {
        [FunctionName("SendNotification")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, new string[] { "get", "post" }, Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            string responseMessage = null;
            Logger.logger = log;

            //read using Microsoft.Extensions.Configuration
            new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
            try
            {
                log.LogInformation("C# HTTP trigger function processed a request.");

                string requestBody = new StreamReader(req.Body).ReadToEndAsync().Result;
                dynamic data = JsonConvert.DeserializeObject(requestBody);

                log.LogInformation($"request data --> {data}");

                string sourceNumber = data?.SourceNumber;
                string targetNumber = data?.OutboundNumber;

                if(!string.IsNullOrEmpty(targetNumber) && !string.IsNullOrEmpty(sourceNumber))
                {
                    string isSmsSend = data?.SMS?.Send;

                    log.LogInformation($"sourceNumber --> {sourceNumber}");
                    log.LogInformation($"targerNumber --> {targetNumber}");

                    if (!string.IsNullOrEmpty(isSmsSend) && isSmsSend == "true")
                    {
                        string message = data?.SMS?.Message;

                        if (!string.IsNullOrEmpty(message))
                        {
                            var sendSMS = new SendSMS();
                            Task.Run(() => { sendSMS.SendOneToOneSms(sourceNumber, targetNumber, message); });
                        }
                        else
                        {
                            responseMessage = "SMS notification sent Failed, please pass message in request body.";
                        }
                    }

                    string isInitiatePhoneCall = data?.PhoneCall?.Send;
                    if (!string.IsNullOrEmpty(isInitiatePhoneCall) && isInitiatePhoneCall == "true")
                    {
                        string callbackUrl = $"{req.Scheme}://{req.Host}/api/";
                        string audioUrl = data?.PhoneCall?.PlayAudioUrl;
                        var phoneCall = new Phonecall(callbackUrl);
                        Task.Run(() => { phoneCall.InitiatePhoneCall(sourceNumber, targetNumber, audioUrl); });
                    }

                    if(string.IsNullOrEmpty(responseMessage))
                        responseMessage = "Notification sent successfully.";
                }
                else
                {
                    responseMessage = "Notification sent failed. Pass target/source number in the request body.";
                }
            }
            catch(Exception ex)
            {
                log.LogError($"Send Notification failed with exception -->  {ex.Message}");
            }

            return new OkObjectResult(responseMessage);
        }
    }
}
