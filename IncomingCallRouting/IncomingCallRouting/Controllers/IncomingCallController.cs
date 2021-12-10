using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Azure.Communication.CallingServer;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;

namespace IncomingCallRouting.Controllers
{
    [ApiController]
    public class IncomingCallController : Controller
    {
        private readonly CallingServerClient callingServerClient;
        List<Task> incomingCalls;
        CallConfiguration callConfiguration;
        public IncomingCallController(IConfiguration configuration, ILogger<IncomingCallController> logger)
        {
            Logger.SetLoggerInstance(logger);
            callingServerClient = new CallingServerClient(configuration["ResourceConnectionString"]);
            incomingCalls = new List<Task>();
            callConfiguration = CallConfiguration.getCallConfiguration(configuration);
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
                        string incomingCallContext = eventData.Split("\"incomingCallContext\":\"")[1].Split("\"}")[0];
                        incomingCalls.Add(Task.Run(async () => await new IncomingCallHandler(callingServerClient, callConfiguration).Report(incomingCallContext)));
                    }
                }
                return Ok();
            }
            catch (Exception ex)
            {
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
                if(EventAuthHandler.Authorize(secret))
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
                return Json(new { Exception = ex });
            }
        }

    }
}
