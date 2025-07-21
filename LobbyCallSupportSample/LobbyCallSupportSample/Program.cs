using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Core;
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

#region Global Variables for LobbyCallSupportSample

string
    // Configuration variables
    acsConnectionString = 
        builder.Configuration["acsConnectionString"] 
        ?? throw new ArgumentNullException("acsConnectionString"),
    callbackUriHost = 
        builder.Configuration["callbackUriHost"] 
        ?? throw new ArgumentNullException("callbackUriHost"),
    pmaEndpoint = 
        builder.Configuration["pmaEndpoint"] 
        ?? throw new ArgumentNullException("pmaEndpoint"),
    acsGeneratedId =
        builder.Configuration["acsGeneratedId"]
        ?? throw new ArgumentNullException("acsGeneratedId"),
        
    // Track which type of workflow call was last created
    lastWorkflowCallType = string.Empty, // "CallTwo" or "CallThree"
    acsIdentity = string.Empty,
    // Call connection IDs
    targetCallConnectionId = string.Empty,
    lobbyConnectionId = string.Empty, // User's incoming call
    lobbyCallerId = string.Empty, // User's incoming call
    callConnectionId2 = string.Empty; // ACS user's redirected call

CallAutomationClient client =
    new(pmaEndpoint: new Uri(pmaEndpoint), connectionString: acsConnectionString);
#endregion

#region Event Handler

app.MapPost("/api/LobbyCallSupportEventHandler", async (EventGridEvent[] eventGridEvents, ILogger<Program> logger) =>
{
    StringBuilder msgLog = new(); // to make string builder thread-safe; declared here
    msgLog.AppendLine("""

            ~~~~~~~~~~~~ /api/LobbyCallSupportEventHandler  ~~~~~~~~~~~~
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
                    fromCallerId = acsIdentity = incomingCallEventData.FromCommunicationIdentifier.RawId,
                    toCallerId = incomingCallEventData.ToCommunicationIdentifier.RawId;

                // Lobby Call: Answer 
                if (toCallerId.Contains(acsGeneratedId))
                {
                    #region Answer Call
                    Uri callbackUri = new (new Uri(callbackUriHost), $"/api/callbacks");
                    AnswerCallOptions options = new (incomingCallEventData.IncomingCallContext, callbackUri)
                    {
                        OperationContext = "LobbyCall", 
                        CallIntelligenceOptions = new CallIntelligenceOptions
                        {
                            CognitiveServicesEndpoint = new Uri("https://cognitive-service-waferwire.cognitiveservices.azure.com/")
                        }
                    };

                    AnswerCallResult answerCallResult = await client.AnswerCallAsync(options);
                    lobbyConnectionId = answerCallResult.CallConnection.CallConnectionId;

                    msgLog.AppendLine($"""
                        User Call(Inbound) Answered by Call Automation.
                        From Caller Raw Id: {fromCallerId}
                        To Caller Raw Id:   {toCallerId}
                        Lobby Call Connection Id: {lobbyConnectionId}
                        Correlation Id:           {incomingCallEventData.CorrelationId}
                        Lobby Call answered successfully.
                        """);
                    #endregion
                }
                
                else
                {
                    //msgLog.AppendLine($"Call filtered out - not matching expected scenarios");
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

#region Callback Handler

app.MapPost("/api/callbacks", async (CloudEvent[] cloudEvents, ILogger<Program> logger) =>
{
    StringBuilder msgLog = new(); // to make string builder thread-safe; declared here
    
    foreach (var cloudEvent in cloudEvents)
    {
        CallAutomationEventBase parsedEvent = CallAutomationEventParser.Parse(cloudEvent);
        var callConnection = client.GetCallConnection(parsedEvent.CallConnectionId);
        if (parsedEvent is CallConnected callConnected)
        {
            Console.WriteLine($"~~~~~~~~~~~~  /api/callbacks ~~~~~~~~~~~~ ");
            Console.WriteLine($"Received callConnected.CallConnectionId : {callConnected.CallConnectionId}");
            if ((callConnected.OperationContext??string.Empty).Equals("LobbyCall", StringComparison.Ordinal))
            {
                // added logs to avoid multiple logs for the same callback
                msgLog.AppendLine($"""
                        ~~~~~~~~~~~~  /api/callbacks ~~~~~~~~~~~~ 
                    Received call event  : {parsedEvent.GetType()}
                    Lobby Call Connection Id: {callConnected.CallConnectionId}
                    Correlation Id:           {callConnected.CorrelationId}
                    """);

                // record lobby caller id and connection id 
                CallConnection lobbyCallConnection = client.GetCallConnection(callConnected.CallConnectionId);
                CallConnectionProperties callConnectionProperties = lobbyCallConnection.GetCallConnectionProperties();
                lobbyCallerId = callConnectionProperties.Source.RawId;
                lobbyConnectionId = callConnectionProperties.CallConnectionId;
                Console.WriteLine($"""
                    Lobby Caller Id:     {lobbyCallerId}
                    Lobby Connection Id: {lobbyConnectionId}
                    """);

                #region Play lobby waiting message
                // setup cognitive service end point
                CallMedia callMedia = !string.IsNullOrEmpty(callConnected.CallConnectionId) ?
                    client.GetCallConnection(callConnected.CallConnectionId).GetCallMedia()
                    : throw new ArgumentNullException("Call connection id is empty");
                TextSource textSource =
                    new("You are currently in a lobby call, we will notify the admin that you are waiting.")
                    {
                        VoiceName = "en-US-NancyNeural"
                    };

                List<CommunicationIdentifier> playTo =
                    new() { new CommunicationUserIdentifier(acsIdentity) };

                PlayOptions playToOptions = new(playSource: textSource, playTo: playTo)
                {
                    OperationContext = "playToContext"
                };
                await callMedia.PlayAsync(playToOptions);
                #endregion
            }
        }
        else if (parsedEvent is PlayCompleted playCompleted)
        {
            // added logs to avoid multiple logs for the same callback
            msgLog.AppendLine($"""
                        ~~~~~~~~~~~~  /api/callbacks ~~~~~~~~~~~~ 
                    Received event: {parsedEvent.GetType()}                    
                    """);

            // TODO: Notify Target Cal user
            // By Recognition or pop up in Client app

            #region Move Participant
            try
            {
                msgLog.AppendLine($"""
                    ~~~~~~~~~~~~  /api/callbacks ~~~~~~~~~~~~
                    Move Participant operation started..
                    Source Caller Id:     {lobbyCallerId}
                    Source Connection Id: {lobbyConnectionId}
                    Target Connection Id: {targetCallConnectionId}
                    """);

                // Get the target connection
                CallConnection targetConnection = client.GetCallConnection(targetCallConnectionId);

                // Get participants from source connection for reference
                CallConnection sourceConnection = client.GetCallConnection(lobbyConnectionId);

                // Create participant identifier based on the input
                CommunicationIdentifier participantToMove;
                if (lobbyCallerId.StartsWith("+"))
                {
                    // Phone number
                    participantToMove = new PhoneNumberIdentifier(lobbyCallerId);
                }
                else if (lobbyCallerId.StartsWith("8:acs:"))
                {
                    // ACS Communication User
                    participantToMove = new CommunicationUserIdentifier(lobbyCallerId);
                }
                else
                {
                    return Results.BadRequest("Invalid participant format. Use phone number (+1234567890) or ACS user ID (8:acs:...)");
                }

                var response = await targetConnection.MoveParticipantsAsync(options: new([participantToMove], lobbyConnectionId));
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
            #endregion
        }
        else if (parsedEvent is MoveParticipantSucceeded moveParticipantSucceeded)
        {
            // added logs to avoid multiple logs for the same callback
            msgLog.AppendLine($"""
                        ~~~~~~~~~~~~  /api/callbacks ~~~~~~~~~~~~ 
                    Received event: {parsedEvent.GetType()}
                    Call Connection Id: {moveParticipantSucceeded.CallConnectionId}
                    Correlation Id:      {moveParticipantSucceeded.CorrelationId}
                    """);
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

#region Lobby Call Support Workflow Endpoints
app.MapPost("/TargetCallToAcsUser(Create)", async (string acsTarget, ILogger<Program> logger) =>
{
    StringBuilder msgLog = new();
    msgLog.AppendLine("""

            ~~~~~~~~~~~~ /TargetCall(Create)  ~~~~~~~~~~~~
        """);

    Uri callbackUri = new(new Uri(callbackUriHost), "/api/callbacks");
    CallInvite callInvite = new(new CommunicationUserIdentifier(acsTarget));
    var createCallOptions = new CreateCallOptions(callInvite, callbackUri) {
        CallIntelligenceOptions = new CallIntelligenceOptions
        {
            CognitiveServicesEndpoint = new Uri("https://cognitive-service-waferwire.cognitiveservices.azure.com/")
        }};
    CreateCallResult createCallResult = await client.CreateCallAsync(createCallOptions);

    targetCallConnectionId = createCallResult.CallConnectionProperties.CallConnectionId;

    msgLog.Append($"""  
        TargetCall:
        -----------
        From: Call Automation
        To:   {acsTarget}
        Target Call Connection Id: {targetCallConnectionId}
        Correlation Id:            {createCallResult.CallConnectionProperties.CorrelationId}        
        """);

    Console.WriteLine(msgLog.ToString());
    return Results.Text(msgLog.ToString(), "text/plain");
}).WithTags("Lobby Call Support APIs");
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
}).WithTags("Lobby Call Support APIs");

#endregion 

app.Run();
