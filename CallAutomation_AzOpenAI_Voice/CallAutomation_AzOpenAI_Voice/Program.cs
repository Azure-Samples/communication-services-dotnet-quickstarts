extern alias AzureIdentityAlias;
using Azure.Communication.CallAutomation;
using Azure.Core;
using AzureIdentityAlias::Azure.Identity;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);

// ---- Settings (ACS) ----
var acsConnectionString = builder.Configuration.GetValue<string>("AcsConnectionString");
ArgumentNullException.ThrowIfNullOrEmpty(acsConnectionString);

// Parse ACS endpoint + access key (for optional HMAC mode)
var (acsEndpoint, acsAccessKeyBase64) = AcsConnectionString.Parse(acsConnectionString);

// Choose auth mode: "AAD" (recommended) or "HMAC"
var authMode = "HMAC";

// Call Automation Client -> **use your ACS resource endpoint**
var credential = new AzureIdentityAlias::Azure.Identity.DefaultAzureCredential();
var client = new CallAutomationClient(pmaEndpoint: new Uri("https://uswe3-03.sdf.pma.teams.microsoft.com"), acsConnectionString);

var app = builder.Build();
var appBaseUrl = Environment.GetEnvironmentVariable("VS_TUNNEL_URL")?.TrimEnd('/');
if (string.IsNullOrEmpty(appBaseUrl))
{
    appBaseUrl = builder.Configuration.GetValue<string>("DevTunnelUri")?.TrimEnd('/');
}
Console.WriteLine($"appBaseUrl : {appBaseUrl}");

app.MapGet("/", () => "Hello ACS CallAutomation!");

// Acquire AAD token for communication.azure.com
async Task<AccessToken> GetAccessTokenAsync()
{
    var tokenRequestContext = new TokenRequestContext(new[] { "https://communication.azure.com/.default" });
    return await credential.GetTokenAsync(tokenRequestContext, CancellationToken.None);
}

// ---------------------------
// Incoming call -> answer with ACS-managed WS streaming
// ---------------------------
app.MapPost("/api/incomingCall", async (
    [FromBody] EventGridEvent[] eventGridEvents,
    ILogger<Program> logger) =>
{
    foreach (var eventGridEvent in eventGridEvents)
    {
        Console.WriteLine($"Incoming Call event received.");

        // Subscription validation handshake
        if (eventGridEvent.TryGetSystemEventData(out object eventData))
        {
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

        var mediaStreamingOptions = new MediaStreamingOptions(
            MediaStreamingContent.Audio,
            MediaStreamingAudioChannel.Mixed,
            MediaStreamingTransport.Websocket,
            true)
        {
            EnableBidirectional = true,
            AudioFormat = AudioFormat.Pcm24KMono
        };

        var options = new AnswerCallOptions(incomingCallContext, callbackUri)
        {
            MediaStreamingOptions = mediaStreamingOptions,
        };

        AnswerCallResult answerCallResult = await client.AnswerCallAsync(options);
        logger.LogInformation($"Answered call for connection id: {answerCallResult.CallConnection.CallConnectionId}");

        // ACS provides a websocket subscription URL that YOUR APP should connect to as a client
        var streamUrl = answerCallResult.CallConnectionProperties.MediaStreamingSubscription?.StreamUrl;
        logger.LogInformation($"Media Streaming subscription StreamUrl: {streamUrl}");

        if (string.IsNullOrWhiteSpace(streamUrl))
        {
            Thread.Sleep(4000);

            var props = client.GetCallConnection(answerCallResult.CallConnection.CallConnectionId);
            streamUrl = props.GetCallConnectionProperties().Value?.MediaStreamingSubscription?.StreamUrl;
            if (string.IsNullOrWhiteSpace(streamUrl))
            {
                logger.LogError("No MediaStreamingSubscription.StreamUrl was returned.");
            }
        }

         _ = Task.Run(() => AcsMediaStreamProcessor.ConnectAndProcessMediaStreamAsync(client, streamUrl, logger, builder.Configuration));


        //// // Connect as a WS client(AAD or HMAC) — no / ws endpoint needed
        //AccessToken accessToken = default;
        //if (authMode.Equals("AAD", StringComparison.OrdinalIgnoreCase))
        //{
        //    accessToken = await GetAccessTokenAsync();
        //    logger.LogInformation($"Access Token acquired: {accessToken.Token}");
        //}

        //_ = Task.Run(() => AcsMediaConnector.ConnectToAcsMediaAsync(
        //    streamUrl!, acsEndpoint, acsAccessKeyBase64, authMode, accessToken, logger, builder.Configuration));
    }

    return Results.Ok();
});

// ---------------------------
// Callbacks
// ---------------------------
app.MapPost("/api/callbacks/{contextId}", async (
    [FromBody] CloudEvent[] cloudEvents,
    [FromRoute] string contextId,
    [Required] string callerId,
    ILogger<Program> logger) =>
{
    foreach (var cloudEvent in cloudEvents)
    {
        CallAutomationEventBase parsedEvent = CallAutomationEventParser.Parse(cloudEvent);
        logger.LogInformation(
                    "Received call event: {type}, callConnectionID: {connId}, serverCallId: {serverId}",
                    parsedEvent.GetType(),
                    parsedEvent.CallConnectionId,
                    parsedEvent.ServerCallId);
        logger.LogInformation($"Event received: {JsonConvert.SerializeObject(parsedEvent, Formatting.Indented)}");
    }

    return Results.Ok();
});

app.Run();
