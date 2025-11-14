using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;

var builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Swagger for development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();

// --- Configuration and Globals ---
string GetConfigValue(string key) => builder.Configuration[key] ?? throw new ArgumentNullException(paramName: key);

string acsConnectionString = GetConfigValue(key: "acsConnectionString");
string callbackUriHost = GetConfigValue(key: "callbackUriHost");
string outboundPhoneNumber = GetConfigValue(key: "acsOutboundPhoneNumber");
string inboundPhoneNumber = GetConfigValue(key: "acsInboundPhoneNumber");
string userPhoneNumber = GetConfigValue(key: "acsUserPhoneNumber");
string acsUserId2 = GetConfigValue(key: "acsTestIdentity2");
string acsUserId3 = GetConfigValue(key: "acsTestIdentity3");

string lastWorkflowType = string.Empty;
string mainCallConnectionId = string.Empty;
string userCallConnectionId = string.Empty;
string redirectedCallConnectionId = string.Empty;

CallAutomationClient callAutomationClient = new(connectionString: acsConnectionString);

// --- Event Grid Handler ---
app.MapPost(pattern: "/api/MoveParticipantEvent", handler: async (EventGridEvent[] eventGridEvents, ILogger<Program> logger) =>
{
    logger.LogInformation(message: "~~~~~~~~~~~~ /api/MoveParticipantEvent (Event Grid Hook) ~~~~~~~~~~~~");
    foreach (var eventGridEvent in eventGridEvents)
    {
        if (!eventGridEvent.TryGetSystemEventData(out object eventData)) continue;

        if (eventData is SubscriptionValidationEventData validationData)
        {
            return Results.Ok(value: new SubscriptionValidationResponse { ValidationResponse = validationData.ValidationCode });
        }

        if (eventData is AcsIncomingCallEventData incomingCallData)
        {
            logger.LogInformation(message: $"Event received: {eventGridEvent.EventType}");

            string fromRawId = incomingCallData.FromCommunicationIdentifier.RawId;
            string toRawId = incomingCallData.ToCommunicationIdentifier.RawId;

            // User calls from their phone number to ACS inbound number
            if (fromRawId.Contains(value: userPhoneNumber))
            {
                var callbackUri = new Uri(baseUri: new Uri(uriString: callbackUriHost), relativeUri: "/api/callbacks");
                var answerOptions = new AnswerCallOptions(incomingCallContext: incomingCallData.IncomingCallContext, callbackUri: callbackUri)
                {
                    OperationContext = "IncomingCallFromUser"
                };

                AnswerCallResult answerResult = await callAutomationClient.AnswerCallAsync(options: answerOptions);
                userCallConnectionId = answerResult.CallConnection.CallConnectionId;

                logger.LogInformation(message: $"""
                    User Call Answered by Call Automation.
                    From: {fromRawId}
                    To:   {toRawId}
                    Connection Id: {userCallConnectionId}
                    Correlation Id: {incomingCallData.CorrelationId}
                    """);
            }
            // ACS inbound number calls ACS outbound number (workflow triggered)
            else if (fromRawId.Contains(value: inboundPhoneNumber))
            {
                string redirectTarget = lastWorkflowType == "CallThree" ? acsUserId3 : acsUserId2;
                await RedirectCallAsync(callAutomationClient: callAutomationClient, incomingCallContext: incomingCallData.IncomingCallContext, targetAcsUserId: redirectTarget);

                logger.LogInformation(message: $"""
                    Call redirected to ACS User Identity: {redirectTarget}
                    From: {fromRawId}
                    To:   {toRawId}
                    """);
            }
        }
    }
    return Results.Text(content: "Success!", contentType: "text/plain");
});

// --- Callback Handler ---
app.MapPost(pattern: "/api/callbacks", handler: async (CloudEvent[] cloudEvents, ILogger<Program> logger) =>
{
    foreach (var cloudEvent in cloudEvents)
    {
        var automationEvent = CallAutomationEventParser.Parse(cloudEvent: cloudEvent);

        if (automationEvent is CallConnected callConnectedEvent)
        {
            string workflow = (callConnectedEvent.OperationContext ?? string.Empty) switch
            {
                "CallTwo" => "Call 2",
                "CallThree" => "Call 3",
                _ => null
            };
            if (workflow != null)
            {
                logger.LogInformation(message: $"""
                    ~~~~~~~~~~~~  /api/callbacks ~~~~~~~~~~~~ 
                    Received call event: {automationEvent.GetType()}
                    {workflow} Internal Connection Id: {callConnectedEvent.CallConnectionId}
                    Correlation Id: {callConnectedEvent.CorrelationId}
                    """);
            }
        }
        else if (automationEvent is MoveParticipantSucceeded moveParticipantSucceeded)
        {
            logger.LogInformation("MoveParticipantSucceeded: {ConnId}", moveParticipantSucceeded.CallConnectionId);
        }
        else if (automationEvent is CallDisconnected callDisconnectedEvent)
        {
            logger.LogInformation(message: $"""
                ~~~~~~~~~~~~  /api/callbacks ~~~~~~~~~~~~ 
                Received event: {automationEvent.GetType()}
                Call Connection Id: {callDisconnectedEvent.CallConnectionId}
                """);
        }
    }
    return Results.Text(content: "Success!", contentType: "text/plain");
}).Produces(statusCode: StatusCodes.Status200OK);

// --- Workflow Endpoints ---
app.MapPost(pattern: "/CreateCall1(UserCallToCallAutomation)", handler: async (ILogger<Program> logger) =>
{
    logger.LogInformation(message: "~~~~~~~~~~~~ /CreateCall1(UserCallToCallAutomation) ~~~~~~~~~~~~");
    var createCallResult = await CreateCallAsync(
        callAutomationClient: callAutomationClient,
        fromPhoneNumber: userPhoneNumber,
        toPhoneNumber: inboundPhoneNumber,
        callbackUriHost: callbackUriHost);

    mainCallConnectionId = createCallResult.CallConnectionProperties.CallConnectionId;

    logger.LogInformation(message: $"""
        Call 1: From {userPhoneNumber} To {inboundPhoneNumber}
        Target Call Connection Id: {mainCallConnectionId}
        Correlation Id: {createCallResult.CallConnectionProperties.CorrelationId}
        """);
    return Results.Text(content: "Success!", contentType: "text/plain");
}).WithTags(tags: "Move Participants APIs");

app.MapPost(pattern: "/CreateCall2(ToPstnUserFirstAndRedirectToAcsIentity)", handler: async (ILogger<Program> logger) =>
{
    logger.LogInformation(message: "~~~~~~~~~~~~ /CreateCall2(ToPstnUserFirstAndRedirectToAcsIentity) ~~~~~~~~~~~~");
    var createCallResult = await CreateCallAsync(
        callAutomationClient: callAutomationClient,
        fromPhoneNumber: inboundPhoneNumber,
        toPhoneNumber: outboundPhoneNumber,
        callbackUriHost: callbackUriHost,
        operationContext: "CallTwo");

    lastWorkflowType = "CallTwo";
    userCallConnectionId = createCallResult.CallConnectionProperties.CallConnectionId;

    logger.LogInformation(message: $"""
        Call 2: From {inboundPhoneNumber} To {outboundPhoneNumber}
        Source Call Connection Id: {userCallConnectionId}
        Correlation Id: {createCallResult.CallConnectionProperties.CorrelationId}
        Redirect Call2 to: {acsUserId2}
        """);
    return Results.Text(content: "Success!", contentType: "text/plain");
}).WithTags(tags: "Move Participants APIs");

app.MapPost(pattern: "/CreateCall3(ToPstnUserFirstAndRedirectToAcsIentity)", handler: async (ILogger<Program> logger) =>
{
    logger.LogInformation(message: "~~~~~~~~~~~~ /CreateCall3(ToPstnUserFirstAndRedirectToAcsIentity) ~~~~~~~~~~~~");
    var createCallResult = await CreateCallAsync(
        callAutomationClient: callAutomationClient,
        fromPhoneNumber: inboundPhoneNumber,
        toPhoneNumber: outboundPhoneNumber,
        callbackUriHost: callbackUriHost,
        operationContext: "CallThree");

    lastWorkflowType = "CallThree";
    redirectedCallConnectionId = createCallResult.CallConnectionProperties.CallConnectionId;

    logger.LogInformation(message: $"""
        Call 3: From {inboundPhoneNumber} To {outboundPhoneNumber}
        Source Call Connection Id: {redirectedCallConnectionId}
        Correlation Id: {createCallResult.CallConnectionProperties.CorrelationId}
        Redirect Call3 to: {acsUserId3}
        """);
    return Results.Text(content: "Success!", contentType: "text/plain");
}).WithTags(tags: "Move Participants APIs");

app.MapPost(pattern: "/MoveParticipant", handler: async (MoveParticipantsRequest moveRequest, ILogger<Program> logger) =>
{
    logger.LogInformation(message: "~~~~~~~~~~~~ /MoveParticipant Operation ~~~~~~~~~~~~");
    try
    {
        logger.LogInformation(message: $"""
            Source Caller Id:     {moveRequest.ParticipantToMove}
            Source Connection Id: {moveRequest.SourceCallConnectionId}
            Target Connection Id: {moveRequest.TargetCallConnectionId}
            """);

        var targetConnection = callAutomationClient.GetCallConnection(callConnectionId: moveRequest.TargetCallConnectionId);

        CommunicationIdentifier participantToMove = moveRequest.ParticipantToMove.StartsWith(value: "+")
            ? new PhoneNumberIdentifier(phoneNumber: moveRequest.ParticipantToMove)
            : moveRequest.ParticipantToMove.StartsWith(value: "8:acs:")
                ? new CommunicationUserIdentifier(id: moveRequest.ParticipantToMove)
                : throw new Exception(message: "Invalid participant format, Use phone number (+1234567890) or ACS user ID (8:acs:...).");

        var moveResponse = await targetConnection.MoveParticipantsAsync(
            options: new MoveParticipantsOptions(targetParticipants: new[] { participantToMove }, fromCall: moveRequest.SourceCallConnectionId));

        if (moveResponse.GetRawResponse().Status is >= 200 and <= 299)
        {
            logger.LogInformation(message: "Move Participants operation completed successfully.");
        }
        else
        {
            throw new Exception(message: $"Move Participants operation failed with status code: {moveResponse.GetRawResponse().Status}");
        }

        return Results.Text(content: "Success!", contentType: "text/plain");
    }
    catch (Exception ex)
    {
        logger.LogError(message: $"Error in /MoveParticipant: {ex.Message}");
        throw;
    }
}).WithTags(tags: "Move Participants APIs");

app.MapGet(pattern: "/GetParticipants/{callConnectionId}", handler: async (string callConnectionId, ILogger<Program> logger) =>
{
    logger.LogInformation(message: $"~~~~~~~~~~~~ /GetParticipants/{callConnectionId} ~~~~~~~~~~~~");
    try
    {
        var callConnection = callAutomationClient.GetCallConnection(callConnectionId: callConnectionId);
        var participantsResponse = await callConnection.GetParticipantsAsync();

        var participantInfoList = participantsResponse.Value
            .Select(selector: p => new
            {
                RawId = p.Identifier.RawId,
                IdentifierType = p.Identifier.GetType().Name,
                PhoneNumber = p.Identifier is PhoneNumberIdentifier phone ? phone.PhoneNumber : null,
                AcsUserId = p.Identifier is CommunicationUserIdentifier user ? user.Id : null,
            })
            .OrderBy(keySelector: p => p.AcsUserId)
            .Select(selector: (p, index) => $"{index + 1}. {(string.IsNullOrWhiteSpace(value: p.AcsUserId) ? $"{p.IdentifierType} - RawId: {p.RawId}, Phone: {p.PhoneNumber}" : $"{p.IdentifierType} - RawId: {p.AcsUserId}")}");

        if (!participantInfoList.Any())
        {
            return Results.NotFound(value: new
            {
                Message = "No participants found for the specified call connection.",
                CallConnectionId = callConnectionId
            });
        }

        var infoText = $"""
            No of Participants: {participantInfoList.Count()}
            Participants:
            -------------
            {string.Join(separator: "\n", values: participantInfoList)}
            """;
        logger.LogInformation(message: infoText);
        return Results.Text(content: infoText, contentType: "text/plain");
    }
    catch (Exception ex)
    {
        logger.LogError(message: $"Error getting participants for call {callConnectionId}: {ex.Message}");
        throw;
    }
}).WithTags(tags: "Move Participants APIs");

app.Run();

static async Task<CreateCallResult> CreateCallAsync(
    CallAutomationClient callAutomationClient,
    string fromPhoneNumber,
    string toPhoneNumber,
    string callbackUriHost,
    string? operationContext = null)
{
    var callbackUri = new Uri(baseUri: new Uri(uriString: callbackUriHost), relativeUri: "/api/callbacks");
    var caller = new PhoneNumberIdentifier(phoneNumber: fromPhoneNumber);
    var callInvite = new CallInvite(targetPhoneNumberIdentity: new PhoneNumberIdentifier(phoneNumber: toPhoneNumber), callerIdNumber: caller);
    var createCallOptions = new CreateCallOptions(callInvite: callInvite, callbackUri: callbackUri)
    {
        OperationContext = operationContext
    };
    return await callAutomationClient.CreateCallAsync(options: createCallOptions);
}

static async Task RedirectCallAsync(
    CallAutomationClient callAutomationClient,
    string incomingCallContext,
    string targetAcsUserId)
{
    var callInvite = new CallInvite(targetIdentity: new CommunicationUserIdentifier(id: targetAcsUserId));
    await callAutomationClient.RedirectCallAsync(incomingCallContext: incomingCallContext, callInvite: callInvite);
}

public class MoveParticipantsRequest
{
    public string ParticipantToMove { get; set; } = string.Empty;
    public string SourceCallConnectionId { get; set; } = string.Empty;
    public string TargetCallConnectionId { get; set; } = string.Empty;
}