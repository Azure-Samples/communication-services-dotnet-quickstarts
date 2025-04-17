using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Call_Automation_GCCH.Middleware
{
    public class WebSocketMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<WebSocketMiddleware> _logger;

        public WebSocketMiddleware(RequestDelegate next, ILogger<WebSocketMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                if (context.Request.Path == "/ws")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        try
                        {
                            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                            await Helper.ProcessRequest(webSocket, _logger);
                        }
                        catch (Exception wsEx)
                        {
                            _logger.LogError(wsEx, "Error during WebSocket processing");
                            if (!context.Response.HasStarted)
                            {
                                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                            }
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    }
                }
                else
                {
                    await _next(context);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in WebSocketMiddleware: {ex.Message}");
                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                }
            }
        }
    }
}