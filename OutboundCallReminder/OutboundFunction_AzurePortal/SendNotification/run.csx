#r "Newtonsoft.Json"

#load "Logger.csx"
#load "Phonecall.csx"
#load "Sendsms.csx"

using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

public static async Task<IActionResult> Run(HttpRequest req, ILogger log)
{
    string responseMessage = null;
    Logger.SetLoggerInstance(log);

    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
    dynamic data = JsonConvert.DeserializeObject(requestBody);
    string isSendNotification;

    try
    {
        isSendNotification = data?.SendNotification;
    }
    catch(Exception ex)
    {
        isSendNotification = null;
    }

    if (!string.IsNullOrEmpty(isSendNotification) && isSendNotification.ToLower() == "true")
    {
        string sourceNumber = data?.SourceNumber;
        string targetNumber = data?.OutboundNumber;

        if(!string.IsNullOrEmpty(targetNumber) && !string.IsNullOrEmpty(sourceNumber))
        {
            string isSmsSend = data?.SMS?.Send;
            Logger.LogMessage(Logger.MessageType.INFORMATION, $"sourceNumber --> {sourceNumber}");
            Logger.LogMessage(Logger.MessageType.INFORMATION, $"targerNumber --> {targetNumber}");

            if (!string.IsNullOrEmpty(isSmsSend) && isSmsSend.ToLower() == "true")
            {
                string message = data?.SMS?.Message;
                if (!string.IsNullOrEmpty(message))
                {
                    var sendSMS = new SendSMS();
                    Task.Run(() => { sendSMS.SendOneToOneSms(sourceNumber, targetNumber, message); });
                }
                else
                {
                    responseMessage = "SMS notification request body missing message text. Please a message text to the request body.";
                }
            }

            string isInitiatePhoneCall = data?.PhoneCall?.Send;
            if (!string.IsNullOrEmpty(isInitiatePhoneCall) && isInitiatePhoneCall.ToLower() == "true")
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
    else
    {
        if (EventAuthHandler.Authorize(req))
        {
            // handling callbacks
            if (!string.IsNullOrEmpty(requestBody))
            {
                Task.Run(() =>
                {
                    EventDispatcher.Instance.ProcessNotification(requestBody);
                });
                responseMessage = "OK";
            }
        }
    }

    return new OkObjectResult(responseMessage);
}