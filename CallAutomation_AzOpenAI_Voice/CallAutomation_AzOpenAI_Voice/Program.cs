using Azure.Communication.CallAutomation;
using Azure.Core;
using Azure.Identity;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ---- Settings (ACS) ----
var acsConnectionString = builder.Configuration.GetValue<string>("AcsConnectionString");
ArgumentNullException.ThrowIfNullOrEmpty(acsConnectionString);

// Parse ACS endpoint + access key (for optional HMAC mode)
var (acsEndpoint, acsAccessKeyBase64) = AcsConnectionString.Parse(acsConnectionString);

// Choose auth mode: "AAD" (recommended) or "HMAC"
var authMode = "HMAC";

// Call Automation Client -> **use your ACS resource endpoint**
var credential = new DefaultAzureCredential();
var client = new CallAutomationClient(new Uri("https://uswc-01.sdf.pma.teams.microsoft.com"), new Uri(acsEndpoint), credential);

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
            logger.LogError("No MediaStreamingSubscription.StreamUrl was returned.");
            continue;
        }

        // Connect as a WS client (AAD or HMAC) — no /ws endpoint needed
        AccessToken accessToken = default;
        if (authMode.Equals("AAD", StringComparison.OrdinalIgnoreCase))
        {
            accessToken = await GetAccessTokenAsync();
            logger.LogInformation($"Access Token acquired: {accessToken.Token}");
        }

        _ = Task.Run(() => ConnectToAcsMediaAsync(
            streamUrl!, acsEndpoint, acsAccessKeyBase64, authMode, accessToken, logger, builder.Configuration));
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

// ---------------------------
// WS client: connect out to ACS streamUrl (AAD or HMAC)
// ---------------------------
static async Task ConnectToAcsMediaAsync(
    string streamUrl,
    string acsEndpoint,
    string acsAccessKeyBase64,
    string authMode,
    AccessToken aadToken,
    ILogger logger,
    IConfiguration configuration) // <-- add this parameter
{
    var uri = new Uri(streamUrl);
    var host = uri.Authority;

    using var ws = new ClientWebSocket();

    // Required headers (taken from your design doc)
    // X-Ms-Host must match <resource>.communication.azure.com
    ws.Options.SetRequestHeader("X-Ms-Host", new Uri(acsEndpoint).Authority);

    // RFC1123 UTC date and empty-body SHA256 (base64)
    // For empty string, this should be: 47DEQpj8HBSa+/TImW+5JCeuQeRkm5NMpJWZG3hSuFU=

    if (authMode.Equals("AAD", StringComparison.OrdinalIgnoreCase))
    {
        ws.Options.SetRequestHeader("Authorization", $"Bearer {aadToken.Token}");
    }
    else
    {
        var contentHash = ComputeSha256Base64(string.Empty);

        const string signedHeaders = "date;host;x-ms-content-sha256";
        var date = DateTime.UtcNow.ToString("R");
        var stringToSign = $"GET\n{uri.PathAndQuery}\n{date};{new Uri(acsEndpoint).Authority};{contentHash}";

        var signature = ComputeHmacSha256Base64(stringToSign, acsAccessKeyBase64);
        var authorizationHeader = $"HMAC-SHA256 SignedHeaders={signedHeaders}&Signature={signature}";

        // Diagnostics (avoid printing secrets)
        Console.WriteLine("✅ HMAC signature generated");
        Console.WriteLine($"🛣️ PathAndQuery: {uri.PathAndQuery}");
        Console.WriteLine($"🔒 ContentHash: {contentHash}");
        Console.WriteLine($"🧾 StringToSign (verbatim):\n{stringToSign}");
        Console.WriteLine($"🔑 Signature(Base64): {signature}");

        ws.Options.SetRequestHeader("date", date);
        ws.Options.SetRequestHeader("x-ms-content-sha256", contentHash);
        ws.Options.SetRequestHeader("Authorization", authorizationHeader);
    }

    try
    {
        logger.LogInformation($"Connecting WS to {streamUrl} ...");
        await ws.ConnectAsync(uri, CancellationToken.None);
        logger.LogInformation("WS connected.");

        // Start receiving messages in background
        var receiveTask = ReceiveMessagesWithAuth(ws);

        // Example: Send initial message if needed
        // await SendMessage(ws, "Hello from authenticated client");

        // Wait for the receive task to complete (when connection closes)
        await receiveTask;

        // Receive loop — PCM 24k mono frames will arrive as Binary messages
        var buffer = new byte[64 * 1024];
        while (ws.State == WebSocketState.Open)
        {
            var result = await ws.ReceiveAsync(buffer: new ArraySegment<byte>(buffer), cancellationToken: CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                logger.LogWarning($"WS closed by remote. Status: {result.CloseStatus}, {result.CloseStatusDescription}");
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing", CancellationToken.None);
                break;
            }
            //if (result.MessageType == WebSocketMessageType.Binary)
            //{
            //    // TODO: Handle audio frame(s) in buffer[0..result.Count] (PCM 24K mono)
            //    // e.g., enqueue to your media processing pipeline
            //    var bytes = Encoding.UTF8.GetBytes(result.ToString());

            //    await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);

            //}
            //else if (result.MessageType == WebSocketMessageType.Text)
            //{
            //    //// Optional: handle control messages
            //    //var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
            //    //logger.LogDebug($"WS text: {text}");
            //    // Example: Send a message after connection 
            //}

            var mediaService = new AcsMediaStreamingHandler(ws, configuration);

            // Set the single WebSocket connection
            await mediaService.ProcessWebSocketAsync();

        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, $"WS connection failed for {streamUrl}");
        // Consider retry/backoff here
    }

    static async Task ReceiveMessagesWithAuth(ClientWebSocket webSocket)
    {
        byte[] buffer = new byte[4096];
        StringBuilder messageBuilder = new StringBuilder();

        while (webSocket.State == WebSocketState.Open)
        {
            try
            {
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine("Server initiated close");
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure,
                        "Closing", CancellationToken.None);
                }
                else if (result.MessageType == WebSocketMessageType.Text)
                {
                    string chunk = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    messageBuilder.Append(chunk);

                    if (result.EndOfMessage)
                    {
                        string completeMessage = messageBuilder.ToString();
                        Console.WriteLine($"-----Authenticated message received: {completeMessage}");

                        // Process the complete message here
                        await ProcessReceivedMessage(webSocket, completeMessage);

                        messageBuilder.Clear();
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Binary)
                {
                    Console.WriteLine($"Binary message received: {result.Count} bytes");
                    // Handle binary messages if needed
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving authenticated message: {ex.Message}");
                break;
            }
        }
    }

    static async Task ProcessReceivedMessage(ClientWebSocket webSocket, string message)
    {
        try
        {
            // Example message processing logic
            if (message.Contains("ping"))
            {
                await SendMessage(webSocket, "pong");
                Console.WriteLine("Responded to ping with pong");
            }
            else if (message.Contains("metadata"))
            {
                Console.WriteLine("Received metadata message");
                // Handle metadata
            }
            else if (message.Contains("audioData"))
            {
                Console.WriteLine("Received audio data");
                // Handle audio data
            }
            else
            {
                Console.WriteLine("Received unknown message type " + message);
            }
            // Add more message processing logic as needed
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing message: {ex.Message}");
        }
    }

    static async Task SendMessage(ClientWebSocket webSocket, string message)
    {
        byte[] messageBytes = Encoding.UTF8.GetBytes(message);
        await webSocket.SendAsync(
            new ArraySegment<byte>(messageBytes),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None);
        Console.WriteLine($"Message sent: {message}");
    }

    static string ComputeHmacSha256Base64(string stringToSign, string base64Key)
    {
        byte[] keyBytes;
        try
        {
            keyBytes = Convert.FromBase64String(base64Key);
        }
        catch (FormatException fe)
        {
            throw new InvalidOperationException("API key must be Base64 encoded.", fe);
        }

        using var hmac = new HMACSHA256(keyBytes);
        var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
        return Convert.ToBase64String(signatureBytes);
    }

    static string ComputeSha256Base64(string data)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }
}

// ---------------------------
// Helpers (ACS connection string + HMAC tools)
// ---------------------------
static class AcsConnectionString
{
    public static (string endpoint, string accessKeyBase64) Parse(string connectionString)
    {
        // Example: "endpoint=https://<resource>.communication.azure.com/;accesskey=<base64>"
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        string endpoint = "";
        string accessKey = "";

        foreach (var p in parts)
        {
            var kv = p.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
            if (kv.Length == 2)
            {
                var key = kv[0].Trim().ToLowerInvariant();
                var val = kv[1].Trim();
                if (key == "endpoint") endpoint = val.TrimEnd('/');
                if (key == "accesskey") accessKey = val;
            }
        }

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(accessKey))
            throw new InvalidOperationException("Invalid ACS connection string.");

        return (endpoint, accessKey);
    }
}

static class HmacAuth
{
    public static string ComputeContentHashBase64(string content)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(content ?? string.Empty));
        return Convert.ToBase64String(bytes);
    }
}
