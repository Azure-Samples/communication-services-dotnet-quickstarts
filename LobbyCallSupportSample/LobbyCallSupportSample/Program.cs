using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Core;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Configuration helper
string GetConfig(string key) => builder.Configuration[key] ?? throw new ArgumentNullException(paramName: key);

string acsConnectionString = GetConfig(key: "acsConnectionString"),
       cognitiveServiceEndpoint = GetConfig(key: "cognitiveServiceEndpoint"),
       callbackUriHost = GetConfig(key: "callbackUriHost"),
       acsLobbyCallReceiver = GetConfig(key: "acsLobbyCallReceiver"),
       acsTargetCallReceiver = GetConfig(key: "acsTargetCallReceiver");

const string confirmationMessage = "A user is waiting in lobby, do you want to add the lobby user to your call?";
const string lobbyMessage = "You are currently in a lobby call, we will notify the admin that you are waiting.";

string acsUserId = string.Empty,
       targetCallConnectionId = string.Empty,
       lobbyCallConnectionId = string.Empty,
       lobbyUserId = string.Empty;

WebSocket? webSocket = null;
CallAutomationClient callAutomationClient = new(connectionString: acsConnectionString);

// Event Handler
app.MapPost("/api/LobbyCallSupportEventHandler", async (EventGridEvent[] events, ILogger<Program> logger) =>
{
    logger.LogInformation("~~~ /api/LobbyCallSupportEventHandler ~~~");
    try
    {
        foreach (var eventGridEvent in events)
        {
            if (!eventGridEvent.TryGetSystemEventData(out var eventData)) continue;
            switch (eventData)
            {
                case SubscriptionValidationEventData validationEvent:
                    return Results.Ok(value: new SubscriptionValidationResponse { ValidationResponse = validationEvent.ValidationCode });

                case AcsIncomingCallEventData incomingCallEvent:
                    logger.LogInformation("Event: {Type}", eventGridEvent.EventType);
                    acsUserId = incomingCallEvent.FromCommunicationIdentifier.RawId;
                    var toIdentifier = incomingCallEvent.ToCommunicationIdentifier.RawId;
                    if (toIdentifier.Contains(acsLobbyCallReceiver) || toIdentifier.Contains(acsTargetCallReceiver))
                    {
                        var callbackUri = new Uri(baseUri: new Uri(callbackUriHost), relativeUri: "/api/callbacks");
                        var answerCallOptions = new AnswerCallOptions(
                            incomingCallContext: incomingCallEvent.IncomingCallContext,
                            callbackUri: callbackUri)
                        {
                            OperationContext = !toIdentifier.Contains(acsTargetCallReceiver) ? "LobbyCall" : "OtherCall",
                            CallIntelligenceOptions = new CallIntelligenceOptions
                            {
                                CognitiveServicesEndpoint = new Uri(uriString: cognitiveServiceEndpoint)
                            }
                        };
                        AnswerCallResult answerResult = await callAutomationClient.AnswerCallAsync(options: answerCallOptions);
                        if (toIdentifier.Contains(acsTargetCallReceiver))
                        {
                            targetCallConnectionId = answerResult.CallConnection.CallConnectionId;
                            logger.LogInformation(
                                "Target Call Answered. From: {From}, To: {To}, ConnId: {Conn}, CorrId: {Corr}",
                                acsUserId, toIdentifier, targetCallConnectionId, incomingCallEvent.CorrelationId);
                        }
                        else
                        {
                            lobbyCallConnectionId = answerResult.CallConnection.CallConnectionId;
                            logger.LogInformation(
                                "Lobby Call Answered. From: {From}, To: {To}, ConnId: {Conn}, CorrId: {Corr}",
                                acsUserId, toIdentifier, lobbyCallConnectionId, incomingCallEvent.CorrelationId);
                        }
                    }
                    break;
            }
        }
        return Results.Ok();
    }
    catch (Exception ex)
    {
        logger.LogError(exception: ex, message: "Error");
        throw;
    }
});

// Callback Handler
app.MapPost("/api/callbacks", async (CloudEvent[] events, ILogger<Program> logger) =>
{
    try
    {
        foreach (var cloudEvent in events)
        {
            var callEvent = CallAutomationEventParser.Parse(cloudEvent);
            var callConnection = callAutomationClient.GetCallConnection(callConnectionId: callEvent.CallConnectionId);
            logger.LogInformation("~~~ /api/callbacks ~~~\n Event: {callEvent}", callEvent);
            switch (callEvent)
            {
                case CallConnected callConnected when (callConnected.OperationContext ?? "") == "LobbyCall":
                    logger.LogInformation("\nCallConnected: {ConnId}", callConnected.CallConnectionId);
                    CallConnectionProperties callProperties = callConnection.GetCallConnectionProperties();
                    lobbyUserId = callProperties.Source.RawId;
                    lobbyCallConnectionId = callProperties.CallConnectionId;
                    logger.LogInformation("Lobby Caller: {Caller}, Conn: {Conn}", lobbyUserId, lobbyCallConnectionId);
                    var callMedia = callAutomationClient.GetCallConnection(callConnected.CallConnectionId).GetCallMedia();
                    var textSource = new TextSource(text: lobbyMessage) { VoiceName = "en-US-NancyNeural" };
                    await callMedia.PlayAsync(
                        options: new PlayOptions(
                            playSource: textSource,
                            playTo: [new CommunicationUserIdentifier(id: acsUserId)])
                        {
                            OperationContext = "playToContext"
                        });
                    break;

                case PlayCompleted:
                    logger.LogInformation("PlayCompleted event");
                    if (webSocket is null || webSocket.State != WebSocketState.Open)
                    {
                        logger.LogError("WebSocket unavailable");
                        return Results.NotFound(value: "Message not sent");
                    }
                    await webSocket.SendAsync(
                        buffer: Encoding.UTF8.GetBytes(confirmationMessage),
                        messageType: WebSocketMessageType.Text,
                        endOfMessage: true,
                        cancellationToken: CancellationToken.None);
                    logger.LogInformation("Target notified: {Msg}", confirmationMessage);
                    return Results.Ok(value: $"Target notified: {confirmationMessage}");

                case MoveParticipantSucceeded moveParticipantSucceeded:
                    logger.LogInformation("MoveParticipantSucceeded: {ConnId}", moveParticipantSucceeded.CallConnectionId);
                    var targetConnection = callAutomationClient.GetCallConnection(callConnectionId: moveParticipantSucceeded.CallConnectionId);
                    var participants = await targetConnection.GetParticipantsAsync();
                    LogParticipants(participants: participants.Value, logger: logger);
                    break;

                case CallDisconnected callDisconnected:
                    logger.LogInformation("CallDisconnected: {ConnId}", callDisconnected.CallConnectionId);
                    break;
            }
        }
        return Results.Ok();
    }
    catch (Exception ex)
    {
        logger.LogError(exception: ex, message: "Error");
        throw;
    }
}).Produces(statusCode: 200);

// Get Participants
app.MapGet("/GetParticipants/{connId}", async (string connId, ILogger<Program> logger) =>
{
    logger.LogInformation("~~~ /GetParticipants/{ConnId} ~~~", connId);
    try
    {
        var callConnection = callAutomationClient.GetCallConnection(callConnectionId: connId);
        var participants = await callConnection.GetParticipantsAsync();
        if (!participants.Value.Any())
            return Results.NotFound(value: new { Message = "No participants found.", CallConnectionId = connId });
        LogParticipants(participants: participants.Value, logger: logger);
        return Results.Ok();
    }
    catch (Exception ex)
    {
        logger.LogError("Error getting participants: {Msg}", ex.Message);
        throw;
    }
}).WithTags("Lobby Call Support APIs");

// WebSocket
app.UseWebSockets();
app.Map("/ws", async context =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        return;
    }
    webSocket = await context.WebSockets.AcceptWebSocketAsync();
    var buffer = new byte[4096];
    while (webSocket.State == WebSocketState.Open)
    {
        try
        {
            var receiveResult = await webSocket.ReceiveAsync(
                buffer: new ArraySegment<byte>(buffer),
                cancellationToken: CancellationToken.None);
            var clientMessage = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
            logger.LogInformation("Client response: {Msg}", clientMessage);

            if (receiveResult.MessageType == WebSocketMessageType.Close)
            {
                await webSocket.CloseAsync(
                    closeStatus: WebSocketCloseStatus.NormalClosure,
                    statusDescription: "Closing",
                    cancellationToken: CancellationToken.None);
            }
            else if (clientMessage.Equals("yes", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation("Move Participant...");
                try
                {
                    var targetConnection = callAutomationClient.GetCallConnection(callConnectionId: targetCallConnectionId);
                    CommunicationIdentifier participant = lobbyUserId.StartsWith("+")
                        ? new PhoneNumberIdentifier(phoneNumber: lobbyUserId)
                        : new CommunicationUserIdentifier(id: lobbyUserId);
                    var moveResult = await targetConnection.MoveParticipantsAsync(
                        options: new MoveParticipantsOptions(
                            targetParticipants: [participant],
                            fromCall: lobbyCallConnectionId));
                    var rawResponse = moveResult.GetRawResponse();
                    if (rawResponse.Status is >= 200 and <= 299)
                        logger.LogInformation("Move Participant operation is initiated.");
                    else
                        throw new Exception(message: $"Move failed: {rawResponse.Status}");
                }
                catch (Exception ex)
                {
                    logger.LogError("Move error: {Msg}", ex.Message);
                    throw;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError("WebSocket error: {Msg}", ex.Message);
        }
    }
});

app.Run();

// Helper: Log participants
static void LogParticipants(IEnumerable<CallParticipant> participants, ILogger logger)
{
    var participantInfo = participants.Select(participant => participant.Identifier switch
    {
        PhoneNumberIdentifier phone => $"Phone - RawId: {phone.RawId}, Phone: {phone.PhoneNumber}",
        CommunicationUserIdentifier user => $"ACSUser - RawId: {user.Id}",
        _ => $"{participant.Identifier.GetType().Name} - RawId: {participant.Identifier.RawId}"
    }).ToList();

    logger.LogInformation(
        "Participants ({Count}):\n{List}",
        participantInfo.Count,
        string.Join("\n", participantInfo.Select((info, index) => $"{index + 1}. {info}")));
}