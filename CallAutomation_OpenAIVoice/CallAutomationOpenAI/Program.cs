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
Uri pmaEndpoint = new UriBuilder("https://uswc-01.sdf.pma.teams.microsoft.com:6448").Uri;
var client = new CallAutomationClient(pmaEndpoint, connectionString: acsConnectionString);


var key = builder.Configuration.GetValue<string>("AzureOpenAIServiceKey");
ArgumentNullException.ThrowIfNullOrEmpty(key);

var endpoint = builder.Configuration.GetValue<string>("AzureOpenAIServiceEndpoint");
ArgumentNullException.ThrowIfNullOrEmpty(endpoint);

//Register and make CallAutomationClient accessible via dependency injection
builder.Services.AddSingleton(client);
builder.Services.AddSingleton<WebSocketHandlerService>();

var app = builder.Build();

var devTunnelUri = builder.Configuration.GetValue<string>("DevTunnelUri");
ArgumentNullException.ThrowIfNullOrEmpty(devTunnelUri);

var transportUrl = devTunnelUri.Replace("https", "wss") + "ws";

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
        var callbackUri = new Uri(new Uri(devTunnelUri), $"/api/callbacks/{Guid.NewGuid()}?callerId={callerId}");
        Console.WriteLine($"Callback Url: {callbackUri}");

        MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(new Uri(transportUrl),
                MediaStreamingContent.Audio, MediaStreamingAudioChannel.Mixed, startMediaStreaming: true);
      
        var options = new AnswerCallOptions(incomingCallContext, callbackUri)
        {
            MediaStreamingOptions = mediaStreamingOptions,
        };

        AnswerCallResult answerCallResult = await client.AnswerCallAsync(options);
        Console.WriteLine($"Answered call for connection id: {answerCallResult.CallConnection.CallConnectionId}");

        //Use EventProcessor to process CallConnected event
        var answer_result = await answerCallResult.WaitForEventProcessorAsync();
        if (answer_result.IsSuccess)
        {
            Console.WriteLine($"Call connected event received for CorrelationId id: {answer_result.SuccessResult.CorrelationId}");
        }
    }
    return Results.Ok();
});

// api to handle call back events
app.MapPost("/api/callbacks/{contextId}", async (
    [FromBody] CloudEvent[] cloudEvents,
    [FromRoute] string contextId,
    [Required] string callerId,
    CallAutomationClient callAutomationClient,
    ILogger<Program> logger) =>
{

    foreach (var cloudEvent in cloudEvents)
    {
        CallAutomationEventBase @event = CallAutomationEventParser.Parse(cloudEvent);
        logger.LogInformation($"Event received: {JsonConvert.SerializeObject(cloudEvent)}");

        var callConnection = client.GetCallConnection(@event.CallConnectionId);

        if (@event is MediaStreamingStopped)
        {
            logger.LogInformation("Received media streaming event: {type}", @event.GetType());
        }

        if (@event is MediaStreamingFailed)
        {
            logger.LogInformation($"Received media streaming event: {@event.GetType()}, " +
                    $"SubCode: {@event?.ResultInformation?.SubCode}, Message: {@event?.ResultInformation?.Message}");
        }

        if (@event is MediaStreamingStarted)
        {
            Console.WriteLine($"MediaStreaming started event received for connection id: {@event.CallConnectionId}");
        }

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
            var mediaService = context.RequestServices.GetRequiredService<WebSocketHandlerService>();

            var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            mediaService.SetConnection(webSocket);

            // Set the single WebSocket connection
            var openAiModelName = builder.Configuration.GetValue<string>("AzureOpenAIDeploymentModelName");
            var systemPrompt = builder.Configuration.GetValue<string>("SystemPrompt");
            await mediaService.ProcessWebSocketAsync(endpoint, key, openAiModelName, systemPrompt);
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