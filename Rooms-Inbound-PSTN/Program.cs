using System.Collections.Concurrent;
using Azure;
using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Communication.Identity;
using Azure.Communication.Rooms;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// --- Configuration and globals ---
string GetConfigValue(string key) => builder.Configuration[key]
    ?? throw new ArgumentNullException(paramName: key, message: $"'{key}' is not set in appsettings.json.");

string acsConnectionString = GetConfigValue("AcsConnectionString");
string callbackUriHost = GetConfigValue("CallbackUriHost");

// user1: ACS identity that joins the Room from the browser. user2: ACS number that receives the call.
// user3: second ACS number the call is placed from (outbound caller ID), and the participant moved into the Room.
var user1 = new CommunicationUserIdentifier(GetConfigValue("User1"));
var user2 = new PhoneNumberIdentifier(GetConfigValue("User2"));
var user3 = new PhoneNumberIdentifier(GetConfigValue("User3"));
//var tunnelUrl = Environment.GetEnvironmentVariable("VS_TUNNEL_URL");
//Console.WriteLine(tunnelUrl);
//callbackUriHost = string.IsNullOrEmpty(tunnelUrl) ? callbackUriHost : tunnelUrl;
var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");

var callAutomationClient = new CallAutomationClient(acsConnectionString);
var roomsClient = new RoomsClient(acsConnectionString);
var identityClient = new CommunicationIdentityClient(acsConnectionString);

var session = new SessionState();

// --- Event Grid webhook: Microsoft.Communication.IncomingCall ---
app.MapPost("/api/incomingCall", async (EventGridEvent[] eventGridEvents, ILogger<Program> logger) =>
{
    foreach (var eventGridEvent in eventGridEvents)
    {
        if (!eventGridEvent.TryGetSystemEventData(out object eventData)) continue;

        if (eventData is SubscriptionValidationEventData validationData)
        {
            return Results.Ok(new SubscriptionValidationResponse { ValidationResponse = validationData.ValidationCode });
        }

        if (eventData is AcsIncomingCallEventData incomingCallData)
        {
            string fromRawId = incomingCallData.FromCommunicationIdentifier.RawId;
            string toRawId = incomingCallData.ToCommunicationIdentifier.RawId;

            // The subscription is resource-wide; only answer the call placed from user3 to user2.
            if (!IsPhoneNumber(fromRawId, user3) || !IsPhoneNumber(toRawId, user2))
            {
                logger.LogInformation("Ignoring incoming call from {From} to {To}.", fromRawId, toRawId);
                continue;
            }

            var answerOptions = new AnswerCallOptions(incomingCallData.IncomingCallContext, callbackUri);
            AnswerCallResult answerResult = await callAutomationClient.AnswerCallAsync(answerOptions);
            session.SourceCallConnectionId = answerResult.CallConnectionProperties.CallConnectionId;
            session.Track(session.SourceCallConnectionId);

            logger.LogInformation("Answered incoming PSTN call. From: {From} To: {To} CallConnectionId: {Id} CorrelationId: {Correlation}",
                fromRawId, toRawId, session.SourceCallConnectionId, incomingCallData.CorrelationId);
        }
    }

    return Results.Text("Success!", "text/plain");
}).WithTags("Events");

// --- Call Automation callbacks ---
app.MapPost("/api/callbacks", (CloudEvent[] cloudEvents, ILogger<Program> logger) =>
{
    foreach (var cloudEvent in cloudEvents)
    {
        CallAutomationEventBase automationEvent = CallAutomationEventParser.Parse(cloudEvent);
        logger.LogInformation("Received call event: {Type}, CallConnectionId: {Id}", automationEvent.GetType().Name, automationEvent.CallConnectionId);

        switch (automationEvent)
        {
            case CallConnected:
                session.ConnectedCalls[automationEvent.CallConnectionId] = true;
                break;
            case CallDisconnected:
                session.ConnectedCalls.TryRemove(automationEvent.CallConnectionId, out _);
                break;
            case MoveParticipantSucceeded:
                logger.LogInformation("MoveParticipant succeeded.");
                break;
            case MoveParticipantFailed failed:
                logger.LogError("MoveParticipant failed: {Message}", failed.ResultInformation?.Message);
                break;
            case ConnectFailed connectFailed:
                logger.LogError("Connect to Room failed: {Message}", connectFailed.ResultInformation?.Message);
                break;
            case CreateCallFailed createCallFailed:
                logger.LogError("Outbound call failed: {Message}", createCallFailed.ResultInformation?.Message);
                break;
        }
    }

    return Results.Text("Success!", "text/plain");
}).WithTags("Events");

// --- Workflow endpoints (run in this order from Swagger) ---

// 1. Create the Room and prepare the browser join page.
app.MapPost("/createRoom", async (ILogger<Program> logger) =>
{
    var createRoomOptions = new CreateRoomOptions
    {
        ValidFrom = DateTimeOffset.UtcNow,
        ValidUntil = DateTimeOffset.UtcNow.AddDays(10),
        PstnDialOutEnabled = true,
        Participants = new List<RoomParticipant> { new RoomParticipant(user1) { Role = ParticipantRole.Presenter } }
    };

    CommunicationRoom room = await roomsClient.CreateRoomAsync(createRoomOptions);
    session.RoomId = room.Id;

    var tokenResponse = await identityClient.GetTokenAsync(user1, new[] { CommunicationTokenScope.VoIP });
    session.CreateJoinSession(tokenResponse.Value.Token, room.Id, out string nonce);

    logger.LogInformation("Created Room {RoomId}.", room.Id);
    return Results.Ok(new
    {
        roomId = room.Id,
        next = $"Open https://localhost:<port>/roomJoinClient?session={nonce} in your browser, click 'Join Room', then call /connectToRoom."
    });
}).WithTags("Rooms Inbound PSTN APIs");

// 2. Connect Call Automation to the Room call (a participant must already have joined it).
app.MapPost("/connectToRoom", async (ILogger<Program> logger) =>
{
    if (session.RoomId is null) return Results.Conflict("Call /createRoom first.");

    var connectOptions = new ConnectCallOptions(new RoomCallLocator(session.RoomId), callbackUri);
    ConnectCallResult connectResult = await callAutomationClient.ConnectCallAsync(connectOptions);
    session.RoomCallConnectionId = connectResult.CallConnectionProperties.CallConnectionId;
    session.Track(session.RoomCallConnectionId);

    logger.LogInformation("Connecting to Room {RoomId}. Room call connection id: {Id}", session.RoomId, session.RoomCallConnectionId);
    return Results.Ok(new { roomCallConnectionId = session.RoomCallConnectionId });
}).WithTags("Rooms Inbound PSTN APIs");

// 3. Place the test call from user3 to user2. It raises IncomingCall, which /api/incomingCall answers.
app.MapPost("/dialInboundCall", async (ILogger<Program> logger) =>
{
    var callInvite = new CallInvite(user2, user3);
    var createCallOptions = new CreateCallOptions(callInvite, callbackUri);
    CreateCallResult createCallResult = await callAutomationClient.CreateCallAsync(createCallOptions);
    string outboundCallConnectionId = createCallResult.CallConnectionProperties.CallConnectionId;
    session.Track(outboundCallConnectionId);

    logger.LogInformation("Placed outbound call from {From} to {To}. CallConnectionId: {Id}", user3.PhoneNumber, user2.PhoneNumber, outboundCallConnectionId);
    return Results.Ok(new { outboundCallConnectionId });
}).WithTags("Rooms Inbound PSTN APIs");

// 4. Move the answered PSTN caller into the Room call.
app.MapPost("/moveParticipant", async (ILogger<Program> logger) =>
{
    if (session.RoomCallConnectionId is null || session.SourceCallConnectionId is null)
    {
        return Results.Conflict("The Room call must be connected and the inbound call answered first.");
    }

    // Both calls must be Established (CallConnected received) or the move fails with error 8501.
    if (!session.ConnectedCalls.ContainsKey(session.RoomCallConnectionId) ||
        !session.ConnectedCalls.ContainsKey(session.SourceCallConnectionId))
    {
        return Results.Conflict("Both the Room call and the inbound call must be connected. Check the logs for CallConnected events and retry.");
    }

    var roomCallConnection = callAutomationClient.GetCallConnection(session.RoomCallConnectionId);
    var moveOptions = new MoveParticipantsOptions(new CommunicationIdentifier[] { user3 }, session.SourceCallConnectionId);

    try
    {
        Response<MoveParticipantsResult> moveResult = await roomCallConnection.MoveParticipantsAsync(moveOptions);
        int status = moveResult.GetRawResponse().Status;
        if (status is < 200 or > 299)
        {
            return Results.Problem($"MoveParticipants failed with status code {status}.");
        }

        logger.LogInformation("MoveParticipant initiated: {Phone} -> Room {RoomId}.", user3.PhoneNumber, session.RoomId);
        return Results.Ok($"MoveParticipant initiated. {user3.PhoneNumber} -> Room {session.RoomId}.");
    }
    catch (RequestFailedException ex)
    {
        logger.LogError("MoveParticipants failed: Status={Status}, ErrorCode={Code}, Message={Message}", ex.Status, ex.ErrorCode, ex.Message);
        return Results.Problem(ex.Message, statusCode: ex.Status == 0 ? 500 : ex.Status);
    }
}).WithTags("Rooms Inbound PSTN APIs");

// 5. End the session: hang up every call this app created, then delete the Room.
app.MapPost("/cleanup", async (ILogger<Program> logger) =>
{
    // Deleting a Room doesn't end its call, so hang up first.
    foreach (string callConnectionId in session.TrackedCalls.Keys)
    {
        try
        {
            await callAutomationClient.GetCallConnection(callConnectionId).HangUpAsync(true);
            logger.LogInformation("Hung up call connection {Id}.", callConnectionId);
        }
        catch (RequestFailedException ex) when (ex.Status == 404 || ex.ErrorCode == "8522")
        {
            // Already ended.
        }
        catch (Exception ex)
        {
            logger.LogWarning("Failed to hang up {Id}: {Message}", callConnectionId, ex.Message);
        }
    }

    if (session.RoomId is not null)
    {
        await roomsClient.DeleteRoomAsync(session.RoomId);
        logger.LogInformation("Deleted Room {RoomId}.", session.RoomId);
    }

    session.Reset();
    return Results.Ok("Session cleaned up.");
}).WithTags("Rooms Inbound PSTN APIs");

// --- Browser join client (served from localhost only) ---
string joinClientPath = Path.Combine(AppContext.BaseDirectory, "RoomJoinClient", "index.html");

// The page and the token handoff are only served to localhost so they aren't reachable through the Dev Tunnel.
bool IsLocalhost(HttpContext context) =>
    string.Equals(context.Request.Host.Host, "localhost", StringComparison.OrdinalIgnoreCase);

app.MapGet("/roomJoinClient", (HttpContext context) =>
    IsLocalhost(context) && File.Exists(joinClientPath)
        ? Results.File(joinClientPath, "text/html; charset=utf-8")
        : Results.NotFound()).ExcludeFromDescription();

app.MapGet("/session", (string id, HttpContext context) =>
{
    if (!IsLocalhost(context) || !session.TryRedeemJoinSession(id, out var data)) return Results.NotFound();
    context.Response.Headers.CacheControl = "no-store";
    return Results.Json(new { token = data.Token, roomId = data.RoomId });
}).ExcludeFromDescription();

app.Run();

// "from"/"to" raw ids for phone numbers look like "4:+1555..." (or without '+'); compare digits only.
static bool IsPhoneNumber(string rawId, PhoneNumberIdentifier expected)
{
    string actual = rawId.StartsWith("4:") ? rawId[2..] : rawId;
    return string.Equals(actual.TrimStart('+'), expected.PhoneNumber.TrimStart('+'), StringComparison.Ordinal);
}

// State for the single demo session. The call ids come from Call Automation and are used to correlate events.
class SessionState
{
    public string? RoomId;
    public string? RoomCallConnectionId;
    public string? SourceCallConnectionId;

    // Call connections created by this app; hung up during cleanup.
    public ConcurrentDictionary<string, bool> TrackedCalls { get; } = new();
    // Call connections for which a CallConnected event has been received.
    public ConcurrentDictionary<string, bool> ConnectedCalls { get; } = new();

    public record JoinData(string Token, string RoomId);
    readonly ConcurrentDictionary<string, JoinData> joinSessions = new();

    public void Track(string callConnectionId) => TrackedCalls[callConnectionId] = true;

    // The access token is handed to the browser once, via a random nonce, so it never appears in a URL.
    public void CreateJoinSession(string token, string roomId, out string nonce)
    {
        nonce = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16));
        joinSessions[nonce] = new JoinData(token, roomId);
    }

    public bool TryRedeemJoinSession(string nonce, out JoinData data) => joinSessions.TryRemove(nonce, out data!);

    public void Reset()
    {
        RoomId = RoomCallConnectionId = SourceCallConnectionId = null;
        TrackedCalls.Clear();
        ConnectedCalls.Clear();
        joinSessions.Clear();
    }
}
