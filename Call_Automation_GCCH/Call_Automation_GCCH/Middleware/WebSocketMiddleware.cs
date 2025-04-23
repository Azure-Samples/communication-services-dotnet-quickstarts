using System.Net.WebSockets;
using Microsoft.AspNetCore.Http;

namespace Call_Automation_GCCH.Middleware
{
    public class WebSocketMiddleware
    {
        private readonly RequestDelegate _next;

        public WebSocketMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path == "/ws" && context.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                await ProcessWebSocketRequest(webSocket);
            }
            else
            {
                await _next(context);
            }
        }

        private async Task ProcessWebSocketRequest(WebSocket webSocket)
        {
            // Handle WebSocket communication
            await Task.CompletedTask;
        }
    }
}
