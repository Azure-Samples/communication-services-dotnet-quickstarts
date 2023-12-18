using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

/* Read config values from appsettings.json*/
var acsConnectionString = builder.Configuration.GetValue<string>("AcsConnectionString");
ArgumentNullException.ThrowIfNullOrEmpty(acsConnectionString);

var cognitiveServicesEndpoint = builder.Configuration.GetValue<string>("CognitiveServiceEndpoint");
ArgumentNullException.ThrowIfNullOrEmpty(cognitiveServicesEndpoint);

var transportUrl = builder.Configuration.GetValue<string>("TransportUrl");
ArgumentNullException.ThrowIfNullOrEmpty(transportUrl);

var acsPhoneNumber = builder.Configuration.GetValue<string>("AcsPhoneNumber");
ArgumentNullException.ThrowIfNullOrEmpty(acsPhoneNumber);

var locale = builder.Configuration.GetValue<string>("Locale");
ArgumentNullException.ThrowIfNullOrEmpty(locale);

var callbackUriHost = builder.Configuration.GetValue<string>("CallbackUriHost");
ArgumentNullException.ThrowIfNullOrEmpty(callbackUriHost);

var agentPhoneNumber = builder.Configuration.GetValue<string>("AgentPhoneNumber");
ArgumentNullException.ThrowIfNullOrEmpty(agentPhoneNumber);

/* Call Automation Client */
var client = new CallAutomationClient(pmaEndpoint: new Uri("PMA_ENDPOINT"), connectionString: acsConnectionString);

/* Register and make CallAutomationClient accessible via dependency injection */
builder.Services.AddSingleton(client);
var app = builder.Build();

string hellpIVRPrompt = "Hello, thank you for calling Contoso Utility service. Please type your date of birth in the format of Date, Month, Year. Example 01011990. before adding Agent to the call. This call is being recorded.";
string addAgentPrompt = "Sure, please stay on the line. I’m going to add the adgent to the call";
string incorrectDobPrompt = "I’m sorry, Date of birth seems incorrect format. Please type your date of birth in the format of Date, Month, Year. Example 01011990 ";
string addParticipantFailurePrompt = "It looks like all I can’t connect you to an agent right now, but we will get the next available agent to call you back as soon as possible.";
string goodbyePrompt = "Thank you for calling! Goodbye";
string timeoutSilencePrompt = "I’m sorry, I didn’t receive input from you. Please type your date of birth in the format of Date, Month, Year. Example 01011990. to help you further";
string goodbyeContext = "Goodbye";
string addAgentContext = "AddAgent";
string incorrectDobContext = "IncorrectDob";
string addParticipantFailureContext = "FailedToAddParticipant";
string DobRegex = "^(0[1-9]|[12][0-9]|3[01])(0[1-9]|1[012])[12][0-9]{3}$";
bool isTrasncriptionActive = false;
var maxTimeout = 2;

string recordingId = string.Empty;
string recordingLocation = string.Empty;

app.MapPost("/api/incomingCall", async (
    [FromBody] EventGridEvent[] eventGridEvents,
    ILogger<Program> logger) =>
{
    foreach (var eventGridEvent in eventGridEvents)
    {
        logger.LogInformation("Call event received:{eventType}", eventGridEvent.EventType);

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
            logger.LogInformation("Incoming call - correlationId: {cor}, Callback url: {callbackuri}, transport Url: {transurl}",
                incomingCallEventData.CorrelationId, callbackUri, transportUrl);

            TranscriptionOptions transcriptionOptions = new TranscriptionOptions(new Uri(transportUrl),
                TranscriptionTransport.Websocket, locale, false);

            var options = new AnswerCallOptions(incomingCallEventData.IncomingCallContext, callbackUri)
            {
                CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
                TranscriptionOptions = transcriptionOptions
            };

            AnswerCallResult answerCallResult = await client.AnswerCallAsync(options);
            var callConnectionMedia = answerCallResult.CallConnection.GetCallMedia();

            /* Use EventProcessor to process CallConnected event */
            var answer_result = await answerCallResult.WaitForEventProcessorAsync();
            if (answer_result.IsSuccess)
            {
                logger.LogInformation(
                "Received call event: {type}, callConnectionID: {connId}, serverCallId: {serverId}",
                answer_result.GetType(),
                answer_result.SuccessResult.CallConnectionId,
                answer_result.SuccessResult.ServerCallId);

                /* Start the recording */
                CallLocator callLocator = new ServerCallLocator(answer_result.SuccessResult.ServerCallId);
                var recordingResult = await client.GetCallRecording().StartAsync(new StartRecordingOptions(callLocator));
                recordingId = recordingResult.Value.RecordingId;
                logger.LogInformation("Recording started. RecordingId: {recid}", recordingId);

                /* Start the Transcription */
                await InitiateTranscription(callConnectionMedia);
                logger.LogInformation("Transcription initiated.");

                await PauseOrStopTranscriptionAndRecording(callConnectionMedia, logger, false, recordingId);

                /* Play hello prompt to user */
                await HandleDtmfRecognizeAsync(callConnectionMedia, callerId, hellpIVRPrompt, "hellocontext");
            }
            client.GetEventProcessor().AttachOngoingEventProcessor<PlayCompleted>(
                 answerCallResult.CallConnection.CallConnectionId, async (playCompletedEvent) =>
             {
                 logger.LogInformation("Received call event: {type}, context: {con}", playCompletedEvent.GetType(), playCompletedEvent.OperationContext);

                 if (playCompletedEvent.OperationContext == addAgentContext)
                 {
                     // Add Agent
                     var callInvite = new CallInvite(new PhoneNumberIdentifier(agentPhoneNumber),
                         new PhoneNumberIdentifier(acsPhoneNumber));

                     var addParticipantOptions = new AddParticipantOptions(callInvite)
                     {
                         OperationContext = addAgentContext
                     };

                     var addParticipantResult = await answerCallResult.CallConnection.AddParticipantAsync(addParticipantOptions);
                     logger.LogInformation($"Adding agent to the call: {addParticipantResult.Value?.InvitationId}");
                 }
                 else if (playCompletedEvent.OperationContext == goodbyeContext || playCompletedEvent.OperationContext == addParticipantFailureContext)
                 {
                     await PauseOrStopTranscriptionAndRecording(callConnectionMedia, logger, true, recordingId);
                     await answerCallResult.CallConnection.HangUpAsync(true);
                 }
             });
            client.GetEventProcessor().AttachOngoingEventProcessor<RecognizeCompleted>(
                answerCallResult.CallConnection.CallConnectionId, async (recognizeCompletedEvent) =>
            {
                logger.LogInformation("Received call event: {type}, context: {con}", recognizeCompletedEvent.GetType(), recognizeCompletedEvent.OperationContext);
                if (recognizeCompletedEvent.RecognizeResult is DtmfResult)
                {
                    var dtmfResult = recognizeCompletedEvent.RecognizeResult as DtmfResult;

                    //Take action for Recognition through DTMF 
                    var tones = dtmfResult?.ConvertToString();
                    Regex regex = new(DobRegex);
                    Match match = regex.Match(tones);
                    if (match.Success)
                    {
                        await ResumeTranscriptionAndRecording(callConnectionMedia, logger, recordingId);
                        await HandlePlayAsync(callConnectionMedia, addAgentPrompt, addAgentContext);
                    }
                    else
                    {
                        await HandleDtmfRecognizeAsync(callConnectionMedia, callerId, incorrectDobPrompt, incorrectDobContext);
                    }
                }
            });
            client.GetEventProcessor().AttachOngoingEventProcessor<AddParticipantSucceeded>(
               answerCallResult.CallConnection.CallConnectionId, async (addParticipantSucceededEvent) =>
               {
                   logger.LogInformation("Received call event: {type}, context: {con}", addParticipantSucceededEvent.GetType(), addParticipantSucceededEvent.OperationContext);
               });
            client.GetEventProcessor().AttachOngoingEventProcessor<ParticipantsUpdated>(
              answerCallResult.CallConnection.CallConnectionId, async (participantsUpdatedEvent) =>
              {
                  logger.LogInformation("Received call event: {type}, participants: {parts}, sequenceId: {seqid}",
                      participantsUpdatedEvent.GetType(), participantsUpdatedEvent.Participants.Count(), participantsUpdatedEvent.SequenceNumber);
              });
            client.GetEventProcessor().AttachOngoingEventProcessor<CallDisconnected>(
              answerCallResult.CallConnection.CallConnectionId, async (callDisconnectedEvent) =>
              {
                  logger.LogInformation("Received call event: {type}", callDisconnectedEvent.GetType());
              });
            client.GetEventProcessor().AttachOngoingEventProcessor<AddParticipantFailed>(
              answerCallResult.CallConnection.CallConnectionId, async (addParticipantFailedEvent) =>
              {
                  logger.LogInformation(
                      "Received call event: {type}, CorrelationId: {corId}, subCode: {sCode}, message: {mess}, context: {con}",
                      addParticipantFailedEvent.GetType(),
                      addParticipantFailedEvent.CorrelationId,
                      addParticipantFailedEvent.ResultInformation?.SubCode,
                      addParticipantFailedEvent.ResultInformation?.Message,
                      addParticipantFailedEvent.OperationContext);

                  await HandlePlayAsync(callConnectionMedia, addParticipantFailurePrompt, addParticipantFailureContext);
              });
            client.GetEventProcessor().AttachOngoingEventProcessor<PlayFailed>(
                answerCallResult.CallConnection.CallConnectionId, async (playFailedEvent) =>
            {
                logger.LogInformation(
               "Received call event: {type}, callConnectionID: {connId}, subCode: {sCode}, message: {mess}, context: {con}",
               playFailedEvent.GetType(),
               playFailedEvent.CallConnectionId,
               playFailedEvent.ResultInformation?.SubCode,
               playFailedEvent.ResultInformation?.Message,
               playFailedEvent.OperationContext);

                await PauseOrStopTranscriptionAndRecording(callConnectionMedia, logger, true, recordingId);
                await answerCallResult.CallConnection.HangUpAsync(true);
            });

            client.GetEventProcessor().AttachOngoingEventProcessor<RecognizeFailed>(
                answerCallResult.CallConnection.CallConnectionId, async (recognizeFailedEvent) =>
            {
                logger.LogInformation(
                "Received call event: {type}, CorrelationId: {corId}, subCode: {sCode}, message: {mess}, context: {con}",
                recognizeFailedEvent.GetType(),
                recognizeFailedEvent.CorrelationId,
                recognizeFailedEvent.ResultInformation?.SubCode,
                recognizeFailedEvent.ResultInformation?.Message,
                recognizeFailedEvent.OperationContext);

                if (MediaEventReasonCode.RecognizeInitialSilenceTimedOut.Equals(recognizeFailedEvent.ResultInformation?.SubCode.Value.ToString()) && maxTimeout > 0)
                {
                    logger.LogInformation("Retrying recognize...");
                    maxTimeout--;
                    await HandleDtmfRecognizeAsync(callConnectionMedia, callerId, timeoutSilencePrompt, "retryContext");
                }
                else
                {
                    logger.LogInformation("Playing goodbye message...");
                    await HandlePlayAsync(callConnectionMedia, goodbyePrompt, goodbyeContext);
                }
            });

            client.GetEventProcessor().AttachOngoingEventProcessor<TranscriptionStarted>(
                answerCallResult.CallConnection.CallConnectionId, async (transcriptionStarted) =>
                {
                    logger.LogInformation("Received transcription event: {type}", transcriptionStarted.GetType());
                });

            client.GetEventProcessor().AttachOngoingEventProcessor<TranscriptionResumed>(
                answerCallResult.CallConnection.CallConnectionId, async (transcriptionResumed) =>
                {
                    logger.LogInformation("Received transcription event: {type}", transcriptionResumed.GetType());
                });

            client.GetEventProcessor().AttachOngoingEventProcessor<TranscriptionStopped>(
                answerCallResult.CallConnection.CallConnectionId, async (transcriptionStopped) =>
                {
                    isTrasncriptionActive = false;
                    logger.LogInformation("Received transcription event: {type}", transcriptionStopped.GetType());
                });

            client.GetEventProcessor().AttachOngoingEventProcessor<TranscriptionFailed>(
                answerCallResult.CallConnection.CallConnectionId, async (TranscriptionFailed) =>
                {
                    logger.LogInformation(
                     "Received transcription event: {type}, CorrelationId: {corId}, SubCode: {sode}, Message: {message}",
                     TranscriptionFailed.GetType(),
                     TranscriptionFailed.CorrelationId,
                     TranscriptionFailed?.ResultInformation?.SubCode,
                     TranscriptionFailed?.ResultInformation?.Message);
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

async Task ResumeTranscriptionAndRecording(CallMedia callMedia, ILogger logger, string recordingId)
{
    await InitiateTranscription(callMedia);
    logger.LogInformation("Transcription reinitiated.");

    await client.GetCallRecording().ResumeAsync(recordingId);
    logger.LogInformation($"Recording resumed. RecordingId: {recordingId}");
}

async Task PauseOrStopTranscriptionAndRecording(CallMedia callMedia, ILogger logger, bool stopRecording, string recordingId)
{
    if (isTrasncriptionActive)
    {
        await callMedia.StopTranscriptionAsync();
        logger.LogInformation("Transcription stopped.");
    }

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

async Task HandleDtmfRecognizeAsync(CallMedia callConnectionMedia, string callerId, string message, string context)
{
    // Play greeting message
    var greetingPlaySource = new TextSource(message)
    {
        VoiceName = "en-US-NancyNeural"
    };

    var recognizeOptions =
        new CallMediaRecognizeDtmfOptions(
            targetParticipant: CommunicationIdentifier.FromRawId(callerId), maxTonesToCollect: 8)
        {
            InterruptPrompt = false,
            InterToneTimeout = TimeSpan.FromSeconds(5),
            Prompt = greetingPlaySource,
            OperationContext = context,
            InitialSilenceTimeout = TimeSpan.FromSeconds(15)
        };

    var recognize_result = await callConnectionMedia.StartRecognizingAsync(recognizeOptions);
}

async Task HandlePlayAsync(CallMedia callConnectionMedia, string textToPlay, string context)
{
    // Play message
    var playSource = new TextSource(textToPlay)
    {
        VoiceName = "en-US-NancyNeural"
    };

    var playOptions = new PlayToAllOptions(playSource) { OperationContext = context };
    await callConnectionMedia.PlayToAllAsync(playOptions);
}

async Task InitiateTranscription(CallMedia callConnectionMedia)
{
    StartTranscriptionOptions startTrasnscriptionOption = new StartTranscriptionOptions()
    {
        Locale = "en-US",
        OperationContext = "StartTranscript"
    };

    await callConnectionMedia.StartTranscriptionAsync(startTrasnscriptionOption);
    isTrasncriptionActive = true;
}

app.Run();