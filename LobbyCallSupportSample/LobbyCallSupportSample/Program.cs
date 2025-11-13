using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Core;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using System.Net.WebSockets;
using System.Text;

// Bootstrap
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

// Config helper
string GetConfig(string key) => builder.Configuration[key] ?? throw new ArgumentNullException(key);
string acsConnectionString = GetConfig("acsConnectionString"),
       cognitiveServiceEndpoint = GetConfig("cognitiveServiceEndpoint"),
       callbackUriHost = GetConfig("callbackUriHost"),
       acsLobbyCallReceiver = GetConfig("acsLobbyCallReceiver"),
       acsTargetCallReceiver = GetConfig("acsTargetCallReceiver"),
       acsTargetCallSender = GetConfig("acsTargetCallSender");

const string confirmMsg = "A user is waiting in lobby, do you want to add the lobby user to your call?";
const string lobbyMsg = "You are currently in a lobby call, we will notify the admin that you are waiting.";

string acsUserId = string.Empty,
       targetConnId = string.Empty,
       lobbyConnId = string.Empty,
       lobbyUserId = string.Empty;

WebSocket? ws = null;
CallAutomationClient callClient = new(acsConnectionString);

// Event Handler
app.MapPost("/api/LobbyCallSupportEventHandler", async (EventGridEvent[] events, ILogger<Program> log) =>
{
    log.LogInformation("~~~ /api/LobbyCallSupportEventHandler ~~~");
    try
    {
        foreach (var e in events)
        {
            if (!e.TryGetSystemEventData(out var data)) continue;
            switch (data)
            {
                case SubscriptionValidationEventData s:
                    return Results.Ok(new SubscriptionValidationResponse { ValidationResponse = s.ValidationCode });
                case AcsIncomingCallEventData inc:
                    log.LogInformation("Event: {Type}", e.EventType);
                    acsUserId = inc.FromCommunicationIdentifier.RawId;
                    var toId = inc.ToCommunicationIdentifier.RawId;
                    if (toId.Contains(acsLobbyCallReceiver) || toId.Contains(acsTargetCallReceiver))
                    {
                        var cbUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
                        var opts = new AnswerCallOptions(inc.IncomingCallContext, cbUri)
                        {
                            OperationContext = !toId.Contains(acsTargetCallReceiver) ? "LobbyCall" : "OtherCall",
                            CallIntelligenceOptions = new() { CognitiveServicesEndpoint = new Uri(cognitiveServiceEndpoint) }
                        };
                        AnswerCallResult res = await callClient.AnswerCallAsync(opts);
                        if (toId.Contains(acsTargetCallReceiver))
                        {
                            targetConnId = res.CallConnection.CallConnectionId;
                            log.LogInformation("Target Call Answered. From: {From}, To: {To}, ConnId: {Conn}, CorrId: {Corr}",
                                acsUserId, toId, targetConnId, inc.CorrelationId);
                        }
                        else
                        {
                            lobbyConnId = res.CallConnection.CallConnectionId;
                            log.LogInformation("Lobby Call Answered. From: {From}, To: {To}, ConnId: {Conn}, CorrId: {Corr}",
                                acsUserId, toId, lobbyConnId, inc.CorrelationId);
                        }
                    }
                    break;
            }
        }
        return Results.Ok();
    }
    catch (Exception ex)
    {
        log.LogError(ex, "Error");
        throw;
    }
});

// Callback Handler
app.MapPost("/api/callbacks", async (CloudEvent[] events, ILogger<Program> log) =>
{
    try
    {
        foreach (var ce in events)
        {
            var ev = CallAutomationEventParser.Parse(ce);
            var conn = callClient.GetCallConnection(ev.CallConnectionId);
            switch (ev)
            {
                case CallConnected cc when (cc.OperationContext ?? "") == "LobbyCall":
                    log.LogInformation("~~~ /api/callbacks ~~~\nCallConnected: {ConnId}", cc.CallConnectionId);
                    CallConnectionProperties props = conn.GetCallConnectionProperties();
                    lobbyUserId = props.Source.RawId;
                    lobbyConnId = props.CallConnectionId;
                    log.LogInformation("Lobby Caller: {Caller}, Conn: {Conn}", lobbyUserId, lobbyConnId);
                    var media = callClient.GetCallConnection(cc.CallConnectionId).GetCallMedia();
                    var textSrc = new TextSource(lobbyMsg) { VoiceName = "en-US-NancyNeural" };
                    await media.PlayAsync(new PlayOptions(textSrc, [new CommunicationUserIdentifier(acsUserId)]) { OperationContext = "playToContext" });
                    break;
                case PlayCompleted:
                    log.LogInformation("PlayCompleted event");
                    if (ws is null || ws.State != WebSocketState.Open)
                    {
                        log.LogError("WebSocket unavailable");
                        return Results.NotFound("Message not sent");
                    }
                    await ws.SendAsync(Encoding.UTF8.GetBytes(confirmMsg), WebSocketMessageType.Text, true, CancellationToken.None);
                    log.LogInformation("Target notified: {Msg}", confirmMsg);
                    return Results.Ok($"Target notified: {confirmMsg}");
                case MoveParticipantSucceeded mps:
                    log.LogInformation("MoveParticipantSucceeded: {ConnId}", mps.CallConnectionId);
                    var tgtConn = callClient.GetCallConnection(mps.CallConnectionId);
                    var parts = await tgtConn.GetParticipantsAsync();
                    LogParts(parts.Value, log);
                    break;
                case CallDisconnected cd:
                    log.LogInformation("CallDisconnected: {ConnId}", cd.CallConnectionId);
                    break;
            }
        }
        return Results.Ok();
    }
    catch (Exception ex)
    {
        log.LogError(ex, "Error");
        throw;
    }
}).Produces(200);

// Target Call Creation
app.MapPost("/TargetCallToAcsUser(Call Replaced with client app)", async (string tgt, ILogger<Program> log) =>
{
    log.LogInformation("~~~ /TargetCall(Create) ~~~");
    var cbUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    CreateCallResult res = await callClient.CreateCallAsync(new CreateCallOptions(new CallInvite(new CommunicationUserIdentifier(tgt)), cbUri)
    {
        CallIntelligenceOptions = new() { CognitiveServicesEndpoint = new Uri("") }
    });
    targetConnId = res.CallConnectionProperties.CallConnectionId;
    log.LogInformation("TargetCall: From: CallAutomation, To: {Tgt}, ConnId: {Conn}, CorrId: {Corr}",
        tgt, targetConnId, res.CallConnectionProperties.CorrelationId);
    return Results.Ok();
}).WithTags("Lobby Call Support APIs");

// Get Participants
app.MapGet("/GetParticipants/{connId}", async (string connId, ILogger<Program> log) =>
{
    log.LogInformation("~~~ /GetParticipants/{ConnId} ~~~", connId);
    try
    {
        var conn = callClient.GetCallConnection(connId);
        var parts = await conn.GetParticipantsAsync();
        if (!parts.Value.Any())
            return Results.NotFound(new { Message = "No participants found.", CallConnectionId = connId });
        LogParts(parts.Value, log);
        return Results.Ok();
    }
    catch (Exception ex)
    {
        log.LogError("Error getting participants: {Msg}", ex.Message);
        throw;
    }
}).WithTags("Lobby Call Support APIs");

// WebSocket
app.UseWebSockets();
app.Map("/ws", async ctx =>
{
    var log = ctx.RequestServices.GetRequiredService<ILogger<Program>>();
    // log.LogInformation("WebSocket request");
    if (!ctx.WebSockets.IsWebSocketRequest)
    {
        ctx.Response.StatusCode = 400;
        return;
    }
    ws = await ctx.WebSockets.AcceptWebSocketAsync();
    var buf = new byte[4096];
    while (ws.State == WebSocketState.Open)
    {
        try
        {
            var res = await ws.ReceiveAsync(new ArraySegment<byte>(buf), CancellationToken.None);
            var msg = Encoding.UTF8.GetString(buf, 0, res.Count);
            log.LogInformation("Client response: {Msg}", msg);
            if (res.MessageType == WebSocketMessageType.Close)
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
            else if (msg.Equals("yes", StringComparison.OrdinalIgnoreCase))
            {
                log.LogInformation("Move Participant...");
                try
                {
                    var tgtConn = callClient.GetCallConnection(targetConnId);
                    CommunicationIdentifier part = lobbyUserId.StartsWith("+")
                        ? new PhoneNumberIdentifier(lobbyUserId)
                        : new CommunicationUserIdentifier(lobbyUserId);
                    var moveRes = await tgtConn.MoveParticipantsAsync(new([part], lobbyConnId));
                    var raw = moveRes.GetRawResponse();
                    if (raw.Status is >= 200 and <= 299)
                        log.LogInformation("Move successful");
                    else
                        throw new Exception($"Move failed: {raw.Status}");
                }
                catch (Exception ex)
                {
                    log.LogError("Move error: {Msg}", ex.Message);
                    throw;
                }
            }
        }
        catch (Exception ex)
        {
            log.LogError("WebSocket error: {Msg}", ex.Message);
        }
    }
});

app.Run();

// Helper: Log participants
static void LogParts(IEnumerable<CallParticipant> parts, ILogger log)
{
    var info = parts.Select(p => p.Identifier switch
    {
        PhoneNumberIdentifier ph => $"Phone - RawId: {ph.RawId}, Phone: {ph.PhoneNumber}",
        CommunicationUserIdentifier user => $"ACSUser - RawId: {user.Id}",
        _ => $"{p.Identifier.GetType().Name} - RawId: {p.Identifier.RawId}"
    }).ToList();
    log.LogInformation("Participants ({Count}):\n{List}", info.Count, string.Join("\n", info.Select((x, i) => $"{i + 1}. {x}")));
}
