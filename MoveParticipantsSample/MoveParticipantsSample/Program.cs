using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();

// --- Config & Globals ---
string GetConfig(string key) => builder.Configuration[key] ?? throw new ArgumentNullException(key);
string acsConnectionString = GetConfig("acsConnectionString"),
       callbackUriHost = GetConfig("callbackUriHost"),
       acsOutboundPhoneNumber = GetConfig("acsOutboundPhoneNumber"),
       acsInboundPhoneNumber = GetConfig("acsInboundPhoneNumber"),
       acsUserPhoneNumber = GetConfig("acsUserPhoneNumber"),
       acsTestIdentity2 = GetConfig("acsTestIdentity2"),
       acsTestIdentity3 = GetConfig("acsTestIdentity3");

string lastWorkflowCallType = string.Empty, callConnectionId = string.Empty, callConnectionId1 = string.Empty, callConnectionId2 = string.Empty;
CallAutomationClient client = new(acsConnectionString);

// --- Event Grid Handler ---
app.MapPost("/api/MoveParticipantEvent", async (EventGridEvent[] events, ILogger<Program> logger) =>
{
    logger.LogInformation("~~~~~~~~~~~~ /api/MoveParticipantEvent (Event Grid Hook) ~~~~~~~~~~~~");
    foreach (var e in events)
    {
        if (!e.TryGetSystemEventData(out var data)) continue;
        if (data is SubscriptionValidationEventData sub)
            return Results.Ok(new SubscriptionValidationResponse { ValidationResponse = sub.ValidationCode });

        if (data is AcsIncomingCallEventData inc)
        {
            logger.LogInformation($"Event received: {e.EventType}");
            string fromId = inc.FromCommunicationIdentifier.RawId, toId = inc.ToCommunicationIdentifier.RawId;
            if (fromId.Contains(acsUserPhoneNumber))
            {
                AnswerCallResult answer = await client.AnswerCallAsync(new(incomingCallContext: inc.IncomingCallContext, callbackUri: new(new Uri(callbackUriHost), "/api/callbacks")) { OperationContext = "IncomingCallFromUser" });
                callConnectionId1 = answer.CallConnection.CallConnectionId;
                logger.LogInformation($"""
                    User Call Answered by Call Automation.
                    From: {fromId}
                    To:   {toId}
                    Connection Id: {callConnectionId1}
                    Correlation Id: {inc.CorrelationId}
                    """);
            }
            else if (fromId.Contains(acsInboundPhoneNumber))
            {
                string target = lastWorkflowCallType == "CallThree" ? acsTestIdentity3 : acsTestIdentity2;
                await RedirectCallAsync(client, inc.IncomingCallContext, target);
                logger.LogInformation($"""
                    Call redirected to ACS User Identity: {target}
                    From: {fromId}
                    To:   {toId}
                    """);
            }
        }
    }
    return Results.Text("Success!", "text/plain");
});

// --- Callback Handler ---
app.MapPost("/api/callbacks", async (CloudEvent[] events, ILogger<Program> logger) =>
{
    foreach (var ce in events)
    {
        var evt = CallAutomationEventParser.Parse(ce);
        if (evt is CallConnected cc)
        {
            string? which = (cc.OperationContext ?? "") switch
            {
                "CallTwo" => "Call 2",
                "CallThree" => "Call 3",
                _ => null
            };
            if (which != null)
                logger.LogInformation($"""
                    ~~~~~~~~~~~~  /api/callbacks ~~~~~~~~~~~~ 
                    Received call event: {evt.GetType()}
                    {which} Internal Connection Id: {cc.CallConnectionId}
                    Correlation Id: {cc.CorrelationId}
                    """);
        }
        else if (evt is CallDisconnected cd)
        {
            logger.LogInformation($"""
                ~~~~~~~~~~~~  /api/callbacks ~~~~~~~~~~~~ 
                Received event: {evt.GetType()}
                Call Connection Id: {cd.CallConnectionId}
                """);
        }
    }
    return Results.Text("Sccess!", "text/plain");
}).Produces(StatusCodes.Status200OK);

// --- Workflow Endpoints ---
app.MapPost("/CreateCall1(UserCallToCallAutomation)", async (ILogger<Program> logger) =>
{
    logger.LogInformation("~~~~~~~~~~~~ /CreateCall1(UserCallToCallAutomation) ~~~~~~~~~~~~");
    var result = await CreateCallAsync(client, acsUserPhoneNumber, acsInboundPhoneNumber, callbackUriHost);
    callConnectionId = result.CallConnectionProperties.CallConnectionId;
    logger.LogInformation($"""
        Call 1: From {acsUserPhoneNumber} To {acsInboundPhoneNumber}
        Target Call Connection Id: {callConnectionId}
        Correlation Id: {result.CallConnectionProperties.CorrelationId}
        """);
    return Results.Text("Success!", "text/plain");
}).WithTags("Move Participants APIs");

app.MapPost("/CreateCall2(ToPstnUserFirstAndRedirectToAcsIentity)", async (ILogger<Program> logger) =>
{
    logger.LogInformation("~~~~~~~~~~~~ /CreateCall2(ToPstnUserFirstAndRedirectToAcsIentity) ~~~~~~~~~~~~");
    var result = await CreateCallAsync(client, acsInboundPhoneNumber, acsOutboundPhoneNumber, callbackUriHost, "CallTwo");
    lastWorkflowCallType = "CallTwo";
    callConnectionId1 = result.CallConnectionProperties.CallConnectionId;
    logger.LogInformation($"""
        Call 2: From {acsInboundPhoneNumber} To {acsOutboundPhoneNumber}
        Source Call Connection Id: {callConnectionId1}
        Correlation Id: {result.CallConnectionProperties.CorrelationId}
        Redirect Call2 to: {acsTestIdentity2}
        """);
    return Results.Text("Success!", "text/plain");
}).WithTags("Move Participants APIs");

app.MapPost("/CreateCall3(ToPstnUserFirstAndRedirectToAcsIentity)", async (ILogger<Program> logger) =>
{
    logger.LogInformation("~~~~~~~~~~~~ /CreateCall3(ToPstnUserFirstAndRedirectToAcsIentity) ~~~~~~~~~~~~");
    var result = await CreateCallAsync(client, acsInboundPhoneNumber, acsOutboundPhoneNumber, callbackUriHost, "CallThree");
    lastWorkflowCallType = "CallThree";
    callConnectionId2 = result.CallConnectionProperties.CallConnectionId;
    logger.LogInformation($"""
        Call 3: From {acsInboundPhoneNumber} To {acsOutboundPhoneNumber}
        Source Call Connection Id: {callConnectionId2}
        Correlation Id: {result.CallConnectionProperties.CorrelationId}
        Redirect Call3 to: {acsTestIdentity3}
        """);
    return Results.Text("Success!", "text/plain");
}).WithTags("Move Participants APIs");

app.MapPost("/MoveParticipant", async (MoveParticipantsRequest req, ILogger<Program> logger) =>
{
    logger.LogInformation("~~~~~~~~~~~~ /MoveParticipant Operation ~~~~~~~~~~~~");
    try
    {
        logger.LogInformation($"""
            Source Caller Id:     {req.ParticipantToMove}
            Source Connection Id: {req.SourceCallConnectionId}
            Target Connection Id: {req.TargetCallConnectionId}
            """);
        var targetConn = client.GetCallConnection(req.TargetCallConnectionId);
        CommunicationIdentifier participant = req.ParticipantToMove.StartsWith("+")
            ? new PhoneNumberIdentifier(req.ParticipantToMove)
            : req.ParticipantToMove.StartsWith("8:acs:")
                ? new CommunicationUserIdentifier(req.ParticipantToMove)
                : throw new Exception("Invalid participant format, Use phone number (+1234567890) or ACS user ID (8:acs:...).");
        var resp = await targetConn.MoveParticipantsAsync(new([participant], req.SourceCallConnectionId));
        if (resp.GetRawResponse().Status is >= 200 and <= 299)
            logger.LogInformation("Move Participants operation completed successfully.");
        else
            throw new Exception($"Move Participants operation failed with status code: {resp.GetRawResponse().Status}");
        return Results.Text("Success!", "text/plain");
    }
    catch (Exception ex)
    {
        logger.LogError($"Error in /MoveParticipant: {ex.Message}");
        throw;
    }
}).WithTags("Move Participants APIs");

app.MapGet("/GetParticipants/{callConnectionId}", async (string callConnectionId, ILogger<Program> logger) =>
{
    logger.LogInformation($"~~~~~~~~~~~~ /GetParticipants/{callConnectionId} ~~~~~~~~~~~~");
    try
    {
        var callConn = client.GetCallConnection(callConnectionId);
        var participants = await callConn.GetParticipantsAsync();
        var info = participants.Value.Select(p => new
        {
            p.Identifier.RawId,
            Type = p.Identifier.GetType().Name,
            PhoneNumber = p.Identifier is PhoneNumberIdentifier phone ? phone.PhoneNumber : null,
            AcsUserId = p.Identifier is CommunicationUserIdentifier user ? user.Id : null,
        })
        .OrderBy(p => p.AcsUserId)
        .Select((p, i) => $"{i + 1}. {(string.IsNullOrWhiteSpace(p.AcsUserId) ? $"{p.Type} - RawId: {p.RawId}, Phone: {p.PhoneNumber}" : $"{p.Type} - RawId: {p.AcsUserId}")}");
        if (!info.Any())
            return Results.NotFound(new { Message = "No participants found for the specified call connection.", CallConnectionId = callConnectionId });
        var output = $"""
            No of Participants: {info.Count()}
            Participants:
            -------------
            {string.Join("\n", info)}
            """;
        logger.LogInformation(output);
        return Results.Text(output, "text/plain");
    }
    catch (Exception ex)
    {
        logger.LogError($"Error getting participants for call {callConnectionId}: {ex.Message}");
        throw;
    }
}).WithTags("Move Participants APIs");

app.Run();

// --- Helper methods and models ---
static async Task<CreateCallResult> CreateCallAsync(CallAutomationClient client, string from, string to, string callbackUriHost, string? opCtx = null)
    => await client.CreateCallAsync(new(new(new PhoneNumberIdentifier(to), new PhoneNumberIdentifier(from)), new(new Uri(callbackUriHost), "/api/callbacks")) { OperationContext = opCtx });

static async Task RedirectCallAsync(CallAutomationClient client, string ctx, string target)
    => await client.RedirectCallAsync(ctx, new(new CommunicationUserIdentifier(target)));

public class MoveParticipantsRequest
{
    public string ParticipantToMove { get; set; } = string.Empty;
    public string SourceCallConnectionId { get; set; } = string.Empty;
    public string TargetCallConnectionId { get; set; } = string.Empty;
}