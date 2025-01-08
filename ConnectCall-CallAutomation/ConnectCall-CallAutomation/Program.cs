using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Communication.Rooms;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
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

//Get Target phone number from appsettings.json
var targetPhoneNumber = builder.Configuration.GetValue<string>("TargetPhoneNumber");
ArgumentNullException.ThrowIfNullOrEmpty(targetPhoneNumber);

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

//File Audio URL
var fileSourceUri = "";

//Transport URL
var transportUrl = "";

//Bring Your Own Storage URL
var bringYourOwnStorageUrl = "";

string callConnectionId = string.Empty;
string handlePrompt = "Welcome to the Contoso Utilities. Thank you!";
bool isBYOS = false;
bool isRejectCall = false;

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

app.MapPost("/createRoom", async (List<string> participants, bool pstnDialOutEnabled, ILogger <Program> logger) =>
{
    // create RoomsClient
    var roomsClient = new RoomsClient(acsConnectionString);
    var roomParticipants = new List<RoomParticipant>();
    foreach (var participant in participants)
    {
        roomParticipants.Add(new RoomParticipant(new CommunicationUserIdentifier(participant)));
    }
    
    var options = new CreateRoomOptions()
    {
        PstnDialOutEnabled = pstnDialOutEnabled,
        Participants = roomParticipants,
        ValidFrom = DateTime.UtcNow,
        ValidUntil = DateTime.UtcNow.AddMinutes(30)
    };

    var response = await roomsClient.CreateRoomAsync(options);
    logger.LogInformation($"ROOM ID: {response.Value.Id}");
    return response;
});

app.MapPost("/connectApi", async (string? roomCallId, string? groupCallId, string? serverCallId, ILogger<Program> logger) =>
{
    CallLocator callLocator;
    if (roomCallId != null)
    {
        callLocator = new RoomCallLocator(roomCallId);
    }
    else if (groupCallId != null)
    {
        callLocator = new GroupCallLocator(groupCallId);
    }
    else if (serverCallId != null)
    {
        callLocator = new ServerCallLocator(serverCallId);
    }
    else
    {
        throw new ArgumentNullException(nameof(callLocator));
    }
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
            logger.LogInformation($"Media Streaming state: {callConnectionProperties.MediaStreamingSubscription.State}");
            logger.LogInformation($"Transcription state: {callConnectionProperties.TranscriptionSubscription.State}");

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
        else if (parsedEvent is RecognizeCanceled recognizeCanceled)
        {
            logger.LogInformation($"Received call event: {recognizeCanceled.GetType()}");
            callConnectionId = recognizeCanceled.CallConnectionId;
        }
        else if (parsedEvent is PlayStarted playStarted)
        {
            logger.LogInformation($"Received call event: {playStarted.GetType()}");
            callConnectionId = playStarted.CallConnectionId;
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

            if (isRejectCall)
            {
                await callAutomationClient.RejectCallAsync(incomingCallContext);
                logger.LogInformation($"Call Rejected, recject call setting is: {isRejectCall}");
                return Results.Ok();
            }
            //TranscriptionOptions transcriptionOptions = new TranscriptionOptions(new Uri(transportUrl),
            //    "en-us", false, TranscriptionTransport.Websocket);

            //MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(new Uri(transportUrl), MediaStreamingContent.Audio,
            //    MediaStreamingAudioChannel.Unmixed, MediaStreamingTransport.Websocket, false);

            var options = new AnswerCallOptions(incomingCallContext, callbackUri)
            {
                CallIntelligenceOptions = new CallIntelligenceOptions
                {
                    CognitiveServicesEndpoint = new Uri(cognitiveServiceEndpoint),
                    //MediaStreamingOptions = mediaStreamingOptions,
                    //TranscriptionOptions = transcriptionOptions
                }
            };
            // setup CustomCalling context
            options.CustomCallingContext.AddSipUui("OBOuuivalue");

            options.CustomCallingContext.AddSipX("XheaderOBO", "value");

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

        if (eventData is AcsRecordingFileStatusUpdatedEventData statusUpdated)
        {
            var metadataLocation = statusUpdated.RecordingStorageInfo.RecordingChunks[0].MetadataLocation;
            var contentLocation = statusUpdated.RecordingStorageInfo.RecordingChunks[0].ContentLocation;
            var deletecLocation = statusUpdated.RecordingStorageInfo.RecordingChunks[0].DeleteLocation;
            logger.LogInformation($"Metadata Location:--> {metadataLocation}");
            logger.LogInformation($"Content Location:--> {contentLocation}");
            logger.LogInformation($"Delete Location:--> {deletecLocation}");

            if (!isBYOS)
            {
                await downloadRecording(contentLocation, metadataLocation);
                await DownloadRecordingMetadata(metadataLocation, logger);
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

app.MapPost("/removeParticipant", async (string targetPhoneNumber, ILogger < Program> logger) =>
{
    var response = await RemoveParticipantAsync(targetPhoneNumber);
    return Results.Ok(response);
});

app.MapPost("/playMedia", async (bool isPlayToAll, ILogger<Program> logger) =>
{
    Console.WriteLine(isPlayToAll);
    await PlayMediaAsync(isPlayToAll);
    return Results.Ok();
});

app.MapPost("/interruptPlayMedia", async (ILogger<Program> logger) =>
{
    await InterruptPlayMediaAsync();
    return Results.Ok();
});

app.MapPost("/recognizeMedia", async (bool isDtmf, bool isSpeech, bool isSpeechOrDtmf, ILogger<Program> logger) =>
{
    await RecognizeMediaAsync(isDtmf, isSpeech, isSpeechOrDtmf);
    return Results.Ok();
});

app.MapPost("/sendDTMFTones", async (ILogger<Program> logger) =>
{
    await SendDtmfToneAsync();
    return Results.Ok();
});

app.MapPost("/startContinuousDTMFTones", async (ILogger<Program> logger) =>
{
    await StartContinuousDtmfAsync();
    return Results.Ok();
});

app.MapPost("/stopContinuousDTMFTones", async (ILogger<Program> logger) =>
{
    await StopContinuousDtmfAsync();
    return Results.Ok();
});

app.MapPost("/holdParticipant", async (bool isPlaySource, ILogger<Program> logger) =>
{
    await HoldParticipantAsync(isPlaySource);
    return Results.Ok();
});

app.MapPost("/unholdParticipant", async (ILogger<Program> logger) =>
{
    await UnholdParticipantAsync();
    return Results.Ok();
});

app.MapPost("/cancelAllMediaOperation", async (ILogger<Program> logger) =>
{
    await CancelAllMediaOperationAsync();
    return Results.Ok();
});

app.MapPost("/startMediaStreaming", async (ILogger<Program> logger) =>
{
    await StartMediaStreamingAsync();
    return Results.Ok();
});

app.MapPost("/stopMediaStreaming", async (ILogger<Program> logger) =>
{
    await StopMediaStreamingAsync();
    return Results.Ok();
});

app.MapPost("/startTranscription", async (ILogger<Program> logger) =>
{
    await StartTranscriptionAsync();
    return Results.Ok();
});

app.MapPost("/updateTranscription", async (ILogger<Program> logger) =>
{
    await UpdateTranscriptionAsync();
    return Results.Ok();
});

app.MapPost("/stopTranscription", async (ILogger<Program> logger) =>
{
    await StopTranscriptionAsync();
    return Results.Ok();
});
app.MapPost("/startRecording", async (bool isPauseOnStart, bool isByos, ILogger<Program> logger) =>
{
    isBYOS = isByos;
    await StartRecordingAsync(isPauseOnStart, isByos, logger);
    return Results.Ok();
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

async Task PlayMediaAsync(bool isPlayToAll)
{
    CallMedia callMedia = GetCallMedia();

    //FileSource fileSource = new FileSource(new Uri(fileSourceUri));

    TextSource textSource = new TextSource("Hi, this is test source played through play source thanks. Goodbye!.")
    {
        VoiceName = "en-US-NancyNeural"
    };

    SsmlSource ssmlSource = new SsmlSource("<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"><voice name=\"en-US-JennyNeural\">Hi, this is ssml test played through ssml source thanks. Goodbye!</voice></speak>");

    List<PlaySource> playSources = new List<PlaySource>()
    {
        //fileSource,
        textSource,ssmlSource
    };

    if (isPlayToAll)
    {
        PlayToAllOptions playToAllOptions = new PlayToAllOptions(playSources)
        {
            OperationContext = "playToAllContext",
            OperationCallbackUri = callbackUri
        };
        await callMedia.PlayToAllAsync(playToAllOptions);
    }
    else
    {

        CommunicationIdentifier target = GetCommunicationTargetIdentifier();

        var playTo = new List<CommunicationIdentifier> { target };

        PlayOptions playOptions = new PlayOptions(playSources, playTo)
        {
            OperationContext = "playToAllContext",
            OperationCallbackUri = callbackUri,
            //InterruptHoldAudio = true
        };

        await callMedia.PlayAsync(playOptions);
    }
}

async Task InterruptPlayMediaAsync()
{
    CallMedia callMedia = GetCallMedia();
    TextSource textSource = new TextSource("Hi, this is test source played through play source thanks. Goodbye!.")
    {
        VoiceName = "en-US-NancyNeural"
    };
    PlayToAllOptions playToAllOptions = new PlayToAllOptions(textSource)
    {
        Loop = true,
        OperationContext = "playToAllContext",
        OperationCallbackUri = callbackUri,
        InterruptCallMediaOperation = false
    };
    await callMedia.PlayToAllAsync(playToAllOptions);

    //Interrupt play media
    TextSource interruptTextSource = new TextSource("Hi, this is Interrupt play source!.")
    {
        VoiceName = "en-US-NancyNeural"
    };
    PlayToAllOptions interruptPlayToAllOptions = new PlayToAllOptions(interruptTextSource)
    {
        OperationContext = "playToAllContext",
        OperationCallbackUri = callbackUri,
        InterruptCallMediaOperation = true
    };
    await callMedia.PlayToAllAsync(interruptPlayToAllOptions);

}

async Task RecognizeMediaAsync(bool isDtmf, bool isSpeech, bool isSpeechOrDtmf)
{
    CommunicationIdentifier target = GetCommunicationTargetIdentifier();

    CallMediaRecognizeOptions recognizeOptions;

    FileSource fileSource = new FileSource(new Uri(fileSourceUri));

    if (isDtmf)
    {
        //DTMF only.
        recognizeOptions =
                new CallMediaRecognizeDtmfOptions(
                    targetParticipant: target, maxTonesToCollect: 4)
                {
                    InterruptPrompt = false,
                    InterToneTimeout = TimeSpan.FromSeconds(5),
                    OperationContext = "DtmfContext",
                    InitialSilenceTimeout = TimeSpan.FromSeconds(15),
                    Prompt = fileSource,
                    OperationCallbackUri = callbackUri
                };

    }
    else if (isSpeech)
    {
        //SPEECH only.
        recognizeOptions = new CallMediaRecognizeSpeechOptions(targetParticipant: target)
        {
            InterruptPrompt = false,
            OperationContext = "SpeechContext",
            InitialSilenceTimeout = TimeSpan.FromSeconds(15),
            Prompt = fileSource,
            EndSilenceTimeout = TimeSpan.FromSeconds(15),
            OperationCallbackUri = callbackUri
        };
    }
    else if (isSpeechOrDtmf)
    {
        //Speech or DTMF
        recognizeOptions =
                new CallMediaRecognizeSpeechOrDtmfOptions(
                    targetParticipant: target, maxTonesToCollect: 4)
                {
                    InterruptPrompt = false,
                    OperationContext = "SpeechOrDTMFContext",
                    InitialSilenceTimeout = TimeSpan.FromSeconds(15),
                    Prompt = fileSource,
                    EndSilenceTimeout = TimeSpan.FromSeconds(5),
                    OperationCallbackUri = callbackUri
                };
    }
    else
    {
        TextSource textSource = new TextSource("Say confirm or cancel")
        {
            VoiceName = "en-US-NancyNeural"
        };
        recognizeOptions =
            new CallMediaRecognizeChoiceOptions(targetParticipant: target, GetChoices())
            {
                InterruptCallMediaOperation = false,
                InterruptPrompt = false,
                InitialSilenceTimeout = TimeSpan.FromSeconds(10),
                Prompt = textSource,
                OperationContext = "ChoiceContext",
                OperationCallbackUri = callbackUri
            };
    }
    CallMedia callMedia = GetCallMedia();

    await callMedia.StartRecognizingAsync(recognizeOptions);
}

async Task SendDtmfToneAsync()
{
    CommunicationIdentifier target = GetCommunicationTargetIdentifier();

    List<DtmfTone> tones = new List<DtmfTone>
        {
            DtmfTone.Zero,
            DtmfTone.One
        };

    CallMedia callMedia = GetCallMedia();

    await callMedia.SendDtmfTonesAsync(tones, target);
}

async Task StartContinuousDtmfAsync()
{
    CommunicationIdentifier target = GetCommunicationTargetIdentifier();

    CallMedia callMedia = GetCallMedia();

    await callMedia.StartContinuousDtmfRecognitionAsync(target);
}

async Task StopContinuousDtmfAsync()
{
    CommunicationIdentifier target = GetCommunicationTargetIdentifier();

    CallMedia callMedia = GetCallMedia();

    await callMedia.StopContinuousDtmfRecognitionAsync(target);
}

async Task HoldParticipantAsync(bool isPlaySource)
{
    CommunicationIdentifier target = GetCommunicationTargetIdentifier();

    CallMedia callMedia = GetCallMedia();

    //FileSource fileSource = new FileSource(new Uri(fileSourceUri));
    TextSource textSource = new TextSource("You are hold. Please wait some time..!") { VoiceName = "en-US-NancyNeural" };

    if (isPlaySource)
    {
        HoldOptions holdOptions = new HoldOptions(target)
        {
            PlaySource = textSource,
            OperationContext = "holdUserContext",
            OperationCallbackUri = callbackUri
        };
        await callMedia.HoldAsync(holdOptions);
    }
    else
    {
        await callMedia.HoldAsync(target);
    }
}

async Task UnholdParticipantAsync()
{
    CommunicationIdentifier target = GetCommunicationTargetIdentifier();

    CallMedia callMedia = GetCallMedia();

    await callMedia.UnholdAsync(target);
}

async Task CancelAllMediaOperationAsync()
{
    CallMedia callMedia = GetCallMedia();

    await callMedia.CancelAllMediaOperationsAsync();
}

async Task StartMediaStreamingAsync()
{
    CallMedia callMedia = GetCallMedia();

    CallConnectionProperties callConnectionProperties = GetCallConnectionProperties();

    if (callConnectionProperties.MediaStreamingSubscription.State.Equals("inactive"))
    {
        await callMedia.StartMediaStreamingAsync();
    }
    else
    {
        Console.WriteLine("Media streaming is already active");
    }
}

async Task StopMediaStreamingAsync()
{
    CallMedia callMedia = GetCallMedia();

    CallConnectionProperties callConnectionProperties = GetCallConnectionProperties();

    if (callConnectionProperties.MediaStreamingSubscription.State.Equals("active"))
    {
        await callMedia.StopMediaStreamingAsync();
    }
    else
    {
        Console.WriteLine("Media streaming is not active");
    }
}

async Task StartTranscriptionAsync()
{
    CallMedia callMedia = GetCallMedia();

    CallConnectionProperties callConnectionProperties = GetCallConnectionProperties();

    if (callConnectionProperties.TranscriptionSubscription.State.Equals("inactive"))
    {
        await callMedia.StartTranscriptionAsync();
    }
    else
    {
        Console.WriteLine("Transcription is already active");
    }
}

async Task StopTranscriptionAsync()
{
    CallMedia callMedia = GetCallMedia();

    CallConnectionProperties callConnectionProperties = GetCallConnectionProperties();

    if (callConnectionProperties.TranscriptionSubscription.State.Equals("active"))
    {
        await callMedia.StopTranscriptionAsync();
    }
    else
    {
        Console.WriteLine("Transcription is not active");
    }
}

async Task UpdateTranscriptionAsync()
{
    CallMedia callMedia = GetCallMedia();

    CallConnectionProperties callConnectionProperties = GetCallConnectionProperties();

    if (callConnectionProperties.TranscriptionSubscription.State.Equals("active"))
    {
        await callMedia.UpdateTranscriptionAsync("en-au");
    }
    else
    {
        Console.WriteLine("Transcription is not active");
    }
}


async Task StartRecordingAsync(bool isPauseOnStart, bool isByos, ILogger<Program> logger)
{
    CallMedia callMedia = GetCallMedia();
    CallConnectionProperties callConnectionProperties = GetCallConnectionProperties();
    var CallConnectionId = callConnectionId;
    StartRecordingOptions recordingOptions = new StartRecordingOptions(new ServerCallLocator(callConnectionProperties.ServerCallId))
    {
        RecordingContent = RecordingContent.Audio,
        RecordingChannel = RecordingChannel.Unmixed,
        RecordingFormat = RecordingFormat.Wav,
        PauseOnStart = isPauseOnStart,
        RecordingStateCallbackUri = callbackUri,
        RecordingStorage = isByos && !string.IsNullOrEmpty(bringYourOwnStorageUrl) ? RecordingStorage.CreateAzureBlobContainerRecordingStorage(new Uri(bringYourOwnStorageUrl)) : null
    };
    logger.LogInformation($"Pause On Start-->: {recordingOptions.PauseOnStart}");
    var playTask = HandlePlayAsync(callMedia, handlePrompt, "handlePromptContext");

    var recordingTask = callAutomationClient.GetCallRecording().StartAsync(recordingOptions);
    await Task.WhenAll(playTask, recordingTask);
    var recordingId = recordingTask.Result.Value.RecordingId;
    logger.LogInformation($"Call recording id--> {recordingId}");

    var state = await GetRecordingState(recordingId, logger);
    if (state == "active")
    {

        await callAutomationClient.GetCallRecording().PauseAsync(recordingId);
        logger.LogInformation($"Recording is Paused.");
        await GetRecordingState(recordingId, logger);

        await Task.Delay(5000);
        await callAutomationClient.GetCallRecording().ResumeAsync(recordingId);
        logger.LogInformation($"Recording is resumed.");
        await GetRecordingState(recordingId, logger);
    }
    else
    {
        await Task.Delay(5000);
        await callAutomationClient.GetCallRecording().ResumeAsync(recordingId);
        logger.LogInformation($"Recording is Resumed.");
        await GetRecordingState(recordingId, logger);
    }

    await Task.Delay(5000);
    await callAutomationClient.GetCallRecording().StopAsync(recordingId);
    logger.LogInformation($"Recording is Stopped.");
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

CommunicationIdentifier GetCommunicationTargetIdentifier()
{
    CommunicationIdentifier target = new PhoneNumberIdentifier(targetPhoneNumber);

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


async Task HandlePlayAsync(CallMedia callConnectionMedia, string textToPlay, string context)
{
    var playSource = new TextSource(textToPlay)
    {
        VoiceName = "en-US-NancyNeural"
    };
    var playOptions = new PlayToAllOptions(playSource) { OperationContext = context };
    await callConnectionMedia.PlayToAllAsync(playOptions);
}


async Task downloadRecording(string contentLocation, string metadataLocation)
{
    string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    var recordingDownloadUri = new Uri(contentLocation);
    string format = await GetFormat(metadataLocation);
    var response = await callAutomationClient.GetCallRecording().DownloadToAsync(recordingDownloadUri, $"{downloadsPath}\\test.{format}");
}

async Task<string> GetFormat(string metadataLocation)
{
    string format = string.Empty;
    var metaDataDownloadUri = new Uri(metadataLocation);
    var metaDataResponse = await callAutomationClient.GetCallRecording().DownloadStreamingAsync(metaDataDownloadUri);
    using (StreamReader streamReader = new StreamReader(metaDataResponse))
    {
        // Read the JSON content from the stream and parse it into an object
        string jsonContent = await streamReader.ReadToEndAsync();

        // Parse the JSON string
        JObject jsonObject = JObject.Parse(jsonContent);

        // Access the "format" value from the "recordingInfo" object
        format = (string)jsonObject["recordingInfo"]["format"];
    }
    return format;
}

async Task<string> GetRecordingState(string recordingId, ILogger<Program> logger)
{
    var result = await callAutomationClient.GetCallRecording().GetStateAsync(recordingId);
    string state = result.Value.RecordingState.ToString();
    logger.LogInformation($"Recording Status:->  {state}");
    logger.LogInformation($"Recording Type:-> {result.Value.RecordingKind.ToString()}");
    return state;
}

async Task DownloadRecordingMetadata(string metadataLocation, ILogger<Program> logger)
{
    if (!string.IsNullOrEmpty(metadataLocation))
    {
        string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        var recordingDownloadUri = new Uri(metadataLocation);
        var response = await callAutomationClient.GetCallRecording().DownloadToAsync(recordingDownloadUri, $"{downloadsPath}\\recordingMetadata.json");
    }
    else
    {
        logger.LogError("Metadata location is empty.");
    }
}

app.Run();