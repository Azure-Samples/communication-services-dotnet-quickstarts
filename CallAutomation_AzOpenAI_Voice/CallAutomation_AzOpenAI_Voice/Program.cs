using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);

//Get ACS Connection String from appsettings.json
var acsConnectionString = builder.Configuration.GetValue<string>("AcsConnectionString");
ArgumentNullException.ThrowIfNullOrEmpty(acsConnectionString);

//Call Automation Client
var pmaEndpoint = new Uri("https://uswc-01.sdf.pma.teams.microsoft.com:6448");
var client = new CallAutomationClient(pmaEndpoint, connectionString: acsConnectionString);

//Register and make CallAutomationClient accessible via dependency injection
builder.Services.AddSingleton(client);
builder.Services.AddSingleton(builder.Configuration);

var app = builder.Build();

var appBaseUrl = Environment.GetEnvironmentVariable("VS_TUNNEL_URL")?.TrimEnd('/');

if (string.IsNullOrEmpty(appBaseUrl))
{
    var websiteHostName = Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME");
    appBaseUrl = $"https://{websiteHostName}";
}

app.MapGet("/", () => "Hello ACS CallAutomation!");

app.MapPost("/api/incomingCall", async (
    [FromBody] EventGridEvent[] eventGridEvents,
    ILogger<Program> logger) =>
{
    foreach (var eventGridEvent in eventGridEvents)
    {
        logger.LogInformation($"Incoming Call event received.");

        // Handle system events
        if (eventGridEvent.TryGetSystemEventData(out object eventData))
        {
            // Handle the subscription validation event.
            if (eventData is SubscriptionValidationEventData subscriptionValidationEventData)
            {
                var responseData = new SubscriptionValidationResponse
                {
                    ValidationResponse = subscriptionValidationEventData.ValidationCode
                };
                return Results.Ok(responseData);
            }
        }

        var jsonObject = Helper.GetJsonObject(eventGridEvent.Data);
        var callerId = Helper.GetCallerId(jsonObject);
        var incomingCallContext = Helper.GetIncomingCallContext(jsonObject);
        var callbackUri = new Uri(new Uri(appBaseUrl), $"/api/callbacks/{Guid.NewGuid()}?callerId={callerId}");
        Console.WriteLine($"Callback Url: {callbackUri}");
        var websocketUri = appBaseUrl.Replace("https", "wss") + "ws";
        Console.WriteLine($"WebSocket Url: {callbackUri}");

        var mediaStreamingOptions = new MediaStreamingOptions(
                new Uri(websocketUri),
                MediaStreamingContent.Audio,
                MediaStreamingAudioChannel.Mixed,
                startMediaStreaming: true);
      
        var options = new AnswerCallOptions(incomingCallContext, callbackUri)
        {
            MediaStreamingOptions = mediaStreamingOptions,
        };

        AnswerCallResult answerCallResult = await client.AnswerCallAsync(options);
        Console.WriteLine($"Answered call for connection id: {answerCallResult.CallConnection.CallConnectionId}");
    }
    return Results.Ok();
});

// api to handle call back events
app.MapPost("/api/callbacks/{contextId}", async (
    [FromBody] CloudEvent[] cloudEvents,
    [FromRoute] string contextId,
    [Required] string callerId,
    ILogger<Program> logger) =>
{

    foreach (var cloudEvent in cloudEvents)
    {
        CallAutomationEventBase @event = CallAutomationEventParser.Parse(cloudEvent);
        logger.LogInformation($"Event received: {JsonConvert.SerializeObject(cloudEvent, Formatting.Indented)}");
    }

    return Results.Ok();
});

app.UseWebSockets();

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            var mediaService = new AcsMediaStreamingHandler(webSocket, builder.Configuration);            

            // Set the single WebSocket connection
            await mediaService.ProcessWebSocketAsync();
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
    else
    {
        await next(context);
    }
});

app.Run();