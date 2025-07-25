using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using System.Text;

#region Bootstrap
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
#endregion

#region Global Variables for Move Participants Scenario

string
    // Configuration variables
    acsConnectionString = 
        builder.Configuration["acsConnectionString"] 
        ?? throw new ArgumentNullException("acsConnectionString"),
    callbackUriHost = 
        builder.Configuration["callbackUriHost"] 
        ?? throw new ArgumentNullException("callbackUriHost"),

    // Phone numbers and identities for Move Participants scenario
    acsOutboundPhoneNumber = 
        builder.Configuration["acsOutboundPhoneNumber"] 
        ?? throw new ArgumentNullException("acsOutboundPhoneNumber"),
    acsInboundPhoneNumber = 
        builder.Configuration["acsInboundPhoneNumber"]  
        ?? throw new ArgumentNullException("acsInboundPhoneNumber"),
    acsUserPhoneNumber = 
        builder.Configuration["acsUserPhoneNumber"] 
        ?? throw new ArgumentNullException("acsUserPhoneNumber"),
    acsTestIdentity2 = 
        builder.Configuration["acsTestIdentity2"] 
        ?? throw new ArgumentNullException("acsTestIdentity2"),
    acsTestIdentity3 =  
        builder.Configuration["acsTestIdentity3"] 
        ?? throw new ArgumentNullException("acsTestIdentity3"),

    // Track which type of workflow call was last created
    lastWorkflowCallType = string.Empty, // "CallTwo" or "CallThree"

    // Call connection IDs
    callConnectionId = string.Empty,
    callConnectionId1 = string.Empty, // User's incoming call
    callConnectionId2 = string.Empty; // ACS user's redirected call

CallAutomationClient client =  new(connectionString: acsConnectionString);
#endregion

#region Configuration Endpoint

app.MapPost("/setConfigurations", (ConfigurationRequest configurationRequest, ILogger<Program> logger) =>
{
    if (configurationRequest != null)
    {
        acsConnectionString = 
            !string.IsNullOrEmpty(configurationRequest.AcsConnectionString) 
                ? configurationRequest.AcsConnectionString 
                : throw new ArgumentNullException(nameof(configurationRequest.AcsConnectionString));
        callbackUriHost = 
            !string.IsNullOrEmpty(configurationRequest.CallbackUriHost) 
                ? configurationRequest.CallbackUriHost 
                : throw new ArgumentNullException(nameof(configurationRequest.CallbackUriHost));
        acsOutboundPhoneNumber = 
            !string.IsNullOrEmpty(configurationRequest.AcsOutboundPhoneNumber) 
                ? configurationRequest.AcsOutboundPhoneNumber 
                : throw new ArgumentNullException(nameof(configurationRequest.AcsOutboundPhoneNumber));
        acsInboundPhoneNumber = 
            !string.IsNullOrEmpty(configurationRequest.AcsInboundPhoneNumber) 
                ? configurationRequest.AcsInboundPhoneNumber 
                : throw new ArgumentNullException(nameof(configurationRequest.AcsInboundPhoneNumber));
        acsUserPhoneNumber = 
            !string.IsNullOrEmpty(configurationRequest.AcsUserPhoneNumber) 
                ? configurationRequest.AcsUserPhoneNumber 
                : throw new ArgumentNullException(nameof(configurationRequest.AcsUserPhoneNumber));
        acsTestIdentity2 = 
            !string.IsNullOrEmpty(configurationRequest.AcsTestIdentity2) 
                ? configurationRequest.AcsTestIdentity2 
                : throw new ArgumentNullException(nameof(configurationRequest.AcsTestIdentity2));
        acsTestIdentity3 = 
            !string.IsNullOrEmpty(configurationRequest.AcsTestIdentity3) 
                ? configurationRequest.AcsTestIdentity3 
                : throw new ArgumentNullException(nameof(configurationRequest.AcsTestIdentity3));
    }

    string logMsg = """
        Configuration is set successfully. 
        Initialized call automation client.
        """;
    Console.WriteLine(logMsg);
    return Results.Ok(logMsg);
}).WithTags("Configuration");

#endregion

#region Move Participants Event Handler

app.MapPost("/api/MoveParticipantEvent", async (EventGridEvent[] eventGridEvents, ILogger<Program> logger) =>
{
    StringBuilder msgLog = new(); // to make string builder thread-safe; declared here
    msgLog.AppendLine("""

            ~~~~~~~~~~~~ /api/MoveParticipantEvent (Event Grid Hook)  ~~~~~~~~~~~~
        """);
    foreach (var eventGridEvent in eventGridEvents)
    {
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
            if (eventData is AcsIncomingCallEventData incomingCallEventData)
            {
                msgLog.AppendLine($"Event received: {eventGridEvent.EventType}");
                
                string 
                    fromCallerId = incomingCallEventData.FromCommunicationIdentifier.RawId,
                    toCallerId = incomingCallEventData.ToCommunicationIdentifier.RawId;
                
                // Call 1: User calls from their phone number to ACS inbound number
                if (fromCallerId.Contains(acsUserPhoneNumber))
                {
                    Uri callbackUri = new (new Uri(callbackUriHost), $"/api/callbacks");
                    AnswerCallOptions options = new (incomingCallEventData.IncomingCallContext, callbackUri)
                    {
                        OperationContext = "IncomingCallFromUser"
                    };

                    AnswerCallResult answerCallResult = await client.AnswerCallAsync(options);
                    callConnectionId1 = answerCallResult.CallConnection.CallConnectionId;

                    msgLog.AppendLine($"""
                        User Call Answered by Call Automation.
                        From Caller Raw Id: {fromCallerId}
                        To Caller Raw Id:   {toCallerId}
                        Internal Connection Id: {callConnectionId1}
                        Correlation Id:         {incomingCallEventData.CorrelationId}
                        """);
                }
                // Call2 2: ACS inbound number calls ACS outbound number (workflow triggered)
                else if (fromCallerId.Contains(acsInboundPhoneNumber))
                {
                    // Check which type of workflow call this is and redirect accordingly
                    if (lastWorkflowCallType == "CallTwo")
                    {
                        // Redirect the call to ACS User Identity 2
                        CallInvite callInvite = new(new CommunicationUserIdentifier(acsTestIdentity2));
                        var redirectCallResult = await client.RedirectCallAsync(incomingCallEventData.IncomingCallContext, callInvite);

                        // Console.WriteLine($"Call2 redirected to ACS User Identity 2: {acsTestIdentity2}");
                        msgLog.AppendLine($"""
                            Call2 redirected to ACS User Identity 2: {acsTestIdentity2}
                            From Caller Raw Id: {fromCallerId}
                            To Caller Raw Id  : {toCallerId}
                            """);
                    }
                    else if (lastWorkflowCallType == "CallThree")
                    {
                        //Console.WriteLine($"Processing Call Three - Redirecting to ACS User Identity 3");

                        // Redirect the call to ACS User Identity 3
                        CallInvite callInvite = new (new CommunicationUserIdentifier(acsTestIdentity3));
                        var redirectCallResult = await client.RedirectCallAsync(incomingCallEventData.IncomingCallContext, callInvite);
                        msgLog.AppendLine($"""
                            Call3 redirected to ACS User Identity 3: {acsTestIdentity3}
                            From Caller Raw Id: {fromCallerId}
                            To Caller Raw Id  : {toCallerId}
                            """);
                    }
                    else
                    {
                        logger.LogWarning($"Unknown workflow call type: {lastWorkflowCallType}. Defaulting to Call Two behavior.");

                        // Default to Call Two behavior
                        CallInvite callInvite = new (new CommunicationUserIdentifier(acsTestIdentity2));
                        var redirectCallResult = await client.RedirectCallAsync(incomingCallEventData.IncomingCallContext, callInvite);

                        msgLog.AppendLine($"Default: Redirected to ACS User Identity 2: {acsTestIdentity2}");
                    }
                }
                else
                {
                    //msgLogInEvent.AppendLine($"Call filtered out - not matching expected scenarios");
                }
            }
        }
    }
    var logToSend = msgLog.ToString(); // avoiding multiple logs
    msgLog.Clear();

    Console.WriteLine(logToSend);
    return Results.Text(logToSend, "text/plain");
});

#endregion

#region Main Callback Handler

app.MapPost("/api/callbacks", async (CloudEvent[] cloudEvents, ILogger<Program> logger) =>
{
    StringBuilder msgLog = new(); // to make string builder thread-safe; declared here
    
    foreach (var cloudEvent in cloudEvents)
    {
        CallAutomationEventBase parsedEvent = CallAutomationEventParser.Parse(cloudEvent);
        var callConnection = client.GetCallConnection(parsedEvent.CallConnectionId);
        if (parsedEvent is CallConnected callConnected)
        {
            if ((callConnected.OperationContext??string.Empty).Equals("CallTwo", StringComparison.Ordinal))
            {
                // added logs to avoid multiple logs for the same callback
                msgLog.AppendLine($"""
                        ~~~~~~~~~~~~  /api/callbacks ~~~~~~~~~~~~ 
                    Received call event  : {parsedEvent.GetType()}
                    Call 2 Internal Connection Id: {callConnected.CallConnectionId}
                    Correlation Id:                {callConnected.CorrelationId}
                    """);
            }
            else if ((callConnected.OperationContext??string.Empty).Equals("CallThree", StringComparison.Ordinal))
            {
                // added logs to avoid multiple logs for the same callback
                msgLog.AppendLine($"""
                        ~~~~~~~~~~~~  /api/callbacks ~~~~~~~~~~~~ 
                    Received call event  : {parsedEvent.GetType()}
                    Call 3 Internal Connection Id: {callConnected.CallConnectionId}
                                       Correlation Id: {callConnected.CorrelationId}
                    """);
            }
        }
        else if (parsedEvent is CallDisconnected callDisconnected)
        {
            // added logs to avoid multiple logs for the same callback
            msgLog.AppendLine($"""
                        ~~~~~~~~~~~~  /api/callbacks ~~~~~~~~~~~~ 
                    Received event: {parsedEvent.GetType()}
                    Call Connection Id: {callDisconnected.CallConnectionId}
                    
                    """);
        }
        else
        {
            // msgLog.AppendLine($"Received event: {parsedEvent.GetType()}");
        }
    }
    // Log the final message
    if (0 != msgLog.Length)
    {
        Console.WriteLine(msgLog.ToString());
    }
    return Results.Text((0 == msgLog.Length) ? string.Empty : msgLog.ToString(), "text/plain");
}).Produces(StatusCodes.Status200OK);

#endregion

#region Move Participants Workflow Endpoints

// Helper endpoints for Move Participants workflow
app.MapPost("/CreateCall1(UserCallToCallAutomation)", async (ILogger<Program> logger) =>
{
    StringBuilder msgLog = new();
    msgLog.AppendLine("""

            ~~~~~~~~~~~~ /CreateCall1(UserCallToCallAutomation)  ~~~~~~~~~~~~
        """);

    Uri callbackUri = new(new Uri(callbackUriHost), "/api/callbacks");
    PhoneNumberIdentifier caller = new (acsUserPhoneNumber);
    CallInvite callInvite = new (new PhoneNumberIdentifier(acsInboundPhoneNumber), caller);
    CreateCallOptions createCallOptions = new(callInvite, callbackUri);
    CreateCallResult createCallResult = await client.CreateCallAsync(createCallOptions);

    callConnectionId = createCallResult.CallConnectionProperties.CallConnectionId;

    msgLog.Append($"""  
        Call 1(External PSTN to Call Automation, then be answered by Call Automation):
        ------------------------------------------------------------------------------
        From: {acsUserPhoneNumber}
        To:   {acsInboundPhoneNumber}
        Target Call Connection Id: {callConnectionId}
        Correlation Id:            {createCallResult.CallConnectionProperties.CorrelationId}        
        """);

    Console.WriteLine(msgLog.ToString());
    return Results.Text(msgLog.ToString(), "text/plain");
}).WithTags("Move Participants APIs");

app.MapPost("/CreateCall2(ToPstnUserFirstAndRedirectToAcsIentity)", async (ILogger<Program> logger) =>
{
    StringBuilder msgLog = new();
    msgLog.AppendLine("""

            ~~~~~~~~~~~~ /CreateCall2(ToPstnUserFirstAndRedirectToAcsIentity) ~~~~~~~~~~~~
        """);
    Uri callbackUri = new(new Uri(callbackUriHost), $"/api/callbacks");
    PhoneNumberIdentifier caller = new(acsInboundPhoneNumber);
    CallInvite callInvite = new (new PhoneNumberIdentifier(acsOutboundPhoneNumber), caller);
    CreateCallOptions createCallOptions = new (callInvite, callbackUri)
    {
        OperationContext = "CallTwo"
    };

    CreateCallResult createCallResult = await client.CreateCallAsync(createCallOptions);
    lastWorkflowCallType = "CallTwo"; // Track this as Call 2
    callConnectionId1 = createCallResult.CallConnectionProperties.CallConnectionId;

    msgLog.AppendLine($"""
        Call 2:
        -------
        From: {acsInboundPhoneNumber} 
        To:   {acsOutboundPhoneNumber}
        Source Call Connection Id: {callConnectionId1}
        Correlation Id:            {createCallResult.CallConnectionProperties.CorrelationId}
        Rediect Call2 to: {acsTestIdentity2}
        """);
    Console.WriteLine(msgLog.ToString());
    return Results.Text(msgLog.ToString(), "text/plain");
}).WithTags("Move Participants APIs");

app.MapPost("/CreateCall3(ToPstnUserFirstAndRedirectToAcsIentity)", async (ILogger<Program> logger) =>
{
    StringBuilder msgLog = new();
    msgLog.AppendLine("""

            ~~~~~~~~~~~~ /CreateCall3(ToPstnUserFirstAndRedirectToAcsIentity)  ~~~~~~~~~~~~
        """);
    Uri callbackUri = new(new Uri(callbackUriHost), $"/api/callbacks");
    PhoneNumberIdentifier caller = new(acsInboundPhoneNumber);
    CallInvite callInvite = new(new PhoneNumberIdentifier(acsOutboundPhoneNumber), caller);
    CreateCallOptions createCallOptions = new(callInvite, callbackUri)
    {
        OperationContext = "CallThree"
    };

    CreateCallResult createCallResult = await client.CreateCallAsync(createCallOptions);
    lastWorkflowCallType = "CallThree"; // Track this as Call 3
    callConnectionId2 = createCallResult.CallConnectionProperties.CallConnectionId;

    msgLog.AppendLine($"""
        Call 3:
        -------
        From: {acsInboundPhoneNumber} 
        To:   {acsOutboundPhoneNumber}
        Source Call Connection Id: {callConnectionId2}
        Correlation Id:            {createCallResult.CallConnectionProperties.CorrelationId}
        Rediect Call2 to: {acsTestIdentity3}
        """);

    Console.WriteLine(msgLog.ToString());
    return Results.Text(msgLog.ToString(), "text/plain");
}).WithTags("Move Participants APIs");

app.MapPost("/MoveParticipant", async (MoveParticipantsRequest request, ILogger<Program> logger) =>
{
    StringBuilder msgLog = new();
    msgLog.AppendLine("""

            ~~~~~~~~~~~~ /MoveParticipant Operation ~~~~~~~~~~~~
        """);
    try
    {
        msgLog.AppendLine($"""
            Source Caller Id:     {request.ParticipantToMove}
            Source Connection Id: {request.SourceCallConnectionId}
            Target Connection Id: {request.TargetCallConnectionId}
            """);

        // Get the target connection (where we want to move participants to)
        var targetConnection = client.GetCallConnection(request.TargetCallConnectionId);

        // Get participants from source connection for reference
        var sourceConnection = client.GetCallConnection(request.SourceCallConnectionId);

        // Create participant identifier based on the input
        CommunicationIdentifier participantToMove;
        if (request.ParticipantToMove.StartsWith("+"))
        {
            // Phone number
            participantToMove = new PhoneNumberIdentifier(request.ParticipantToMove);
        }
        else if (request.ParticipantToMove.StartsWith("8:acs:"))
        {
            // ACS Communication User
            participantToMove = new CommunicationUserIdentifier(request.ParticipantToMove);
        }
        else
        {
            return Results.BadRequest("Invalid participant format. Use phone number (+1234567890) or ACS user ID (8:acs:...)");
        }

        var response = await targetConnection.MoveParticipantsAsync(options: new([participantToMove], request.SourceCallConnectionId));
        var rawResponse = response.GetRawResponse();
        if (rawResponse.Status >= 200 && rawResponse.Status <= 299)
        {
            msgLog.AppendLine().AppendLine("Move Participants operation completed successfully.");
        }
        else
        {
            throw new Exception($"Move Participants operation failed with status code: {rawResponse.Status}");
        }

        Console.WriteLine(msgLog.ToString());
        return Results.Text(msgLog.ToString(), "text/plain");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error in manual move participants operation: {ex.Message}");
        return Results.BadRequest(new
        {
            Success = false,
            Error = ex.Message,
            Message = "Move participants operation failed."
        });
    }
}).WithTags("Move Participants APIs");

app.MapGet("/GetParticipants/{callConnectionId}", async (string callConnectionId, ILogger<Program> logger) =>
{
    StringBuilder msgLog = new();
    msgLog.AppendLine($"""

            ~~~~~~~~~~~~ /GetParticipants/{callConnectionId} ~~~~~~~~~~~~
        """);
    try
    {
        var callConnection = client.GetCallConnection(callConnectionId);
        var participants = await callConnection.GetParticipantsAsync();

        var participantinfo = participants.Value.Select(p => new
        {
            p.Identifier.RawId,
            Type = p.Identifier.GetType().Name,
            PhoneNumber = p.Identifier is PhoneNumberIdentifier phone ? phone.PhoneNumber : null,
            AcsUserId = p.Identifier is CommunicationUserIdentifier user ? user.Id : null,
        }).OrderBy(p => p.AcsUserId) // to display phone numbers first
            .Select(p => new
            {
                Info = string.IsNullOrWhiteSpace(p.AcsUserId)
                    ? $"{p.Type}       - RawId: {p.RawId}, Phone: {p.PhoneNumber}" // extra space for alignment
                    : $"{p.Type} - RawId: {p.AcsUserId}"
            });

        if (!participantinfo.Any())
        {
            return Results.NotFound(new
            {
                Message = "No participants found for the specified call connection.",
                CallConnectionId = callConnectionId
            });
        }
        else
        {
            msgLog.AppendLine($"""

            No of Participants: {participantinfo.Count()}
            Participants: 
            -------------
            {string.Join("\n", participantinfo.Select((p, index) => $"{index + 1}. {p.Info}"))}
            """);
            Console.WriteLine(msgLog.ToString());
            return Results.Text(msgLog.ToString(), "text/plain");
        }
    }
    catch (Exception ex)
    {
        logger.LogError($"Error getting participants for call {callConnectionId}: {ex.Message}");
        return Results.BadRequest(new
        {
            Error = ex.Message,
            CallConnectionId = callConnectionId
        });
    }
}).WithTags("Move Participants APIs");

#endregion 

app.Run();

public class ConfigurationRequest
{
    public string? AcsConnectionString { get; set; }
    public string? CallbackUriHost { get; set; }
    public string? AcsOutboundPhoneNumber { get; set; }
    public string? AcsInboundPhoneNumber { get; set; }
    public string? AcsUserPhoneNumber { get; set; }
    public string? AcsTestIdentity2 { get; set; }
    public string? AcsTestIdentity3 { get; set; }
}

public class MoveParticipantsRequest
{
    public string ParticipantToMove { get; set; } = string.Empty;
    public string SourceCallConnectionId { get; set; } = string.Empty;
    public string TargetCallConnectionId { get; set; } = string.Empty;
}