using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using IncomingCallRouting.EventHandler;
using IncomingCallRouting.Models;
using IncomingCallRouting.Utils;
using Azure.Communication.CallAutomation;
using Logger = IncomingCallRouting.Utils.Logger;

namespace IncomingCallRouting.Controllers
{
    [ApiController]
    public class IncomingCallController : Controller
    {
        private readonly CallAutomationClient callAutomationClient;
        CallConfiguration callConfiguration;
        EventAuthHandler eventAuthHandler;

        public IncomingCallController(IConfiguration configuration, ILogger<IncomingCallController> logger)
        {
            Logger.SetLoggerInstance(logger);
            var options = new CallAutomationClientOptions { Diagnostics = { LoggedHeaderNames = { "*" } } };
            //callAutomationClient = new CallAutomationClient(new Uri(configuration["PmaUri"]), configuration["ResourceConnectionString"], options);
            callAutomationClient = new CallAutomationClient(configuration["ResourceConnectionString"], options);
            eventAuthHandler = new EventAuthHandler(configuration["SecretValue"]);
            callConfiguration = CallConfiguration.GetCallConfiguration(configuration, eventAuthHandler.GetSecretQuerystring);
        }

        /// Web hook to receive the incoming call Event
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("OnIncomingCall")]
        public IActionResult OnIncomingCall([FromBody] EventGridEvent[] eventGridEvents)
        {
            try
            {
                EventGridEvent eventGridEvent = eventGridEvents.FirstOrDefault();

                if (eventGridEvents.FirstOrDefault().EventType == SystemEventNames.EventGridSubscriptionValidation)
                {
                    var eventData = eventGridEvent.Data.ToObjectFromJson<SubscriptionValidationEventData>();
                    var responseData = new SubscriptionValidationResponse
                    {
                        ValidationResponse = eventData.ValidationCode
                    };

                    if (responseData.ValidationResponse != null)
                    {
                        return Ok(responseData);
                    }
                }
                else if (eventGridEvent.EventType.Equals("Microsoft.Communication.IncomingCall"))
                {
                    //Fetch incoming call context and ivr participant from request
                    var eventData = eventGridEvent.Data.ToObjectFromJson<IncomingCallData>(new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    if (eventData != null)
                    {
                        var incomingCallContext = eventData.IncomingCallContext;
                        var ivrParticipant = eventData.To.RawId;
                        
                        if ( (callConfiguration.IvrParticipants.Contains(ivrParticipant) || callConfiguration.IvrParticipants[0] == "*")
                            && callConfiguration.TargetParticipant != ivrParticipant)
                        {
                            _ = new IncomingCallHandler(callAutomationClient, callConfiguration).Report(incomingCallContext);
                        }
 
                    }
                }
                return Ok();
            }
            catch (Exception ex)
            {
                Logger.LogMessage(Logger.MessageType.ERROR, $"Fails in OnIncomingCall ---> {ex.Message}");
                return Json(new { Exception = ex });
            }
        }

        /// <summary>
        /// Extracting event from the json.
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("CallingServerAPICallBacks")]
        public IActionResult CallingServerAPICallBacks([FromBody] object request, [FromQuery] string secret)
        {
            try
            {
                if(eventAuthHandler.Authorize(secret))
                {
                    if (request != null)
                    {
                        EventDispatcher.Instance.ProcessNotification(request.ToString());
                    }
                    return Ok();
                }
                else
                {
                    return Unauthorized();
                }
            }
            catch (Exception ex)
            {
                Logger.LogMessage(Logger.MessageType.ERROR, $"Fails with CallingServerAPICallBack ---> {ex.Message}");
                return Json(new { Exception = ex });
            }
        }

    }
}
