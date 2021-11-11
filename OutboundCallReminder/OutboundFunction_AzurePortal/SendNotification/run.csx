#r "Newtonsoft.Json"

#load "Logger.csx"
#load "Phonecall.csx"
#load "Sendsms.csx"

using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

public static IActionResult Run(HttpRequest req, ILogger log)
{
    string responseMessage = null;

    try
    {
        Logger.SetLoggerInstance(log);

        string requestBody = new StreamReader(req.Body).ReadToEnd();
        dynamic data = JsonConvert.DeserializeObject(requestBody);
        string isSendNotification;

        try
        {
            isSendNotification = data?.SendNotification;
        }
        catch(Exception ex)
        {
            Logger.LogMessage(Logger.MessageType.INFORMATION, ex.Message);
            isSendNotification = null;
        }

        if (!string.IsNullOrWhiteSpace(isSendNotification) && isSendNotification.ToLower() == "true")
        {
            string sourceNumber = data?.SourceNumber;
            string targetNumber = data?.OutboundNumber;

            if(!string.IsNullOrWhiteSpace(targetNumber) && !string.IsNullOrWhiteSpace(sourceNumber))
            {
                string isSmsSend = data?.SMS?.Send;
                Logger.LogMessage(Logger.MessageType.INFORMATION, $"sourceNumber --> {sourceNumber}");
                Logger.LogMessage(Logger.MessageType.INFORMATION, $"targerNumber --> {targetNumber}");

                if (!string.IsNullOrWhiteSpace(isSmsSend) && isSmsSend.ToLower() == "true")
                {
                    string message = data?.SMS?.Message;
                    if (!string.IsNullOrWhiteSpace(message))
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
                if (!string.IsNullOrWhiteSpace(isInitiatePhoneCall) && isInitiatePhoneCall.ToLower() == "true")
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
                if (!string.IsNullOrWhiteSpace(requestBody))
                {
                    Task.Run(() =>
                    {
                        EventDispatcher.Instance.ProcessNotification(requestBody);
                    });
                    responseMessage = "OK";
                }
            }
        }
    }
    catch(Exception ex)
    {
        responseMessage = ex.Message;
    }

    return new OkObjectResult(responseMessage);
}