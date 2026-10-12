using System.Collections.Concurrent;
using Azure;
using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Communication.Identity;
using Azure.Communication.Rooms;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using System.Security.Cryptography;
using System.Text;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Two listeners, both loopback-only: the webhook port (forward this one through the Dev Tunnel)
// and the operator port (never forward it). Neither is reachable from other machines on the network.
int webhookPort = builder.Configuration.GetValue<int?>("WebhookPort") ?? 8080;
int operatorPort = builder.Configuration.GetValue<int?>("OperatorPort") ?? 5142;
builder.WebHost.ConfigureKestrel(kestrel =>
{
    kestrel.ListenLocalhost(webhookPort, listen => listen.UseHttps());
    kestrel.ListenLocalhost(operatorPort);
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "Value of OperatorApiKey from appsettings.json. Click Authorize and paste it here.",
        Name = "X-Api-Key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" } }, Array.Empty<string>() }
    });
});
var app = builder.Build();

// Only /api/* (the Event Grid and callback webhooks) may be reached on the public port that the Dev Tunnel forwards.
// Everything else (Swagger, workflow endpoints, join page, token handoff) is served only on the local operator port.
// The check uses the port the connection arrived on, not the Host header, because Dev Tunnels rewrite Host to localhost.
app.Use(async (context, next) =>
{
    if (!context.Request.Path.StartsWithSegments("/api") && context.Connection.LocalPort != operatorPort)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    await next();
});

// The workflow endpoints (createRoom, connectToRoom, dialInboundCall, moveParticipant, cleanup) also need the X-Api-Key header.
// Swagger docs are public on the operator port. The join page and /session are protected by the one-time session code.
string? operatorApiKey = builder.Configuration["OperatorApiKey"];
if (string.IsNullOrWhiteSpace(operatorApiKey) || operatorApiKey.StartsWith("<"))
    throw new ArgumentNullException(paramName: "OperatorApiKey", message: "'OperatorApiKey' is not set in appsettings.json. Use any long random string.");
byte[] operatorApiKeyBytes = Encoding.UTF8.GetBytes(operatorApiKey);
string[] openPaths = { "/api", "/swagger", "/roomJoinClient", "/session" };
app.Use(async (context, next) =>
{
    if (!openPaths.Any(path => context.Request.Path.StartsWithSegments(path)))
    {
        byte[] supplied = Encoding.UTF8.GetBytes(context.Request.Headers["X-Api-Key"].ToString());
        if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(supplied), SHA256.HashData(operatorApiKeyBytes)))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }
    }
    await next();
});

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
    // Don't replace a Room while the previous session's Room or calls are still active; they must be released via /cleanup first.
    if (session.RoomId is not null || !session.TrackedCalls.IsEmpty)
        return Results.Conflict("A session is already active. Call /cleanup first, then create a new Room.");

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
        next = $"Open http://localhost:{operatorPort}/roomJoinClient?session={nonce} in your browser, click 'Join Room', then call /connectToRoom."
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
    var unresolved = new List<string>();
    foreach (string callConnectionId in session.TrackedCalls.Keys)
    {
        try
        {
            await callAutomationClient.GetCallConnection(callConnectionId).HangUpAsync(true);
            logger.LogInformation("Hung up call connection {Id}.", callConnectionId);
            session.TrackedCalls.TryRemove(callConnectionId, out _);
        }
        catch (RequestFailedException ex) when (ex.Status == 404 || ex.ErrorCode == "8522")
        {
            session.TrackedCalls.TryRemove(callConnectionId, out _); // Already ended.
        }
        catch (Exception ex)
        {
            logger.LogWarning("Failed to hang up {Id}: {Message}", callConnectionId, ex.Message);
            unresolved.Add(callConnectionId);
        }
    }

    // Keep the session (and the Room) until every call is ended, so calling /cleanup again retries the failed hang-ups.
    if (unresolved.Count > 0)
    {
        return Results.Json(new
        {
            status = "PartialFailure",
            message = "Some calls could not be hung up and may still be active. The Room was not deleted and the session was kept. Call /cleanup again to retry.",
            unresolvedCallConnectionIds = unresolved
        }, statusCode: StatusCodes.Status500InternalServerError);
    }

    if (session.RoomId is not null)
    {
        try
        {
            await roomsClient.DeleteRoomAsync(session.RoomId);
            logger.LogInformation("Deleted Room {RoomId}.", session.RoomId);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            logger.LogInformation("Room {RoomId} was already deleted.", session.RoomId);
        }
    }

    session.Reset();
    return Results.Ok("Session cleaned up.");}).WithTags("Rooms Inbound PSTN APIs");

// --- Browser join client (operator port only) ---
string joinClientPath = Path.Combine(AppContext.BaseDirectory, "RoomJoinClient", "index.html");

app.MapGet("/roomJoinClient", (HttpContext context) =>
    File.Exists(joinClientPath)
        ? Results.File(joinClientPath, "text/html; charset=utf-8")
        : Results.NotFound()).ExcludeFromDescription();

app.MapGet("/session", (string id, HttpContext context) =>
{
    if (!session.TryRedeemJoinSession(id, out var data)) return Results.NotFound();
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
