using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

var acsConnectionString = builder.Configuration.GetValue<string>("AcsConnectionString");
ArgumentNullException.ThrowIfNullOrEmpty(acsConnectionString);

var cognitiveServicesEndpoint = builder.Configuration.GetValue<string>("CognitiveServicesEndpoint");
ArgumentNullException.ThrowIfNullOrEmpty(cognitiveServicesEndpoint);


var acsPhoneNumber = builder.Configuration.GetValue<string>("AcsPhoneNumber");
ArgumentNullException.ThrowIfNullOrEmpty(acsPhoneNumber);

var callbackUriHost = builder.Configuration.GetValue<string>("CallbackUriHost");
ArgumentNullException.ThrowIfNullOrEmpty(callbackUriHost);

var client = new CallAutomationClient(connectionString: acsConnectionString);

app.UseSwagger();
app.UseSwaggerUI();

string callConnectionId = string.Empty;
string recordingId = string.Empty;
string recordingLocation = string.Empty;
Uri eventCallbackUri = null;
string callerId = string.Empty;


app.MapPost("/api/events",async (EventGridEvent[] eventGridEvents, ILogger<Program> logger) =>
{
    foreach (var eventGridEvent in eventGridEvents)
    {
        logger.LogInformation($"Recording event received:{eventGridEvent.EventType}");
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
                callerId = incomingCallEventData.FromCommunicationIdentifier.RawId;
                Console.WriteLine("Caller ID-->" +  callerId);
                var callbackUri = new Uri(new Uri(callbackUriHost), $"/api/callbacks");
                logger.LogInformation($"Incoming call - correlationId: {incomingCallEventData.CorrelationId}, " +
                    $"Callback url: {callbackUri}");

                eventCallbackUri = callbackUri;

                var options = new AnswerCallOptions(incomingCallEventData.IncomingCallContext, callbackUri)
                {
                    CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) }
                };

                AnswerCallResult answerCallResult = await client.AnswerCallAsync(options);
                var callConnectionMedia = answerCallResult.CallConnection.GetCallMedia();

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

app.MapPost("/api/callbacks", async (CloudEvent[] cloudEvents, ILogger<Program> logger) =>
{
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
            logger.LogInformation($"Received call event: {callConnected.GetType()}");
            callConnectionId = callConnected.CallConnectionId;
            CallConnectionProperties callConnectionProperties = GetCallConnectionProperties();
            logger.LogInformation("************************************************************");
            logger.LogInformation($"CORRELATION ID: {callConnectionProperties.CorrelationId}");
            logger.LogInformation("************************************************************");
        }
        else if (parsedEvent is RecognizeCompleted recognizeCompleted)
        {
            logger.LogInformation($"Received call event: {recognizeCompleted.GetType()}");
            callConnectionId = recognizeCompleted.CallConnectionId;

            switch (recognizeCompleted.RecognizeResult)
            {
                case DtmfResult dtmfResult:
                    var tones = dtmfResult.Tones;
                    logger.LogInformation("Recognize completed succesfully, tones={tones}", tones);
                    break;
                case ChoiceResult choiceResult:
                    var labelDetected = choiceResult.Label;
                    var phraseDetected = choiceResult.RecognizedPhrase;
                    logger.LogInformation("Recognize completed succesfully, labelDetected={labelDetected}, phraseDetected={phraseDetected}", labelDetected, phraseDetected);
                    break;
                case SpeechResult speechResult:
                    var text = speechResult.Speech;
                    logger.LogInformation("Recognize completed succesfully, text={text}", text);
                    break;
                default:
                    logger.LogInformation("Recognize completed succesfully, recognizeResult={recognizeResult}", recognizeCompleted.RecognizeResult);
                    break;
            }
        }
        else if (parsedEvent is RecognizeFailed recognizeFailed)
        {
            callConnectionId = recognizeFailed.CallConnectionId;
            logger.LogInformation($"Received call event: {recognizeFailed.GetType()}, CorrelationId: {recognizeFailed.CorrelationId}, " +
                       $"subCode: {recognizeFailed.ResultInformation?.SubCode}, message: {recognizeFailed.ResultInformation?.Message}, context: {recognizeFailed.OperationContext}");
        }
        else if (parsedEvent is PlayCompleted playCompleted)
        {
            logger.LogInformation($"Received call event: {playCompleted.GetType()}");
            callConnectionId = playCompleted.CallConnectionId;
        }
        else if (parsedEvent is PlayFailed playFailed)
        {
            callConnectionId = playFailed.CallConnectionId;
            logger.LogInformation($"Received call event: {playFailed.GetType()}, CorrelationId: {playFailed.CorrelationId}, " +
                      $"subCode: {playFailed.ResultInformation?.SubCode}, message: {playFailed.ResultInformation?.Message}, context: {playFailed.OperationContext}");
        }
      
        else if (parsedEvent is PlayCanceled playCanceled)
        {
            logger.LogInformation($"Received call event: {playCanceled.GetType()}");
            callConnectionId = playCanceled.CallConnectionId;
        }
        else if (parsedEvent is RecognizeCanceled recognizeCanceled)
        {
            logger.LogInformation($"Received call event: {recognizeCanceled.GetType()}");
            callConnectionId = recognizeCanceled.CallConnectionId;
        }
        else if (parsedEvent is SendDtmfTonesCompleted sendDtmfTonesCompleted)
        {
            logger.LogInformation($"Received call event: {sendDtmfTonesCompleted.GetType()}");
            callConnectionId = sendDtmfTonesCompleted.CallConnectionId;
        }
        else if (parsedEvent is SendDtmfTonesFailed sendDtmfTonesFailed)
        {
            callConnectionId = sendDtmfTonesFailed.CallConnectionId;
            logger.LogInformation($"Received call event: {sendDtmfTonesFailed.GetType()}, CorrelationId: {sendDtmfTonesFailed.CorrelationId}, " +
                      $"subCode: {sendDtmfTonesFailed.ResultInformation?.SubCode}, message: {sendDtmfTonesFailed.ResultInformation?.Message}, context: {sendDtmfTonesFailed.OperationContext}");
        }
        else if (parsedEvent is ContinuousDtmfRecognitionToneReceived continuousDtmfRecognitionToneReceived)
        {
            logger.LogInformation($"Received call event: {continuousDtmfRecognitionToneReceived.GetType()}");
            callConnectionId = continuousDtmfRecognitionToneReceived.CallConnectionId;

            logger.LogInformation("Tone?detected:?sequenceId={sequenceId},?tone={tone}",
            continuousDtmfRecognitionToneReceived.SequenceId,
            continuousDtmfRecognitionToneReceived.Tone);

        }
        else if (parsedEvent is ContinuousDtmfRecognitionStopped continuousDtmfRecognitionStopped)
        {
            logger.LogInformation($"Received call event: {continuousDtmfRecognitionStopped.GetType()}");
            callConnectionId = continuousDtmfRecognitionStopped.CallConnectionId;
        }
        else if (parsedEvent is ContinuousDtmfRecognitionToneFailed continuousDtmfRecognitionToneFailed)
        {
            callConnectionId = continuousDtmfRecognitionToneFailed.CallConnectionId;
            logger.LogInformation($"Received call event: {continuousDtmfRecognitionToneFailed.GetType()}, CorrelationId: {continuousDtmfRecognitionToneFailed.CorrelationId}, " +
                      $"subCode: {continuousDtmfRecognitionToneFailed.ResultInformation?.SubCode}, message: {continuousDtmfRecognitionToneFailed.ResultInformation?.Message}, context: {continuousDtmfRecognitionToneFailed.OperationContext}");
        }
        else if (parsedEvent is HoldAudioStarted holdAudioStarted)
        {
            logger.LogInformation($"Received call event: {holdAudioStarted.GetType()}");
            callConnectionId = holdAudioStarted.CallConnectionId;
        }
        else if (parsedEvent is HoldAudioPaused holdAudioPaused)
        {
            logger.LogInformation($"Received call event: {holdAudioPaused.GetType()}");
            callConnectionId = holdAudioPaused.CallConnectionId;
        }
        else if (parsedEvent is HoldAudioResumed holdAudioResumed)
        {
            logger.LogInformation($"Received call event: {holdAudioResumed.GetType()}");
            callConnectionId = holdAudioResumed.CallConnectionId;
        }
        else if (parsedEvent is HoldAudioCompleted holdAudioCompleted)
        {
            logger.LogInformation($"Received call event: {holdAudioCompleted.GetType()}");
            callConnectionId = holdAudioCompleted.CallConnectionId;
        }
        else if (parsedEvent is HoldFailed holdFailed)
        {
            callConnectionId = holdFailed.CallConnectionId;
            logger.LogInformation($"Received call event: {holdFailed.GetType()}, CorrelationId: {holdFailed.CorrelationId}, " +
                      $"subCode: {holdFailed.ResultInformation?.SubCode}, message: {holdFailed.ResultInformation?.Message}, context: {holdFailed.OperationContext}");
        }
        else if (parsedEvent is CallDisconnected callDisconnected)
        {
            logger.LogInformation($"Received call event: {callDisconnected.GetType()}");
        }
        else if (parsedEvent is CreateCallFailed createCallFailed)
        {
            callConnectionId = createCallFailed.CallConnectionId;
            logger.LogInformation($"Received call event: {createCallFailed.GetType()}, CorrelationId: {createCallFailed.CorrelationId}, " +
                      $"subCode: {createCallFailed.ResultInformation?.SubCode}, message: {createCallFailed.ResultInformation?.Message}, context: {createCallFailed.OperationContext}");
        }
    }
    return Results.Ok();
}).Produces(StatusCodes.Status200OK);

#region Outbound Call

app.MapPost("/outboundCallToPstnAsync", async (string targetPhoneNumber, ILogger<Program> logger) =>
{
    PhoneNumberIdentifier target = new PhoneNumberIdentifier(targetPhoneNumber);
    PhoneNumberIdentifier caller = new PhoneNumberIdentifier(acsPhoneNumber);
    

    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    eventCallbackUri = callbackUri;
    CallInvite callInvite =  new CallInvite(target, caller);
   
    var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
    {
        CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
        
    };

    CreateCallResult createCallResult = await client.CreateCallAsync(createCallOptions);

    logger.LogInformation($"Created async pstn call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
}).WithTags("Create Outbound Call APIs");

app.MapPost("/outboundCallToPstn", (string targetPhoneNumber, ILogger<Program> logger) =>
{
    PhoneNumberIdentifier target = new PhoneNumberIdentifier(targetPhoneNumber);
    PhoneNumberIdentifier caller = new PhoneNumberIdentifier(acsPhoneNumber);


    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    eventCallbackUri = callbackUri;
    CallInvite callInvite = new CallInvite(target, caller);

    var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
    {
        CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },

    };

    CreateCallResult createCallResult = client.CreateCall(createCallOptions);

    logger.LogInformation($"Created call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
}).WithTags("Create Outbound Call APIs");

app.MapPost("/outboundCallToAcsAsync", async (string acsTarget, ILogger<Program> logger) =>
{
    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    eventCallbackUri = callbackUri;
    CallInvite callInvite = new CallInvite(new CommunicationUserIdentifier(acsTarget));

    var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
    {
        CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },

    };

    CreateCallResult createCallResult = await client.CreateCallAsync(createCallOptions);

    logger.LogInformation($"Created async acs call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
}).WithTags("Create Outbound Call APIs");

app.MapPost("/outboundCallToAcs", (string acsTarget, ILogger<Program> logger) =>
{
    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    eventCallbackUri = callbackUri;
    CallInvite callInvite = new CallInvite(new CommunicationUserIdentifier(acsTarget));

    var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
    {
        CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },

    };

    CreateCallResult createCallResult = client.CreateCall(createCallOptions);

    logger.LogInformation($"Created acs call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
}).WithTags("Create Outbound Call APIs");

app.MapPost("/outboundCallToTeamsAsync", async (string teamsObjectId, ILogger<Program> logger) =>
{
    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    eventCallbackUri = callbackUri;
    CallInvite callInvite = new CallInvite(new MicrosoftTeamsUserIdentifier(teamsObjectId));

    var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
    {
        CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },

    };

    CreateCallResult createCallResult = await client.CreateCallAsync(createCallOptions);

    logger.LogInformation($"Created async teams call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
}).WithTags("Create Outbound Call APIs");

app.MapPost("/outboundCallToTeams", (string teamsObjectId, ILogger<Program> logger) =>
{
    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    eventCallbackUri = callbackUri;
    CallInvite callInvite = new CallInvite(new MicrosoftTeamsUserIdentifier(teamsObjectId));

    var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
    {
        CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },

    };

    CreateCallResult createCallResult = client.CreateCall(createCallOptions);

    logger.LogInformation($"Created teams call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
}).WithTags("Create Outbound Call APIs");

#endregion

# region Play Media

app.MapPost("/playMediaToPstnTargetAsync", async (string pstnTarget, ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    
    TextSource textSource = new TextSource("Hi, this is test source played through play source thanks. Goodbye!.")
    {
        VoiceName = "en-US-NancyNeural"
    };

    List<CommunicationIdentifier> playTo = new List<CommunicationIdentifier> { new PhoneNumberIdentifier(pstnTarget) };
    PlayOptions playToOptions = new PlayOptions(textSource, playTo)
    {
        OperationContext = "playToContext"
    };

    await callMedia.PlayAsync(playToOptions);

    return Results.Ok();
}).WithTags("Play Media APIs");

app.MapPost("/playMediaToPstnTarget", (string pstnTarget, ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();

    TextSource textSource = new TextSource("Hi, this is test source played through play source thanks. Goodbye!.")
    {
        VoiceName = "en-US-NancyNeural"
    };

    List<CommunicationIdentifier> playTo = new List<CommunicationIdentifier> { new PhoneNumberIdentifier(pstnTarget) };
    PlayOptions playToOptions = new PlayOptions(textSource, playTo)
    {
        OperationContext = "playToContext"
    };

    callMedia.Play(playToOptions);

    return Results.Ok();
}).WithTags("Play Media APIs");

app.MapPost("/playMediaToAcsTargetAsync", async (string acsTarget, ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();

    TextSource textSource = new TextSource("Hi, this is test source played through play source thanks. Goodbye!.")
    {
        VoiceName = "en-US-NancyNeural"
    };

    List<CommunicationIdentifier> playTo = new List<CommunicationIdentifier> { new CommunicationUserIdentifier(acsTarget) };
    PlayOptions playToOptions = new PlayOptions(textSource, playTo)
    {
        OperationContext = "playToContext"
    };

    await callMedia.PlayAsync(playToOptions);

    return Results.Ok();
}).WithTags("Play Media APIs");

app.MapPost("/playMediaToAcsTarget", (string acsTarget, ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();

    TextSource textSource = new TextSource("Hi, this is test source played through play source thanks. Goodbye!.")
    {
        VoiceName = "en-US-NancyNeural"
    };

    List<CommunicationIdentifier> playTo = new List<CommunicationIdentifier> { new CommunicationUserIdentifier(acsTarget) };
    PlayOptions playToOptions = new PlayOptions(textSource, playTo)
    {
        OperationContext = "playToContext"
    };

    callMedia.Play(playToOptions);

    return Results.Ok();
}).WithTags("Play Media APIs");

app.MapPost("/playMediaToTeamsTarget", (string teamsObjectId, ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();

    TextSource textSource = new TextSource("Hi, this is test source played through play source thanks. Goodbye!.")
    {
        VoiceName = "en-US-NancyNeural"
    };

    List<CommunicationIdentifier> playTo = new List<CommunicationIdentifier> { new MicrosoftTeamsUserIdentifier(teamsObjectId) };
    PlayOptions playToOptions = new PlayOptions(textSource, playTo)
    {
        OperationContext = "playToContext"
    };

    callMedia.Play(playToOptions);

    return Results.Ok();
}).WithTags("Play Media APIs");

app.MapPost("/playMediaToAllAsync", async (ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();

    TextSource textSource = new TextSource("Hi, this is test source played through play source thanks. Goodbye!.")
    {
        VoiceName = "en-US-NancyNeural"
    };

    PlayToAllOptions playToAllOptions = new PlayToAllOptions(textSource)
    {
        OperationContext = "playToAllContext"
    };
    await callMedia.PlayToAllAsync(playToAllOptions);

    return Results.Ok();
}).WithTags("Play Media APIs");

app.MapPost("/playMediaToAll", (ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();

    TextSource textSource = new TextSource("Hi, this is test source played through play source thanks. Goodbye!.")
    {
        VoiceName = "en-US-NancyNeural"
    };

    PlayToAllOptions playToAllOptions = new PlayToAllOptions(textSource)
    {
        OperationContext = "playToAllContext"
    };
    callMedia.PlayToAll(playToAllOptions);

    return Results.Ok();
}).WithTags("Play Media APIs");

app.MapPost("/playBargeInAsync", async (ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();

    TextSource textSource = new TextSource("Hi, this is barge in test played through play source thanks. Goodbye!.")
    {
        VoiceName = "en-US-NancyNeural"
    };

    PlayToAllOptions playToAllOptions = new PlayToAllOptions(textSource)
    {
        OperationContext = "playToAllContext",
        InterruptCallMediaOperation = true
    };
    await callMedia.PlayToAllAsync(playToAllOptions);

    return Results.Ok();
}).WithTags("Play Media APIs");

#endregion 

#region Recognization

app.MapPost("/recognizeDTMFAsync", async (string pstnTarget,ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    TextSource textSource = new TextSource("Hi, this is recognize test. please provide input thanks!.")
    {
        VoiceName = "en-US-NancyNeural"
    };

    var target = new PhoneNumberIdentifier(pstnTarget);

    var recognizeOptions =
            new CallMediaRecognizeDtmfOptions(
                targetParticipant: target, maxTonesToCollect: 4)
            {
                InterruptPrompt = false,
                InterToneTimeout = TimeSpan.FromSeconds(5),
                OperationContext = "DtmfContext",
                InitialSilenceTimeout = TimeSpan.FromSeconds(15),
                Prompt = textSource
            };

    await callMedia.StartRecognizingAsync(recognizeOptions);

    return Results.Ok();
}).WithTags("Start Recognization APIs");

app.MapPost("/recognizeDTMF", (string pstnTarget, ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    TextSource textSource = new TextSource("Hi, this is recognize test. please provide input thanks!.")
    {
        VoiceName = "en-US-NancyNeural"
    };

    var target = new PhoneNumberIdentifier(pstnTarget);

    var recognizeOptions =
            new CallMediaRecognizeDtmfOptions(
                targetParticipant: target, maxTonesToCollect: 4)
            {
                InterruptPrompt = false,
                InterToneTimeout = TimeSpan.FromSeconds(5),
                OperationContext = "DtmfContext",
                InitialSilenceTimeout = TimeSpan.FromSeconds(15),
                Prompt = textSource
            };

    callMedia.StartRecognizing(recognizeOptions);

    return Results.Ok();
}).WithTags("Start Recognization APIs");


app.MapPost("/recognizeSpeechAsync", async (string pstnTarget, ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    TextSource textSource = new TextSource("Hi, this is recognize test. please provide input thanks!.")
    {
        VoiceName = "en-US-NancyNeural"
    };

    var target = new PhoneNumberIdentifier(pstnTarget);

    var recognizeOptions = new CallMediaRecognizeSpeechOptions(targetParticipant: target)
    {
        InterruptPrompt = false,
        OperationContext = "SpeechContext",
        InitialSilenceTimeout = TimeSpan.FromSeconds(15),
        Prompt = textSource,
        EndSilenceTimeout = TimeSpan.FromSeconds(15)
    };

    await callMedia.StartRecognizingAsync(recognizeOptions);

    return Results.Ok();
}).WithTags("Start Recognization APIs");

app.MapPost("/recognizeSpeech", (string pstnTarget, ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    TextSource textSource = new TextSource("Hi, this is recognize test. please provide input thanks!.")
    {
        VoiceName = "en-US-NancyNeural"
    };

    var target = new PhoneNumberIdentifier(pstnTarget);

    var recognizeOptions = new CallMediaRecognizeSpeechOptions(targetParticipant: target)
    {
        InterruptPrompt = false,
        OperationContext = "SpeechContext",
        InitialSilenceTimeout = TimeSpan.FromSeconds(15),
        Prompt = textSource,
        EndSilenceTimeout = TimeSpan.FromSeconds(15)
    };

    callMedia.StartRecognizing(recognizeOptions);

    return Results.Ok();
}).WithTags("Start Recognization APIs");

app.MapPost("/recognizeSpeechOrDtmfAsync", async (string pstnTarget, ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    TextSource textSource = new TextSource("Hi, this is recognize test. please provide input thanks!.")
    {
        VoiceName = "en-US-NancyNeural"
    };

    var target = new PhoneNumberIdentifier(pstnTarget);

    var recognizeOptions =
               new CallMediaRecognizeSpeechOrDtmfOptions(
                   targetParticipant: target, maxTonesToCollect: 4)
               {
                   InterruptPrompt = false,
                   OperationContext = "SpeechOrDTMFContext",
                   InitialSilenceTimeout = TimeSpan.FromSeconds(15),
                   Prompt = textSource,
                   EndSilenceTimeout = TimeSpan.FromSeconds(5)
               };
    await callMedia.StartRecognizingAsync(recognizeOptions);

    return Results.Ok();
}).WithTags("Start Recognization APIs");

app.MapPost("/recognizeSpeechOrDtmf", (string pstnTarget, ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    TextSource textSource = new TextSource("Hi, this is recognize test. please provide input thanks!.")
    {
        VoiceName = "en-US-NancyNeural"
    };

    var target = new PhoneNumberIdentifier(pstnTarget);

    var recognizeOptions =
               new CallMediaRecognizeSpeechOrDtmfOptions(
                   targetParticipant: target, maxTonesToCollect: 4)
               {
                   InterruptPrompt = false,
                   OperationContext = "SpeechOrDTMFContext",
                   InitialSilenceTimeout = TimeSpan.FromSeconds(15),
                   Prompt = textSource,
                   EndSilenceTimeout = TimeSpan.FromSeconds(5)
               };
    callMedia.StartRecognizingAsync(recognizeOptions);

    return Results.Ok();
}).WithTags("Start Recognization APIs");

app.MapPost("/recognizeChoiceAsync", async (string pstnTarget, ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    TextSource textSource = new TextSource("Hi, this is recognize test. please provide input thanks!.")
    {
        VoiceName = "en-US-NancyNeural"
    };

    var target = new PhoneNumberIdentifier(pstnTarget);


    var recognizeOptions =
        new CallMediaRecognizeChoiceOptions(targetParticipant: target, GetChoices())
        {
            InterruptCallMediaOperation = false,
            InterruptPrompt = false,
            InitialSilenceTimeout = TimeSpan.FromSeconds(10),
            Prompt = textSource,
            OperationContext = "ChoiceContext"
        };


    await callMedia.StartRecognizingAsync(recognizeOptions);

    return Results.Ok();
}).WithTags("Start Recognization APIs");

app.MapPost("/recognizeChoice", (string pstnTarget, ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    TextSource textSource = new TextSource("Hi, this is recognize test. please provide input thanks!.")
    {
        VoiceName = "en-US-NancyNeural"
    };

    var target = new PhoneNumberIdentifier(pstnTarget);


    var recognizeOptions =
        new CallMediaRecognizeChoiceOptions(targetParticipant: target, GetChoices())
        {
            InterruptCallMediaOperation = false,
            InterruptPrompt = false,
            InitialSilenceTimeout = TimeSpan.FromSeconds(10),
            Prompt = textSource,
            OperationContext = "ChoiceContext"
        };

    callMedia.StartRecognizingAsync(recognizeOptions);

    return Results.Ok();
}).WithTags("Start Recognization APIs");

#endregion

# region DTMF

app.MapPost("/sendDTMFTonesAsync", async (string pstnTarget,ILogger<Program> logger) =>
{
    CommunicationIdentifier target = new PhoneNumberIdentifier(pstnTarget);

    List<DtmfTone> tones = new List<DtmfTone>
        {
            DtmfTone.Zero,
            DtmfTone.One
        };

    CallMedia callMedia = GetCallMedia();

    await callMedia.SendDtmfTonesAsync(tones, target);
    return Results.Ok();
}).WithTags("Send or Start DTMF APIs");

app.MapPost("/sendDTMFTones", (string pstnTarget, ILogger<Program> logger) =>
{
    CommunicationIdentifier target = new PhoneNumberIdentifier(pstnTarget);

    List<DtmfTone> tones = new List<DtmfTone>
        {
            DtmfTone.Zero,
            DtmfTone.One
        };

    CallMedia callMedia = GetCallMedia();

    callMedia.SendDtmfTones(tones, target);
    return Results.Ok();
}).WithTags("Send or Start DTMF APIs");

app.MapPost("/startContinuousDTMFTonesAsync", async (string pstnTarget, ILogger <Program> logger) =>
{
    CommunicationIdentifier target = new PhoneNumberIdentifier(pstnTarget);

    CallMedia callMedia = GetCallMedia();

    await callMedia.StartContinuousDtmfRecognitionAsync(target);
    return Results.Ok();
}).WithTags("Send or Start DTMF APIs");

app.MapPost("/startContinuousDTMFTones", (string pstnTarget, ILogger<Program> logger) =>
{
    CommunicationIdentifier target = new PhoneNumberIdentifier(pstnTarget);

    CallMedia callMedia = GetCallMedia();

    callMedia.StartContinuousDtmfRecognition(target);
    return Results.Ok();
}).WithTags("Send or Start DTMF APIs");

app.MapPost("/stopContinuousDTMFTonesAsync", async (string pstnTarget, ILogger <Program> logger) =>
{
    CommunicationIdentifier target = new PhoneNumberIdentifier(pstnTarget);

    CallMedia callMedia = GetCallMedia();

    await callMedia.StopContinuousDtmfRecognitionAsync(target);
    return Results.Ok();
}).WithTags("Send or Start DTMF APIs");

app.MapPost("/stopContinuousDTMFTones",(string pstnTarget, ILogger<Program> logger) =>
{
    CommunicationIdentifier target = new PhoneNumberIdentifier(pstnTarget);

    CallMedia callMedia = GetCallMedia();

    callMedia.StopContinuousDtmfRecognition(target);
    return Results.Ok();
}).WithTags("Send or Start DTMF APIs");

# endregion

# region Hold/Unhold

app.MapPost("/holdParticipantAsync", async (string pstnTarget, bool isPlaySource, ILogger<Program> logger) =>
{
    CommunicationIdentifier target = new PhoneNumberIdentifier(pstnTarget);

    CallMedia callMedia = GetCallMedia();
    TextSource textSource = new TextSource("You are on hold please wait..")
    {
        VoiceName = "en-US-NancyNeural"
    };

    if (isPlaySource)
    {
        HoldOptions holdOptions = new HoldOptions(target)
        {
            PlaySource = textSource,
            OperationContext = "holdUserContext"
        };
        await callMedia.HoldAsync(holdOptions);
    }
    else
    {
        HoldOptions holdOptions = new HoldOptions(target)
        {
            OperationContext = "holdUserContext"
        };
        await callMedia.HoldAsync(holdOptions);
    }

    return Results.Ok();
}).WithTags("Hold Participant APIs");

app.MapPost("/holdParticipant", (string pstnTarget, bool isPlaySource, ILogger<Program> logger) =>
{
    CommunicationIdentifier target = new PhoneNumberIdentifier(pstnTarget);

    CallMedia callMedia = GetCallMedia();
    TextSource textSource = new TextSource("You are on hold please wait..")
    {
        VoiceName = "en-US-NancyNeural"
    };

    if (isPlaySource)
    {
        HoldOptions holdOptions = new HoldOptions(target)
        {
            PlaySource = textSource,
            OperationContext = "holdUserContext"
        };
        callMedia.Hold(holdOptions);
    }
    else
    {
        HoldOptions holdOptions = new HoldOptions(target)
        {
            OperationContext = "holdUserContext"
        };
        callMedia.Hold(holdOptions);
    }

    return Results.Ok();
}).WithTags("Hold Participant APIs");

app.MapPost("/interrupAudioAndAnnounceAsync", async (string pstnTarget, ILogger <Program> logger) =>
{
    CommunicationIdentifier target = new PhoneNumberIdentifier(pstnTarget);

    CallMedia callMedia = GetCallMedia();

    TextSource textSource = new TextSource("Hi, This is interrup audio and announcement test")
    {
        VoiceName = "en-US-NancyNeural"
    };

    InterruptAudioAndAnnounceOptions interruptAudio = new InterruptAudioAndAnnounceOptions(textSource, target)
    {
        OperationContext = "innterruptContext"
    };

    await callMedia.InterruptAudioAndAnnounceAsync(interruptAudio);

    return Results.Ok();
}).WithTags("Hold Participant APIs");

app.MapPost("/interrupAudioAndAnnounce", (string pstnTarget, ILogger<Program> logger) =>
{
    CommunicationIdentifier target = new PhoneNumberIdentifier(pstnTarget);

    CallMedia callMedia = GetCallMedia();

    TextSource textSource = new TextSource("Hi, This is interrupt audio and announcement test.")
    {
        VoiceName = "en-US-NancyNeural"
    };

    InterruptAudioAndAnnounceOptions interruptAudio = new InterruptAudioAndAnnounceOptions(textSource, target)
    {
        OperationContext = "innterruptContext"
    };

    callMedia.InterruptAudioAndAnnounce(interruptAudio);

    return Results.Ok();
}).WithTags("Hold Participant APIs");


app.MapPost("/unholdParticipantAsync", async (string pstnTarget, ILogger <Program> logger) =>
{
    CommunicationIdentifier target = new PhoneNumberIdentifier(pstnTarget);

    CallMedia callMedia = GetCallMedia();

    UnholdOptions unholdOptions = new UnholdOptions(target)
    {
        OperationContext = "unholdUserContext"
    };

    await callMedia.UnholdAsync(unholdOptions);
    return Results.Ok();
}).WithTags("Hold Participant APIs");

app.MapPost("/unholdParticipant", (string pstnTarget, ILogger<Program> logger) =>
{
    CommunicationIdentifier target = new PhoneNumberIdentifier(pstnTarget);

    CallMedia callMedia = GetCallMedia();

    UnholdOptions unholdOptions = new UnholdOptions(target)
    {
        OperationContext = "unholdUserContext"
    };

    callMedia.Unhold(unholdOptions);
    return Results.Ok();
}).WithTags("Hold Participant APIs");

app.MapPost("/interruptHoldWithPlay", (string pstnTarget, ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();

    TextSource textSource = new TextSource("Hi, This is interrupt audio and announcement test.")
    {
        VoiceName = "en-US-NancyNeural"
    };

    List<CommunicationIdentifier> playTo = new List<CommunicationIdentifier> { new PhoneNumberIdentifier(pstnTarget) };
    PlayOptions playToOptions = new PlayOptions(textSource, playTo)
    {
        OperationContext = "playToContext",
        InterruptHoldAudio = true
    };

    callMedia.Play(playToOptions);

    return Results.Ok();
}).WithTags("Hold Participant APIs");

# endregion

#region Disconnect call

app.MapPost("/hangupAsync", async (bool isForEveryOne, ILogger<Program> logger) =>
{
    CallConnection callConnection = GetConnection();
    await callConnection.HangUpAsync(isForEveryOne);
    return Results.Ok();
}).WithTags("Disconnect call APIs");

app.MapPost("/hangup", (bool isForEveryOne, ILogger<Program> logger) =>
{
    CallConnection callConnection = GetConnection();
    callConnection.HangUp(isForEveryOne);
    return Results.Ok();
}).WithTags("Disconnect call APIs");

#endregion

CallMedia GetCallMedia()
{
    CallMedia callMedia = !string.IsNullOrEmpty(callConnectionId) ?
        client.GetCallConnection(callConnectionId).GetCallMedia()
        : throw new ArgumentNullException("Call connection id is empty");

    return callMedia;
}

CallConnection GetConnection()
{
    CallConnection callConnection = !string.IsNullOrEmpty(callConnectionId) ?
        client.GetCallConnection(callConnectionId)
        : throw new ArgumentNullException("Call connection id is empty");
    return callConnection;
}

CallConnectionProperties GetCallConnectionProperties()
{
    CallConnectionProperties callConnectionProperties = !string.IsNullOrEmpty(callConnectionId) ?
       client.GetCallConnection(callConnectionId).GetCallConnectionProperties()
       : throw new ArgumentNullException("Call connection id is empty");
    return callConnectionProperties;
}

List<RecognitionChoice> GetChoices()
{
    return new List<RecognitionChoice> {
            new RecognitionChoice("Confirm", new List<string> {
                "Confirm",
                "First",
                "One"
            }) {
                Tone = DtmfTone.One
            },
            new RecognitionChoice("Cancel", new List<string> {
                "Cancel",
                "Second",
                "Two"
            }) {
                Tone = DtmfTone.Two
            }
        };
}

app.Run();
