using Azure;
using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Communication.Identity;
using Azure.Communication.Rooms;
using Azure.Core;
using Azure.Messaging;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();

//Get ACS Connection String from appsettings.json
var acsConnectionString = builder.Configuration.GetValue<string>("AcsConnectionString");
ArgumentNullException.ThrowIfNullOrEmpty(acsConnectionString);

//Get acs phone number from appsettings.json
var acsPhoneNumber = builder.Configuration.GetValue<string>("AcsPhoneNumber");
ArgumentNullException.ThrowIfNullOrEmpty(acsPhoneNumber);

//Get participant phone number from appsettings.json
var participantPhoneNumber = builder.Configuration.GetValue<string>("ParticipantPhoneNumber");
ArgumentNullException.ThrowIfNullOrEmpty(participantPhoneNumber);

//Get Dev Tunnel Uri from appsettings.json
var callbackUriHost = builder.Configuration.GetValue<string>("CallbackUriHost");
ArgumentNullException.ThrowIfNullOrEmpty(callbackUriHost);

//Call back URL
var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");

string callConnectionId = string.Empty;

string roomId = string.Empty;

CallAutomationClient callAutomationClient;
callAutomationClient = new CallAutomationClient(acsConnectionString);

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
    roomId = response.Value.Id;
    logger.LogInformation($"ROOM ID: {response.Value.Id}");
    return new
    {
        user1Id = user1.Value.RawId,
        User1Token = user1Token.Value.Token,
        user2Id = user2.Value.RawId,
        User2Token = user2Token.Value.Token,
        RoomId = response.Value.Id
    };
});

app.MapPost("/connectCall", async (ILogger<Program> logger) =>
{
    if (!string.IsNullOrEmpty(roomId))
    {
        CallLocator callLocator = new RoomCallLocator(roomId);

        ConnectCallOptions connectCallOptions = new ConnectCallOptions(callLocator, callbackUri);

        ConnectCallResult result = await callAutomationClient.ConnectCallAsync(connectCallOptions);
        logger.LogInformation($"CALL CONNECTION ID : {result.CallConnectionProperties.CallConnectionId}");
        callConnectionId = result.CallConnectionProperties.CallConnectionId;
        logger.LogInformation($"CONNECT REQUEST CORRELATION ID: {result.CallConnectionProperties.CorrelationId}");
    }
    else
    {
        throw new ArgumentNullException(nameof(roomId));
    }
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
        else if (parsedEvent is ConnectFailed connectFailed)
        {
            callConnectionId = connectFailed.CallConnectionId;
            logger.LogInformation($"Received call event: {connectFailed.GetType()}, CorrelationId: {connectFailed.CorrelationId}, " +
                      $"subCode: {connectFailed.ResultInformation?.SubCode}, message: {connectFailed.ResultInformation?.Message}, context: {connectFailed.OperationContext}");
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
        else if (parsedEvent is CallDisconnected callDisconnected)
        {
            logger.LogInformation($"Received call event: {callDisconnected.GetType()}");
        }
    }
    return Results.Ok();
}).Produces(StatusCodes.Status200OK);

app.MapPost("/addParticipant", async (ILogger<Program> logger) =>
{
    CallInvite callInvite;

    CallConnection callConnection = GetConnection();

    string operationContext = "addPSTNUserContext";
    callInvite = new CallInvite(new PhoneNumberIdentifier(participantPhoneNumber),
              new PhoneNumberIdentifier(acsPhoneNumber));

    var addParticipantOptions = new AddParticipantOptions(callInvite)
    {
        OperationContext = operationContext,
        InvitationTimeoutInSeconds = 30,
        OperationCallbackUri = callbackUri
    };

    await callConnection.AddParticipantAsync(addParticipantOptions);
    return Results.Ok();
});

app.MapPost("/hangUp", async (ILogger<Program> logger) =>
{
    CallConnection callConnection = GetConnection();
    await callConnection.HangUpAsync(true);
    return Results.Ok();
});

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