using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using JsonException = Newtonsoft.Json.JsonException;
using System.Net.Http.Headers;
using CallAutomation_MCS_Sample;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Get ACS Connection String from appsettings.json
var acsConnectionString = builder.Configuration.GetValue<string>("AcsConnectionString");
ArgumentNullException.ThrowIfNullOrEmpty(acsConnectionString);

//Call Automation Client
var client = new CallAutomationClient(connectionString: acsConnectionString);

//Get the Cognitive Services endpoint from appsettings.json
var cognitiveServicesEndpoint = builder.Configuration.GetValue<string>("CognitiveServiceEndpoint");
ArgumentNullException.ThrowIfNullOrEmpty(cognitiveServicesEndpoint);

//Get Agent Phone number from appsettings.json
var agentPhonenumber = builder.Configuration.GetValue<string>("AgentPhoneNumber");
ArgumentNullException.ThrowIfNullOrEmpty(agentPhonenumber);

// Get Direct Line Secret from appsettings.json
var directLineSecret = builder.Configuration.GetValue<string>("DirectLineSecret");
ArgumentNullException.ThrowIfNullOrEmpty(directLineSecret);

// Create an HTTP client to communicate with the Direct Line service
HttpClient httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", directLineSecret);

var baseUri = Environment.GetEnvironmentVariable("VS_TUNNEL_URL")?.TrimEnd('/');
var baseWssUri = baseUri.Split("https://")[1];

if (string.IsNullOrEmpty(baseUri))
{
    baseUri = builder.Configuration["BaseUri"];
}

ConcurrentDictionary<string, CallContext> CallStore = new();

var app = builder.Build();

app.MapGet("/", () => "Hello ACS CallAutomation - MCS Sample!");

app.MapPost("/api/incomingCall", async (
    [FromBody] EventGridEvent[] eventGridEvents,
    ILogger<Program> logger) =>
{
    foreach (var eventGridEvent in eventGridEvents)
    {
        logger.LogInformation($"Incoming Call event received : {JsonConvert.SerializeObject(eventGridEvent)}");
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
        var jsonObject = JsonNode.Parse(eventGridEvent.Data).AsObject();
        var incomingCallContext = (string)jsonObject["incomingCallContext"];
        var callbackUri = new Uri(baseUri + $"/api/calls/{Guid.NewGuid()}");
        
        var answerCallOptions = new AnswerCallOptions(incomingCallContext, callbackUri)
        {
            CallIntelligenceOptions = new CallIntelligenceOptions()
            {
                CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint)
            },
            TranscriptionOptions = new TranscriptionOptions(new Uri($"wss://{baseWssUri}/ws"), "en-US", true, TranscriptionTransport.Websocket)
            {
                EnableIntermediateResults = true
            }
        };

        try
        {
            AnswerCallResult answerCallResult = await client.AnswerCallAsync(answerCallOptions);

            var correlationId = answerCallResult?.CallConnectionProperties.CorrelationId;
            logger.LogInformation($"Correlation Id: {correlationId}");

            if (correlationId != null)
            {
                CallStore[correlationId] = new CallContext()
                {
                    CorrelationId = correlationId
                };
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Answer call exeception : {ex.StackTrace}");
        }
    }
    return Results.Ok();
});

app.MapPost("/api/calls/{contextId}", async (
    [FromBody] CloudEvent[] cloudEvents,
    [FromRoute] string contextId,
    ILogger<Program> logger) =>
{
    foreach (var cloudEvent in cloudEvents)
    {
        CallAutomationEventBase @event = CallAutomationEventParser.Parse(cloudEvent);
        logger.LogInformation($"Event received: {JsonConvert.SerializeObject(@event)}");

        var callConnection = client.GetCallConnection(@event.CallConnectionId);
        var callMedia = callConnection?.GetCallMedia();
        var correlationId = @event.CorrelationId;

        if (callConnection == null || callMedia == null)
        {
            return Results.BadRequest($"Call objects failed to get for connection id {@event.CallConnectionId}.");
        }

        if (@event is CallConnected callConnected)
        {
            var conversation = await StartConversationAsync();
            var conversationId = conversation.ConversationId;
            if (CallStore.ContainsKey(correlationId))
            {
                CallStore[correlationId].ConversationId = conversationId;
            }

            Console.WriteLine("Conversation started. Type 'exit' to stop.");
            Console.WriteLine($"Conversation ID: {conversationId}");

            // Start listening for bot responses asynchronously
            var cts = new CancellationTokenSource();
            Task.Run(() => ListenToBotWebSocketAsync(conversation.StreamUrl, callConnection, cts.Token));

            await SendMessageAsync(conversationId, "Hi");
        }

        if (@event is PlayFailed)
        {
            logger.LogInformation("Play Failed");
        }

        if (@event is PlayCompleted)
        {
            logger.LogInformation("Play Completed");            
        }

        if (@event is TranscriptionStarted transcriptionStarted)
        {
            app.Logger.LogInformation($"Transcription started: {transcriptionStarted.OperationContext}");
        }

        if (@event is TranscriptionStopped transcriptionStopped)
        {
            logger.LogInformation($"Transcription stopped: {transcriptionStopped.OperationContext}");
        }
        
        if(@event is CallDisconnected callDisconnected)
        {
            logger.LogInformation("Call Disconnected");
            _ = CallStore.TryRemove(@event.CorrelationId, out CallContext context);

        }
    }
    return Results.Ok();
}).Produces(StatusCodes.Status200OK);

// setup web socket for stream in
app.UseWebSockets();
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            // Extract correlation ID and call connection ID
            var correlationId = context.Request.Headers["x-ms-call-correlation-id"].FirstOrDefault();
            var callConnectionId = context.Request.Headers["x-ms-call-connection-id"].FirstOrDefault();
            var callMedia = callConnectionId != null ? client.GetCallConnection(callConnectionId)?.GetCallMedia() : null;
            // Log the extracted values
            Console.WriteLine($"****************************** Correlation ID: {correlationId}");
            Console.WriteLine($"****************************** Call Connection ID: {callConnectionId}");
            var conversationId = CallStore[correlationId].ConversationId;

            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            try
            {
                string partialData = "";

                while (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseSent)
                {
                    byte[] receiveBuffer = new byte[4096];
                    var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(1200)).Token;
                    WebSocketReceiveResult receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), cancellationToken);

                    if (receiveResult.MessageType != WebSocketMessageType.Close)
                    {
                        string data = Encoding.UTF8.GetString(receiveBuffer).TrimEnd('\0');

                        try
                        {
                            if (receiveResult.EndOfMessage)
                            {
                                data = partialData + data;
                                partialData = "";

                                if (data != null)
                                {
                                    Console.WriteLine($"\n [{DateTime.UtcNow}] {data}");

                                    if (data.Contains("Intermediate"))
                                    {
                                        Console.WriteLine($"\n Canceling prompt");
                                        if (callMedia != null)
                                            await callMedia.CancelAllMediaOperationsAsync();
                                    }
                                    else
                                    {
                                        var streamingData = StreamingData.Parse(data);
                                        if (streamingData is TranscriptionMetadata transcriptionMetadata)
                                        {
                                            callMedia = client.GetCallConnection(transcriptionMetadata.CallConnectionId)?.GetCallMedia();
                                        }
                                        if (streamingData is TranscriptionData transcriptiopnData)
                                        {
                                            Console.WriteLine($"\n [{DateTime.UtcNow}] {transcriptiopnData.Text}");

                                            if (transcriptiopnData.ResultState == TranscriptionResultState.Final)
                                            {
                                                if (conversationId == null) conversationId = CallStore[correlationId].ConversationId;

                                                if (!string.IsNullOrEmpty(conversationId))
                                                {
                                                    await SendMessageAsync(conversationId, transcriptiopnData.Text);
                                                }
                                                else
                                                {
                                                    Console.WriteLine($"\n Conversation Id is null");

                                                }
                                            }
                                        }

                                    }
                                }

                                }
                            else
                            {
                                partialData = partialData + data;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Exception 1 -> {ex}");
                        }
                    }
                }
                //transcriptFileStream?.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception 2 -> {ex}");
            }
            finally
            {
                
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


async Task<Conversation> StartConversationAsync()
{
    var response = await httpClient.PostAsync("https://directline.botframework.com/v3/directline/conversations", null);
    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadAsStringAsync();
    return JsonConvert.DeserializeObject<Conversation>(content);
}

async Task ListenToBotWebSocketAsync(string streamUrl, CallConnection callConnection, CancellationToken cancellationToken)
{
    if (string.IsNullOrEmpty(streamUrl))
    {
        Console.WriteLine("WebSocket streaming is not enabled for this MCS bot.");
        return;
    }

    using (var webSocket = new ClientWebSocket())
    {
        try
        {
            await webSocket.ConnectAsync(new Uri(streamUrl), cancellationToken);

            var buffer = new byte[4096]; // Set the buffer size to 4096 bytes
            var messageBuilder = new StringBuilder();

            while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                messageBuilder.Clear(); // Reset buffer for each new message
                WebSocketReceiveResult result;
                do
                {
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                    messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

                } while (!result.EndOfMessage); // Continue until we've received the full message

                string rawMessage = messageBuilder.ToString();
                var botActivity = ExtractLatestBotActivity(rawMessage);

                if (botActivity.Type == "message")
                {
                    Console.WriteLine($"\nPlaying Bot Response: {botActivity.Text}\n");
                    await PlayToAllAsync(callConnection.GetCallMedia(), botActivity.Text);
                }
                else if (botActivity.Type == "endOfConversation")
                {
                    Console.WriteLine($"\nEnd of Conversation\n");
                    await callConnection.HangUpAsync(true);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WebSocket error: {ex.Message}");
        }
        finally
        {
            if (webSocket.State == WebSocketState.Open)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken);
            }
        }
    }
}

async Task SendMessageAsync(string conversationId, string message)
{
    var messagePayload = new
    {
        type = "message",
        from = new { id = "user1" },
        text = message
    };
    string messageJson = JsonConvert.SerializeObject(messagePayload);
    StringContent content = new StringContent(messageJson, Encoding.UTF8, "application/json");

    var response = await httpClient.PostAsync($"https://directline.botframework.com/v3/directline/conversations/{conversationId}/activities", content);
    response.EnsureSuccessStatusCode();
}


static BotActivity ExtractLatestBotActivity(string rawMessage)
{
    try
    {
        using var doc = JsonDocument.Parse(rawMessage);

        Console.WriteLine($"Raw Message: {rawMessage}");

        if (doc.RootElement.TryGetProperty("activities", out var activities) && activities.ValueKind == JsonValueKind.Array)
        {
            // Iterate in reverse order to get the latest message
            for (int i = activities.GetArrayLength() - 1; i >= 0; i--)
            {
                var activity = activities[i];

                if (activity.TryGetProperty("type", out var type))
                {
                    if (type.GetString() == "message")
                    {
                        if (activity.TryGetProperty("from", out var from) &&
                            from.TryGetProperty("id", out var fromId) &&
                            fromId.GetString() != "user1") // Ensure message is from bot
                        {
                            if (activity.TryGetProperty("speak", out var speak))
                            {
                                //Console.WriteLine($"Voice content: {speak}");
                                return new BotActivity()
                                {
                                    Type = "message",
                                    Text = RemoveReferences(speak.GetString())
                                };
                            }

                            if (activity.TryGetProperty("text", out var text))
                            {
                                return new BotActivity()
                                {
                                    Type = "message",
                                    Text = RemoveReferences(text.GetString())
                                };
                            }
                        }
                    }
                    else if(type.GetString() == "endOfConversation")
                    {
                        return new BotActivity()
                        {
                            Type = "endOfConversation"
                        };
                    }

                }
            }
        }
    }
    catch (JsonException)
    {
        Console.WriteLine("Warning: Received unexpected JSON format.");
    }
    return new BotActivity()
        {
            Type = "Error",
            Text = "Sorry, Something went wrong"
        };
}

static string RemoveReferences(string input)
{
    // Remove inline references like [1], [2], etc.
    string withoutInlineRefs = Regex.Replace(input, @"\[\d+\]", "");

    // Remove reference list at the end (lines starting with [number]:)
    string withoutRefList = Regex.Replace(withoutInlineRefs, @"\n\[\d+\]:.*(\n|$)", "");

    return withoutRefList.Trim();
}

async Task PlayToAllAsync(CallMedia callConnectionMedia, string message)
 {
    var ssmlPlaySource = new SsmlSource($"<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"><voice name=\"en-US-NancyNeural\">{message}</voice></speak>");

    var playOptions = new PlayToAllOptions(ssmlPlaySource)
    {
        OperationContext = "Testing"
    };

    await callConnectionMedia.PlayToAllAsync(playOptions);
 }


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.Run();
