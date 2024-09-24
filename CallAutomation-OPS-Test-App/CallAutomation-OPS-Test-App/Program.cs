using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Azure.Communication.CallAutomation 1.3.0-alpha.20240903.2

var acsConnectionString = builder.Configuration.GetValue<string>("AcsConnectionString");
ArgumentNullException.ThrowIfNullOrEmpty(acsConnectionString);

var cognitiveServicesEndpoint = builder.Configuration.GetValue<string>("CognitiveServiceEndpoint");
ArgumentNullException.ThrowIfNullOrEmpty(cognitiveServicesEndpoint);

//var transportUrl = builder.Configuration.GetValue<string>("TransportUrl");
//ArgumentNullException.ThrowIfNullOrEmpty(transportUrl);

var acsPhoneNumber = builder.Configuration.GetValue<string>("AcsPhoneNumber");
ArgumentNullException.ThrowIfNullOrEmpty(acsPhoneNumber);

var locale = builder.Configuration.GetValue<string>("Locale");
ArgumentNullException.ThrowIfNullOrEmpty(locale);

var callbackUriHost = builder.Configuration.GetValue<string>("CallbackUriHost");
ArgumentNullException.ThrowIfNullOrEmpty(callbackUriHost);

var fileSourceUri = builder.Configuration.GetValue<string>("FileSourceUri");
ArgumentNullException.ThrowIfNullOrEmpty(fileSourceUri);

var callerPhoneNumber = builder.Configuration.GetValue<string>("CallerPhoneNumber");

var participantPhoneNumber = builder.Configuration.GetValue<string>("ParticipantPhoneNumber");

var callerTeamsUser = builder.Configuration.GetValue<string>("CallerTeamsUser");

var participantTeamsUser = builder.Configuration.GetValue<string>("ParticipantTeamsUser");

var transcription = builder.Configuration.GetValue<string>("StartTranscription");

var mediaStreaming = builder.Configuration.GetValue<string>("StartMediaStreaming");

bool startMediaStreaming = !string.IsNullOrEmpty(mediaStreaming) && mediaStreaming == "true" ? true : false;

bool startTranscription = !string.IsNullOrEmpty(transcription) && transcription == "true" ? true : false;

var client = new CallAutomationClient(connectionString: acsConnectionString);

builder.Services.AddSingleton(client);
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


string callConnectionId = string.Empty;

Uri eventCallbackUri;

app.MapPost("/api/incomingCall", async (
    [FromBody] EventGridEvent[] eventGridEvents,
    ILogger<Program> logger) =>
{
    foreach (var eventGridEvent in eventGridEvents)
    {
        logger.LogInformation($"Call event received:{eventGridEvent.EventType}");


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
        }

        if (eventData is AcsIncomingCallEventData incomingCallEventData)
        {
            var callerId = incomingCallEventData.FromCommunicationIdentifier.RawId;
            var callbackUri = new Uri(new Uri(callbackUriHost), $"/api/callbacks/{Guid.NewGuid()}?callerId={callerId}");
            logger.LogInformation($"Incoming call - correlationId: {incomingCallEventData.CorrelationId}, " +
                $"Callback url: {callbackUri}");

            eventCallbackUri = callbackUri;

            //TranscriptionOptions transcriptionOptions = new TranscriptionOptions(new Uri(transportUrl),
            //    TranscriptionTransport.Websocket, locale, true);

            //MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(new Uri(transportUrl), MediaStreamingTransport.Websocket, MediaStreamingContent.Audio,
            //    MediaStreamingAudioChannel.Unmixed);

            var options = new AnswerCallOptions(incomingCallEventData.IncomingCallContext, callbackUri)
            {
                CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
                //TranscriptionOptions = transcriptionOptions,
                //MediaStreamingOptions = mediaStreamingOptions
            };

            AnswerCallResult answerCallResult = await client.AnswerCallAsync(options);
            var callConnectionMedia = answerCallResult.CallConnection.GetCallMedia();


            var answer_result = await answerCallResult.WaitForEventProcessorAsync();
            if (answer_result.IsSuccess)
            {
                logger.LogInformation($"Received call event: {answer_result.GetType()}, callConnectionID: {answer_result.SuccessResult.CallConnectionId}, " +
                    $"serverCallId: {answer_result.SuccessResult.ServerCallId}");

                callConnectionId = answer_result.SuccessResult.CallConnectionId;

                CallConnectionProperties callConnectionProperties = GetCallConnectionProperties();
                logger.LogInformation($"CORRELATION ID: {callConnectionProperties.CorrelationId}");
                //logger.LogInformation($"Media Streaming state: {callConnectionProperties.MediaStreamingSubscription.State}");
                //logger.LogInformation($"Transcription state: {callConnectionProperties.TranscriptionSubscription.State}");

            }
            client.GetEventProcessor().AttachOngoingEventProcessor<PlayCompleted>(
                 answerCallResult.CallConnection.CallConnectionId, async (playCompletedEvent) =>
                 {
                     logger.LogInformation($"Received call event: {playCompletedEvent.GetType()}, context: {playCompletedEvent.OperationContext}");
                     callConnectionId = playCompletedEvent.CallConnectionId;
                 });
            client.GetEventProcessor().AttachOngoingEventProcessor<RecognizeCompleted>(
                answerCallResult.CallConnection.CallConnectionId, async (recognizeCompletedEvent) =>
                {
                    logger.LogInformation($"Received call event: {recognizeCompletedEvent.GetType()}, context: {recognizeCompletedEvent.OperationContext}");
                    callConnectionId = recognizeCompletedEvent.CallConnectionId;

                    switch (recognizeCompletedEvent.RecognizeResult)
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
                            logger.LogInformation("Recognize completed succesfully, recognizeResult={recognizeResult}", recognizeCompletedEvent.RecognizeResult);
                            break;
                    }

                });
            client.GetEventProcessor().AttachOngoingEventProcessor<AddParticipantSucceeded>(
               answerCallResult.CallConnection.CallConnectionId, async (addParticipantSucceededEvent) =>
               {
                   logger.LogInformation($"Received call event: {addParticipantSucceededEvent.GetType()}, context: {addParticipantSucceededEvent.OperationContext}");
                   callConnectionId = addParticipantSucceededEvent.CallConnectionId;
               });
            client.GetEventProcessor().AttachOngoingEventProcessor<ParticipantsUpdated>(
              answerCallResult.CallConnection.CallConnectionId, async (participantsUpdatedEvent) =>
              {
                  logger.LogInformation($"Received call event: {participantsUpdatedEvent.GetType()}, participants: {participantsUpdatedEvent.Participants.Count()}, sequenceId: {participantsUpdatedEvent.SequenceNumber}");
                  callConnectionId = participantsUpdatedEvent.CallConnectionId;
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
                  callConnectionId = addParticipantFailedEvent.CallConnectionId;
              });
            client.GetEventProcessor().AttachOngoingEventProcessor<PlayFailed>(
                answerCallResult.CallConnection.CallConnectionId, async (playFailedEvent) =>
                {
                    logger.LogInformation($"Received call event: {playFailedEvent.GetType()}, CorrelationId: {playFailedEvent.CorrelationId}, " +
                        $"subCode: {playFailedEvent.ResultInformation?.SubCode}, message: {playFailedEvent.ResultInformation?.Message}, context: {playFailedEvent.OperationContext}");
                    callConnectionId = playFailedEvent.CallConnectionId;
                });

            client.GetEventProcessor().AttachOngoingEventProcessor<RecognizeFailed>(
                answerCallResult.CallConnection.CallConnectionId, async (recognizeFailedEvent) =>
                {
                    logger.LogInformation($"Received call event: {recognizeFailedEvent.GetType()}, CorrelationId: {recognizeFailedEvent.CorrelationId}, " +
                        $"subCode: {recognizeFailedEvent.ResultInformation?.SubCode}, message: {recognizeFailedEvent.ResultInformation?.Message}, context: {recognizeFailedEvent.OperationContext}");
                    callConnectionId = recognizeFailedEvent.CallConnectionId;
                });

            client.GetEventProcessor().AttachOngoingEventProcessor<TranscriptionStarted>(
                answerCallResult.CallConnection.CallConnectionId, async (transcriptionStarted) =>
                {
                    logger.LogInformation($"Received transcription event: {transcriptionStarted.GetType()}");
                    callConnectionId = transcriptionStarted.CallConnectionId;
                });

            client.GetEventProcessor().AttachOngoingEventProcessor<TranscriptionStopped>(
                answerCallResult.CallConnection.CallConnectionId, async (transcriptionStopped) =>
                {
                    callConnectionId = transcriptionStopped.CallConnectionId;
                    logger.LogInformation("Received transcription event: {type}", transcriptionStopped.GetType());
                });

            client.GetEventProcessor().AttachOngoingEventProcessor<TranscriptionFailed>(
                answerCallResult.CallConnection.CallConnectionId, async (transcriptionFailed) =>
                {
                    callConnectionId = transcriptionFailed.CallConnectionId;
                    logger.LogInformation($"Received transcription event: {transcriptionFailed.GetType()}, CorrelationId: {transcriptionFailed.CorrelationId}, " +
                        $"SubCode: {transcriptionFailed?.ResultInformation?.SubCode}, Message: {transcriptionFailed?.ResultInformation?.Message}");
                });

            client.GetEventProcessor().AttachOngoingEventProcessor<MediaStreamingStarted>(
                answerCallResult.CallConnection.CallConnectionId, async (mediaStreamingStarted) =>
                {
                    logger.LogInformation($"Received event: {mediaStreamingStarted.GetType()}");
                    callConnectionId = mediaStreamingStarted.CallConnectionId;
                });

            client.GetEventProcessor().AttachOngoingEventProcessor<MediaStreamingStopped>(
                answerCallResult.CallConnection.CallConnectionId, async (mediaStreamingStopped) =>
                {
                    logger.LogInformation("Received event: {type}", mediaStreamingStopped.GetType());
                    callConnectionId = mediaStreamingStopped.CallConnectionId;
                });

            client.GetEventProcessor().AttachOngoingEventProcessor<MediaStreamingFailed>(
                answerCallResult.CallConnection.CallConnectionId, async (mediaStreamingFailed) =>
                {
                    callConnectionId = mediaStreamingFailed.CallConnectionId;
                    logger.LogInformation($"Received event: {mediaStreamingFailed.GetType()}, CorrelationId: {mediaStreamingFailed.CorrelationId}, " +
                        $"SubCode: {mediaStreamingFailed?.ResultInformation?.SubCode}, Message: {mediaStreamingFailed?.ResultInformation?.Message}");
                });

            client.GetEventProcessor().AttachOngoingEventProcessor<PlayCanceled>(
                answerCallResult.CallConnection.CallConnectionId, async (playCanceled) =>
                {
                    logger.LogInformation($"Received event: {playCanceled.GetType()}");
                    callConnectionId = playCanceled.CallConnectionId;
                });

            client.GetEventProcessor().AttachOngoingEventProcessor<RecognizeCanceled>(
               answerCallResult.CallConnection.CallConnectionId, async (recognizeCanceled) =>
               {
                   logger.LogInformation($"Received event: {recognizeCanceled.GetType()}");
                   callConnectionId = recognizeCanceled.CallConnectionId;
               });

            client.GetEventProcessor().AttachOngoingEventProcessor<SendDtmfTonesCompleted>(
               answerCallResult.CallConnection.CallConnectionId, async (sendDtmfTonesCompleted) =>
               {
                   logger.LogInformation($"Received event: {sendDtmfTonesCompleted.GetType()}");
                   callConnectionId = sendDtmfTonesCompleted.CallConnectionId;
               });

            client.GetEventProcessor().AttachOngoingEventProcessor<SendDtmfTonesFailed>(
                answerCallResult.CallConnection.CallConnectionId, async (sendDtmfTonesFailed) =>
                {
                    callConnectionId = sendDtmfTonesFailed.CallConnectionId;
                    logger.LogInformation($"Received event: {sendDtmfTonesFailed.GetType()}, CorrelationId: {sendDtmfTonesFailed.CorrelationId}, " +
                        $"SubCode: {sendDtmfTonesFailed?.ResultInformation?.SubCode}, Message: {sendDtmfTonesFailed?.ResultInformation?.Message}");
                });

            client.GetEventProcessor().AttachOngoingEventProcessor<ContinuousDtmfRecognitionToneReceived>(
               answerCallResult.CallConnection.CallConnectionId, async (continuousDtmfRecognitionToneReceived) =>
               {
                   callConnectionId = continuousDtmfRecognitionToneReceived.CallConnectionId;
                   logger.LogInformation($"Received event: {continuousDtmfRecognitionToneReceived.GetType()}");

                    logger.LogInformation("Tone?detected:?sequenceId={sequenceId},?tone={tone}",
                    continuousDtmfRecognitionToneReceived.SequenceId,
                    continuousDtmfRecognitionToneReceived.Tone);
               });

            client.GetEventProcessor().AttachOngoingEventProcessor<ContinuousDtmfRecognitionStopped>(
               answerCallResult.CallConnection.CallConnectionId, async (continuousDtmfRecognitionStopped) =>
               {
                   logger.LogInformation($"Received event: {continuousDtmfRecognitionStopped.GetType()}");
                   callConnectionId = continuousDtmfRecognitionStopped.CallConnectionId;
               });

            client.GetEventProcessor().AttachOngoingEventProcessor<ContinuousDtmfRecognitionToneFailed>(
                answerCallResult.CallConnection.CallConnectionId, async (continuousDtmfRecognitionToneFailed) =>
                {
                    callConnectionId = continuousDtmfRecognitionToneFailed.CallConnectionId;
                    logger.LogInformation($"Received event: {continuousDtmfRecognitionToneFailed.GetType()}, CorrelationId: {continuousDtmfRecognitionToneFailed.CorrelationId}, " +
                        $"SubCode: {continuousDtmfRecognitionToneFailed?.ResultInformation?.SubCode}, Message: {continuousDtmfRecognitionToneFailed?.ResultInformation?.Message}");
                });

            client.GetEventProcessor().AttachOngoingEventProcessor<HoldFailed>(
                answerCallResult.CallConnection.CallConnectionId, async (holdFailed) =>
                {
                    callConnectionId = holdFailed.CallConnectionId;
                    logger.LogInformation($"Received event: {holdFailed.GetType()}, CorrelationId: {holdFailed.CorrelationId}, " +
                        $"SubCode: {holdFailed?.ResultInformation?.SubCode}, Message: {holdFailed?.ResultInformation?.Message}");
                });

            client.GetEventProcessor().AttachOngoingEventProcessor<ParticipantsUpdated>(
                answerCallResult.CallConnection.CallConnectionId, async (ParticipantsUpdated) =>
                {
                    callConnectionId = ParticipantsUpdated.CallConnectionId;
                    logger.LogInformation($"Received event: {ParticipantsUpdated.GetType()}");
                    callConnectionId = ParticipantsUpdated.CallConnectionId;
                });
        }
    }
    return Results.Ok();
});


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

app.MapPost("/addParticipant", async (bool isTeamsUser ,ILogger<Program> logger) =>
{
    Console.WriteLine(isTeamsUser);
    var response = await AddParticipantAsync(isTeamsUser);
    return Results.Ok(response);
});

app.MapPost("/playMedia", async (bool isPlayToAll, bool isTeamsUser, bool isPlayMediaToCaller, ILogger <Program> logger) =>
{
    Console.WriteLine(isPlayToAll);
    await PlayMediaAsync(isPlayToAll, isTeamsUser, isPlayMediaToCaller);
    return Results.Ok();
});

app.MapPost("/recognizeMedia", async (bool isDtmf, bool isSpeechOrDtmf, bool isTeamsUser, bool isPlayMediaToCaller, ILogger <Program> logger) =>
{
    await RecognizeMediaAsync(isDtmf, isSpeechOrDtmf, isTeamsUser, isPlayMediaToCaller);
    return Results.Ok();
});

app.MapPost("/sendDTMFTones", async (bool isTeamsUser, bool isPlayMediaToCaller, ILogger <Program> logger) =>
{
    Console.WriteLine(isTeamsUser);
    await SendDtmfToneAsync(isTeamsUser, isPlayMediaToCaller);
    return Results.Ok();
});

app.MapPost("/startContinuousDTMFTones", async (bool isTeamsUser, bool isPlayMediaToCaller, ILogger <Program> logger) =>
{
    Console.WriteLine(isTeamsUser);
    await StartContinuousDtmfAsync(isTeamsUser, isPlayMediaToCaller);
    return Results.Ok();
});

app.MapPost("/stopContinuousDTMFTones", async (bool isTeamsUser, bool isPlayMediaToCaller, ILogger <Program> logger) =>
{
    Console.WriteLine(isTeamsUser);
    await StopContinuousDtmfAsync(isTeamsUser, isPlayMediaToCaller);
    return Results.Ok();
});

app.MapPost("/holdParticipant", async (bool isTeamsUser,bool isPlaySource, bool isPlayMediaToCaller, ILogger <Program> logger) =>
{
    Console.WriteLine(isTeamsUser);
    await HoldParticipantAsync(isTeamsUser, isPlaySource, isPlayMediaToCaller);
    return Results.Ok();
});

app.MapPost("/unholdParticipant", async (bool isTeamsUser, bool isPlayMediaToCaller, ILogger <Program> logger) =>
{
    Console.WriteLine(isTeamsUser);
    await UnholdParticipantAsync(isTeamsUser, isPlayMediaToCaller);
    return Results.Ok();
});

app.MapPost("/cancelAllMediaOperation", async (ILogger<Program> logger) =>
{
    await CancelAllMediaOperaionAsync();
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

async Task<AddParticipantResult> AddParticipantAsync(bool isTeamsUser)
{
    CallInvite callInvite;

    CallConnection callConnection = GetConnection();

    string operationContext = isTeamsUser ? "addTeamsUserContext" : "addPSTNUserContext";

    if (isTeamsUser)
    {
        callInvite = new CallInvite(new MicrosoftTeamsUserIdentifier(participantTeamsUser));
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

async Task PlayMediaAsync(bool isPlayToAll,bool isTeamsUser, bool isPlayMediaToCaller)
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
        fileSource,textSource,ssmlSource
    };

    if (isPlayToAll) {
        PlayToAllOptions playToAllOptions = new PlayToAllOptions(playSources)
        {
            OperationContext="playToAllContext"
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

async Task RecognizeMediaAsync(bool isDtmf, bool isSpeechOrDtmf, bool isTeamsUser, bool isPlayMediaToCaller)
{
    CommunicationIdentifier target = GetCommunicationTargetIdentifier(isTeamsUser, isPlayMediaToCaller);

    Console.WriteLine("Target User:--> " + target);

    CallMediaRecognizeOptions recognizeOptions = null;

    FileSource fileSource = new FileSource(new Uri(fileSourceUri));

    if (isDtmf && !isSpeechOrDtmf)
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
                    Prompt = fileSource
                };

    }
    else if (!isDtmf && !isSpeechOrDtmf) {
        //SPEECH only.
        recognizeOptions = new CallMediaRecognizeSpeechOptions(targetParticipant: target)
                {
                    InterruptPrompt = false,
                    OperationContext = "SpeechContext",
                    InitialSilenceTimeout = TimeSpan.FromSeconds(15),
                    Prompt = fileSource,
                    EndSilenceTimeout = TimeSpan.FromSeconds(15)
                };
    }
    else if(!isDtmf && isSpeechOrDtmf)
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
                    EndSilenceTimeout = TimeSpan.FromSeconds(5)
                };
    }

    //recognizeOptions =
    //    new CallMediaRecognizeChoiceOptions(targetParticipant: target, GetChoices())
    //    {
    //        InterruptCallMediaOperation = false,
    //        InterruptPrompt = false,
    //        InitialSilenceTimeout = TimeSpan.FromSeconds(10),
    //        Prompt = fileSource,
    //        OperationContext = "ChoiceContext"
    //    };

    CallMedia callMedia = GetCallMedia();

    await callMedia.StartRecognizingAsync(recognizeOptions);
}

async Task SendDtmfToneAsync(bool isTeamsUser, bool isPlayMediaToCaller)
{
    CommunicationIdentifier target = GetCommunicationTargetIdentifier(isTeamsUser, isPlayMediaToCaller);

    Console.WriteLine("Target User:--> " + target);

    List<DtmfTone> tones = new List<DtmfTone>
        {
            DtmfTone.Zero,
            DtmfTone.One
        };

    CallMedia callMedia = GetCallMedia();

    await callMedia.SendDtmfTonesAsync(tones, target);
}

async Task StartContinuousDtmfAsync(bool isTeamsUser, bool isPlayMediaToCaller)
{
    CommunicationIdentifier target = GetCommunicationTargetIdentifier(isTeamsUser, isPlayMediaToCaller);

    Console.WriteLine("Target User:--> " + target);

    CallMedia callMedia = GetCallMedia();

    await callMedia.StartContinuousDtmfRecognitionAsync(target);
}

async Task StopContinuousDtmfAsync(bool isTeamsUser, bool isPlayMediaToCaller)
{
    CommunicationIdentifier target = GetCommunicationTargetIdentifier(isTeamsUser, isPlayMediaToCaller);

    Console.WriteLine("Target User:--> " + target);

    CallMedia callMedia = GetCallMedia();

    await callMedia.StopContinuousDtmfRecognitionAsync(target);
}

async Task HoldParticipantAsync(bool isTeamsUser, bool isPlaySource, bool isPlayMediaToCaller)
{
    CommunicationIdentifier target = GetCommunicationTargetIdentifier(isTeamsUser, isPlayMediaToCaller);

    Console.WriteLine("Target User:--> " + target);

    CallMedia callMedia = GetCallMedia();

    FileSource fileSource = new FileSource(new Uri(fileSourceUri));

    if (isPlaySource)
    {
        HoldOptions holdOptions = new HoldOptions(target)
        {
            PlaySourceInfo = fileSource,
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

    await Task.Delay(5000);

    CallParticipant participant = await GetParticipantAsync(target);
    if (participant != null)
    {
        Console.WriteLine("Participant:-->" + participant.Identifier.RawId.ToString());
        Console.WriteLine("Is Participant on hold:-->" + participant.IsOnHold);
    }
}

async Task UnholdParticipantAsync(bool isTeamsUser, bool isPlayMediaToCaller)
{
    CommunicationIdentifier target = GetCommunicationTargetIdentifier(isTeamsUser, isPlayMediaToCaller);

    Console.WriteLine("Target User:--> " + target);

    CallMedia callMedia = GetCallMedia();

    UnholdOptions unholdOptions = new UnholdOptions(target)
    {
        OperationContext = "unholdUserContext"
    };

    await callMedia.UnholdAsync(unholdOptions);

    await Task.Delay(5000);

    CallParticipant participant = await GetParticipantAsync(target);
    if (participant != null)
    {
        Console.WriteLine("Participant:-->" + participant.Identifier.RawId.ToString());
        Console.WriteLine("Is Participant on hold:-->" + participant.IsOnHold);
    }
}

async Task CancelAllMediaOperaionAsync()
{
    CallMedia callMedia = GetCallMedia();

    await callMedia.CancelAllMediaOperationsAsync();
}

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

    foreach (var participant in list.Value) {
        Console.WriteLine("----------------------------------------------------------------------");
        Console.WriteLine("Participant:-->" + participant.Identifier.RawId.ToString());
        Console.WriteLine("Is Participant on hold:-->" + participant.IsOnHold);
        Console.WriteLine("----------------------------------------------------------------------");
    }
}

//async Task StartMediaStreamingAsync()
//{
//    CallMedia callMedia = GetCallMedia();

//    CallConnectionProperties callConnectionProperties = GetCallConnectionProperties();

//    if (callConnectionProperties.MediaStreamingSubscription.State.Equals("inactive"))
//    {
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

CommunicationIdentifier GetCommunicationTargetIdentifier(bool isTeamsUser, bool isPlayMediaToCaller)
{
    string teamsIdentifier = isPlayMediaToCaller ? callerTeamsUser : participantTeamsUser;

    string pstnIdentifier = isPlayMediaToCaller ? callerPhoneNumber : participantPhoneNumber;

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