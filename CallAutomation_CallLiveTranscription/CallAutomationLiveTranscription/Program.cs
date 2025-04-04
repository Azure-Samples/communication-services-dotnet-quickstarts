using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using CallAutomation_LiveTranscription;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

/* Read config values from appsettings.json*/
var acsConnectionString = builder.Configuration.GetValue<string>("AcsConnectionString");
ArgumentNullException.ThrowIfNullOrEmpty(acsConnectionString);

var cognitiveServicesEndpoint = builder.Configuration.GetValue<string>("CognitiveServiceEndpoint");
ArgumentNullException.ThrowIfNullOrEmpty(cognitiveServicesEndpoint);

var acsPhoneNumber = builder.Configuration.GetValue<string>("AcsPhoneNumber");
ArgumentNullException.ThrowIfNullOrEmpty(acsPhoneNumber);

var locale = builder.Configuration.GetValue<string>("Locale");
ArgumentNullException.ThrowIfNullOrEmpty(locale);

var callbackUriHost = builder.Configuration.GetValue<string>("CallbackUriHost");
ArgumentNullException.ThrowIfNullOrEmpty(callbackUriHost);

var agentPhoneNumber = builder.Configuration.GetValue<string>("AgentPhoneNumber");
ArgumentNullException.ThrowIfNullOrEmpty(agentPhoneNumber);

/* Call Automation Client */
var client = new CallAutomationClient(connectionString: acsConnectionString);

Uri callbackUri = null!;

/* Register and make CallAutomationClient accessible via dependency injection */
builder.Services.AddSingleton(client);
var app = builder.Build();

string helpIVRPrompt = "Welcome to the Contoso Utilities. To access your account, we need to verify your identity. Please enter your date of birth in the format DDMMYYYY using the keypad on your phone. Once we’ve validated your identity we will connect you to the next available agent. Please note this call will be recorded!";
string addAgentPrompt = "Thank you for verifying your identity. We are now connecting you to the next available agent. Please hold the line and we will be with you shortly. Thank you for your patience.";
string incorrectDobPrompt = "Sorry, we were unable to verify your identity based on the date of birth you entered. Please try again. Remember to enter your date of birth in the format DDMMYYYY using the keypad on your phone. Once you've entered your date of birth, press the pound key. Thank you!";
string addParticipantFailurePrompt = "We're sorry, we were unable to connect you to an agent at this time, we will get the next available agent to call you back as soon as possible.";
string goodbyePrompt = "Thank you for calling Contoso Utilities. We hope we were able to assist you today. Goodbye";
string timeoutSilencePrompt = "I’m sorry, I didn’t receive any input. Please type your date of birth in the format of DDMMYYYY.";
string goodbyeContext = "Goodbye";
string addAgentContext = "AddAgent";
string incorrectDobContext = "IncorrectDob";
string addParticipantFailureContext = "FailedToAddParticipant";
string DobRegex = "^(0[1-9]|[12][0-9]|3[01])(0[1-9]|1[012])[12][0-9]{3}$";
var maxTimeout = 2;

string recordingId = string.Empty;
string recordingLocation = string.Empty;

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
            var rawId = incomingCallEventData.FromCommunicationIdentifier.RawId;
            var callerId = JsonSerializer.Serialize(rawId);
            callbackUri = new Uri(new Uri(callbackUriHost), $"/api/callbacks/{Guid.NewGuid()}?identifier={callerId}");
            var websocketUri = callbackUriHost.Replace("https", "wss") + "/ws";
            logger.LogInformation($"Incoming call - correlationId: {incomingCallEventData.CorrelationId}, " +
                $"Callback url: {callbackUri}, websocket Url: {websocketUri}");

            TranscriptionOptions transcriptionOptions = new TranscriptionOptions(new Uri(websocketUri),
                locale, true, TranscriptionTransport.Websocket);

            var options = new AnswerCallOptions(incomingCallEventData.IncomingCallContext, callbackUri)
            {
                CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
                TranscriptionOptions = transcriptionOptions
            };

            AnswerCallResult answerCallResult = await client.AnswerCallAsync(options);
            logger.LogInformation($"Call answered successfully for caller: {rawId}");
        }
    }
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
                logger.LogInformation("Call recording file status updated");
                recordingLocation = statusUpdated.RecordingStorageInfo.RecordingChunks[0].ContentLocation;
                logger.LogInformation($"The recording location is : {recordingLocation}");
            }
        }
    }
    return Results.Ok();
});

// api to handle call back events
app.MapPost("/api/callbacks/{contextId}", async (
    [FromBody] CloudEvent[] cloudEvents,
    [FromRoute] string contextId,
    [Required] string identifier,
    ILogger<Program> logger) =>
{
    string? callerId = JsonSerializer.Deserialize<string>(identifier);
    foreach (var cloudEvent in cloudEvents)
    {
        CallAutomationEventBase parsedEvent = CallAutomationEventParser.Parse(cloudEvent);
        logger.LogInformation(
                    "Received call event: {type}, callConnectionID: {connId}, serverCallId: {serverId}",
                    parsedEvent.GetType(),
                    parsedEvent.CallConnectionId,
                    parsedEvent.ServerCallId);

        if (parsedEvent is CallConnected callConnected)
        {
            logger.LogInformation($"Received call event: {callConnected.GetType()} with callerId - {callerId}");
            var callConnectionId = callConnected.CallConnectionId;

            CallConnectionProperties callConnectionProperties = GetCallConnectionProperties(callConnectionId);

            logger.LogInformation($"Correlation Id: {callConnectionProperties.CorrelationId}");

            logger.LogInformation($"Transcription state: {callConnectionProperties.TranscriptionSubscription.State}");

            /* Start the recording */
            CallLocator callLocator = new ServerCallLocator(callConnectionProperties.ServerCallId);

            StartRecordingOptions startRecordingOptions = new StartRecordingOptions(callLocator)
            {
                RecordingContent = RecordingContent.AudioVideo,
                RecordingChannel = RecordingChannel.Mixed,
                RecordingFormat = RecordingFormat.Mp4,
                RecordingStateCallbackUri = callbackUri,
                PauseOnStart = true
            };
            var recordingResult = await client.GetCallRecording().StartAsync(startRecordingOptions);
            recordingId = recordingResult.Value.RecordingId;
            logger.LogInformation($"Recording started. RecordingId: {recordingId}");
        }
        else if (parsedEvent is RecognizeCompleted recognizeCompleted)
        {
            logger.LogInformation($"Received call event: {recognizeCompleted.GetType()}, context: {recognizeCompleted.OperationContext}");
            CallMedia callMedia = GetCallMedia(recognizeCompleted.CallConnectionId);

            if (recognizeCompleted.RecognizeResult is DtmfResult)
            {
                var dtmfResult = recognizeCompleted.RecognizeResult as DtmfResult;

                //Take action for Recognition through DTMF 
                var tones = dtmfResult?.ConvertToString();
                Regex regex = new(DobRegex);
                Match match = regex.Match(tones!);
                if (match.Success)
                {
                    await ResumeTranscriptionAndRecording(callMedia, logger, recordingId);
                }
                else
                {
                    await HandleDtmfRecognizeAsync(callMedia, callerId!, incorrectDobPrompt, incorrectDobContext);
                }
            }
        }
        else if (parsedEvent is RecognizeFailed recognizeFailed)
        {
            CallMedia callMedia = GetCallMedia(recognizeFailed.CallConnectionId);
            CallConnection callConnection = GetConnection(recognizeFailed.CallConnectionId);

            LogEventDetails(recognizeFailed, logger);

            if (MediaEventReasonCode.RecognizeInitialSilenceTimedOut.Equals(recognizeFailed.ResultInformation?.SubCode!.Value.ToString()) && maxTimeout > 0)
            {
                logger.LogInformation("Retrying recognize...");
                maxTimeout--;
                await HandleDtmfRecognizeAsync(callMedia, callerId!, timeoutSilencePrompt, "retryContext");
            }
            else
            {
                logger.LogInformation("Playing goodbye message...");
                await HandlePlayAsync(callMedia, goodbyePrompt, goodbyeContext);
            }

        }
        else if (parsedEvent is PlayStarted playStarted)
        {
            logger.LogInformation($"Received call event: {playStarted.GetType()}");

        }
        else if (parsedEvent is PlayCompleted playCompleted)
        {
            logger.LogInformation($"Received call event: {playCompleted.GetType()}");

            CallMedia callMedia = GetCallMedia(playCompleted.CallConnectionId);
            CallConnection callConnection = GetConnection(playCompleted.CallConnectionId);

            if (playCompleted.OperationContext == addAgentContext)
            {
                // Add Agent
                var callInvite = new CallInvite(new PhoneNumberIdentifier(agentPhoneNumber),
                    new PhoneNumberIdentifier(acsPhoneNumber));

                var addParticipantOptions = new AddParticipantOptions(callInvite)
                {
                    OperationContext = addAgentContext
                };

                var addParticipantResult = await callConnection.AddParticipantAsync(addParticipantOptions);
                logger.LogInformation($"Adding agent to the call: {addParticipantResult.Value?.InvitationId}");
            }
            else if (playCompleted.OperationContext == goodbyeContext || playCompleted.OperationContext == addParticipantFailureContext)
            {
                await StopTranscriptionAndRecording(callMedia, logger, playCompleted.CallConnectionId, recordingId);
                await callConnection.HangUpAsync(true);
            }
        }
        else if (parsedEvent is PlayFailed playFailed)
        {
            CallMedia callMedia = GetCallMedia(playFailed.CallConnectionId);
            CallConnection callConnection = GetConnection(playFailed.CallConnectionId);

            LogEventDetails(playFailed, logger);

            await StopTranscriptionAndRecording(callMedia, logger, playFailed.CallConnectionId, recordingId);
            await callConnection.HangUpAsync(true);
        }
        else if (parsedEvent is AddParticipantSucceeded addParticipantSucceeded)
        {
            logger.LogInformation($"Received call event: {addParticipantSucceeded.GetType()}, context: {addParticipantSucceeded.OperationContext}");
        }
        else if (parsedEvent is AddParticipantFailed addParticipantFailed)
        {
            CallMedia callMedia = GetCallMedia(addParticipantFailed.CallConnectionId);

            LogEventDetails(addParticipantFailed, logger);

            await HandlePlayAsync(callMedia, addParticipantFailurePrompt, addParticipantFailureContext);

        }
        else if (parsedEvent is TranscriptionStarted transcriptionStarted)
        {
            logger.LogInformation($"Received call event: {transcriptionStarted.GetType()}");
            logger.LogInformation($"Transcription status:-  {transcriptionStarted.TranscriptionUpdate.TranscriptionStatus}");

            CallMedia callMedia = GetCallMedia(transcriptionStarted.CallConnectionId);

            // stop transcription for the first time before receiving user input.
            if (string.IsNullOrEmpty(transcriptionStarted.OperationContext))
            {
                StopTranscriptionOptions options = new StopTranscriptionOptions()
                {
                    OperationContext = "nextRecognizeContext"
                };
                await callMedia.StopTranscriptionAsync(options);
            }
            else if (!string.IsNullOrEmpty(transcriptionStarted.OperationContext) && transcriptionStarted.OperationContext.Equals("StartTranscriptionContext"))
            {
                await HandlePlayAsync(callMedia, addAgentPrompt, addAgentContext);
            }
        }
        else if (parsedEvent is TranscriptionStopped transcriptionStopped)
        {
            logger.LogInformation("Received transcription event: {type}", transcriptionStopped.GetType());
            logger.LogInformation($"Transcription status:-  {transcriptionStopped.TranscriptionUpdate.TranscriptionStatus}");

            CallMedia callMedia = GetCallMedia(transcriptionStopped.CallConnectionId);

            if (!string.IsNullOrEmpty(transcriptionStopped.OperationContext) && transcriptionStopped.OperationContext.Equals("nextRecognizeContext"))
            {
                // get user input with inactive recording and transcription.
                await HandleDtmfRecognizeAsync(callMedia, callerId!, helpIVRPrompt, "hellocontext");
            }
        }
        else if (parsedEvent is TranscriptionFailed transcriptionFailed)
        {
            LogEventDetails(transcriptionFailed, logger);
        }
        else if (parsedEvent is RecordingStateChanged recordingStateChanged)
        {
            logger.LogInformation("Received transcription event: {type}", recordingStateChanged.GetType());
            logger.LogInformation("Recording state:--> {type}", recordingStateChanged.State);
        }
        else if (parsedEvent is CallDisconnected callDisconnected)
        {
            logger.LogInformation($"Received call event: {callDisconnected.GetType()}");
        }
    }
    return Results.Ok();
});

app.MapGet("/download", (ILogger<Program> logger) =>
{
    if (!string.IsNullOrEmpty(recordingLocation))
    {
        string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        var date = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"Recording_{date}.mp4";
        client.GetCallRecording().DownloadTo(new Uri(recordingLocation), $"{downloadsPath}\\{fileName}");
    }
    else
    {
        logger.LogError("Recording is not available");
    }

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

async Task StopTranscriptionAndRecording(CallMedia callMedia, ILogger logger, string callConnectionId, string recordingId)
{
    CallConnectionProperties callConnectionProperties = GetCallConnectionProperties(callConnectionId);
    RecordingStateResult recordingStateResult = await client.GetCallRecording().GetStateAsync(recordingId);

    if (callConnectionProperties.TranscriptionSubscription.State == TranscriptionSubscriptionState.Active)
    {
        await callMedia.StopTranscriptionAsync();
        logger.LogInformation("Stopping transcription");
    }

    if (recordingStateResult.RecordingState == RecordingState.Active)
    {
        await client.GetCallRecording().StopAsync(recordingId);
        logger.LogInformation("Stopping recording");
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
    var textSource = new TextSource(textToPlay)
    {
        VoiceName = "en-US-NancyNeural"
    };

    var playOptions = new PlayToAllOptions(textSource) { OperationContext = context };
    await callConnectionMedia.PlayToAllAsync(playOptions);
}

async Task InitiateTranscription(CallMedia callConnectionMedia)
{
    StartTranscriptionOptions startTranscriptionOptions = new StartTranscriptionOptions()
    {
        Locale = "en-US",
        OperationContext = "StartTranscriptionContext"
    };

    await callConnectionMedia.StartTranscriptionAsync(startTranscriptionOptions);
}

CallConnection GetConnection(string callConnectionId)
{
    CallConnection callConnection = !string.IsNullOrEmpty(callConnectionId) ?
        client.GetCallConnection(callConnectionId)
        : throw new ArgumentNullException(nameof(callConnectionId));
    return callConnection;
}

CallMedia GetCallMedia(string callConnectionId)
{
    CallMedia callMedia = !string.IsNullOrEmpty(callConnectionId) ?
        client.GetCallConnection(callConnectionId).GetCallMedia()
        : throw new ArgumentNullException(nameof(callConnectionId));
    return callMedia;
}

CallConnectionProperties GetCallConnectionProperties(string callConnectionId)
{
    CallConnectionProperties callConnectionProperties = !string.IsNullOrEmpty(callConnectionId) ?
       client.GetCallConnection(callConnectionId).GetCallConnectionProperties()
       : throw new ArgumentNullException(nameof(callConnectionId));
    return callConnectionProperties;
}

void LogEventDetails(CallAutomationEventBase eventBase, ILogger<Program> logger)
{
    if (eventBase != null)
    {
        logger.LogInformation($"Received {eventBase.GetType()} event");
        logger.LogInformation($"CallConnectionId:--> {eventBase.CallConnectionId}");
        logger.LogInformation($"CorrelationId:--> {eventBase.CorrelationId}");
        logger.LogInformation($"OperationContext:--> {eventBase?.OperationContext}");
        if (eventBase != null && eventBase.ResultInformation != null)
        {
            logger.LogInformation($"Message:--> {eventBase.ResultInformation.Message}");
            logger.LogInformation($"Code:--> {eventBase.ResultInformation?.Code}");
            logger.LogInformation($"Code:--> {eventBase.ResultInformation?.SubCode}");
        }
    }
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