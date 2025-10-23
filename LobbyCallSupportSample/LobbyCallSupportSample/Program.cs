using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Core;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using System.Net.WebSockets;
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
    cognitiveServiceEndpoint =
        builder.Configuration["cognitiveServiceEndpoint"]
        ?? throw new ArgumentNullException("cognitiveServiceEndpoint"),
    callbackUriHost = 
        builder.Configuration["callbackUriHost"] 
        ?? throw new ArgumentNullException("callbackUriHost"),
    acsGeneratedIdForLobbyCallReceiver =
        builder.Configuration["acsGeneratedIdForLobbyCallReceiver"]
        ?? throw new ArgumentNullException("acsGeneratedIdForLobbyCallReceiver"),
    acsGeneratedIdForTargetCallReceiver =
        builder.Configuration["acsGeneratedIdForTargetCallReceiver"]
        ?? throw new ArgumentNullException("acsGeneratedIdForTargetCallReceiver"),
    acsGeneratedIdForTargetCallSender =
        builder.Configuration["acsGeneratedIdForTargetCallSender"]
        ?? throw new ArgumentNullException("acsGeneratedIdForTargetCallSender"),
    confirmMessageToTargetCall = "A user is waiting in lobby, do you want to add the lobby user to your call?",
    textToPlayToLobbyUser = "You are currently in a lobby call, we will notify the admin that you are waiting.",
    // Track which type of workflow call was last created
    lastWorkflowCallType = string.Empty, // "CallTwo" or "CallThree"
    acsIdentity = string.Empty,
    // Call connection IDs
    targetCallConnectionId = string.Empty,
    lobbyConnectionId = string.Empty, // User's incoming call connection id
    lobbyCallerId = string.Empty, // User's incoming caller id
    callConnectionId2 = string.Empty; // ACS user's redirected call

// Web socket
WebSocket? webSocket = null;

CallAutomationClient client =
    new(connectionString: acsConnectionString);
#endregion

#region Event Handler

app.MapPost("/api/LobbyCallSupportEventHandler", async (EventGridEvent[] eventGridEvents, ILogger<Program> logger) =>
{
    StringBuilder msgLog = new(); // to make string builder thread-safe; declared here
    msgLog.AppendLine("""

            ~~~~~~~~~~~~ /api/LobbyCallSupportEventHandler  ~~~~~~~~~~~~
        """);
    try
    {

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
                        fromCallerId =
                        acsIdentity = incomingCallEventData.FromCommunicationIdentifier.RawId,
                        toCallerId = incomingCallEventData.ToCommunicationIdentifier.RawId;

                    // Lobby Call: Answer 
                    if (toCallerId.Contains(acsGeneratedIdForLobbyCallReceiver) || toCallerId.Contains(acsGeneratedIdForTargetCallReceiver))
                    {
                        #region Answer Call
                        Uri callbackUri = new(new Uri(callbackUriHost), $"/api/callbacks");
                        AnswerCallOptions options = new(incomingCallEventData.IncomingCallContext, callbackUri)
                        {
                            OperationContext = !toCallerId.Contains(acsGeneratedIdForTargetCallReceiver) ? "LobbyCall" : "OtherCall",
                            CallIntelligenceOptions = new CallIntelligenceOptions
                            {
                                CognitiveServicesEndpoint = new Uri(cognitiveServiceEndpoint)
                            }
                        };

                        AnswerCallResult answerCallResult = await client.AnswerCallAsync(options);

                        if (toCallerId.Contains(acsGeneratedIdForTargetCallReceiver))
                        {
                            targetCallConnectionId = answerCallResult.CallConnection.CallConnectionId;

                            msgLog.AppendLine($"""
                            Target Call(Inbound) Answered by Call Automation.
                            From Caller Raw Id: {fromCallerId}
                            To Caller Raw Id:   {toCallerId}
                            Target Call Connection Id: {targetCallConnectionId}
                            Correlation Id:           {incomingCallEventData.CorrelationId}
                            Target Call answered successfully.
                            """);
                        }
                        else
                        {
                            lobbyConnectionId = answerCallResult.CallConnection.CallConnectionId;

                            msgLog.AppendLine($"""
                            User Call(Inbound) Answered by Call Automation.
                            From Caller Raw Id: {fromCallerId}
                            To Caller Raw Id:   {toCallerId}
                            Lobby Call Connection Id: {lobbyConnectionId}
                            Correlation Id:           {incomingCallEventData.CorrelationId}
                            Lobby Call answered successfully.
                            """);
                        }
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
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error occurred: {ex.Message}");
        return Results.Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
    }
});

#endregion

#region Callback Handler

app.MapPost("/api/callbacks", async (CloudEvent[] cloudEvents, ILogger<Program> logger) =>
{
    StringBuilder msgLog = new(); // to make string builder thread-safe; declared here
    try
    {
        foreach (var cloudEvent in cloudEvents)
        {
            CallAutomationEventBase parsedEvent = CallAutomationEventParser.Parse(cloudEvent);
            var callConnection = client.GetCallConnection(parsedEvent.CallConnectionId);
            if (parsedEvent is CallConnected callConnected)
            {
                Console.WriteLine($"~~~~~~~~~~~~  /api/callbacks ~~~~~~~~~~~~ ");
                Console.WriteLine($"Received callConnected.CallConnectionId : {callConnected.CallConnectionId}");
                if ((callConnected.OperationContext ?? string.Empty).Equals("LobbyCall", StringComparison.Ordinal))
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
                    Console.WriteLine($"""
                    Playing Media to Lobby Call..
                    """);
                    CallMedia callMedia = !string.IsNullOrEmpty(callConnected.CallConnectionId) ?
                        client.GetCallConnection(callConnected.CallConnectionId).GetCallMedia()
                        : throw new ArgumentNullException("Call connection id is empty");
                    TextSource textSource =
                        new(textToPlayToLobbyUser)
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
                // By pop up in Client app
                if (webSocket is null || webSocket.State != WebSocketState.Open)
                {
                    msgLog.AppendLine("ERROR: Web socket is not available.");
                    return Results.NotFound("Message not sent");
                    // throw new ArgumentNullException("web socket is not available.");
                }

                // Notify Client
                var msg = System.Text.Encoding.UTF8.GetBytes(confirmMessageToTargetCall);
                await webSocket.SendAsync(new ArraySegment<byte>(msg), WebSocketMessageType.Text, true, CancellationToken.None);
                msgLog.AppendLine($"Target Call notified with message: {confirmMessageToTargetCall}");
                return Results.Ok("Target Call notified with message: {confirmMessageToTargetCall}");
            }
            else if (parsedEvent is MoveParticipantSucceeded moveParticipantSucceeded)
            {
                msgLog.AppendLine($"""
                        ~~~~~~~~~~~~  /api/callbacks ~~~~~~~~~~~~ 
                    Received event: {parsedEvent.GetType()}
                    Call Connection Id: {moveParticipantSucceeded.CallConnectionId}
                    Correlation Id:      {moveParticipantSucceeded.CorrelationId}
                    """);
                // move 
                // Get the updated participants list
                msgLog.AppendLine($"""

                    ~~~~~~~~~~~~ Participants in Target Connection({targetCallConnectionId}) ~~~~~~~~~~~~
                """);
                try
                {
                    CallConnection targetConnection = client.GetCallConnection(moveParticipantSucceeded.CallConnectionId);
                    var participants = await targetConnection.GetParticipantsAsync();

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
                        Console.WriteLine("No participants found for the specified call connection.");
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
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error getting participants for call {targetCallConnectionId}: {ex.Message}");

                }
                // end: Get the updated participants list
                // end: move
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
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error occurred: {ex.Message}");
        return Results.Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
    }
}).Produces(StatusCodes.Status200OK);

#endregion

#region Lobby Call Support Workflow Endpoints
app.MapPost("/TargetCallToAcsUser(Call Replaced with client app)", async (string acsTarget, ILogger<Program> logger) =>
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
            CognitiveServicesEndpoint = new Uri("") // Cognitive service URL
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

#region Websocket implementation
app.UseWebSockets();
app.Map("/ws", async context =>
{
    Console.WriteLine("Received WEB SOCKET request.");
    if (context.WebSockets.IsWebSocketRequest)
    {
        webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var buffer = new byte[1024 * 4];

        // Keep alive with a read loop
        while (webSocket.State == WebSocketState.Open)
        {
            try
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                var jsResponse = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"Received response from Client App: {jsResponse}");
                // Move participant to target call if response is "yes"

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine($"result.MessageType: {result.MessageType}");
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
                else
                {
                    // Process incoming message or ignore
                    if (jsResponse.Equals("yes", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"Move Participant operation begins..");
                        // Call the Move Participants API
                        #region Move Participant
                        try
                        {
                            Console.WriteLine($"""
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
                            else
                            {
                                // ACS Communication User
                                participantToMove = new CommunicationUserIdentifier(lobbyCallerId);
                            }

                            var response = await targetConnection.MoveParticipantsAsync(options: new([participantToMove], lobbyConnectionId));
                            var rawResponse = response.GetRawResponse();
                            if (rawResponse.Status >= 200 && rawResponse.Status <= 299)
                            {
                                Console.WriteLine();
                                Console.WriteLine("Move Participants operation completed successfully.");
                            }
                            else
                            {
                                throw new Exception($"Move Participants operation failed with status code: {rawResponse.Status}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error in move participants operation: {ex.Message}");
                        }
                        #endregion

                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("----- Web socket error -----");
                Console.WriteLine(ex.Message);
                Console.WriteLine("----- End: Web socket error -----");

            }
        }
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});
#endregion

app.Run();
