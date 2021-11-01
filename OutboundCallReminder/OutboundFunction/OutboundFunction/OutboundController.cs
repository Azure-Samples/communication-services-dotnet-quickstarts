using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace OutboundFunction
{
    public static class OutboundController
    {
        [FunctionName("OutboundController")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, new string[] { "get", "post" }, Route = "outboundcall/callback")] HttpRequest req,
            ILogger log)
        {
            try
            {
                if (EventAuthHandler.Authorize(req))
                {
                    // handling callbacks
                    var data = new StreamReader(req.Body).ReadToEndAsync().Result;

                    if (!string.IsNullOrEmpty(data))
                    {
                        log.LogInformation($"Getting callback with req data --> {data}");
                        Task.Run(() => 
                        { 
                            EventDispatcher.Instance.ProcessNotification(data);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError($"Send Notification failed with exception -->  {ex.Message}");
            }
            return new OkObjectResult("OK");
        }
    }
}
