using System.ComponentModel.DataAnnotations;
using System.IO;
using Azure.Communication.CallAutomation;
using Azure.Communication.Media;
using Azure.Identity;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using CallAutomationOpenAI;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

MediaClientOptions mediaClientOptions = new MediaClientOptions("msit", false, null);
var credential = new DefaultAzureCredential();
var uri = new Uri(builder.Configuration.GetValue<string>("AcsConnectionString"));
// TODO: Replace with your ACS endpoint and access key
var _mediaClient = new MediaClient("endpoint=https://<YOUR_ACS_RESOURCE>.unitedstates.communication.azure.com/;accesskey=<YOUR_ACCESS_KEY>", mediaClientOptions);
TestCallRoomConnector? testCallConnector = null;

var client = new CallAutomationClient(uri, credential);




// Initialize and start TestCallRoomConnector at startup
var loggerFactory = app.Services.GetService(typeof(ILoggerFactory)) as ILoggerFactory;
var startupLogger = loggerFactory?.CreateLogger<Program>() ?? throw new Exception("LoggerFactory not found");
testCallConnector = new TestCallRoomConnector(_mediaClient, startupLogger, app.Services);
AcsMediaStreamingHandler? acsHandlerInstance = null;

var appBaseUrl = builder.Configuration.GetValue<string>("DevTunnelUri");

_ = Task.Run(async () =>
{

});

app.MapGet("/", () => "Hello ACS CallAutomation!");

app.MapGet("/testcall", async (ILogger<Program> logger) =>
{
    if (testCallConnector == null)
        return Results.Problem("TestCallRoomConnector not initialized");
    if (!testCallConnector.IsConnected)
        return Results.Problem($"Not connected: {testCallConnector.LastError}");
    if (acsHandlerInstance == null)
        return Results.Problem("AcsMediaStreamingHandler not initialized");

    // Send test message to AcsMediaStreamingHandler
    await acsHandlerInstance.WriteInputStream("What is the weather today in redmond");

    return Results.Ok(new { message = "Sent test message to room", room = testCallConnector._sessionId });
});

app.MapGet("/testclient", async (ILogger<Program> logger) =>
{
    if (testCallConnector == null)
        return Results.Problem("TestCallRoomConnector not initialized");
    if (!testCallConnector.IsConnected)
        return Results.Problem($"Not connected: {testCallConnector.LastError}");
    if (acsHandlerInstance == null)
        return Results.Problem("AcsMediaStreamingHandler not initialized");

    // Send test message to AcsMediaStreamingHandler
    using Stream audioStream = File.OpenRead($"whats_the_weather.wav");
    using (MemoryStream memoryStream = new MemoryStream())
    {
        audioStream.CopyTo(memoryStream);
        await acsHandlerInstance.SendMessageAsync(memoryStream.ToArray());
    }

    return Results.Ok(new { message = "Sent test message to room", room = testCallConnector._sessionId });
});


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

        testCallConnector._sessionId = answerCallResult.CallConnectionProperties.CorrelationId;

        var connected = await testCallConnector.ConnectAsync();
        if (connected)
        {
            startupLogger.LogInformation($"TestCallRoomConnector started and connected to {testCallConnector._sessionId}");
            try
            {
                // Start AzureOpenAIService after connection
                // Provide a dummy AcsMediaStreamingHandler for initialization
                acsHandlerInstance = new AcsMediaStreamingHandler(testCallConnector);
                var openAIService = new AzureOpenAIService(acsHandlerInstance, builder.Configuration);
                acsHandlerInstance.aiServiceHandler = openAIService;
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

app.UseWebSockets();


app.Run();