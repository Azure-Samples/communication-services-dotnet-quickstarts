using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid.SystemEvents;
using Azure.Messaging.EventGrid;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Your ACS resource connection string
var acsConnectionString = "";

// Your ACS resource phone number will act as source number to start outbound call
var acsPhoneNumber = "";

// Target phone number you want to receive the call.
var targetPhoneNumber = "";

var participantPhoneNumber = "";

// Base url of the app
var callbackUriHost = "";

// Your cognitive service endpoint
var cognitiveServiceEndpoint = "";

// User Id of the target teams user you want to receive the call.
var targetTeamsUserId = "09cb0248-6a74-4284-9e8e-6011727ff7f4";

// User Id of the participant teams user you want to add to the call.
var participantTeamsUserId = "";

var fileSourceUri = "";

var transportUrl = "";


string callConnectionId = string.Empty;
string recordingId = string.Empty;
string recordingLocation = string.Empty;
Uri eventCallbackUri = null;


CallAutomationClient callAutomationClient = new CallAutomationClient(new Uri("https://uswe-04.sdf.pma.teams.microsoft.com:6448"),acsConnectionString);
builder.Services.AddApplicationInsightsTelemetry();
var app = builder.Build();

app.MapPost("/outboundCall", async (bool isAcsUser, ILogger<Program> logger) =>
{
    PhoneNumberIdentifier target = new PhoneNumberIdentifier(targetPhoneNumber);
    PhoneNumberIdentifier caller = new PhoneNumberIdentifier(acsPhoneNumber);
    //MicrosoftTeamsUserIdentifier microsoftTeamsUserIdentifier = new MicrosoftTeamsUserIdentifier(targetTeamsUserId);
    CommunicationUserIdentifier acsUser = new CommunicationUserIdentifier("8:acs:efd3c229-b212-437a-945d-92326f13a1be_00000024-e486-d468-85f4-343a0d00a2c8");

    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    eventCallbackUri = callbackUri;
    CallInvite callInvite = isAcsUser ? new CallInvite(acsUser) : new CallInvite(target, caller);

    //CallInvite callInvite = new CallInvite(new CommunicationUserIdentifier("8:acs:efd3c229-b212-437a-945d-92326f13a1be_00000024-e05d-11cf-35f3-343a0d00c2b8"));

    //TranscriptionOptions transcriptionOptions = new TranscriptionOptions(new Uri(transportUrl),
    //    "en-us", false, TranscriptionTransport.Websocket);

    //MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(new Uri(transportUrl), MediaStreamingContent.Audio,
    //    MediaStreamingAudioChannel.Unmixed, MediaStreamingTransport.Websocket, false);

    var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
    {
        CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServiceEndpoint) },
        //MediaStreamingOptions = mediaStreamingOptions,
        //TranscriptionOptions = transcriptionOptions
    };

    CreateCallResult createCallResult = await callAutomationClient.CreateCallAsync(createCallOptions);

    logger.LogInformation($"Created call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
});

app.MapPost("/api/recordingFileStatus", (EventGridEvent[] eventGridEvents, ILogger<Program> logger) =>
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
            //logger.LogInformation($"Media Streaming state: {callConnectionProperties.MediaStreamingSubscription.State}");
            //logger.LogInformation($"Transcription state: {callConnectionProperties.TranscriptionSubscription.State}");

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
        else if (parsedEvent is AddParticipantSucceeded addParticipantSucceeded)
        {
            logger.LogInformation($"Received call event: {addParticipantSucceeded.GetType()}");
            callConnectionId = addParticipantSucceeded.CallConnectionId;
        }
        else if (parsedEvent is  AddParticipantFailed addParticipantFailed)
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
        else if (parsedEvent is RemoveParticipantFailed removeParticipantFailed )
        {
            callConnectionId = removeParticipantFailed.CallConnectionId;
            logger.LogInformation($"Received call event: {removeParticipantFailed.GetType()}, CorrelationId: {removeParticipantFailed.CorrelationId}, " +
                      $"subCode: {removeParticipantFailed.ResultInformation?.SubCode}, message: {removeParticipantFailed.ResultInformation?.Message}, context: {removeParticipantFailed.OperationContext}");
        }
        else if (parsedEvent is RecordingStateChanged recordingStateChanged)
        {
            logger.LogInformation($"Received call event: {recordingStateChanged.GetType()}");
            callConnectionId = recordingStateChanged.CallConnectionId;
            logger.LogInformation($"Recording State: {recordingStateChanged.State}");
        }
        else if (parsedEvent is TranscriptionStarted transcriptionStarted)
        {
            logger.LogInformation($"Received call event: {transcriptionStarted.GetType()}");
            callConnectionId = transcriptionStarted.CallConnectionId;
        }
        else if (parsedEvent is TranscriptionStopped transcriptionStopped)
        {
            logger.LogInformation($"Received call event: {transcriptionStopped.GetType()}");
            callConnectionId = transcriptionStopped.CallConnectionId;
        }
        else if (parsedEvent is TranscriptionUpdated transcriptionUpdated)
        {
            logger.LogInformation($"Received call event: {transcriptionUpdated.GetType()}");
            callConnectionId = transcriptionUpdated.CallConnectionId;
        }
        else if (parsedEvent is TranscriptionFailed transcriptionFailed)
        {
            callConnectionId = transcriptionFailed.CallConnectionId;
            logger.LogInformation($"Received call event: {transcriptionFailed.GetType()}, CorrelationId: {transcriptionFailed.CorrelationId}, " +
                      $"subCode: {transcriptionFailed.ResultInformation?.SubCode}, message: {transcriptionFailed.ResultInformation?.Message}, context: {transcriptionFailed.OperationContext}");
        }
        else if (parsedEvent is MediaStreamingStarted mediaStreamingStarted)
        {
            logger.LogInformation($"Received call event: {mediaStreamingStarted.GetType()}");
            callConnectionId = mediaStreamingStarted.CallConnectionId;
        }
        else if (parsedEvent is MediaStreamingStopped mediaStreamingStopped)
        {
            logger.LogInformation($"Received call event: {mediaStreamingStopped.GetType()}");
            callConnectionId = mediaStreamingStopped.CallConnectionId;
        }
        else if (parsedEvent is MediaStreamingFailed mediaStreamingFailed)
        {
            callConnectionId = mediaStreamingFailed.CallConnectionId;
            logger.LogInformation($"Received call event: {mediaStreamingFailed.GetType()}, CorrelationId: {mediaStreamingFailed.CorrelationId}, " +
                      $"subCode: {mediaStreamingFailed.ResultInformation?.SubCode}, message: {mediaStreamingFailed.ResultInformation?.Message}, context: {mediaStreamingFailed.OperationContext}");
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.MapPost("/addCcaasAgent", async (bool isTeamsUser, ILogger<Program> logger) =>
//{
//    Console.WriteLine(isTeamsUser);
//    var response = await AddCcassAgent(isTeamsUser);
//    return Results.Ok();
//});

app.MapPost("/addAcsParticipant", async (ILogger<Program> logger) =>
{
    await AddACSParticipantAsync();
    return Results.Ok();
});

app.MapPost("/addParticipant", async (bool isTeamsUser, ILogger<Program> logger) =>
{
    Console.WriteLine(isTeamsUser);
    var response = await AddParticipantAsync(isTeamsUser);
    return Results.Ok(response);
});

app.MapPost("/removeParticipant", async (bool isAcsParticipant, bool isAcsTarget, ILogger<Program> logger) =>
{
    await RemoveParticipantAsync(isAcsParticipant, isAcsTarget);
    return Results.Ok();
});

app.MapPost("/playMedia", async (bool isPlayToAll, bool isTeamsUser, bool isAcsUser, bool isPlayMediaToTarget, ILogger <Program> logger) =>
{
    Console.WriteLine(isPlayToAll);
    await PlayMediaAsync(isPlayToAll, isTeamsUser, isAcsUser, isPlayMediaToTarget);
    return Results.Ok();
});

//app.MapPost("/recognizeMedia", async (bool isDtmf, bool isSpeechOrDtmf, bool isTeamsUser, bool isPlayMediaToTarget, ILogger <Program> logger) =>
//{
//    await RecognizeMediaAsync(isDtmf, isSpeechOrDtmf, isTeamsUser, isPlayMediaToTarget);
//    return Results.Ok();
//});

//app.MapPost("/sendDTMFTones", async (bool isTeamsUser, bool isPlayMediaToTarget, ILogger <Program> logger) =>
//{
//    Console.WriteLine(isTeamsUser);
//    await SendDtmfToneAsync(isTeamsUser, isPlayMediaToTarget);
//    return Results.Ok();
//});

//app.MapPost("/startContinuousDTMFTones", async (bool isTeamsUser, bool isPlayMediaToTarget, ILogger <Program> logger) =>
//{
//    Console.WriteLine(isTeamsUser);
//    await StartContinuousDtmfAsync(isTeamsUser, isPlayMediaToTarget);
//    return Results.Ok();
//});

//app.MapPost("/stopContinuousDTMFTones", async (bool isTeamsUser, bool isPlayMediaToTarget, ILogger <Program> logger) =>
//{
//    Console.WriteLine(isTeamsUser);
//    await StopContinuousDtmfAsync(isTeamsUser, isPlayMediaToTarget);
//    return Results.Ok();
//});

//app.MapPost("/holdParticipant", async (bool isTeamsUser, bool isPlaySource, bool isPlayMediaToTarget, ILogger <Program> logger) =>
//{
//    Console.WriteLine(isTeamsUser);
//    await HoldParticipantAsync(isTeamsUser, isPlaySource, isPlayMediaToTarget);
//    return Results.Ok();
//});

//app.MapPost("/unholdParticipant", async (bool isTeamsUser, bool isPlayMediaToTarget, ILogger <Program> logger) =>
//{
//    Console.WriteLine(isTeamsUser);
//    await UnholdParticipantAsync(isTeamsUser, isPlayMediaToTarget);
//    return Results.Ok();
//});

app.MapPost("/cancelAllMediaOperation", async (ILogger<Program> logger) =>
{
    await CancelAllMediaOperationAsync();
    return Results.Ok();
});

app.MapPost("/getParticipantList", async (ILogger<Program> logger) =>
{
    await GetParticipantListAsync();
    return Results.Ok();
});

//app.MapPost("/startMediaStreaming", async (ILogger<Program> logger) =>
//{
//    await StartMediaStreamingAsync();
//    return Results.Ok();
//});

//app.MapPost("/stopMediaStreaming", async (ILogger<Program> logger) =>
//{
//    await StopMediaStreamingAsync();
//    return Results.Ok();
//});

//app.MapPost("/startTranscription", async (ILogger<Program> logger) =>
//{
//    await StartTranscriptionAsync();
//    return Results.Ok();
//});

//app.MapPost("/updateTranscription", async (ILogger<Program> logger) =>
//{
//    await UpdateTranscriptionAsync();
//    return Results.Ok();
//});

//app.MapPost("/stopTranscription", async (ILogger<Program> logger) =>
//{
//    await StopTranscriptionAsync();
//    return Results.Ok();
//});

app.MapPost("/startRecording", async (ILogger<Program> logger) =>
{
    CallConnectionProperties callConnectionProperties = GetCallConnectionProperties();
    var serverCallId = callConnectionProperties.ServerCallId;
    CallLocator callLocator = new ServerCallLocator(serverCallId);
    var recordingOptions = new StartRecordingOptions(callLocator)
    {
        RecordingContent = RecordingContent.Audio,
        RecordingFormat = RecordingFormat.Wav,
        RecordingChannel = RecordingChannel.Unmixed,
        RecordingStateCallbackUri = eventCallbackUri
    };
    var recordingResult = await callAutomationClient.GetCallRecording().StartAsync(recordingOptions);
    recordingId = recordingResult.Value.RecordingId;
    logger.LogInformation($"Recording started. RecordingId: {recordingId}");
    return Results.Ok();
});

app.MapPost("/pauseRecording", async () =>
{
    await callAutomationClient.GetCallRecording().PauseAsync(recordingId);
    return Results.Ok();
});

app.MapPost("/resumeRecording", async () =>
{
    await callAutomationClient.GetCallRecording().ResumeAsync(recordingId);
    return Results.Ok();
});

app.MapPost("/stopRecording", async () =>
{
    await callAutomationClient.GetCallRecording().StopAsync(recordingId);
    return Results.Ok();
});

app.MapGet("/download", async (ILogger<Program> logger) =>
{
    await callAutomationClient.GetCallRecording().DownloadToAsync(new Uri(recordingLocation), "testfile.mp4");
    return Results.Ok();
});

async Task<AddParticipantResult> AddACSParticipantAsync()
{
    CallInvite callInvite;

    CallConnection callConnection = GetConnection();

    string operationContext = "adACSParticipantContext";
    callInvite = new CallInvite(new CommunicationUserIdentifier("8:acs:efd3c229-b212-437a-945d-92326f13a1be_00000024-e43a-cfa2-ac00-343a0d0091b2"));

    var addParticipantOptions = new AddParticipantOptions(callInvite)
    {
        OperationContext = operationContext,
        InvitationTimeoutInSeconds = 30,
    };

    return await callConnection.AddParticipantAsync(addParticipantOptions);
}

//async Task<AddParticipantResult> AddCcassAgent(bool isTeamsUser)
//{
//    CallInvite callInvite;

//    CallConnection callConnection = GetConnection();

//    string operationContext = isTeamsUser ? "addTeamsUserContext" : "addDualPersonaUserContext";

//    //if (isTeamsUser)
//    //{
//    //    callInvite = new CallInvite(new MicrosoftTeamsUserIdentifier(participantTeamsUser));
//    //}
//    //else
//    //{
//    //    callInvite = new CallInvite(new CommunicationUserIdentifier(""));

//    //}

//    callInvite = new CallInvite(new CommunicationUserIdentifier("8:acs:efd3c229-b212-437a-945d-92326f13a1be_d3fdac34-611e-47a4-9740-ff73b88d1096_7e52affa-0cba-487d-8294-3a2f24c490f5"));

//    var addParticipantOptions = new AddParticipantOptions(callInvite)
//    {
//        OperationContext = operationContext,
//        InvitationTimeoutInSeconds = 30,
//    };

//    return await callConnection.AddParticipantAsync(addParticipantOptions);
//}
async Task<AddParticipantResult> AddParticipantAsync(bool isTeamsUser)
{
    CallInvite callInvite;

    CallConnection callConnection = GetConnection();

    string operationContext = isTeamsUser ? "addTeamsUserContext" : "addPSTNUserContext";

    if (isTeamsUser)
    {
        callInvite = new CallInvite(new MicrosoftTeamsUserIdentifier(participantTeamsUserId));
    }
    else
    {
        callInvite = new CallInvite(new PhoneNumberIdentifier(participantPhoneNumber),
           new PhoneNumberIdentifier(acsPhoneNumber));
    }

    var addParticipantOptions = new AddParticipantOptions(callInvite)
    {
        OperationContext = operationContext,
        InvitationTimeoutInSeconds = 30,
    };

    return await callConnection.AddParticipantAsync(addParticipantOptions);
}

async Task<RemoveParticipantResult> RemoveParticipantAsync(bool isAcsParticipant, bool isAcsTarget)
{
    CallConnection callConnection = GetConnection();
    RemoveParticipantOptions removeParticipantOptions;
    if (isAcsParticipant)
    {
        if (isAcsTarget)
        {
            removeParticipantOptions = new RemoveParticipantOptions(new CommunicationUserIdentifier("8:acs:efd3c229-b212-437a-945d-92326f13a1be_00000024-e486-d468-85f4-343a0d00a2c8"))
            {
                OperationContext = "removeACSTargetContext"
            };
        }
        else
        {
            removeParticipantOptions = new RemoveParticipantOptions(new CommunicationUserIdentifier("8:acs:efd3c229-b212-437a-945d-92326f13a1be_00000024-e43a-cfa2-ac00-343a0d0091b2"))
            {
                OperationContext = "removeACSParticipantContext"
            };
        }
    }
    else
    {
        removeParticipantOptions = new RemoveParticipantOptions(new PhoneNumberIdentifier(participantPhoneNumber))
        {
            OperationContext = "removePSTNParticipantContext"
        };
    }

    return await callConnection.RemoveParticipantAsync(removeParticipantOptions);
}

async Task PlayMediaAsync(bool isPlayToAll, bool isTeamsUser, bool isAcsUser, bool isPlayMediaToTarget)
{
    CallMedia callMedia = GetCallMedia();

    FileSource fileSource = new FileSource(new Uri(fileSourceUri));

    TextSource textSource = new TextSource("Hi, this is test source played through play source thanks. Goodbye!.")
    {
        VoiceName = "en-US-NancyNeural"
    };

    SsmlSource ssmlSource = new SsmlSource("<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"><voice name=\"en-US-JennyNeural\">Hi, this is ssml test played through ssml source thanks. Goodbye!</voice></speak>");

    List<PlaySource> playSources = new List<PlaySource>()
    {
        //fileSource,textSource,ssmlSource
        fileSource
        //textSource,ssmlSource
    };

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
        CommunicationIdentifier target = GetCommunicationTargetIdentifier(isTeamsUser,isPlayMediaToTarget);

        List<CommunicationIdentifier> playTo = null;

        if (isAcsUser)
        {
            if (isPlayMediaToTarget)
            {
                playTo = new List<CommunicationIdentifier> { new CommunicationUserIdentifier("8:acs:efd3c229-b212-437a-945d-92326f13a1be_00000024-e486-d468-85f4-343a0d00a2c8") };
            }
            else
            {
                playTo = new List<CommunicationIdentifier> { new CommunicationUserIdentifier("8:acs:efd3c229-b212-437a-945d-92326f13a1be_00000024-e43a-cfa2-ac00-343a0d0091b2") };
            }
        }
        else
        {
            playTo = new List<CommunicationIdentifier> { target };
        }

        PlayOptions playToOptions = new PlayOptions(playSources, playTo)
        {
            OperationContext = "playToContext"
        };
        
        await callMedia.PlayAsync(playToOptions);
    }
}

//async Task RecognizeMediaAsync(bool isDtmf, bool isSpeechOrDtmf, bool isTeamsUser, bool isPlayMediaToTarget)
//{
//    CommunicationIdentifier target = GetCommunicationTargetIdentifier(isTeamsUser, isPlayMediaToTarget);

//    CallMediaRecognizeOptions recognizeOptions = null;

//    FileSource fileSource = new FileSource(new Uri(fileSourceUri));

//    if (isDtmf && !isSpeechOrDtmf)
//    {
//        //DTMF only.
//        recognizeOptions =
//                new CallMediaRecognizeDtmfOptions(
//                    targetParticipant: target, maxTonesToCollect: 4)
//                {
//                    InterruptPrompt = false,
//                    InterToneTimeout = TimeSpan.FromSeconds(5),
//                    OperationContext = "DtmfContext",
//                    InitialSilenceTimeout = TimeSpan.FromSeconds(15),
//                    Prompt = fileSource
//                };

//    }
//    else if (!isDtmf && !isSpeechOrDtmf)
//    {
//        //SPEECH only.
//        recognizeOptions = new CallMediaRecognizeSpeechOptions(targetParticipant: target)
//        {
//            InterruptPrompt = false,
//            OperationContext = "SpeechContext",
//            InitialSilenceTimeout = TimeSpan.FromSeconds(15),
//            Prompt = fileSource,
//            EndSilenceTimeout = TimeSpan.FromSeconds(15)
//        };
//    }
//    else if (!isDtmf && isSpeechOrDtmf)
//    {
//        //Speech or DTMF
//        recognizeOptions =
//                new CallMediaRecognizeSpeechOrDtmfOptions(
//                    targetParticipant: target, maxTonesToCollect: 4)
//                {
//                    InterruptPrompt = false,
//                    OperationContext = "SpeechOrDTMFContext",
//                    InitialSilenceTimeout = TimeSpan.FromSeconds(15),
//                    Prompt = fileSource,
//                    EndSilenceTimeout = TimeSpan.FromSeconds(5)
//                };
//    }

//    //recognizeOptions =
//    //    new CallMediaRecognizeChoiceOptions(targetParticipant: target, GetChoices())
//    //    {
//    //        InterruptCallMediaOperation = false,
//    //        InterruptPrompt = false,
//    //        InitialSilenceTimeout = TimeSpan.FromSeconds(10),
//    //        Prompt = fileSource,
//    //        OperationContext = "ChoiceContext"
//    //    };

//    CallMedia callMedia = GetCallMedia();

//    await callMedia.StartRecognizingAsync(recognizeOptions);
//}

//async Task SendDtmfToneAsync(bool isTeamsUser, bool isPlayMediaToTarget)
//{
//    CommunicationIdentifier target = GetCommunicationTargetIdentifier(isTeamsUser, isPlayMediaToTarget);

//    List<DtmfTone> tones = new List<DtmfTone>
//        {
//            DtmfTone.Zero,
//            DtmfTone.One
//        };

//    CallMedia callMedia = GetCallMedia();

//    await callMedia.SendDtmfTonesAsync(tones, target);
//}

//async Task StartContinuousDtmfAsync(bool isTeamsUser, bool isPlayMediaToTarget)
//{
//    CommunicationIdentifier target = GetCommunicationTargetIdentifier(isTeamsUser, isPlayMediaToTarget);

//    CallMedia callMedia = GetCallMedia();

//    await callMedia.StartContinuousDtmfRecognitionAsync(target);
//}

//async Task StopContinuousDtmfAsync(bool isTeamsUser, bool isPlayMediaToTarget)
//{
//    CommunicationIdentifier target = GetCommunicationTargetIdentifier(isTeamsUser, isPlayMediaToTarget);

//    CallMedia callMedia = GetCallMedia();

//    await callMedia.StopContinuousDtmfRecognitionAsync(target);
//}

//async Task HoldParticipantAsync(bool isTeamsUser, bool isPlaySource, bool isPlayMediaToTarget)
//{
//    CommunicationIdentifier target = GetCommunicationTargetIdentifier(isTeamsUser, isPlayMediaToTarget);

//    CallMedia callMedia = GetCallMedia();

//    FileSource fileSource = new FileSource(new Uri(fileSourceUri));

//    if (isPlaySource)
//    {
//        HoldOptions holdOptions = new HoldOptions(target)
//        {
//            PlaySource = fileSource,
//            OperationContext = "holdUserContext"
//        };
//        await callMedia.HoldAsync(holdOptions);
//    }
//    else
//    {
//        HoldOptions holdOptions = new HoldOptions(target)
//        {
//            OperationContext = "holdUserContext"
//        };
//        await callMedia.HoldAsync(holdOptions);
//    }

//    //await Task.Delay(5000);

//    //CallParticipant participant = await GetParticipantAsync(target);
//    //if (participant != null)
//    //{
//    //    Console.WriteLine("Participant:-->" + participant.Identifier.RawId.ToString());
//    //    Console.WriteLine("Is Participant on hold:-->" + participant.IsOnHold);
//    //}
//}

//async Task UnholdParticipantAsync(bool isTeamsUser, bool isPlayMediaToTarget)
//{
//    CommunicationIdentifier target = GetCommunicationTargetIdentifier(isTeamsUser, isPlayMediaToTarget);

//    CallMedia callMedia = GetCallMedia();

//    UnholdOptions unholdOptions = new UnholdOptions(target)
//    { 
//        OperationContext = "unholdUserContext" 
//    };

//    await callMedia.UnholdAsync(unholdOptions);

//    //await Task.Delay(5000);

//    //CallParticipant participant = await GetParticipantAsync(target);
//    //if (participant != null)
//    //{
//    //    Console.WriteLine("Participant:-->" + participant.Identifier.RawId.ToString());
//    //    Console.WriteLine("Is Participant on hold:-->" + participant.IsOnHold);
//    //}
//}

async Task CancelAllMediaOperationAsync()
{
    CallMedia callMedia = GetCallMedia();

    await callMedia.CancelAllMediaOperationsAsync();
}

//async Task StartMediaStreamingAsync()
//{
//    CallMedia callMedia = GetCallMedia();

//    CallConnectionProperties callConnectionProperties = GetCallConnectionProperties();

//    if (callConnectionProperties.MediaStreamingSubscription.State.Equals("inactive")){
//        await callMedia.StartMediaStreamingAsync();
//    }
//    else
//    {
//        Console.WriteLine("Media streaming is already active");
//    }
//}

//async Task StopMediaStreamingAsync()
//{
//    CallMedia callMedia = GetCallMedia();

//    CallConnectionProperties callConnectionProperties = GetCallConnectionProperties();

//    if (callConnectionProperties.MediaStreamingSubscription.State.Equals("active"))
//    {
//        await callMedia.StopMediaStreamingAsync();
//    }
//    else
//    {
//        Console.WriteLine("Media streaming is not active");
//    }
//}

//async Task StartTranscriptionAsync()
//{
//    CallMedia callMedia = GetCallMedia();

//    CallConnectionProperties callConnectionProperties = GetCallConnectionProperties();

//    if (callConnectionProperties.TranscriptionSubscription.State.Equals("inactive"))
//    {
//        await callMedia.StartTranscriptionAsync();
//    }
//    else
//    {
//        Console.WriteLine("Transcription is already active");
//    }
//}

//async Task StopTranscriptionAsync()
//{
//    CallMedia callMedia = GetCallMedia();

//    CallConnectionProperties callConnectionProperties = GetCallConnectionProperties();

//    if (callConnectionProperties.TranscriptionSubscription.State.Equals("active"))
//    {
//        await callMedia.StopTranscriptionAsync();
//    }
//    else
//    {
//        Console.WriteLine("Transcription is not active");
//    }
//}

//async Task UpdateTranscriptionAsync()
//{
//    CallMedia callMedia = GetCallMedia();

//    CallConnectionProperties callConnectionProperties = GetCallConnectionProperties();

//    if (callConnectionProperties.TranscriptionSubscription.State.Equals("active"))
//    {
//        await callMedia.UpdateTranscriptionAsync("en-au");
//    }
//    else
//    {
//        Console.WriteLine("Transcription is not active");
//    }
//}


async Task<CallParticipant> GetParticipantAsync(CommunicationIdentifier target)
{
    CallConnection callConnection = GetConnection();
    CallParticipant participant = await callConnection.GetParticipantAsync(target);
    return participant;
}

async Task GetParticipantListAsync()
{
    CallConnection callConnection = GetConnection();

    var list = await callConnection.GetParticipantsAsync();

    foreach (var participant in list.Value)
    {
        Console.WriteLine("----------------------------------------------------------------------");
        Console.WriteLine("Participant:-->" + participant.Identifier.RawId.ToString());
        Console.WriteLine("Is Participant on hold:-->" + participant.IsOnHold);
        Console.WriteLine("----------------------------------------------------------------------");
    }
}

CallMedia GetCallMedia()
{
    CallMedia callMedia = !string.IsNullOrEmpty(callConnectionId) ?
        callAutomationClient.GetCallConnection(callConnectionId).GetCallMedia()
        : throw new ArgumentNullException("Call connection id is empty");

    return callMedia;
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

CommunicationIdentifier GetCommunicationTargetIdentifier(bool isTeamsUser, bool isPlayMediaToTarget)
{
    string teamsIdentifier = isPlayMediaToTarget ? targetTeamsUserId : participantTeamsUserId;

    string pstnIdentifier = isPlayMediaToTarget ? targetPhoneNumber : participantPhoneNumber;

    CommunicationIdentifier target = isTeamsUser ? new MicrosoftTeamsUserIdentifier(teamsIdentifier) :
        new PhoneNumberIdentifier(pstnIdentifier);

    return target;
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
