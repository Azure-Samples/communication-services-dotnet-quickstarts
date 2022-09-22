// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Communication.CallingServer.Sample.CallPlayAudio
{
    using System.Net;
    using System.Threading.Tasks;
    using System.Web.Http;

    public class CallPlayTerminateController : ApiController
    {
        [HttpPost]
        [ActionName("callback")]
        [AllowAnonymous]
        public async Task<IHttpActionResult> OnIncomingRequestAsync()
        {
            // Validating the incoming request by using secret set in app.settings
            if (EventAuthHandler.Authorize(Request))
            {
                var data = await Request.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!string.IsNullOrEmpty(data))
                {
                    EventDispatcher.Instance.ProcessNotification(data);
                }

                return Ok();
            }
            else
            {
                return StatusCode(HttpStatusCode.Unauthorized);
            }
        }

    }
}
