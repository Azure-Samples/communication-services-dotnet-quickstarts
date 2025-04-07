using Azure;
using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using CallAutomation_OPS_CallingScenarios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Numerics;
using System.Runtime.Intrinsics.X86;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

var participantPhoneNumber = builder.Configuration.GetValue<string>("ParticipantPhoneNumber");
var callerPhoneNumber = builder.Configuration.GetValue<string>("AcsPhoneNumber");


var callerTeamsUser = builder.Configuration.GetValue<string>("TeamsUserId");

var participantTeamsUser = builder.Configuration.GetValue<string>("ParticipantTeamsUser");
var transfereePhoneNumber = builder.Configuration.GetValue<string>("TransfereePhoneNumber");



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
AnswerCallResult answerCallResult;
ILogger<Program> _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<Program>();

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

            var websocketUri = callbackUriHost.Replace("https", "wss") + "ws";
            logger.LogInformation($"Incoming call - correlationId: {incomingCallEventData.CorrelationId}, " +
                $"Callback url: {callbackUri}, websocket Url: {websocketUri}");

            TranscriptionOptions transcriptionOptions = new TranscriptionOptions(new Uri(websocketUri),
                "en-US", false, TranscriptionTransport.Websocket);
           // var callerPhonenumber = incomingCallEventData.FromCommunicationIdentifier.PhoneNumber.Value;

            var answerCallOptions = new AnswerCallOptions(incomingCallEventData.IncomingCallContext, callbackUri)
            {
                CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) }
               
            };

            // Adding Custom SIP Headers (User-to-User and X-Headers)
            answerCallOptions.CustomCallingContext.AddSipUui("OBOuuivalue"); // User-to-User header
            answerCallOptions.CustomCallingContext.AddSipX("X-CustomHeader1", "HeaderValue1"); // X-header


            answerCallResult = await client.AnswerCallAsync(answerCallOptions);
            var callConnectionMedia = answerCallResult.CallConnection.GetCallMedia();
           

            /* Use EventProcessor to process CallConnected event */
            var answer_result = await answerCallResult.WaitForEventProcessorAsync();
            if (answer_result.IsSuccess)
            {
                logger.LogInformation($"CA Received call event: {answer_result.GetType()}, callConnectionID: {answer_result.SuccessResult.CallConnectionId}, " +
                    $"serverCallId: {answer_result.SuccessResult.ServerCallId}");
                callConnectionId = answer_result.SuccessResult.CallConnectionId;
              
                await Task.Delay(20000);

               // await AddDualPersonaUserCCaaSAgentAsync();

                 await AddTeamsUserAsCCaaSAgentAsync();
                // await MuteParticipantAsync(callConnectionId, callerId);
                await Task.Delay(20000);

                await AddTeamsUserByOrgIdAsync();

                await Task.Delay(20000);

                CommunicationIdentifier target = new PhoneNumberIdentifier("+18332638155");
                await client.GetCallConnection(callConnectionId).MuteParticipantAsync(target).ConfigureAwait(false);
                await answerCallResult.CallConnection.MuteParticipantAsync(target).ConfigureAwait(false);

                CommunicationIdentifier target1 = new MicrosoftTeamsUserIdentifier(teamsUserId);
                await client.GetCallConnection(callConnectionId).MuteParticipantAsync(target1).ConfigureAwait(false);

                await Task.Delay(20000);

                await client.GetCallConnection(callConnectionId).UnmuteParticipantAsync(target1).ConfigureAwait(false);

                //   await Task.Delay(50000);
                //  await PlayMediaAsync(true, false, false);
                // await Task.Delay(50000);
                //await Task.Delay(20000);

                ///* Start the recording */
                //CallLocator callLocator = new ServerCallLocator(answer_result.SuccessResult.ServerCallId);
                //var recordingResult = await client.GetCallRecording().StartAsync(new StartRecordingOptions(callLocator));
                //recordingId = recordingResult.Value.RecordingId;
                //logger.LogInformation($"Recording started. RecordingId: {recordingId}");

                //await Task.Delay(20000);
                //await PlayMediaAsync(false, false, false);

                //await Task.Delay(20000);
                /* Start the recording */

                //var recordingResult = await StartCallRecordingAsync(callConnectionId, callbackUri);

                //recordingId = recordingResult.Value.RecordingId;
                //logger.LogInformation($"Recording started. RecordingId: {recordingId}");

                // /* Start the Transcription */
                // await InitiateTranscription(callConnectionMedia);
                // logger.LogInformation("Transcription initiated.");

                // await PauseOrStopTranscription(callConnectionMedia, logger);

                // await Task.Delay(20000);

                //await PauseOrStopRecording(callConnectionMedia, logger, false, recordingId);

                // // Add CCaaS Agent(dual persona user)
                //await AddDualPersonaUserCCaaSAgentAsync();
                // //await Task.Delay(20000);
                // //Add Teams User as CCaaS Agent
                // await AddTeamsUserAsCCaaSAgentAsync();
                // //await Task.Delay(20000);
                // //Add Teams User (8:orgid)
                // await AddTeamsUserByOrgIdAsync();
                // //await Task.Delay(20000);
                // // Add PSTN User(Teams User with Phone Number)
                // await AddTeamsUserByOrgIdAsync();
                // //await Task.Delay(20000);
                // // Add PSTN User to call
                // await AddPSTNUserToCallAsync();
                // //await Task.Delay(20000);
                // // Transfer call to Teams User(8:orgid)
                // await TransferCallToTeamsUserOrgIdAsync();
                // //await Task.Delay(20000);
                // // Transfer call to PSTN User
                // await TransferCallToPSTNUserAsync();
                // //await Task.Delay(20000);
                // // Add PSTN Participant
                // await AddPSTNParticipantAsync();
                // //await Task.Delay(20000);
                // // Play media to Teams User
                // await PlayMediaAsync(false, true, false);
                // //await Task.Delay(20000);
                // // Play media to PSTN User
                // await PlayMediaAsync(false, false, false);
                // //await Task.Delay(20000);
                // // Play media to all
                // await PlayMediaAsync(true, false, false);
                // //await Task.Delay(20000);
                // // Play media to all
                // await PlayMediaAsync(true, true, false);
                // //await Task.Delay(20000);
                // // Pause Recording
                // await PauseOrStopRecording(callConnectionMedia, logger, false, recordingId);
                // //await Task.Delay(20000);
                // // Resume Recording
                // await ResumeRecording(callConnectionMedia, logger, recordingId);
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

            }
            client.GetEventProcessor().AttachOngoingEventProcessor<PlayFailed>(
               answerCallResult.CallConnection.CallConnectionId, async (playFailedEvent) =>
               {
                   logger.LogInformation($"Received call event: {playFailedEvent.GetType()}, CorrelationId: {playFailedEvent.CorrelationId}, " +
                       $"subCode: {playFailedEvent.ResultInformation?.SubCode}, message: {playFailedEvent.ResultInformation?.Message}, context: {playFailedEvent.OperationContext}");
                   callConnectionId = playFailedEvent.CallConnectionId;
               });
            client.GetEventProcessor().AttachOngoingEventProcessor<PlayCanceled>(
               answerCallResult.CallConnection.CallConnectionId, async (playCanceled) =>
               {
                   logger.LogInformation($"Received event: {playCanceled.GetType()}");
                   callConnectionId = playCanceled.CallConnectionId;
               });


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

  async Task AddDualPersonaUserCCaaSAgentAsync()
{
    var ccaasAgent = new CommunicationUserIdentifier(dualPersonaUserId); // dual persona user
    var callInvite = new CallInvite(ccaasAgent);
    var addParticipantOptions = new AddParticipantOptions(callInvite);

    var addParticipantResult = await answerCallResult.CallConnection.AddParticipantAsync(addParticipantOptions).ConfigureAwait(false);
    _logger.LogInformation($"Adding DualPersonaUser as CCaaS agent  to the call: {addParticipantResult.Value?.InvitationId}");

}

async Task AddTeamsUserAsCCaaSAgentAsync()
{
    var ccaasAgent = new MicrosoftTeamsUserIdentifier(cCaaSAgentUserId); // teams user
    var callInvite = new CallInvite(ccaasAgent);
    var addParticipantOptions = new AddParticipantOptions(callInvite);

    var addParticipantResult = await answerCallResult.CallConnection.AddParticipantAsync(addParticipantOptions).ConfigureAwait(false);
    _logger.LogInformation($"Adding TeamsUser  to the call: {addParticipantResult.Value?.InvitationId}");

}

  async Task AddTeamsUserByOrgIdAsync()
{
    //// add  Teams user (8:orgid)
    var teamsUser = new MicrosoftTeamsUserIdentifier(teamsUserId);
    var callInviteTeams = new CallInvite(teamsUser);
    var addParticipantOptionsTeams = new AddParticipantOptions(callInviteTeams);
    var addParticipantResultTeams = await answerCallResult.CallConnection.AddParticipantAsync(addParticipantOptionsTeams).ConfigureAwait(false);
    _logger.LogInformation($"Adding Teams user to the call: {addParticipantResultTeams.Value?.InvitationId}");
}

  async Task AddPSTNUserToCallAsync()
{
    // add Teams user (PSTN user number)
    var targetteamsUserPSTN = new PhoneNumberIdentifier(teamsUserPSTNNumber);
    var callerPSTNNumber = new PhoneNumberIdentifier(acsPhoneNumber);
    var callInviteTeamsPSTN = new CallInvite(targetteamsUserPSTN, callerPSTNNumber);
    var addParticipantOptionsTeamsPSTN = new AddParticipantOptions(callInviteTeamsPSTN);
    var addParticipantResultTeamsPSTN = await answerCallResult.CallConnection.AddParticipantAsync(addParticipantOptionsTeamsPSTN);
    _logger.LogInformation($"Adding Teams user PSTN to the call: {addParticipantResultTeamsPSTN.Value?.InvitationId}");

}

  async Task TransferCallToTeamsUserOrgIdAsync()
{
    // transfer call to Teams user(8:orgid)

    var transferTeamsUser = new MicrosoftTeamsUserIdentifier(teamsUserId);
    var transferParticipantOptionsTeams = new TransferToParticipantOptions(transferTeamsUser)
    {
        Transferee = new PhoneNumberIdentifier(transfereePhoneNumber)
    };
    var transferParticipantResultTeams = await answerCallResult.CallConnection.TransferCallToParticipantAsync(transferParticipantOptionsTeams);
    _logger.LogInformation("Transferring Teams user to the call: {InvitationId}", transferParticipantResultTeams.Value);

}

  async Task TransferCallToPSTNUserAsync()
{
    // transfer call to Teams user (PSTN user number)
    var transferTeamsUserPSTN = new PhoneNumberIdentifier(teamsUserPSTNNumber);

    var transferParticipantOptionsTeamsPSTN = new TransferToParticipantOptions(transferTeamsUserPSTN)
    {
        Transferee = new PhoneNumberIdentifier(transfereePhoneNumber)
    };
    var transferParticipantResultTeamsPSTN = await answerCallResult.CallConnection.TransferCallToParticipantAsync(transferParticipantOptionsTeamsPSTN);
    _logger.LogInformation("Transferring Teams user PSTN to the call: {InvitationId}", transferParticipantResultTeamsPSTN.Value);
}

  async Task AddPSTNParticipantAsync()
{
    // Create the PSTN participant identifier (replace with the actual PSTN number)
    var pstnPhoneNumberIdentifier = new PhoneNumberIdentifier(participantPhoneNumber);

    // Create your ACS phone number identifier (replace with your ACS number)
    var callerPhoneNumberIdentifier = new PhoneNumberIdentifier(acsPhoneNumber);

    // Create a CallInvite with the PSTN participant and ACS phone number (caller is optional)
    var callInvite = new CallInvite(pstnPhoneNumberIdentifier, callerPhoneNumberIdentifier);

    // Configure AddParticipant options
    var addParticipantsOptions = new AddParticipantOptions(callInvite)
    {
        OperationContext = "AddPSTNUserOperation" // Optional context for tracking
    };

    try
    {
        // Get the call connection and add the PSTN participant to the call
        var addParticipantsResult = await answerCallResult.CallConnection.AddParticipantAsync(addParticipantsOptions);

        
    }
    catch (Exception ex)
    {
        _logger.LogError($"Error adding PSTN participant: {ex.Message}");
    }
}


async Task PlayMediaAsync(bool isPlayToAll, bool isTeamsUser, bool isPlayMediaToCaller)
{
    CallMedia callMedia = GetCallMedia();

    FileSource fileSource = new FileSource(new Uri(fileSourceUri));

    TextSource textSource = new TextSource("Hi, this is text source played through play source thanks. Goodbye!.")
    {
        VoiceName = "en-US-NancyNeural"
    };

    SsmlSource ssmlSource = new SsmlSource("<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"><voice name=\"en-US-JennyNeural\">Hi, this is ssml test played through ssml source thanks. Goodbye!</voice></speak>");

    List<PlaySource> playSources = new List<PlaySource>()
    {
       fileSource

    };
    //textSource,ssmlSource
    if (isPlayToAll)
    {
        PlayToAllOptions playToAllOptions = new PlayToAllOptions(playSources)
        {
            OperationContext = "playToAllContext"
        };
        await callMedia.PlayToAllAsync(playToAllOptions);
    }
    else
    {
        CommunicationIdentifier target = GetCommunicationTargetIdentifier(isTeamsUser, isPlayMediaToCaller);

        Console.WriteLine("Target User:--> " + target);

        var playTo = new List<CommunicationIdentifier> { target };

        PlayOptions playToOptions = new PlayOptions(playSources, playTo)
        {
            OperationContext = "playToContext"
        };

        if (target != null)
        {
            await callMedia.PlayAsync(playToOptions);
        }
        else
        {
            Console.WriteLine("TARGET IS EMPTY...");
        }

    }
}
CallMedia GetCallMedia()
{
    CallMedia callMedia = !string.IsNullOrEmpty(callConnectionId) ?
        client.GetCallConnection(callConnectionId).GetCallMedia()
        : throw new ArgumentNullException("Call connection id is empty");

    return callMedia;
}
CommunicationIdentifier GetCommunicationTargetIdentifier(bool isTeamsUser, bool isPlayMediaToCaller)
{
    string teamsIdentifier = isPlayMediaToCaller ? callerTeamsUser : participantTeamsUser;

    string pstnIdentifier = isPlayMediaToCaller ? callerPhoneNumber : participantPhoneNumber;

    CommunicationIdentifier target = isTeamsUser ? new MicrosoftTeamsUserIdentifier(teamsIdentifier) :
        new PhoneNumberIdentifier(pstnIdentifier);

    return target;
}

async Task<Response<RecordingStateResult>> StartCallRecordingAsync(string callConnectionId, Uri callbackUri)
{
    try
    {
        // Create a CallLocator to locate the call based on the call connection ID
        var callLocator = new ServerCallLocator(callConnectionId);

        // Create the recording options with various configurations
        var recordingOptions = new StartRecordingOptions(callLocator)
        {
            RecordingContent = RecordingContent.Audio,  // You can adjust this to capture other contents (Audio, Video, or Both)
            RecordingFormat = RecordingFormat.Mp3,      // Specify the format of the recording, e.g., MP3, WAV, etc.
            RecordingChannel = RecordingChannel.Mixed,  // Mixed channel will include both the caller and callee's audio
            RecordingStateCallbackUri = callbackUri,    // Webhook URL for recording status notifications (optional)
            PauseOnStart = false                        // Do not pause on start (you can set this to true if you want to pause initially)
        };

        // Start the recording asynchronously
        var recordingResult = await client.GetCallRecording().StartAsync(recordingOptions);

        // Get the recording ID from the result and log it
        var recordingId = recordingResult.Value.RecordingId;
        _logger.LogInformation($"Recording started successfully. RecordingId: {recordingId}");
        return recordingResult;
    }
    catch (Exception ex)
    {
        // Handle any exceptions that may occur
        _logger.LogError($"Error starting call recording: {ex.Message}");
        return null; // Return null or handle the error as needed
    }
}
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
async Task MuteParticipantAsync(string callConnectionId, string userid)
{
    try
    {
        var target = new PhoneNumberIdentifier(userid);
        var callConnection = client.GetCallConnection(callConnectionId);

        Console.WriteLine($"Attempting to mute participant ID: {userid} in call: {callConnectionId}.");

        await callConnection.MuteParticipantAsync(target).ConfigureAwait(false);

        Console.WriteLine($"Successfully muted participant with  ID: {userid} in call: {callConnectionId}.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to mute participant with ACS ID: {userid}. Error: {ex.Message}");
        throw; // Re-throw the exception to allow upstream handling if needed.
    }
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

