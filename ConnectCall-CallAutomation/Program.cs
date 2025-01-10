using Azure;
using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Communication.Identity;
using Azure.Communication.Rooms;
using Azure.Core;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Get ACS Connection String from appsettings.json
var acsConnectionString = builder.Configuration.GetValue<string>("AcsConnectionString");
ArgumentNullException.ThrowIfNullOrEmpty(acsConnectionString);

//Get acs phone number from appsettings.json
var acsPhoneNumber = builder.Configuration.GetValue<string>("AcsPhoneNumber");
ArgumentNullException.ThrowIfNullOrEmpty(acsPhoneNumber);

//Get Dev Tunnel Uri from appsettings.json
var devTunnelUri = builder.Configuration.GetValue<string>("DevTunnelUri");
ArgumentNullException.ThrowIfNullOrEmpty(devTunnelUri);

//Call back URL
var callbackUri = new Uri(new Uri(devTunnelUri), "/api/callbacks");

//Get cognitive service endpoint from appsettings.json
var cognitiveServiceEndpoint = builder.Configuration.GetValue<string>("CognitiveServiceEndpoint");
ArgumentNullException.ThrowIfNullOrEmpty(cognitiveServiceEndpoint);

//Get pma from appsettings.json
var pmaEndpoint = builder.Configuration.GetValue<string>("PmaEndpoint");
ArgumentNullException.ThrowIfNullOrEmpty(pmaEndpoint);

string callConnectionId = string.Empty;

CallAutomationClient callAutomationClient;
if (pmaEndpoint != null)
{
    callAutomationClient = new CallAutomationClient(new Uri(pmaEndpoint), acsConnectionString);
}
else
{
    callAutomationClient = new CallAutomationClient(acsConnectionString);
}
var app = builder.Build();

app.MapPost("/createRoom", async (ILogger<Program> logger) =>
{
    // create RoomsClient
    var roomsClient = new RoomsClient(acsConnectionString);
    var roomParticipants = new List<RoomParticipant>();

    //create CommunicationIdentityClient
    var IdentityClient = new CommunicationIdentityClient(acsConnectionString);
    var scopes = new List<string> { "chat", "voip" };
    var user1 = IdentityClient.CreateUser();
    Response<AccessToken> user1Token = await IdentityClient.GetTokenAsync(user1.Value, scopes: scopes.Select(x => new CommunicationTokenScope(x)));

    var user2 = IdentityClient.CreateUser();
    Response<AccessToken> user2Token = await IdentityClient.GetTokenAsync(user2.Value, scopes: scopes.Select(x => new CommunicationTokenScope(x)));

    string presenter = user1.Value.RawId;
    string attendee = user2.Value.RawId;

    var participant1 = new RoomParticipant(new CommunicationUserIdentifier(presenter))
    {
        Role = ParticipantRole.Presenter
    };
    var participant2 = new RoomParticipant(new CommunicationUserIdentifier(attendee))
    {
        Role = ParticipantRole.Attendee
    };
    roomParticipants.Add(participant1);
    roomParticipants.Add(participant2);

    var options = new CreateRoomOptions()
    {
        PstnDialOutEnabled = true,
        Participants = roomParticipants,
        ValidFrom = DateTime.UtcNow,
        ValidUntil = DateTime.UtcNow.AddMinutes(30)
    };

    var response = await roomsClient.CreateRoomAsync(options);
    logger.LogInformation($"ROOM ID: {response.Value.Id}");
    return new
    {
        User1Token = user1Token.Value.Token,
        User2Token = user2Token.Value.Token,
        RoomId = response.Value.Id
    };
});

app.MapPost("/connectApi", async (string roomCallId, ILogger<Program> logger) =>
{
    CallLocator callLocator = new RoomCallLocator(roomCallId);

    ConnectCallOptions connectCallOptions = new ConnectCallOptions(callLocator, callbackUri)
    {
        CallIntelligenceOptions = new CallIntelligenceOptions()
        {
            CognitiveServicesEndpoint = new Uri(cognitiveServiceEndpoint)
        }
    };

    ConnectCallResult result = await callAutomationClient.ConnectCallAsync(connectCallOptions);
    logger.LogInformation($"CALL CONNECTION ID : {result.CallConnectionProperties.CallConnectionId}");
    callConnectionId = result.CallConnectionProperties.CallConnectionId;

});

app.MapPost("/api/callbacks", (CloudEvent[] cloudEvents, ILogger<Program> logger) =>
{
    foreach (var cloudEvent in cloudEvents)
    {
        CallAutomationEventBase parsedEvent = CallAutomationEventParser.Parse(cloudEvent);
        if (parsedEvent != null)
        {
            logger.LogInformation(
                    "Received call event: {type}, callConnectionID: {connId}, serverCallId: {serverId}",
                    parsedEvent.GetType(),
                    parsedEvent.CallConnectionId,
                    parsedEvent.ServerCallId);
        }

        if (parsedEvent is CallConnected callConnected)
        {
            logger.LogInformation($"Received call event: {callConnected.GetType()}");
            callConnectionId = callConnected.CallConnectionId;
            CallConnectionProperties callConnectionProperties = GetCallConnectionProperties();
            logger.LogInformation($"CORRELATION ID: {callConnectionProperties.CorrelationId}");
        }
        else if (parsedEvent is AddParticipantSucceeded addParticipantSucceeded)
        {
            logger.LogInformation($"Received call event: {addParticipantSucceeded.GetType()}");
            callConnectionId = addParticipantSucceeded.CallConnectionId;
        }
        else if (parsedEvent is AddParticipantFailed addParticipantFailed)
        {
            callConnectionId = addParticipantFailed.CallConnectionId;
            logger.LogInformation($"Received call event: {addParticipantFailed.GetType()}, CorrelationId: {addParticipantFailed.CorrelationId}, " +
                      $"subCode: {addParticipantFailed.ResultInformation?.SubCode}, message: {addParticipantFailed.ResultInformation?.Message}, context: {addParticipantFailed.OperationContext}");
        }
        else if (parsedEvent is RemoveParticipantSucceeded removeParticipantSucceeded)
        {
            logger.LogInformation($"Received call event: {removeParticipantSucceeded.GetType()}");
            callConnectionId = removeParticipantSucceeded.CallConnectionId;
        }
        else if (parsedEvent is RemoveParticipantFailed removeParticipantFailed)
        {
            callConnectionId = removeParticipantFailed.CallConnectionId;
            logger.LogInformation($"Received call event: {removeParticipantFailed.GetType()}, CorrelationId: {removeParticipantFailed.CorrelationId}, " +
                      $"subCode: {removeParticipantFailed.ResultInformation?.SubCode}, message: {removeParticipantFailed.ResultInformation?.Message}, context: {removeParticipantFailed.OperationContext}");
        }
        else if (parsedEvent is CallDisconnected callDisconnected)
        {
            logger.LogInformation($"Received call event: {callDisconnected.GetType()}");
        }
    }
    return Results.Ok();
}).Produces(StatusCodes.Status200OK);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

/* Route for Azure Communication Service eventgrid webhooks*/
app.MapPost("/api/events", async ([FromBody] EventGridEvent[] eventGridEvents, ILogger<Program> logger) =>
{
    foreach (var eventGridEvent in eventGridEvents)
    {
        logger.LogInformation($"Incoming Call event received : {JsonSerializer.Serialize(eventGridEvent)}");

        // Handle system events
        if (eventGridEvent.TryGetSystemEventData(out object eventData))
        {
            // Handle the subscription validation event.
            if (eventData is SubscriptionValidationEventData subscriptionValidationEventData)
            {
                var responseData = new SubscriptionValidationResponse
                {
                    ValidationResponse = subscriptionValidationEventData.ValidationCode
                };
                return Results.Ok(responseData);
            }
        }
        if (eventData is AcsIncomingCallEventData incomingCallEventData)
        {
            var incomingCallContext = incomingCallEventData?.IncomingCallContext;
            var options = new AnswerCallOptions(incomingCallContext, callbackUri)
            {
                CallIntelligenceOptions = new CallIntelligenceOptions
                {
                    CognitiveServicesEndpoint = new Uri(cognitiveServiceEndpoint)
                }
            };

            AnswerCallResult answerCallResult = await callAutomationClient.AnswerCallAsync(options);
            var callConnectionId = answerCallResult.CallConnection.CallConnectionId;
            logger.LogInformation($"Answer call result: {callConnectionId}");

            var callConnectionMedia = answerCallResult.CallConnection.GetCallMedia();
            //Use EventProcessor to process CallConnected event
            var answer_result = await answerCallResult.WaitForEventProcessorAsync();

            if (answer_result.IsSuccess)
            {
                logger.LogInformation($"Call connected event received for connection id: {answer_result.SuccessResult.CallConnectionId}");
                logger.LogInformation($"CORRELATION ID: {answer_result.SuccessResult.CorrelationId}");
            }
        }
    }
    return Results.Ok();
});

app.MapPost("/addParticipant", async (string targetPhoneNumber, ILogger<Program> logger) =>
{
    var response = await AddParticipantAsync(targetPhoneNumber);
    return Results.Ok(response);
});

app.MapPost("/removeParticipant", async (string targetPhoneNumber, ILogger<Program> logger) =>
{
    var response = await RemoveParticipantAsync(targetPhoneNumber);
    return Results.Ok(response);
});

app.MapPost("/disConnectCall", async (ILogger<Program> logger) =>
{
    var callConnection = callAutomationClient.GetCallConnection(callConnectionId);
    await callConnection.HangUpAsync(true);
    return Results.Ok();
});

async Task<AddParticipantResult> AddParticipantAsync(string targetPhoneNumber)
{
    CallInvite callInvite;

    CallConnection callConnection = GetConnection();

    string operationContext = "addPSTNUserContext";
    callInvite = new CallInvite(new PhoneNumberIdentifier(targetPhoneNumber),
              new PhoneNumberIdentifier(acsPhoneNumber));

    var addParticipantOptions = new AddParticipantOptions(callInvite)
    {
        OperationContext = operationContext,
        InvitationTimeoutInSeconds = 30,
        OperationCallbackUri = callbackUri
    };

    return await callConnection.AddParticipantAsync(addParticipantOptions);
}


async Task<RemoveParticipantResult> RemoveParticipantAsync(string targetPhoneNumber)
{
    RemoveParticipantOptions removeParticipantOptions;

    CallConnection callConnection = GetConnection();

    string operationContext = "removePSTNUserContext";
    removeParticipantOptions = new RemoveParticipantOptions(new PhoneNumberIdentifier(targetPhoneNumber))
    {
        OperationContext = operationContext,
        OperationCallbackUri = callbackUri
    };

    return await callConnection.RemoveParticipantAsync(removeParticipantOptions);
}

CallConnection GetConnection()
{
    CallConnection callConnection = !string.IsNullOrEmpty(callConnectionId) ?
        callAutomationClient.GetCallConnection(callConnectionId)
        : throw new ArgumentNullException("Call connection id is empty");
    return callConnection;
}

CallConnectionProperties GetCallConnectionProperties()
{
    CallConnectionProperties callConnectionProperties = !string.IsNullOrEmpty(callConnectionId) ?
       callAutomationClient.GetCallConnection(callConnectionId).GetCallConnectionProperties()
       : throw new ArgumentNullException("Call connection id is empty");
    return callConnectionProperties;
}

app.Run();