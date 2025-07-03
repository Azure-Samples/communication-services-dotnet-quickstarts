using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;

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

string acsConnectionString = 
    "endpoint=https://dacsrecordingtest.unitedstates.communication.azure.com/;accesskey=9lMdkVL4KcqJ3YXGgWS9Fxa1CjPwXs63rEMczJ7DsC9mbWR3hlbtJQQJ99BEACULyCpAArohAAAAAZCS58G3",
    
    // Call connection IDs
    callConnectionId = string.Empty,
    callConnectionId1 = string.Empty, // User's incoming call
    callConnectionId2 = string.Empty, // ACS user's redirected call
    
    // Configuration variables
    callbackUriHost = "https://r4k1s43h-7006.inc1.devtunnels.ms",
    pmaEndpoint     = "https://uswc-02.sdf.pma.teams.microsoft.com",
    
    // Track which type of workflow call was last created
    lastWorkflowCallType = string.Empty, // "CallTwo" or "CallThree"
    
    // Phone numbers and identities for Move Participants scenario
    acsOutboundPhoneNumber = "+18772119545",
    acsInboundPhoneNumber  = "+18332638155", //"+14258007525",
    userPhoneNumber        = "+18662314080",
    acsTestIdentity2       = "8:acs:19ae37ff-1a44-4e19-aade-198eedddbdf2_00000028-704f-6f77-5c4b-5c3a0d004063",
    acsTestIdentity3       = "8:acs:19ae37ff-1a44-4e19-aade-198eedddbdf2_00000028-704f-9bfb-3441-5c3a0d003ab2";

CallAutomationClient client = 
    new(pmaEndpoint: new Uri(pmaEndpoint), connectionString: acsConnectionString);
#endregion

#region Helper Functions
CallConnectionProperties? GetCallConnectionProperties()
{
    if (!string.IsNullOrWhiteSpace(callConnectionId))
    {
        CallConnection connection = client.GetCallConnection(callConnectionId);
        return connection.GetCallConnectionProperties();
    }
    return null;
}

#endregion

#region Configuration Endpoint

app.MapPost("/setConfigurations", (ConfigurationRequest configurationRequest, ILogger<Program> logger) =>
{
    if (configurationRequest != null)
    {
        acsConnectionString = !string.IsNullOrEmpty(configurationRequest.AcsConnectionString) ? configurationRequest.AcsConnectionString : throw new ArgumentNullException(nameof(configurationRequest.AcsConnectionString));
        acsOutboundPhoneNumber = !string.IsNullOrEmpty(configurationRequest.AcsPhoneNumber) ? configurationRequest.AcsPhoneNumber : throw new ArgumentNullException(nameof(configurationRequest.AcsPhoneNumber));
        acsInboundPhoneNumber = !string.IsNullOrEmpty(configurationRequest.AcsInboundSender) ? configurationRequest.AcsInboundSender : throw new ArgumentNullException(nameof(configurationRequest.AcsInboundSender));
        callbackUriHost = !string.IsNullOrEmpty(configurationRequest.CallbackUriHost) ? configurationRequest.CallbackUriHost : throw new ArgumentNullException(nameof(configurationRequest.CallbackUriHost));
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
    Console.WriteLine("\n\t --------- \"/api/MoveParticipantEvent\" -------------------");

    foreach (var eventGridEvent in eventGridEvents)
    {
        Console.WriteLine($"""
            Event name received: {eventGridEvent.EventType}
            """);
            
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
                var fromCallerId = incomingCallEventData.FromCommunicationIdentifier.RawId;
                var toCallerId = incomingCallEventData.ToCommunicationIdentifier.RawId;
                
                Console.WriteLine($"""
                    From Caller Id: {fromCallerId}
                    To Caller Id  : {toCallerId}

                    """);
                
                // Scenario 1: User calls from their phone number to ACS inbound number
                if (fromCallerId.Contains(userPhoneNumber))
                {
                    Console.WriteLine($"=== SCENARIO 1: USER INCOMING CALL ===");
                    
                    var callbackUri = new Uri(new Uri(callbackUriHost), $"/api/callbacks");
                    var options = new AnswerCallOptions(incomingCallEventData.IncomingCallContext, callbackUri)
                    {
                        OperationContext = "IncomingCallFromUser"
                    };

                    AnswerCallResult answerCallResult = await client.AnswerCallAsync(options).ConfigureAwait(false);
                    callConnectionId1 = answerCallResult.CallConnection.CallConnectionId;
                    
                    //Console.WriteLine($"""
                    //    User Call Answered - CallConnectionId1: {callConnectionId1}
                    //    Correlation Id: {incomingCallEventData.CorrelationId}
                    //    Operation Context: IncomingCallFromUser
                    //    === END SCENARIO 1 ===
                    //    """);
                    Console.WriteLine($"User Call Answered - CallConnectionId1: {callConnectionId1}");
                    Console.WriteLine($"Correlation Id: {incomingCallEventData.CorrelationId}");
                    Console.WriteLine($"Operation Context: IncomingCallFromUser");
                    Console.WriteLine($"=== END SCENARIO 1 ===");
                }
                // Scenario 2: ACS inbound number calls ACS outbound number (workflow triggered)
                else if (fromCallerId.Contains(acsInboundPhoneNumber))
                {
                    Console.WriteLine($"=== SCENARIO 2: WORKFLOW CALL TO BE REDIRECTED ===");
                    Console.WriteLine($"Last Workflow Call Type: {lastWorkflowCallType}");
                    
                    // Check which type of workflow call this is and redirect accordingly
                    if (lastWorkflowCallType == "CallTwo")
                    {
                        Console.WriteLine($"Processing Call Two - Redirecting to ACS User Identity 2");
                        
                        // Redirect the call to ACS User Identity 2
                        CallInvite callInvite = new CallInvite(new CommunicationUserIdentifier(acsTestIdentity2));
                        var redirectCallResult = await client.RedirectCallAsync(incomingCallEventData.IncomingCallContext, callInvite);
                         
                        Console.WriteLine($"Call Two Redirected to ACS User Identity 2: {acsTestIdentity2}");
                        Console.WriteLine($"Correlation Id: {incomingCallEventData.CorrelationId}");
                        Console.WriteLine($"Operation Context: BeforeRedirect");
                    }
                    else if (lastWorkflowCallType == "CallThree")
                    {
                        Console.WriteLine($"Processing Call Three - Redirecting to ACS User Identity 3");
                        
                        // Redirect the call to ACS User Identity 3
                        CallInvite callInvite = new CallInvite(new CommunicationUserIdentifier(acsTestIdentity3));
                        var redirectCallResult = await client.RedirectCallAsync(incomingCallEventData.IncomingCallContext, callInvite);
                         
                        Console.WriteLine($"Call Three Redirected to ACS User Identity 3: {acsTestIdentity3}");
                        Console.WriteLine($"Correlation Id: {incomingCallEventData.CorrelationId}");
                        Console.WriteLine($"Operation Context: CallThree");
                    }
                    else
                    {
                        logger.LogWarning($"Unknown workflow call type: {lastWorkflowCallType}. Defaulting to Call Two behavior.");
                        
                        // Default to Call Two behavior
                        CallInvite callInvite = new CallInvite(new CommunicationUserIdentifier(acsTestIdentity2));
                        var redirectCallResult = await client.RedirectCallAsync(incomingCallEventData.IncomingCallContext, callInvite);
                         
                        Console.WriteLine($"Default: Redirected to ACS User Identity 2: {acsTestIdentity2}");
                    }
                    
                    Console.WriteLine($"=== END SCENARIO 2 ===");
                }
                else
                {
                    Console.WriteLine($"Call filtered out - not matching expected scenarios");
                    Console.WriteLine($"Expected: User ({userPhoneNumber}) or ACS Inbound ({acsInboundPhoneNumber})");
                    Console.WriteLine($"Received: {fromCallerId}");
                }
            }
        }
    }
    return Results.Ok();
});

#endregion

#region Main Callback Handler

app.MapPost("/api/callbacks", async (CloudEvent[] cloudEvents, ILogger<Program> logger) =>
{
    Console.WriteLine("\n\t --------- \"/api/callbacks\" -------------------");

    foreach (var cloudEvent in cloudEvents)
    {
        CallAutomationEventBase parsedEvent = CallAutomationEventParser.Parse(cloudEvent);
        Console.WriteLine($"Received call event: {parsedEvent.GetType()}, \n callConnectionId: {parsedEvent.CallConnectionId}, \n serverCallId: {parsedEvent.ServerCallId}");

        var callConnection = client.GetCallConnection(parsedEvent.CallConnectionId);

        if (parsedEvent is CallConnected callConnected)
        {
            Console.WriteLine("\n--------- CallConnected Event Block -------------------");
            /* CallConnectionProperties properties = await callConnection.GetCallConnectionPropertiesAsync().ConfigureAwait(false);
            Console.WriteLine("Fetched Props");
            if (properties == null)
            {
                Console.WriteLine("CallConnectionProperties is null for CallConnected event.");
                logger.LogError("CallConnectionProperties is null for CallConnected event.");
                return Results.BadRequest("CallConnectionProperties is null.");
            }
            */
            // Console.WriteLine($"Received call event: {callConnected.GetType()}");
            Console.WriteLine($"Operation Context: {callConnected.OperationContext}");

            callConnectionId = callConnected.CallConnectionId;  // TODO: 
            CallConnectionProperties callConnectionProperties = GetCallConnectionProperties();

            //Console.WriteLine("************************************************************");
            Console.WriteLine($"Correlation Id: {callConnectionProperties.CorrelationId}");
            //Console.WriteLine($"Call Connection Id: {properties.CallConnectionId}");
            //Console.WriteLine("************************************************************");

            // Handle different operation contexts - simplified without Move Participants logic
            switch (callConnected.OperationContext)
            {
                case "IncomingCallFromUser":
                    Console.WriteLine($"=== CALL ONE CONNECTED ===");
                    Console.WriteLine($"User call connected - CallConnectionId1: {callConnectionId}");
                    callConnectionId1 = callConnectionId;
                    Console.WriteLine($"=== END CALL ONE CONNECTED ===");
                    break;

                default:
                    callConnectionId2 = callConnectionId;
                    Console.WriteLine($"=== CALL TWO/THREE CONNECTED (AFTER REDIRECT) ===");
                    Console.WriteLine($"ACS User call connected - CallConnectionId2: {callConnectionId}");
                    Console.WriteLine($"Call connected. Use /MoveParticipants API to manually move participants.");
                    Console.WriteLine($"=== END CALL TWO/THREE CONNECTED ===");
                    break;
            }
        }
        else if (parsedEvent is CallDisconnected callDisconnected)
        {
            Console.WriteLine($"Call disconnected: {callDisconnected.CallConnectionId}");
        }
        else
        {
            // Log other events but don't process them for Move Participants scenario
            Console.WriteLine($"Received event: {parsedEvent.GetType()} - No action needed for Move Participants scenario");
        }
    }
    return Results.Ok();
}).Produces(StatusCodes.Status200OK);

#endregion

#region Move Participants Workflow Endpoints

// Helper endpoints for Move Participants workflow
app.MapPost("/UserCallToCallAutomation", async (ILogger<Program> logger) =>
{
    Console.WriteLine("\n\t --------- \"/UserCallToCallAutomation - Call 1 API Endpoint \" -------------------");
    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    PhoneNumberIdentifier caller = new PhoneNumberIdentifier(userPhoneNumber);
    CallInvite callInvite = new CallInvite(new PhoneNumberIdentifier(acsInboundPhoneNumber), caller);
    var createCallOptions = new CreateCallOptions(callInvite, callbackUri);
    CreateCallResult createCallResult = await client.CreateCallAsync(createCallOptions);
    // Console.WriteLine($"Created async acs call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");

    Console.WriteLine($"=== Call From User to Call Automation ===");
    Console.WriteLine($"Created call from {userPhoneNumber} to {acsOutboundPhoneNumber}");
    Console.WriteLine($"Connection ID: {createCallResult.CallConnectionProperties.CallConnectionId}");
    Console.WriteLine($"=== END WORKFLOW INITIATION ===");
}).WithTags("Move Participants APIs");

app.MapPost("/CreateCallForMoveParticipantsWorkflow", async(ILogger<Program> logger) =>
{
    Console.WriteLine("\n\t --------- \"/CreateCallForMoveParticipantsWorkflow - Call 2 API Endpoint \" -------------------");
    // Create the outbound call from acsInboundPhoneNumber to acsOutboundPhoneNumber
    // This will trigger the Move Participant Event (Scenario 2) which will redirect to ACS user
    var callbackUri = new Uri(new Uri(callbackUriHost), $"/api/callbacks");
    PhoneNumberIdentifier caller = new PhoneNumberIdentifier(acsInboundPhoneNumber);
    CallInvite callInvite = new CallInvite(new PhoneNumberIdentifier(acsOutboundPhoneNumber), caller);
    var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
    {
        OperationContext = "BeforeRedirect"
    };
    
    CreateCallResult createCallResult = await client.CreateCallAsync(createCallOptions);
    lastWorkflowCallType = "CallTwo"; // Track this as Call 2
    Console.WriteLine($"=== MOVE PARTICIPANTS WORKFLOW INITIATED ===");
    Console.WriteLine($"Created call from {acsInboundPhoneNumber} to {acsOutboundPhoneNumber}");
    Console.WriteLine($"Connection ID: {createCallResult.CallConnectionProperties.CallConnectionId}");
    Console.WriteLine($"This call should trigger MoveParticipantEvent (Scenario 2) which will redirect to ACS user {acsTestIdentity2}");
    Console.WriteLine($"Operation Context: BeforeRedirect");
    Console.WriteLine($"=== END WORKFLOW INITIATION ===");
    
    return Results.Ok(new { 
        ConnectionId = createCallResult.CallConnectionProperties.CallConnectionId,
        FromNumber = acsInboundPhoneNumber,
        ToNumber = acsOutboundPhoneNumber,
        RedirectTarget = acsTestIdentity2,
        Message = "Move Participants workflow initiated. Call created and will be redirected to ACS user."
    });
}).WithTags("Move Participants APIs");

app.MapPost("/CreateCallThreeForMoveParticipantsWorkflow", async (ILogger<Program> logger) =>
{
    Console.WriteLine("\n\t --------- \"/CreateCallThreeForMoveParticipantsWorkflow - Call 3 API Endpoint \" -------------------");

    // Create the outbound call from acsInboundPhoneNumber to acsOutboundPhoneNumber (Call 3)
    // This will trigger the Move Participant Event (Scenario 2) but redirect to ACS User Identity 3
    var callbackUri = new Uri(new Uri(callbackUriHost), $"/api/callbacks");
    PhoneNumberIdentifier caller = new PhoneNumberIdentifier(acsInboundPhoneNumber);
    CallInvite callInvite = new CallInvite(new PhoneNumberIdentifier(acsOutboundPhoneNumber), caller);
    var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
    {
        OperationContext = "CallThree"
    };
    
    CreateCallResult createCallResult = await client.CreateCallAsync(createCallOptions);
    lastWorkflowCallType = "CallThree"; // Track this as Call 3
    Console.WriteLine($"=== CALL THREE WORKFLOW INITIATED ===");
    Console.WriteLine($"Created Call 3 from {acsInboundPhoneNumber} to {acsOutboundPhoneNumber}");
    Console.WriteLine($"Connection ID: {createCallResult.CallConnectionProperties.CallConnectionId}");
    Console.WriteLine($"This call should trigger MoveParticipantEvent (Scenario 2) which will redirect to ACS user {acsTestIdentity3}");
    Console.WriteLine($"Operation Context: CallThree");
    Console.WriteLine($"=== END CALL THREE INITIATION ===");
    
    return Results.Ok(new { 
        ConnectionId = createCallResult.CallConnectionProperties.CallConnectionId,
        FromNumber = acsInboundPhoneNumber,
        ToNumber = acsOutboundPhoneNumber,
        RedirectTarget = acsTestIdentity3,
        Message = "Call Three workflow initiated. Call created and will be redirected to ACS User Identity 3."
    });
}).WithTags("Move Participants APIs");

app.MapPost("/MoveParticipants", async (MoveParticipantsRequest request, ILogger<Program> logger) =>
{
    Console.WriteLine("\n\t --------- \"/MoveParticipants API End Point \" -------------------");

    try
    {
        Console.WriteLine($"=== MANUAL MOVE PARTICIPANTS REQUESTED ===");
        Console.WriteLine($"Source Connection ID: {request.SourceCallConnectionId}");
        Console.WriteLine($"Target Connection ID: {request.TargetCallConnectionId}");
        Console.WriteLine($"Participant to Move: {request.ParticipantToMove}");
        
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
            Console.WriteLine($"Moving phone number participant: {request.ParticipantToMove}");
        }
        else if (request.ParticipantToMove.StartsWith("8:acs:"))
        {
            // ACS Communication User
            participantToMove = new CommunicationUserIdentifier(request.ParticipantToMove);
            Console.WriteLine($"Moving ACS user participant: {request.ParticipantToMove}");
        }
        else
        {
            return Results.BadRequest("Invalid participant format. Use phone number (+1234567890) or ACS user ID (8:acs:...)");
        }
        
        var options = new MoveParticipantsOptions(
            new[] { participantToMove },
            request.SourceCallConnectionId);
        
        var moveParticipantsResult = targetConnection.MoveParticipants(options);
        Console.WriteLine($"Move Participants operation completed successfully");
        Console.WriteLine($"Moved {request.ParticipantToMove} from {request.SourceCallConnectionId} to {request.TargetCallConnectionId}");
        Console.WriteLine($"=== MOVE PARTICIPANTS OPERATION COMPLETE ===");
        
        return Results.Ok(new 
        {
            Success = true,
            Message = $"Successfully moved participant {request.ParticipantToMove} from {request.SourceCallConnectionId} to {request.TargetCallConnectionId}",
            SourceConnectionId = request.SourceCallConnectionId,
            TargetConnectionId = request.TargetCallConnectionId,
            ParticipantMoved = request.ParticipantToMove
        });
    }
    catch (Exception ex)
    {
        logger.LogError($"Error in manual move participants operation: {ex.Message}");
        return Results.BadRequest(new 
        {
            Success = false,
            Error = ex.Message,
            Message = "Move participants operation failed"
        });
    }
}).WithTags("Move Participants APIs");

app.MapGet("/GetParticipants/{callConnectionId}", async (string callConnectionId, ILogger<Program> logger) =>
{
    Console.WriteLine("\n\t --------- \"/GetParticipants/{callConnectionId} API End Point \" -------------------");
    try
    {
        Console.WriteLine($"Getting participants for call connection: {callConnectionId}");
        
        var callConnection = client.GetCallConnection(callConnectionId);
        var participants = await callConnection.GetParticipantsAsync();
        
        var participantList = participants.Value.Select(p => new
        {
            RawId = p.Identifier.RawId,
            Type = p.Identifier.GetType().Name,
            PhoneNumber = p.Identifier is PhoneNumberIdentifier phone ? phone.PhoneNumber : null,
            AcsUserId = p.Identifier is CommunicationUserIdentifier user ? user.Id : null,
            IsMuted = p.IsMuted,
            IsOnHold = p.IsOnHold
        }).ToList();
        
        Console.WriteLine($"Found {participantList.Count} participants in call {callConnectionId}");
        
        return Results.Ok(new
        {
            CallConnectionId = callConnectionId,
            ParticipantCount = participantList.Count,
            Participants = participantList
        });
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

app.MapGet("/GetMoveParticipantsStatus", (ILogger<Program> logger) =>
{
    Console.WriteLine("\n\t --------- \"/GetMoveParticipantsStatus API End Point \" -------------------");

    var status = new
    {
        CallConnectionId1 = callConnectionId1,
        CallConnectionId2 = callConnectionId2,
        UserPhoneNumber = userPhoneNumber,
        AcsInboundPhoneNumber = acsInboundPhoneNumber,
        AcsOutboundPhoneNumber = acsOutboundPhoneNumber,
        AcsTestIdentity2 = acsTestIdentity2,
        AcsTestIdentity3 = acsTestIdentity3,
        LastWorkflowCallType = lastWorkflowCallType,
        Status = string.IsNullOrEmpty(callConnectionId1) ? "Waiting for incoming call from user" :
                string.IsNullOrEmpty(callConnectionId2) ? "Waiting for outbound call to be redirected to ACS user" :
                "Both calls connected - ready for manual Move Participants",
        Instructions = new
        {
            Step1 = $"Configure Event Grid webhook to point to /api/MoveParticipantEvent endpoint",
            Step2 = $"Make a call from {userPhoneNumber} to {acsInboundPhoneNumber}",
            Step3a = $"For Call Two: Use Swagger to trigger /CreateCallForMoveParticipantsWorkflow endpoint",
            Step3b = $"For Call Three: Use Swagger to trigger /CreateCallThreeForMoveParticipantsWorkflow endpoint",  
            Step4a = $"If Call Two: Answer the call to {acsTestIdentity2} from your ACS client app",
            Step4b = $"If Call Three: Answer the call to {acsTestIdentity3} from your ACS client app",
            Step5 = "Manually trigger /MoveParticipants API with connection IDs and participant to move",
            Step6 = "Can trigger multiple Move Participants operations as needed"
        }
    };
    
    Console.WriteLine($"Move Participants Status: {status.Status}");
    return Results.Ok(status);
}).WithTags("Move Participants APIs");

#endregion 

app.Run();

public class ConfigurationRequest
{
    public string? AcsConnectionString { get; set; }
    public string? CongnitiveServiceEndpoint { get; set; }
    public string? AcsPhoneNumber { get; set; }
    public string? AcsInboundSender { get; set; }
    public string? CallbackUriHost { get; set; }
    public string? ACSUserIdentity2 { get; set; }
    public string? ACSUserIdentity3 { get; set; }
}

public class MoveParticipantsRequest
{
    public string SourceCallConnectionId { get; set; } = string.Empty;
    public string TargetCallConnectionId { get; set; } = string.Empty;
    public string ParticipantToMove { get; set; } = string.Empty;
}