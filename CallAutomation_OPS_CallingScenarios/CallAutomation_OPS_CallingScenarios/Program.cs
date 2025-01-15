using Azure;
using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using CallAutomation_OPS_CallingScenarios;
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

var cognitiveServicesEndpoint = builder.Configuration.GetValue<string>("CognitiveServiceEndpoint");
ArgumentNullException.ThrowIfNullOrEmpty(cognitiveServicesEndpoint);
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

var fileSourceUri = builder.Configuration.GetValue<string>("FileSourceUri");
ArgumentNullException.ThrowIfNullOrEmpty(fileSourceUri);

var PMAEndpoint = new Uri("https://uswe-04.sdf.pma.teams.microsoft.com:6448");

MicrosoftTeamsAppIdentifier teamsAppIdentifier = new MicrosoftTeamsAppIdentifier(teamsAppId);

//// Initialize the options with the MicrosoftTeamsAppIdentifier
// Initialize the options with the MicrosoftTeamsAppIdentifier
CallAutomationClientOptions callautomationclientoptions = new CallAutomationClientOptions(CallAutomationClientOptions.ServiceVersion.V2024_09_01_Preview)
{
    //OPSSource = teamsAppIdentifier


};
/* Call Automation Client */
var client = new CallAutomationClient(pmaEndpoint: PMAEndpoint, connectionString: acsConnectionString, callautomationclientoptions);
bool isTranscriptionActive = false;
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

            //var websocketUri = callbackUriHost.Replace("https", "wss") + "ws";
            //logger.LogInformation($"Incoming call - correlationId: {incomingCallEventData.CorrelationId}, " +
            //    $"Callback url: {callbackUri}, websocket Url: {websocketUri}");

            //TranscriptionOptions transcriptionOptions = new TranscriptionOptions(new Uri(websocketUri),
            //    "en-US", false, TranscriptionTransport.Websocket);
            //var callerPhonenumber = incomingCallEventData.FromCommunicationIdentifier.PhoneNumber.Value;

            var answerCallOptions = new AnswerCallOptions(incomingCallEventData.IncomingCallContext, callbackUri);
            //{
            //    CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
            //    TranscriptionOptions = transcriptionOptions
            //};
            // Adding Custom SIP Headers (User-to-User and X-Headers)
            // answerCallOptions.CustomCallingContext.AddSipUui("OBOuuivalue"); // User-to-User header
            // answerCallOptions.CustomCallingContext.AddSipX("X-CustomHeader1", "HeaderValue1"); // X-header

           
            AnswerCallResult answerCallResult = await client.AnswerCallAsync(answerCallOptions);
            var callConnectionMedia = answerCallResult.CallConnection.GetCallMedia();
           

            /* Use EventProcessor to process CallConnected event */
            var answer_result = await answerCallResult.WaitForEventProcessorAsync();
            if (answer_result.IsSuccess)
            {
                logger.LogInformation($"CA Received call event: {answer_result.GetType()}, callConnectionID: {answer_result.SuccessResult.CallConnectionId}, " +
                    $"serverCallId: {answer_result.SuccessResult.ServerCallId}");
                callConnectionId = answer_result.SuccessResult.CallConnectionId;

                

                //var ccaasAgent = new MicrosoftTeamsUserIdentifier(dualPersonaUserId); // teams user
                var ccaasAgent = new CommunicationUserIdentifier(dualPersonaUserId); // dual persona user
                var callInvite = new CallInvite(ccaasAgent);
                var addParticipantOptions = new AddParticipantOptions(callInvite);

                var addParticipantResult = await answerCallResult.CallConnection.AddParticipantAsync(addParticipantOptions).ConfigureAwait(false);
                logger.LogInformation($"Adding CCaaS agent  to the call: {addParticipantResult.Value?.InvitationId}");



                ////// CCaaS agent can perform mute/unmute of the PSTN participant
                //await Task.Delay(20000);
                //////await MuteParticipantAsync(callConnectionId, callerId);
                //CommunicationIdentifier target = new PhoneNumberIdentifier("+18332638155");
                //await client.GetCallConnection(callConnectionId).MuteParticipantAsync(target).ConfigureAwait(false);
                //// await answerCallResult.CallConnection.MuteParticipantAsync(target).ConfigureAwait(false);


                // Define the PSTN participant to be added
                //var pstnPhoneNumber = new PhoneNumberIdentifier("+918328652016"); // Replace with the PSTN number
                //var callerPhoneNumber = new PhoneNumberIdentifier(acsPhoneNumber); // Your ACS Phone Number

                //// Create a CallInvite using the PSTN participant
                //var callInvite = new CallInvite(pstnPhoneNumber, callerPhoneNumber); // Caller is optional

                //// Configure AddParticipant options
                //var addParticipantsOptions = new AddParticipantOptions(callInvite)
                //{
                //    OperationContext = "AddPSTNUserOperation" // Optional context for tracking
                //};

                //// Add PSTN participant to the call
                //var addParticipantsResult = await answerCallResult.CallConnection.AddParticipantAsync(addParticipantsOptions);



                //  await PauseOrStopRecording(callConnectionMedia, logger, true, recordingId);

                //// Retrieve the list of participants
                //var response = await answerCallResult.CallConnection.GetParticipantsAsync();
                //var participants = response.Value; // Extract the list of participants

                //// Iterate over the list and log details
                //foreach (var participant in participants)
                //{
                //    var participantId = participant.Identifier.RawId; // Or PhoneNumber if applicable
                //    logger.LogInformation($"Participant ID: {participantId}, IsMuted: {participant.IsMuted}");
                //}
                //// Add CCaaS agent 

                await Task.Delay(20000);
                ////// add  Teams user (8:orgid)
                //var teamsUser = new MicrosoftTeamsUserIdentifier(teamsUserId);
                //var callInviteTeams = new CallInvite(teamsUser);
                //var addParticipantOptionsTeams = new AddParticipantOptions(callInviteTeams);
                //var addParticipantResultTeams = await answerCallResult.CallConnection.AddParticipantAsync(addParticipantOptionsTeams).ConfigureAwait(false);
                //logger.LogInformation($"Adding Teams user to the call: {addParticipantResultTeams.Value?.InvitationId}");
                //await Task.Delay(20000);

                //CallLocator callLocator = new ServerCallLocator(callConnectionId);
                //var recordingOptions = new StartRecordingOptions(callLocator)
                //{
                //    RecordingContent = RecordingContent.Audio,  // Adjust based on content type
                //    RecordingFormat = RecordingFormat.Mp3,          // Supported format
                //    RecordingChannel = RecordingChannel.Mixed,      // Mixed or unmixed
                //    RecordingStateCallbackUri = callbackUri,
                //    PauseOnStart = false
                //};
                //var recordingResult = await client.GetCallRecording().StartAsync(recordingOptions);
                //recordingId = recordingResult.Value.RecordingId;
                //logger.LogInformation($"Recording started. RecordingId: {recordingId}");

                //await Task.Delay(20000);

                //await PauseOrStopRecording(callConnectionMedia, logger, true, recordingId);

                //// CCaaS agent can perform mute/unmute of the PSTN participant
                //  await Task.Delay(20000);
                ////await MuteParticipantAsync(callConnectionId, callerId);
                //CommunicationIdentifier target = new PhoneNumberIdentifier("+18332638155");
                //await client.GetCallConnection(callConnectionId).MuteParticipantAsync(target).ConfigureAwait(false);
                // await answerCallResult.CallConnection.MuteParticipantAsync(target).ConfigureAwait(false);


                // CommunicationIdentifier target1 = new MicrosoftTeamsUserIdentifier(teamsUserId);

                // HoldOptions holdOptions = new HoldOptions(target1);

                //var res= await callConnectionMedia.HoldAsync(holdOptions).ConfigureAwait(false);
                // logger.LogInformation($"Teams user is put on hold.{res.Content.ToString()}");



                // UnholdOptions unholdOptions = new UnholdOptions(target1);

                // await callConnectionMedia.UnholdAsync(unholdOptions).ConfigureAwait(false);


                //await client.GetCallConnection(callConnectionId).MuteParticipantAsync(target1).ConfigureAwait(false);

                //await Task.Delay(20000);

                //await client.GetCallConnection(callConnectionId).UnmuteParticipantAsync(target1).ConfigureAwait(false);

                //await Task.Delay(20000);
                //CommunicationIdentifier target = new MicrosoftTeamsUserIdentifier("abd436ba-3128-4c3b-9c5a-b020cae8304e");
                //await answerCallResult.CallConnection.RemoveParticipantAsync(target).ConfigureAwait(false);

                //await Task.Delay(20000);

                //// customer is on hold
                // CommunicationIdentifier target = new PhoneNumberIdentifier(teamsUserId);
                //CommunicationIdentifier target = new PhoneNumberIdentifier("+18332638155");
                // Play greeting message
                //var greetingPlaySource = new TextSource("HoldOperation")
                //{
                //    VoiceName = "en-US-NancyNeural"
                //};
                //  FileSource fileSource = new FileSource(new Uri(fileSourceUri));
                // HoldOptions holdOptions = new HoldOptions(target);

                // await callConnectionMedia.HoldAsync(holdOptions);
                //logger.LogInformation("Customer is put on hold.");
                //await Task.Delay(10000);
                //UnholdOptions unholdOptions = new UnholdOptions(target);
                //////// After the consultation, unhold the customer (PSTN user)
                //await callConnectionMedia.UnholdAsync(unholdOptions);
                //logger.LogInformation("PSTN customer is unheld.");
                // await Task.Delay(10000);

                // await client.GetCallConnection(callConnectionId).UnmuteParticipantAsync(target).ConfigureAwait(false);
                //   await client.GetCallConnection(callConnectionId).HangUpAsync(forEveryone: false).ConfigureAwait(false);

                //// add Teams user (PSTN user number)
                //var targetteamsUserPSTN = new PhoneNumberIdentifier(teamsUserPSTNNumber);
                //var callerPSTNNumber = new PhoneNumberIdentifier(acsPhoneNumber);
                //var callInviteTeamsPSTN = new CallInvite(targetteamsUserPSTN, callerPSTNNumber);
                //var addParticipantOptionsTeamsPSTN = new AddParticipantOptions(callInviteTeamsPSTN);
                //var addParticipantResultTeamsPSTN = await answerCallResult.CallConnection.AddParticipantAsync(addParticipantOptionsTeamsPSTN);
                //logger.LogInformation($"Adding Teams user PSTN to the call: {addParticipantResultTeamsPSTN.Value?.InvitationId}");


                //await Task.Delay(20000);

                ///* Start the Transcription */
                //await InitiateTranscription(callConnectionMedia);
                //logger.LogInformation("Transcription initiated.");


                //await Task.Delay(20000);
                //await PauseOrStopTranscription(callConnectionMedia, logger);

                //await Task.Delay(20000);
                //await ResumeTranscription(callConnectionMedia, logger);

                //await Task.Delay(20000);
                //await PauseOrStopTranscription(callConnectionMedia, logger);



                //await answerCallResult.CallConnection.MuteParticipantAsync(targetteamsUserPSTN).ConfigureAwait(false);

                //// Create the ServerCallLocator using the CallConnectionId
                //var serverCallLocator = new ServerCallLocator(callConnectionId);

                //// var callLocator = new ServerCallLocator(answer_result.SuccessResult.ServerCallId);
                //var recordingResult = await client.GetCallRecording().StartAsync(new StartRecordingOptions(serverCallLocator));
                //var recordingId = recordingResult.Value.RecordingId;
                //logger.LogInformation($"Recording started. RecordingId: {recordingId}");

                /////* Start the recording */
                // CallLocator callLocator = new ServerCallLocator(callConnectionId);

                //CallLocator callLocator = new ServerCallLocator(callConnectionId);
                //var recordingOptions = new StartRecordingOptions(callLocator)
                //{
                //    RecordingContent = RecordingContent.AudioVideo,  // Adjust based on content type
                //    RecordingFormat = RecordingFormat.Mp4,          // Supported format
                //    RecordingChannel = RecordingChannel.Mixed,      // Mixed or unmixed
                //    RecordingStateCallbackUri = callbackUri,
                //    PauseOnStart = false,
                //    RecordingStorage = RecordingStorage.CreateAzureBlobContainerRecordingStorage(new Uri("https://waferwirestorage.blob.core.windows.net/ga4byos"))
                //};
                //var recordingResult = await client.GetCallRecording().StartAsync(recordingOptions);
                //recordingId = recordingResult.Value.RecordingId;
                //logger.LogInformation($"Recording started. RecordingId: {recordingId}");


                // transfer call to Teams user(8:orgid)
                //  await  Task.Delay(20000);

                //var transferTeamsUser = new MicrosoftTeamsUserIdentifier("abd436ba-3128-4c3b-9c5a-b020cae8304e");
                //var transferParticipantOptionsTeams = new TransferToParticipantOptions(transferTeamsUser)
                //{
                //    Transferee = new PhoneNumberIdentifier("+18332638155")
                //};
                //var transferParticipantResultTeams = await answerCallResult.CallConnection.TransferCallToParticipantAsync(transferParticipantOptionsTeams);
                //logger.LogInformation("Transferring Teams user to the call: {InvitationId}", transferParticipantResultTeams.Value);


                //await Task.Delay(10000);
                //// customer is on hold
                //CommunicationIdentifier target = new MicrosoftTeamsUserIdentifier("abd436ba-3128-4c3b-9c5a-b020cae8304e");

                //HoldOptions holdOptions = new HoldOptions(target);

                //await callConnectionMedia.HoldAsync(holdOptions);
                //logger.LogInformation("Customer is put on hold.");
                //await Task.Delay(10000);
                //UnholdOptions unholdOptions = new UnholdOptions(target);
                //////// After the consultation, unhold the customer (PSTN user)
                //await callConnectionMedia.UnholdAsync(unholdOptions);
                //logger.LogInformation("PSTN customer is unheld.");


                // transfer call to Teams user (PSTN user number)
                //var transferTeamsUserPSTN = new PhoneNumberIdentifier("+12066663929");

                //var transferParticipantOptionsTeamsPSTN = new TransferToParticipantOptions(transferTeamsUserPSTN)
                //{
                //    Transferee = new PhoneNumberIdentifier("+18332638155")
                //};
                //var transferParticipantResultTeamsPSTN = await answerCallResult.CallConnection.TransferCallToParticipantAsync(transferParticipantOptionsTeamsPSTN);
                //logger.LogInformation("Transferring Teams user PSTN to the call: {InvitationId}", transferParticipantResultTeamsPSTN.Value);
                //await Task.Delay(20000);
                //CommunicationIdentifier target = new PhoneNumberIdentifier("+12066663929");
                //await answerCallResult.CallConnection.RemoveParticipantAsync(target).ConfigureAwait(false);
                //await ResumeRecording(callConnectionMedia, logger, recordingId);

            }

            client.GetEventProcessor().AttachOngoingEventProcessor<RecordingStateChanged>(
             answerCallResult.CallConnection.CallConnectionId, async (recordingStateChangedEvent) =>
             {
                 logger.LogInformation($"Recording State Changed: {recordingStateChangedEvent.GetType()}, context: {recordingStateChangedEvent.OperationContext}");
                 var recordingState = recordingStateChangedEvent.State;

                 // Log the recording state
                 logger.LogInformation($"Recording State Changed: {recordingStateChangedEvent.GetType()}, State: {recordingState}, Context: {recordingStateChangedEvent.OperationContext}");

             });
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
                 // await PauseOrStopRecording(callConnectionMedia, logger, true, recordingId);
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

            client.GetEventProcessor().AttachOngoingEventProcessor<TranscriptionStarted>(
               answerCallResult.CallConnection.CallConnectionId, async (transcriptionStarted) =>
               {
                   logger.LogInformation($"Received transcription event: {transcriptionStarted.GetType()}");
               });

            client.GetEventProcessor().AttachOngoingEventProcessor<TranscriptionStopped>(
                answerCallResult.CallConnection.CallConnectionId, async (transcriptionStopped) =>
                {
                    isTranscriptionActive = false;
                    logger.LogInformation("Received transcription event: {type}", transcriptionStopped.GetType());
                });

            client.GetEventProcessor().AttachOngoingEventProcessor<TranscriptionFailed>(
                answerCallResult.CallConnection.CallConnectionId, async (TranscriptionFailed) =>
                {
                    logger.LogInformation($"Received transcription event: {TranscriptionFailed.GetType()}, CorrelationId: {TranscriptionFailed.CorrelationId}, " +
                        $"SubCode: {TranscriptionFailed?.ResultInformation?.SubCode}, Message: {TranscriptionFailed?.ResultInformation?.Message}");
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

app.MapPost("/pause", async (ILogger<Program> logger, [FromQuery] string recordingId) =>
{
    await client.GetCallRecording().PauseAsync(recordingId);
    return Results.Ok();
});

app.MapPost("/resume", async (ILogger<Program> logger, [FromQuery] string recordingId) =>
{
    await client.GetCallRecording().ResumeAsync(recordingId);
    return Results.Ok();
});

app.MapPost("/stop", async (ILogger<Program> logger, [FromQuery] string recordingId) =>
{
    await client.GetCallRecording().StopAsync(recordingId);
    return Results.Ok();
});
async Task ResumeRecording(CallMedia callMedia, ILogger logger, string recordingId)
{
    
    await client.GetCallRecording().ResumeAsync(recordingId);
    logger.LogInformation($"Recording resumed. RecordingId: {recordingId}");
}

async Task PauseOrStopRecording(CallMedia callMedia, ILogger logger, bool stopRecording, string recordingId)
{
    

    if (stopRecording)
    {
        await client.GetCallRecording().StopAsync(recordingId);
        logger.LogInformation($"Recording stopped. RecordingId: {recordingId}");
    }
    else
    {
        await client.GetCallRecording().PauseAsync(recordingId);
        logger.LogInformation($"Recording paused. RecordingId: {recordingId}");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseWebSockets();
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            await Helper.ProcessRequest(webSocket);
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
    else
    {
        await next(context);
    }
});

app.Run();

async Task InitiateTranscription(CallMedia callConnectionMedia)
{
    StartTranscriptionOptions startTrasnscriptionOption = new StartTranscriptionOptions()
    {
        Locale = "en-US",
        OperationContext = "StartTranscript"
    };

    await callConnectionMedia.StartTranscriptionAsync(startTrasnscriptionOption);
    isTranscriptionActive = true;
}

async Task ResumeTranscription(CallMedia callMedia, ILogger logger)
{
    await InitiateTranscription(callMedia);
    logger.LogInformation("Transcription reinitiated.");

    
}

async Task PauseOrStopTranscription(CallMedia callMedia, ILogger logger)
{
    if (isTranscriptionActive)
    {
        await callMedia.StopTranscriptionAsync();
        isTranscriptionActive = false;
        logger.LogInformation("Transcription stopped.");
    }

     
}
async Task MuteParticipantAsync(string callConnectionId, string userid)
{
    try
    {
        var target = new PhoneNumberIdentifier(userid);
        var callConnection = client.GetCallConnection(callConnectionId);

        Console.WriteLine($"Attempting to mute participant with ACS ID: {userid} in call: {callConnectionId}.");

        await callConnection.MuteParticipantAsync(target).ConfigureAwait(false);

        Console.WriteLine($"Successfully muted participant with ACS ID: {userid} in call: {callConnectionId}.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to mute participant with ACS ID: {userid}. Error: {ex.Message}");
        throw; // Re-throw the exception to allow upstream handling if needed.
    }
}

