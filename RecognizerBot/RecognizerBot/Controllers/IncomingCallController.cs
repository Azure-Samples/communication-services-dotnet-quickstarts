using System;
using System.Linq;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RecognizerBot.EventHandler;
using RecognizerBot.Interfaces;
using RecognizerBot.Models;
using RecognizerBot.Utils;

namespace RecognizerBot.Controllers
{
    [ApiController]
    public class IncomingCallController : ControllerBase
    {
        private readonly IIncomingCallService _incomingCallService;
        readonly CallConfiguration _callConfiguration;

        public IncomingCallController(IConfiguration configuration, ILogger<IncomingCallController> logger, IIncomingCallService incomingCallService)
        {
            _incomingCallService = incomingCallService;
            Logger.SetLoggerInstance(logger);
            _callConfiguration = CallConfiguration.GetCallConfiguration(configuration);
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
                    var eventData = JsonConvert.DeserializeObject<IncomingCallData>(eventGridEvent.Data.ToString());
                    if (eventData != null)
                    {
                        var incomingCallContext = eventData.IncomingCallContext;
                        var ivrParticipant = eventData.To.RawId;
                        var from = eventData.From.RawId;

                        if ( (_callConfiguration.IvrParticipants.Contains(ivrParticipant) || _callConfiguration.IvrParticipants[0] == "*")
                            && _callConfiguration.TargetParticipant != ivrParticipant)
                        {
                            _incomingCallService.HandleCall(incomingCallContext);
                        }
 
                    }
                }
                return Ok();
            }
            catch (Exception ex)
            {
                Logger.LogMessage(Logger.MessageType.ERROR, $"Fails in OnIncomingCall ---> {ex.Message}");
                return BadRequest(new { Exception = ex });
            }
        }

        /// <summary>
        /// Extracting event from the json.
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("CallingServerAPICallBacks")]
        public IActionResult CallingServerAPICallBacks([FromBody] object request)
        {
            try
            {
                if (request != null)
                {
                    EventDispatcher.Instance.ProcessNotification(request.ToString());
                }
                return Ok();
            }
            catch (Exception ex)
            {
                Logger.LogMessage(Logger.MessageType.ERROR, $"Fails with CallingServerAPICallBack ---> {ex.Message}");
                return BadRequest(new { Exception = ex });
            }
        }
    }
}
