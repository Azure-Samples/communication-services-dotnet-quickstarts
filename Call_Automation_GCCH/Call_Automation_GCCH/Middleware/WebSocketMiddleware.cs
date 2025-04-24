using System.Net.WebSockets;
using Azure.Communication.CallAutomation;
using System.Text.Json;
using System.Text;
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
        await Helper.ProcessRequest(webSocket);
      }
      else
      {
        await _next(context);
      }
    }
  }
}
