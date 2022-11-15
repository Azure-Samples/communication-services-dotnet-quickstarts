using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Azure.Communication.CallAutomation;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using System.Text.Json.Nodes;

namespace IncomingCallMediaStreaming.Controllers
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
            callAutomationClient = new CallAutomationClient(configuration["ResourceConnectionString"]);
            eventAuthHandler = new EventAuthHandler(configuration["SecretValue"]);
            callConfiguration = CallConfiguration.GetCallConfiguration(configuration, eventAuthHandler.GetSecretQuerystring);
        }

        [HttpGet]
        [Route("ApiCheck")]
        public IActionResult ApiCheck()
        {
            return Ok("API is working fine...");
        }

        /// Web hook to receive the incoming call Event
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("OnIncomingCall")]
        public IActionResult OnIncomingCall([FromBody] object request)
        {
            try
            {
                var httpContent = new BinaryData(request.ToString()).ToStream();
                EventGridEvent cloudEvent = EventGridEvent.ParseMany(BinaryData.FromStream(httpContent)).FirstOrDefault();

                if (cloudEvent.EventType == SystemEventNames.EventGridSubscriptionValidation)
                {
                    var eventData = cloudEvent.Data.ToObjectFromJson<SubscriptionValidationEventData>();
                    var responseData = new SubscriptionValidationResponse
                    {
                        ValidationResponse = eventData.ValidationCode
                    };

                    if (responseData.ValidationResponse != null)
                    {
                        return Ok(responseData);
                    }
                }
                else if (cloudEvent.EventType.Equals("Microsoft.Communication.IncomingCall"))
                {
                    //Fetch incoming call context from request
                    var eventData = request.ToString();
                    if (eventData != null)
                    {
                        var jsonObject = JsonNode.Parse(cloudEvent.Data).AsObject();
                        var callerId = (string)(jsonObject["from"]["rawId"]);
                        if((callerId != null && callConfiguration.AcceptCallsFrom.Contains(callerId)) || 
                            callConfiguration.AcceptCallsFrom == "*")
                        {
                            string incomingCallContext = (string)jsonObject["incomingCallContext"];
                            Logger.LogMessage(Logger.MessageType.INFORMATION, incomingCallContext);
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
        [Route("CallAutomationApiCallBack")]
        public IActionResult CallAutomationApiCallBack([FromBody] object request, [FromQuery] string secret)
        {
            try
            {
                if (eventAuthHandler.Authorize(secret))
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
                Logger.LogMessage(Logger.MessageType.ERROR, $"CallAutomationApiCallBack fails : ---> {ex.Message}");
                return Json(new { Exception = ex });
            }
        }

    }
}
