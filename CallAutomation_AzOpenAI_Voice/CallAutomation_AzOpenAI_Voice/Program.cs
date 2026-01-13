using System.ComponentModel.DataAnnotations;
using Azure.Communication.CallAutomation;
using Azure.Communication.Media;
using Azure.Communication.Media.Tests;
using Azure.Identity;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using CallAutomationOpenAI;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

MediaClientOptions mediaClientOptions = new MediaClientOptions("dev", false, TimeSpan.FromSeconds(10));
TestCallRoomConnector? testCallConnector = null;
AcsMediaStreamingHandler? acsHandlerInstance = null;
var loggerFactory = app.Services.GetService(typeof(ILoggerFactory)) as ILoggerFactory;
var startupLogger = loggerFactory?.CreateLogger<Program>() ?? throw new Exception("LoggerFactory not found");

// Enable EventSource error reporting
System.Diagnostics.Tracing.EventSource.SetCurrentThreadActivityId(Guid.NewGuid());

    var listener = new MediaSdkEventListener();
    var _mediaClient = new MediaClient(builder.Configuration.GetValue<string>("MediaSDKConnectionString"), mediaClientOptions);

    string serviceOrigin = _mediaClient.ServiceOrigin;
    Console.WriteLine($"Service Origin URL: {serviceOrigin}");
    Console.WriteLine($"GetCurrentDirectory: {Directory.GetCurrentDirectory()}");
    Console.WriteLine($"BaseDirectory: {AppContext.BaseDirectory}");

    testCallConnector = new TestCallRoomConnector(_mediaClient, startupLogger, app.Services);

    var client = new CallAutomationClient(new Uri(builder.Configuration.GetValue<string>("AcsConnectionString")), new DefaultAzureCredential());


    var appBaseUrl = builder.Configuration.GetValue<string>("DevTunnelUri");

app.MapGet("/", () => "Hello ACS CallAutomation!");

app.MapGet("/testcall", async (ILogger<Program> logger) =>
{
    // Send test message to AcsMediaStreamingHandler
    await acsHandlerInstance.WriteInputStream("What is the weather today in redmond");
    return Results.Ok(new { message = "Sent test message to open AI" });
});

app.MapGet("/testclient", async (ILogger<Program> logger) =>
{
    if (testCallConnector == null)
        return Results.Problem("TestCallRoomConnector not initialized");
    if (acsHandlerInstance == null)
        acsHandlerInstance = new AcsMediaStreamingHandler(testCallConnector);

    // Send test message to media sdk
    using Stream audioStream = File.OpenRead($"whats_the_weather.wav");
    using (MemoryStream memoryStream = new MemoryStream())
    {
        audioStream.CopyTo(memoryStream);
        await acsHandlerInstance.SendMessageAsync(memoryStream.ToArray());
    }

    return Results.Ok(new { message = "Sent test message to room"});
});

app.UseWebSockets();


app.MapPost("/api/incomingCall", async (
    [FromBody] EventGridEvent[] eventGridEvents,
    ILogger<Program> logger) =>
{
    foreach (var eventGridEvent in eventGridEvents)
    {
        Console.WriteLine($"Incoming Call event received.");

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
        logger.LogInformation($"Callback Url: {callbackUri}");
        var websocketUri = appBaseUrl.Replace("https", "wss") + "/ws";
        logger.LogInformation($"WebSocket Url: {websocketUri}");

        var mediaStreamingOptions = new MediaStreamingOptions(MediaStreamingAudioChannel.Mixed)
        {
            TransportUri = new Uri(websocketUri),
            MediaStreamingContent = MediaStreamingContent.Audio,
            StartMediaStreaming = true,
            EnableBidirectional = true,
            AudioFormat = AudioFormat.Pcm24KMono
        };

        var options = new AnswerCallOptions(incomingCallContext, callbackUri)
        {
            MediaStreamingOptions = mediaStreamingOptions,
        };

        AnswerCallResult answerCallResult = await client.AnswerCallAsync(options);
        logger.LogInformation($"Answered call for connection id: {answerCallResult.CallConnection.CallConnectionId}");

        var connected = await testCallConnector.ConnectAsync(answerCallResult.CallConnectionProperties.CorrelationId);
        if (connected)
        {
            startupLogger.LogInformation($"TestCallRoomConnector started and connected to {answerCallResult.CallConnectionProperties.CorrelationId}");
            try
            {
                // Start AzureOpenAIService after connection
                // Provide a dummy AcsMediaStreamingHandler for initialization
                acsHandlerInstance = new AcsMediaStreamingHandler(testCallConnector);
                var openAIService = new AzureOpenAIService(acsHandlerInstance, builder.Configuration);
                acsHandlerInstance.aiServiceHandler = openAIService;
                testCallConnector.aiServiceHandler = openAIService;
                openAIService.StartConversation();
                startupLogger.LogInformation("AzureOpenAIService started after TestCallRoomConnector connection.");
            }
            catch (Exception ex)
            {
                startupLogger.LogError($"Failed to start AzureOpenAIService: {ex.Message}");
            }
        }
        else
        {
            startupLogger.LogError($"TestCallRoomConnector failed: {testCallConnector.LastError}");
        }
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
        logger.LogInformation($"Event received: {JsonConvert.SerializeObject(@event, Formatting.Indented)}");
    }

    return Results.Ok();
});


app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            try
            {
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception received {ex}");
            }
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

app.UseWebSockets();


app.Run();            