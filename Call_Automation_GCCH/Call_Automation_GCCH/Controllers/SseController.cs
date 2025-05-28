using System.Text;
using Call_Automation_GCCH.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Call_Automation_GCCH.Controllers
{
    [ApiController]
    [Route("sse")]
    public class SseController : ControllerBase
    {
        private readonly IServerSentEventsService _eventService;

        public SseController(IServerSentEventsService eventService)
        {
            _eventService = eventService;
        }

        /// <summary>
        /// Connect to the server-sent events stream.
        /// This method sets the appropriate headers for SSE and listens for messages.
        /// </summary>
        [HttpGet("connect")]
        [Tags("Developer")]
        public async Task Connect(CancellationToken cancellationToken)
        {
            Response.Headers.Add(HeaderNames.ContentType, "text/event-stream");
            Response.Headers.Add(HeaderNames.CacheControl, "no-cache");
            Response.Headers.Add(HeaderNames.Connection, "keep-alive");

            var responseStream = Response.Body;
            var reader = _eventService.Reader;

            while (!HttpContext.RequestAborted.IsCancellationRequested)
            {
                while (reader.TryRead(out var message))
                {
                    var data = Encoding.UTF8.GetBytes(message);

                    await responseStream.WriteAsync(data, cancellationToken);
                    await responseStream.FlushAsync(cancellationToken);
                }

                await Task.Delay(1000);
            }

        }
    }
}
