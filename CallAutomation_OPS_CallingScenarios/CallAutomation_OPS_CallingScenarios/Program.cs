using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Intrinsics.X86;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

/* Read config values from appsettings.json*/
var acsConnectionString = builder.Configuration.GetValue<string>("AcsConnectionString");
ArgumentNullException.ThrowIfNullOrEmpty(acsConnectionString);

var callbackUriHost = builder.Configuration.GetValue<string>("CallbackUriHost");
ArgumentNullException.ThrowIfNullOrEmpty(callbackUriHost);


var teamsAppId = builder.Configuration.GetValue<string>("TeamsAppId");
ArgumentNullException.ThrowIfNullOrEmpty(teamsAppId);
var acsPhoneNumber = builder.Configuration.GetValue<string>("AcsPhoneNumber");
ArgumentNullException.ThrowIfNullOrEmpty(acsPhoneNumber);

var dualPersonaUserId = builder.Configuration.GetValue<string>("DualPersonaUserId");
ArgumentNullException.ThrowIfNullOrEmpty(dualPersonaUserId);


var cCaaSAgentUserId = builder.Configuration.GetValue<string>("CCaaSAgentUserId");
ArgumentNullException.ThrowIfNullOrEmpty(cCaaSAgentUserId);

var cCaaSAgentUserId1 = builder.Configuration.GetValue<string>("CCaaSAgentUserId1");
ArgumentNullException.ThrowIfNullOrEmpty(cCaaSAgentUserId1);

var teamsUserId = builder.Configuration.GetValue<string>("TeamsUserId");
ArgumentNullException.ThrowIfNullOrEmpty(teamsUserId);

var teamsUserPSTNNumber = builder.Configuration.GetValue<string>("TeamsUserPSTNNumber");
ArgumentNullException.ThrowIfNullOrEmpty(teamsUserPSTNNumber);

var PMAEndpoint = new Uri("https://uswe-04.sdf.pma.teams.microsoft.com:6448");

MicrosoftTeamsAppIdentifier teamsAppIdentifier = new MicrosoftTeamsAppIdentifier(teamsAppId);

// Initialize the options with the MicrosoftTeamsAppIdentifier
CallAutomationClientOptions callautomationclientoptions = new CallAutomationClientOptions
{
    OPSSource = teamsAppIdentifier
};
/* Call Automation Client */
var client = new CallAutomationClient(pmaEndpoint: PMAEndpoint, connectionString: acsConnectionString, callautomationclientoptions);

/* Register and make CallAutomationClient accessible via dependency injection */
builder.Services.AddSingleton(client);
var app = builder.Build();


string recordingId = string.Empty;
string recordingLocation = string.Empty;

string callConnectionId = string.Empty;

app.MapPost("/api/incomingCall", async (
    [FromBody] EventGridEvent[] eventGridEvents,
    ILogger<Program> logger) =>
{
    foreach (var eventGridEvent in eventGridEvents)
    {
        logger.LogInformation($"Call event received:{eventGridEvent.EventType}");

        /* Handle system events */
        if (eventGridEvent.TryGetSystemEventData(out object eventData))
        {
            /* Handle the subscription validation event. */
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
            var callerId = incomingCallEventData.FromCommunicationIdentifier.RawId;
            var callbackUri = new Uri(new Uri(callbackUriHost), $"/api/callbacks/{Guid.NewGuid()}?callerId={callerId}");
            logger.LogInformation($"Incoming call - correlationId: {incomingCallEventData.CorrelationId}, " +
                $"Callback url: {callbackUri}");
            var callerPhonenumber = incomingCallEventData.FromCommunicationIdentifier.PhoneNumber.Value;

            var options = new AnswerCallOptions(incomingCallEventData.IncomingCallContext, callbackUri);

            AnswerCallResult answerCallResult = await client.AnswerCallAsync(options);
            var callConnectionMedia = answerCallResult.CallConnection.GetCallMedia();

            /* Use EventProcessor to process CallConnected event */
            var answer_result = await answerCallResult.WaitForEventProcessorAsync();
            if (answer_result.IsSuccess)
            {
                logger.LogInformation($"CA Received call event: {answer_result.GetType()}, callConnectionID: {answer_result.SuccessResult.CallConnectionId}, " +
                    $"serverCallId: {answer_result.SuccessResult.ServerCallId}");
                callConnectionId = answer_result.SuccessResult.CallConnectionId;

               
                // Add CCaaS agent 
                var ccaasAgent = new CommunicationUserIdentifier(dualPersonaUserId);
                var callInvite = new CallInvite(ccaasAgent);
                var addParticipantOptions = new AddParticipantOptions(callInvite);

                var addParticipantResult = await answerCallResult.CallConnection.AddParticipantAsync(addParticipantOptions);
                logger.LogInformation($"Adding CCaaS agent  to the call: {addParticipantResult.Value?.InvitationId}");


                //// add  Teams user (8:orgid)
                //var teamsUser = new MicrosoftTeamsUserIdentifier(teamsUserId);
                //var callInviteTeams = new CallInvite(teamsUser);
                //var addParticipantOptionsTeams = new AddParticipantOptions(callInviteTeams);
                //var addParticipantResultTeams = await answerCallResult.CallConnection.AddParticipantAsync(addParticipantOptionsTeams);
                //logger.LogInformation($"Adding Teams user to the call: {addParticipantResultTeams.Value?.InvitationId}");

                //// add Teams user (PSTN user number)
                //var targetteamsUserPSTN = new PhoneNumberIdentifier(teamsUserPSTNNumber);
                //var callerPSTNNumber = new PhoneNumberIdentifier(acsPhoneNumber);
                //var callInviteTeamsPSTN = new CallInvite(targetteamsUserPSTN, callerPSTNNumber);
                //var addParticipantOptionsTeamsPSTN = new AddParticipantOptions(callInviteTeamsPSTN);
                //var addParticipantResultTeamsPSTN = await answerCallResult.CallConnection.AddParticipantAsync(addParticipantOptionsTeamsPSTN);
                //logger.LogInformation($"Adding Teams user PSTN to the call: {addParticipantResultTeamsPSTN.Value?.InvitationId}");

                //// customer is on hold
                //CommunicationIdentifier target = new PhoneNumberIdentifier(callerPhonenumber);

                //HoldOptions holdOptions = new HoldOptions(target);
                //await callConnectionMedia.HoldAsync(holdOptions);
                //logger.LogInformation("Customer is put on hold.");

                //UnholdOptions unholdOptions = new UnholdOptions(target);
                //// After the consultation, unhold the customer (PSTN user)
                //await callConnectionMedia.UnholdAsync(unholdOptions);
                //logger.LogInformation("PSTN customer is unheld.");

                //  transfer call to Teams user (8:orgid)
                //var transferTeamsUser = new MicrosoftTeamsUserIdentifier(teamsUserId);
                //var transferParticipantOptionsTeams = new TransferToParticipantOptions(transferTeamsUser);
                //var transferParticipantResultTeams = await answerCallResult.CallConnection.TransferCallToParticipantAsync(transferParticipantOptionsTeams);
                //logger.LogInformation("Transferring Teams user to the call: {InvitationId}", transferParticipantResultTeams.Value);

                // transfer call to Teams user (PSTN user number)
                //var transferTeamsUserPSTN = new PhoneNumberIdentifier(teamsUserPSTNNumber);
                //var transferParticipantOptionsTeamsPSTN = new TransferToParticipantOptions(transferTeamsUserPSTN);
                //var transferParticipantResultTeamsPSTN = await answerCallResult.CallConnection.TransferCallToParticipantAsync(transferParticipantOptionsTeamsPSTN);
                //logger.LogInformation("Transferring Teams user PSTN to the call: {InvitationId}", transferParticipantResultTeamsPSTN.Value);

            }
            client.GetEventProcessor().AttachOngoingEventProcessor<AddParticipantSucceeded>(
               answerCallResult.CallConnection.CallConnectionId, async (addParticipantSucceededEvent) =>
               {
                   logger.LogInformation($"Received call event: {addParticipantSucceededEvent.GetType()}, context: {addParticipantSucceededEvent.OperationContext}");
               });
            client.GetEventProcessor().AttachOngoingEventProcessor<ParticipantsUpdated>(
              answerCallResult.CallConnection.CallConnectionId, async (participantsUpdatedEvent) =>
              {
                  logger.LogInformation($"Received call event: {participantsUpdatedEvent.GetType()}, participants: {participantsUpdatedEvent.Participants.Count()}, sequenceId: {participantsUpdatedEvent.SequenceNumber}");
              });
            client.GetEventProcessor().AttachOngoingEventProcessor<CallDisconnected>(
              answerCallResult.CallConnection.CallConnectionId, async (callDisconnectedEvent) =>
              {
                  logger.LogInformation($"Received call event: {callDisconnectedEvent.GetType()}");
              });
            client.GetEventProcessor().AttachOngoingEventProcessor<AddParticipantFailed>(
              answerCallResult.CallConnection.CallConnectionId, async (addParticipantFailedEvent) =>
              {
                  logger.LogInformation($"Received call event: {addParticipantFailedEvent.GetType()}, CorrelationId: {addParticipantFailedEvent.CorrelationId}, " +
                      $"subCode: {addParticipantFailedEvent.ResultInformation?.SubCode}, message: {addParticipantFailedEvent.ResultInformation?.Message}, context: {addParticipantFailedEvent.OperationContext}");
              });

            client.GetEventProcessor().AttachOngoingEventProcessor<HoldFailed>(
               answerCallResult.CallConnection.CallConnectionId, async (holdFailed) =>
               {
                   callConnectionId = holdFailed.CallConnectionId;
                   logger.LogInformation($"Received event: {holdFailed.GetType()}, CorrelationId: {holdFailed.CorrelationId}, " +
                       $"SubCode: {holdFailed?.ResultInformation?.SubCode}, Message: {holdFailed?.ResultInformation?.Message}");
               });

        }
    }
    return Results.Ok();
});

// api to handle call back events
app.MapPost("/api/callbacks/{contextId}", async (
    [FromBody] CloudEvent[] cloudEvents,
    [FromRoute] string contextId,
    [Required] string callerId,
    CallAutomationClient callAutomationClient,
    ILogger<Program> logger) =>
{
    var eventProcessor = client.GetEventProcessor();
    eventProcessor.ProcessEvents(cloudEvents);
    return Results.Ok();
});

app.MapPost("/api/recordingFileStatus", (EventGridEvent[] eventGridEvents, ILogger<Program> logger) =>
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
            if (eventData is AcsRecordingFileStatusUpdatedEventData statusUpdated)
            {
                recordingLocation = statusUpdated.RecordingStorageInfo.RecordingChunks[0].ContentLocation;
                logger.LogInformation($"The recording location is : {recordingLocation}");
            }
        }
    }
    return Results.Ok();
});

app.MapGet("/download", (ILogger<Program> logger) =>
{
    client.GetCallRecording().DownloadTo(new Uri(recordingLocation), "testfile.wav");
    return Results.Ok();
});


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();