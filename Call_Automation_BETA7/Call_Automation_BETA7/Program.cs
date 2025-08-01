using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Call_Automation_GCCH;
using System.Runtime.CompilerServices;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

string acsConnectionString = builder.Configuration.GetValue<string>("AcsConnectionString");
string cognitiveServicesEndpoint = builder.Configuration.GetValue<string>("CognitiveServiceEndpoint");
string acsPhoneNumber = builder.Configuration.GetValue<string>("AcsPhoneNumber");
string callbackUriHost = builder.Configuration.GetValue<string>("CallbackUriHost");
string fileSourceUri = "https://sample-videos.com/audio/mp3/crowd-cheering.mp3";

string callConnectionId = string.Empty;
string recordingId = string.Empty;
string recordingLocation = string.Empty;
string recordingFileFormat = string.Empty;
Uri eventCallbackUri = null!;
string callerId = string.Empty;
ConfigurationRequest configuration = new();
CallAutomationClient client = new CallAutomationClient(new Uri("https://uswe-03.sdf.pma.teams.microsoft.com"),acsConnectionString);

//CallAutomationClient client = new CallAutomationClient(acsConnectionString);
// flags to set up call automation to redirect the call
var isRedirect = false;
var redirectUser = string.Empty;


//app.MapPost("/setConfigurations", (ConfigurationRequest configurationRequest, ILogger<Program> logger) =>
//{
//    acsConnectionString = string.Empty;
//    cognitiveServicesEndpoint = string.Empty;
//    acsPhoneNumber = string.Empty;
//    callbackUriHost = string.Empty;
//    fileSourceUri = string.Empty;

//    if (configurationRequest != null)
//    {
//        configuration.AcsConnectionString = !string.IsNullOrEmpty(configurationRequest.AcsConnectionString) ? configurationRequest.AcsConnectionString : throw new ArgumentNullException(nameof(configurationRequest.AcsConnectionString));
//        configuration.CongnitiveServiceEndpoint = !string.IsNullOrEmpty(configurationRequest.CongnitiveServiceEndpoint) ? configurationRequest.CongnitiveServiceEndpoint : throw new ArgumentNullException(nameof(configurationRequest.CongnitiveServiceEndpoint));
//        configuration.AcsPhoneNumber = !string.IsNullOrEmpty(configurationRequest.AcsPhoneNumber) ? configurationRequest.AcsPhoneNumber : throw new ArgumentNullException(nameof(configurationRequest.AcsPhoneNumber));
//        configuration.CallbackUriHost = !string.IsNullOrEmpty(configurationRequest.CallbackUriHost) ? configurationRequest.CallbackUriHost : throw new ArgumentNullException(nameof(configurationRequest.CallbackUriHost));
//    }

//    acsConnectionString = configuration.AcsConnectionString;
//    cognitiveServicesEndpoint = configuration.CongnitiveServiceEndpoint;
//    acsPhoneNumber = configuration.AcsPhoneNumber;
//    callbackUriHost = configuration.CallbackUriHost;
//    fileSourceUri = "https://sample-videos.com/audio/mp3/crowd-cheering.mp3";

//    client = new CallAutomationClient(connectionString: acsConnectionString);
//    logger.LogInformation("Initialized call automation client.");
//    return Results.Ok("Configurtion set successfully. Initialized call automation client.");
//}).WithTags("Add Connection string and configuration settings.");

app.MapPost("/api/events", async (EventGridEvent[] eventGridEvents, ILogger<Program> logger) =>
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
                Console.WriteLine("Caller Id--> " + callerId);
                var callbackUri = new Uri(new Uri(callbackUriHost), $"/api/callbacks");
                var websocketUri = callbackUriHost.Replace("https", "wss") + "/ws";
                logger.LogInformation($"Incoming call - correlationId: {incomingCallEventData.CorrelationId}, " +
                    $"Callback url: {callbackUri}");

                eventCallbackUri = callbackUri;

                //MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(
                //MediaStreamingAudioChannel.Mixed);
                //mediaStreamingOptions.TransportUri = new Uri(websocketUri);
                //mediaStreamingOptions.EnableBidirectional = true;
                //mediaStreamingOptions.AudioFormat = AudioFormat.Pcm16KMono;
                //mediaStreamingOptions.EnableDtmfTones = true;
                //mediaStreamingOptions.StartMediaStreaming = true;

                //TranscriptionOptions transcriptionOptions = new TranscriptionOptions();
                //transcriptionOptions.TransportUri = new Uri(websocketUri);
                //transcriptionOptions.EnableIntermediateResults = false;
                //transcriptionOptions.StartTranscription = true;

                TranscriptionOptions transcriptionOptions = new TranscriptionOptions("en-US")
                {
                    TransportUri=new Uri(websocketUri),
                    StartTranscription = true,
                    PiiRedactionOptions = new PiiRedactionOptions()
                    {
                       Enable = true,
                       RedactionType = RedactionType.MaskWithCharacter
                    },
                    EnableSentimentAnalysis = true,
                    SummarizationOptions = new SummarizationOptions()
                    {
                        EnableEndCallSummary = false,
                        Locale = "en-US",
                    }

                };

                var options = new AnswerCallOptions(incomingCallEventData.IncomingCallContext, callbackUri)
                {
                    CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
                    //MediaStreamingOptions = mediaStreamingOptions,
                    TranscriptionOptions = transcriptionOptions,
                };

                if (isRedirect)
                {
                    CallInvite? callInvite = null;

                    if (redirectUser.StartsWith('8'))
                    {
                        var target = new CommunicationUserIdentifier(redirectUser);
                        callInvite = new CallInvite(target);
                    }
                    else if (redirectUser.StartsWith('+'))
                    {
                        var target = new PhoneNumberIdentifier(redirectUser);
                        var sourceCallerId = new PhoneNumberIdentifier(acsPhoneNumber);
                        callInvite = new CallInvite(target, sourceCallerId);
                    }

                    var redirectCallResult = await client.RedirectCallAsync(incomingCallEventData.IncomingCallContext, callInvite);
                    logger.LogInformation($"Call Redirect Configured.");
                }
                else
                {
                    AnswerCallResult answerCallResult = await client.AnswerCallAsync(options);
                    logger.LogInformation($"Call Answer Configured.");
                }

                //var callConnectionMedia = answerCallResult.CallConnection.GetCallMedia();

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

app.MapPost("/api/callbacks", (CloudEvent[] cloudEvents, ILogger<Program> logger) =>
{
    foreach (var cloudEvent in cloudEvents)
    {
        CallAutomationEventBase parsedEvent = CallAutomationEventParser.Parse(cloudEvent);
        logger.LogInformation(
                    "Received call event: {type}, callConnectionID: {connId}, serverCallId: {serverId}",
                    parsedEvent.GetType(),
                    parsedEvent.CallConnectionId,
                    parsedEvent.ServerCallId);

        var callConnection = client.GetCallConnection(parsedEvent.CallConnectionId);

        if (parsedEvent is CallConnected callConnected)
        {

            CallConnectionProperties properties = callConnection.GetCallConnectionProperties();

            logger.LogInformation($"Received call event: {callConnected.GetType()}");
            callConnectionId = callConnected.CallConnectionId;
            CallConnectionProperties callConnectionProperties = GetCallConnectionProperties();
            logger.LogInformation("************************************************************");
            logger.LogInformation($"CORRELATION ID: {callConnectionProperties.CorrelationId}");
            logger.LogInformation("MEDIA STREMAING SUBSCRIPTION STATE:-->" + properties.MediaStreamingSubscription.State);
            logger.LogInformation("TRANSCRIPTION SUBSCRIPTION STATE:-->" + properties.TranscriptionSubscription.State);
            logger.LogInformation("************************************************************");
        }
        else if (parsedEvent is ConnectFailed connectFailed)
        {
            callConnectionId = connectFailed.CallConnectionId;
            logger.LogInformation($"Received call event: {connectFailed.GetType()}, CorrelationId: {connectFailed.CorrelationId}, " +
                      $"subCode: {connectFailed.ResultInformation?.SubCode}, message: {connectFailed.ResultInformation?.Message}, context: {connectFailed.OperationContext}");
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
                    var sentimentAnalysis = choiceResult.SentimentAnalysisResult;
                    logger.LogInformation("Sentiment" + sentimentAnalysis.Sentiment);
                    logger.LogInformation("Language Identified" + choiceResult.LanguageIdentified);
                    logger.LogInformation("Recognize completed succesfully, labelDetected={labelDetected}, phraseDetected={phraseDetected}", labelDetected, phraseDetected);
                    break;
                case SpeechResult speechResult:
                    var text = speechResult.Speech;
                    
                    var speechSemanticAnalysis = speechResult.SentimentAnalysisResult;
                    logger.LogInformation("Sentiment" + speechSemanticAnalysis.Sentiment);
                    logger.LogInformation("Language Identified" + speechResult.LanguageIdentified);
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
        else if (parsedEvent is CancelAddParticipantSucceeded cancelAddParticipantSucceeded)
        {
            logger.LogInformation($"Received call event: {cancelAddParticipantSucceeded.GetType()}");
            callConnectionId = cancelAddParticipantSucceeded.CallConnectionId;
        }
        else if (parsedEvent is CancelAddParticipantFailed cancelAddParticipantFailed)
        {
            callConnectionId = cancelAddParticipantFailed.CallConnectionId;
            logger.LogInformation($"Received call event: {cancelAddParticipantFailed.GetType()}, CorrelationId: {cancelAddParticipantFailed.CorrelationId}, " +
                      $"subCode: {cancelAddParticipantFailed.ResultInformation?.SubCode}, message: {cancelAddParticipantFailed.ResultInformation?.Message}, context: {cancelAddParticipantFailed.OperationContext}");
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
        /*
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
        */
        else if (parsedEvent is HoldFailed holdFailed)
        {
            callConnectionId = holdFailed.CallConnectionId;
            logger.LogInformation($"Received call event: {holdFailed.GetType()}, CorrelationId: {holdFailed.CorrelationId}, " +
                      $"subCode: {holdFailed.ResultInformation?.SubCode}, message: {holdFailed.ResultInformation?.Message}, context: {holdFailed.OperationContext}");
        }
        else if (parsedEvent is TranscriptionStarted transcriptionStarted)
        {
            logger.LogInformation($"Received call event: {transcriptionStarted.GetType()}");
            logger.LogInformation($"Operation context: {transcriptionStarted.OperationContext}");
            callConnectionId = transcriptionStarted.CallConnectionId;
        }
        else if (parsedEvent is TranscriptionStopped transcriptionStopped)
        {
            logger.LogInformation($"Received call event: {transcriptionStopped.GetType()}");
            logger.LogInformation($"Operation context: {transcriptionStopped.OperationContext}");
            callConnectionId = transcriptionStopped.CallConnectionId;
        }
        else if (parsedEvent is TranscriptionUpdated transcriptionUpdated)
        {
            logger.LogInformation($"Received call event: {transcriptionUpdated.GetType()}");
            logger.LogInformation($"Operation context: {transcriptionUpdated.OperationContext}");
            callConnectionId = transcriptionUpdated.CallConnectionId;
        }
        else if (parsedEvent is TranscriptionCallSummaryUpdated transcriptionCallSummaryUpdate)
        {
            logger.LogInformation($"Received call event: {transcriptionCallSummaryUpdate.GetType()}");
            callConnectionId = transcriptionCallSummaryUpdate.CallConnectionId;
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
            logger.LogInformation($"Operation context: {mediaStreamingStarted.OperationContext}");
            callConnectionId = mediaStreamingStarted.CallConnectionId;
        }
        else if (parsedEvent is MediaStreamingStopped mediaStreamingStopped)
        {
            logger.LogInformation($"Received call event: {mediaStreamingStopped.GetType()}");
            logger.LogInformation($"Operation context: {mediaStreamingStopped.OperationContext}");
            callConnectionId = mediaStreamingStopped.CallConnectionId;
        }
        else if (parsedEvent is MediaStreamingFailed mediaStreamingFailed)
        {
            callConnectionId = mediaStreamingFailed.CallConnectionId;
            logger.LogInformation($"Received call event: {mediaStreamingFailed.GetType()}, CorrelationId: {mediaStreamingFailed.CorrelationId}, " +
                      $"subCode: {mediaStreamingFailed.ResultInformation?.SubCode}, message: {mediaStreamingFailed.ResultInformation?.Message}, context: {mediaStreamingFailed.OperationContext}");
        }
        else if (parsedEvent is CallDisconnected callDisconnected)
        {
            logger.LogInformation($"Received call event: {callDisconnected.GetType()}");
            logger.LogInformation($"CORRELATION ID: {callDisconnected.CorrelationId}");
        }
        else if (parsedEvent is CreateCallFailed createCallFailed)
        {
            callConnectionId = createCallFailed.CallConnectionId;
            logger.LogInformation($"Received call event: {createCallFailed.GetType()}, CorrelationId: {createCallFailed.CorrelationId}, " +
                      $"subCode: {createCallFailed.ResultInformation?.SubCode}, message: {createCallFailed.ResultInformation?.Message}, context: {createCallFailed.OperationContext}");
        }
        else if (parsedEvent is CallTransferAccepted callTransferAccepted)
        {
            logger.LogInformation($"Received call event: {callTransferAccepted.GetType()}");
            callConnectionId = callTransferAccepted.CallConnectionId;
        }
        else if (parsedEvent is CallTransferFailed callTransferFailed)
        {
            callConnectionId = callTransferFailed.CallConnectionId;
            logger.LogInformation($"Received call event: {callTransferFailed.GetType()}, CorrelationId: {callTransferFailed.CorrelationId}, " +
                      $"subCode: {callTransferFailed.ResultInformation?.SubCode}, message: {callTransferFailed.ResultInformation?.Message}, context: {callTransferFailed.OperationContext}");
        }
        else if (parsedEvent is RecordingStateChanged recordingStateChanged)
        {
            logger.LogInformation($"Received call event: {recordingStateChanged.GetType()}");
            logger.LogInformation($"Recording State: {recordingStateChanged.State}");
        }
    }
    return Results.Ok();
}).Produces(StatusCodes.Status200OK);


app.MapPost("/setRedirectConfig",
    (bool isRedirectCall, string redirectTarget, ILogger<Program> logger) => {
        isRedirect = isRedirectCall;
        redirectUser = redirectTarget;
    });


#region Outbound Call

app.MapPost("/outboundCallToPstnAsync", async (string targetPhoneNumber, ILogger<Program> logger) =>
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

#region Group Call

app.MapPost("/createGroupCallAsync", async (string targetPhoneNumber, string acsTarget, bool isMixed, bool isStartMediaStreaming, bool isStartTranscription, ILogger <Program> logger) =>
{
    PhoneNumberIdentifier target = new PhoneNumberIdentifier(targetPhoneNumber);
    PhoneNumberIdentifier sourceCallerId = new PhoneNumberIdentifier(acsPhoneNumber);

    CommunicationUserIdentifier acsUser = new CommunicationUserIdentifier(acsTarget);

    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    eventCallbackUri = callbackUri;
    var websocketUri = callbackUriHost.Replace("https", "wss") + "/ws";
    MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(
       isMixed ? MediaStreamingAudioChannel.Mixed : MediaStreamingAudioChannel.Unmixed);
    mediaStreamingOptions.TransportUri = new Uri(websocketUri);
    //mediaStreamingOptions.EnableBidirectional = isEnableBidirectional;
    //mediaStreamingOptions.AudioFormat = isPcm24kMono ? AudioFormat.Pcm24KMono : AudioFormat.Pcm16KMono;
    mediaStreamingOptions.EnableDtmfTones = true;
    mediaStreamingOptions.StartMediaStreaming = isStartMediaStreaming;

    TranscriptionOptions transcriptionOptions = new TranscriptionOptions();
    transcriptionOptions.TransportUri = new Uri(websocketUri);
    transcriptionOptions.EnableIntermediateResults = false;
    transcriptionOptions.StartTranscription = isStartTranscription;

    IEnumerable<CommunicationIdentifier> targets = new List<CommunicationIdentifier>()
    {
        target,
        acsUser
    };

    var createGroupCallOptions = new CreateGroupCallOptions(targets, callbackUri)
    {
        CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
        SourceCallerIdNumber = sourceCallerId,
        //MediaStreamingOptions = mediaStreamingOptions,
        TranscriptionOptions = transcriptionOptions
    };

    CreateCallResult createCallResult = await client.CreateGroupCallAsync(createGroupCallOptions);

    logger.LogInformation($"Created async group call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
}).WithTags("Create Group Call APIs");

app.MapPost("/createGroupCall", (string targetPhoneNumber, string acsTarget, bool isMixed, bool isStartMediaStreaming, bool isStartTranscription, ILogger<Program> logger) =>
{
    PhoneNumberIdentifier target = new PhoneNumberIdentifier(targetPhoneNumber);
    PhoneNumberIdentifier sourceCallerId = new PhoneNumberIdentifier(acsPhoneNumber);

    CommunicationUserIdentifier acsUser = new CommunicationUserIdentifier(acsTarget);

    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    eventCallbackUri = callbackUri;
    var websocketUri = callbackUriHost.Replace("https", "wss") + "/ws";

    MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(
       isMixed ? MediaStreamingAudioChannel.Mixed : MediaStreamingAudioChannel.Unmixed);
    mediaStreamingOptions.TransportUri = new Uri(websocketUri);
    //mediaStreamingOptions.EnableBidirectional = isEnableBidirectional;
    //mediaStreamingOptions.AudioFormat = isPcm24kMono ? AudioFormat.Pcm24KMono : AudioFormat.Pcm16KMono;
    mediaStreamingOptions.EnableDtmfTones = true;
    mediaStreamingOptions.StartMediaStreaming = isStartMediaStreaming;

    TranscriptionOptions transcriptionOptions = new TranscriptionOptions();
    transcriptionOptions.TransportUri = new Uri(websocketUri);
    transcriptionOptions.EnableIntermediateResults = false;
    transcriptionOptions.StartTranscription = isStartTranscription;


    IEnumerable<CommunicationIdentifier> targets = new List<CommunicationIdentifier>()
    {
        target,
        acsUser
    };

    var createGroupCallOptions = new CreateGroupCallOptions(targets, callbackUri)
    {
        CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
        SourceCallerIdNumber = sourceCallerId,
        //MediaStreamingOptions = mediaStreamingOptions,
        TranscriptionOptions = transcriptionOptions
    };

    CreateCallResult createCallResult = client.CreateGroupCall(createGroupCallOptions);

    logger.LogInformation($"Created group call with id: {createCallResult.CallConnectionProperties.CallConnectionId}");
}).WithTags("Create Group Call APIs");

#endregion

#region Connect Api

app.MapPost("/ConnectRoomCallAsync", async (string roomId, ILogger<Program> logger) =>
{
    RoomCallLocator roomCallLocator = !string.IsNullOrEmpty(roomId) ? new RoomCallLocator(roomId) : throw new ArgumentNullException(nameof(roomId));
    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    var websocketUri = callbackUriHost.Replace("https", "wss") + "/ws";
    //MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(new Uri(websocketUri),
    //    MediaStreamingAudioChannel.Unmixed);
    TranscriptionOptions transcriptionOptions = new TranscriptionOptions();
    ConnectCallOptions connectCallOptions = new ConnectCallOptions(roomCallLocator, callbackUri)
    {
        CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
        OperationContext = "ConnectRoomCallContext",
        //MediaStreamingOptions = mediaStreamingOptions,
        TranscriptionOptions = transcriptionOptions
    };
    ConnectCallResult connectCallResult = await client.ConnectCallAsync(connectCallOptions);
    logger.LogInformation($"Connected room async call with connection id: {connectCallResult.CallConnectionProperties.CallConnectionId}");
    return Results.Ok();
}).WithTags("Connect Call APIs");

app.MapPost("/ConnectRoomCall", (string roomId, ILogger<Program> logger) =>
{
    RoomCallLocator roomCallLocator = !string.IsNullOrEmpty(roomId) ? new RoomCallLocator(roomId) : throw new ArgumentNullException(nameof(roomId));
    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    var websocketUri = callbackUriHost.Replace("https", "wss") + "/ws";
    //MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(new Uri(websocketUri), MediaStreamingContent.Audio,
    //    MediaStreamingAudioChannel.Unmixed, MediaStreamingTransport.Websocket, false);
    //TranscriptionOptions transcriptionOptions = new TranscriptionOptions(new Uri(websocketUri),
    //    "en-us", false, TranscriptionTransport.Websocket);
    //MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(new Uri(websocketUri), MediaStreamingContent.Audio,
    //    MediaStreamingAudioChannel.Unmixed, StreamingTransport.Websocket, false);

    //MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(new Uri(websocketUri),
    //    MediaStreamingAudioChannel.Unmixed);
    TranscriptionOptions transcriptionOptions = new TranscriptionOptions();
    ConnectCallOptions connectCallOptions = new ConnectCallOptions(roomCallLocator, callbackUri)
    {
        CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
        OperationContext = "ConnectRoomCallContext",
        //MediaStreamingOptions = mediaStreamingOptions,
        TranscriptionOptions = transcriptionOptions
    };
    ConnectCallResult connectCallResult = client.ConnectCall(connectCallOptions);
    logger.LogInformation($"Connected room call with connection id: {connectCallResult.CallConnectionProperties.CallConnectionId}");
    return Results.Ok();
}).WithTags("Connect Call APIs");

app.MapPost("/ConnectGroupCallAsync", async (string groupId, ILogger<Program> logger) =>
{
    GroupCallLocator groupCallLocator = !string.IsNullOrEmpty(groupId) ? new GroupCallLocator(groupId) : throw new ArgumentNullException(nameof(groupId));
    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    var websocketUri = callbackUriHost.Replace("https", "wss") + "/ws";
    //MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(new Uri(websocketUri), MediaStreamingContent.Audio,
    //    MediaStreamingAudioChannel.Unmixed, MediaStreamingTransport.Websocket, false);
    //TranscriptionOptions transcriptionOptions = new TranscriptionOptions(new Uri(websocketUri),
    //    "en-us", false, TranscriptionTransport.Websocket); 
    //MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(new Uri(websocketUri), MediaStreamingContent.Audio,
    //    MediaStreamingAudioChannel.Unmixed, StreamingTransport.Websocket, false);
    //TranscriptionOptions transcriptionOptions = new TranscriptionOptions(new Uri(websocketUri),
    //    "en-us", false, StreamingTransport.Websocket);
    ConnectCallOptions connectCallOptions = new ConnectCallOptions(groupCallLocator, callbackUri)
    {
        CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
        OperationContext = "ConnectRoomCallContext",
        //MediaStreamingOptions = mediaStreamingOptions,
        //TranscriptionOptions = transcriptionOptions
    };
    ConnectCallResult connectCallResult = await client.ConnectCallAsync(connectCallOptions);
    logger.LogInformation($"Connected group async call with connection id: {connectCallResult.CallConnectionProperties.CallConnectionId}");
    return Results.Ok();
}).WithTags("Connect Call APIs");

app.MapPost("/ConnectGroupCall", (string groupId, ILogger<Program> logger) =>
{
    GroupCallLocator groupCallLocator = !string.IsNullOrEmpty(groupId) ? new GroupCallLocator(groupId) : throw new ArgumentNullException(nameof(groupId));
    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    var websocketUri = callbackUriHost.Replace("https", "wss") + "/ws";
    //MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(new Uri(websocketUri), MediaStreamingContent.Audio,
    //    MediaStreamingAudioChannel.Unmixed, MediaStreamingTransport.Websocket, false);
    //TranscriptionOptions transcriptionOptions = new TranscriptionOptions(new Uri(websocketUri),
    //    "en-us", false, TranscriptionTransport.Websocket);
    //MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(new Uri(websocketUri), MediaStreamingContent.Audio,
    //    MediaStreamingAudioChannel.Unmixed, StreamingTransport.Websocket, false);
    //TranscriptionOptions transcriptionOptions = new TranscriptionOptions(new Uri(websocketUri),
    //    "en-us", false, StreamingTransport.Websocket);
    ConnectCallOptions connectCallOptions = new ConnectCallOptions(groupCallLocator, callbackUri)
    {
        CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
        OperationContext = "ConnectGroupCallContext",
        //MediaStreamingOptions = mediaStreamingOptions,
        //TranscriptionOptions = transcriptionOptions
    };
    ConnectCallResult connectCallResult = client.ConnectCall(connectCallOptions);
    logger.LogInformation($"Connected group call with connection id: {connectCallResult.CallConnectionProperties.CallConnectionId}");
    return Results.Ok();
}).WithTags("Connect Call APIs");

app.MapPost("/ConnectOneToNCallAsync", async (string serverCallId, ILogger<Program> logger) =>
{
    ServerCallLocator serverCallLocator = !string.IsNullOrEmpty(serverCallId) ? new ServerCallLocator(serverCallId) : throw new ArgumentNullException(nameof(serverCallId));
    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    var websocketUri = callbackUriHost.Replace("https", "wss") + "/ws";
    //MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(new Uri(websocketUri), MediaStreamingContent.Audio,
    //    MediaStreamingAudioChannel.Unmixed, MediaStreamingTransport.Websocket, false);
    //TranscriptionOptions transcriptionOptions = new TranscriptionOptions(new Uri(websocketUri),
    //    "en-us", false, TranscriptionTransport.Websocket);
    //MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(new Uri(websocketUri), MediaStreamingContent.Audio,
    //    MediaStreamingAudioChannel.Unmixed, StreamingTransport.Websocket, false);
    //TranscriptionOptions transcriptionOptions = new TranscriptionOptions(new Uri(websocketUri),
    //    "en-us", false, StreamingTransport.Websocket);
    ConnectCallOptions connectCallOptions = new ConnectCallOptions(serverCallLocator, callbackUri)
    {
        CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
        OperationContext = "ConnectOneToNCallContext",
        //MediaStreamingOptions = mediaStreamingOptions,
        //TranscriptionOptions = transcriptionOptions
    };
    ConnectCallResult connectCallResult = await client.ConnectCallAsync(connectCallOptions);
    logger.LogInformation($"Connected 1 to N async call with connection id: {connectCallResult.CallConnectionProperties.CallConnectionId}");
    return Results.Ok();
}).WithTags("Connect Call APIs");

app.MapPost("/ConnectOneToNCall", (string serverCallId, ILogger<Program> logger) =>
{
    ServerCallLocator serverCallLocator = !string.IsNullOrEmpty(serverCallId) ? new ServerCallLocator(serverCallId) : throw new ArgumentNullException(nameof(serverCallId));
    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    var websocketUri = callbackUriHost.Replace("https", "wss") + "/ws";
    //MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(new Uri(websocketUri), MediaStreamingContent.Audio,
    //    MediaStreamingAudioChannel.Unmixed, MediaStreamingTransport.Websocket, false);
    //TranscriptionOptions transcriptionOptions = new TranscriptionOptions(new Uri(websocketUri),
    //    "en-us", false, TranscriptionTransport.Websocket);
    //MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(new Uri(websocketUri), MediaStreamingContent.Audio,
    //    MediaStreamingAudioChannel.Unmixed, StreamingTransport.Websocket, false);
    //TranscriptionOptions transcriptionOptions = new TranscriptionOptions(new Uri(websocketUri),
    //    "en-us", false, StreamingTransport.Websocket);
    ConnectCallOptions connectCallOptions = new ConnectCallOptions(serverCallLocator, callbackUri)
    {
        CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
        OperationContext = "ConnectOneToNCallContext",
        //MediaStreamingOptions = mediaStreamingOptions,
        //TranscriptionOptions = transcriptionOptions
    };
    ConnectCallResult connectCallResult = client.ConnectCall(connectCallOptions);
    logger.LogInformation($"Connected 1 to N call with connection id: {connectCallResult.CallConnectionProperties.CallConnectionId}");
    return Results.Ok();
}).WithTags("Connect Call APIs");

#endregion

#region Add Remove Participant

app.MapPost("/addPstnParticipantAsync", async (string pstnParticipant, ILogger<Program> logger) =>
{
    CallConnection callConnection = GetConnection();
    CallInvite callInvite = new CallInvite(new PhoneNumberIdentifier(pstnParticipant),
           new PhoneNumberIdentifier(acsPhoneNumber));
    var addParticipantOptions = new AddParticipantOptions(callInvite)
    {
        OperationContext = "addPstnUserContext",
        InvitationTimeoutInSeconds = 30,
    };

    var result = await callConnection.AddParticipantAsync(addParticipantOptions);
    return Results.Ok(result);
}).WithTags("Add/Remove Participant APIs");

app.MapPost("/addPstnParticipant", (string pstnParticipant, ILogger<Program> logger) =>
{
    CallConnection callConnection = GetConnection();
    CallInvite callInvite = new CallInvite(new PhoneNumberIdentifier(pstnParticipant),
           new PhoneNumberIdentifier(acsPhoneNumber));
    var addParticipantOptions = new AddParticipantOptions(callInvite)
    {
        OperationContext = "addPstnUserContext",
        InvitationTimeoutInSeconds = 30,
    };

    var result = callConnection.AddParticipant(addParticipantOptions);
    return Results.Ok(result);
}).WithTags("Add/Remove Participant APIs");

app.MapPost("/addAcsParticipantAsync", async (string acsParticipant, ILogger<Program> logger) =>
{
    CallConnection callConnection = GetConnection();
    CallInvite callInvite = new CallInvite(new CommunicationUserIdentifier(acsParticipant));
    var addParticipantOptions = new AddParticipantOptions(callInvite)
    {
        OperationContext = "addAcsUserContext",
        InvitationTimeoutInSeconds = 30,
    };

    var result = await callConnection.AddParticipantAsync(addParticipantOptions);
    return Results.Ok(result);
}).WithTags("Add/Remove Participant APIs");

app.MapPost("/addAcsParticipant", (string acsParticipant, ILogger<Program> logger) =>
{
    CallConnection callConnection = GetConnection();
    CallInvite callInvite = new CallInvite(new CommunicationUserIdentifier(acsParticipant));
    var addParticipantOptions = new AddParticipantOptions(callInvite)
    {
        OperationContext = "addAcsUserContext",
        InvitationTimeoutInSeconds = 30,
    };

    var result = callConnection.AddParticipant(addParticipantOptions);
    return Results.Ok(result);
}).WithTags("Add/Remove Participant APIs");

app.MapPost("/addTeamsParticipantAsync", async (string teamsObjectId, ILogger<Program> logger) =>
{
    CallConnection callConnection = GetConnection();
    CallInvite callInvite = new CallInvite(new MicrosoftTeamsUserIdentifier(teamsObjectId));
    var addParticipantOptions = new AddParticipantOptions(callInvite)
    {
        OperationContext = "addTeamsUserContext",
        InvitationTimeoutInSeconds = 30,
    };

    var result = await callConnection.AddParticipantAsync(addParticipantOptions);
    return Results.Ok(result);
}).WithTags("Add/Remove Participant APIs");

app.MapPost("/addTeamsParticipant", (string teamsObjectId, ILogger<Program> logger) =>
{
    CallConnection callConnection = GetConnection();
    CallInvite callInvite = new CallInvite(new MicrosoftTeamsUserIdentifier(teamsObjectId));
    var addParticipantOptions = new AddParticipantOptions(callInvite)
    {
        OperationContext = "addTeamsUserContext",
        InvitationTimeoutInSeconds = 30,
    };

    var result = callConnection.AddParticipant(addParticipantOptions);
    return Results.Ok(result);
}).WithTags("Add/Remove Participant APIs");

app.MapPost("/removePstnParticipantAsync", async (string pstnTarget, ILogger<Program> logger) =>
{
    CallConnection callConnection = GetConnection();
    RemoveParticipantOptions removeParticipantOptions = new RemoveParticipantOptions(new PhoneNumberIdentifier(pstnTarget))
    {
        OperationContext = "removePstnParticipantContext"
    };

    await callConnection.RemoveParticipantAsync(removeParticipantOptions);
    return Results.Ok();
}).WithTags("Add/Remove Participant APIs");

app.MapPost("/removePstnParticipant", (string pstnTarget, ILogger<Program> logger) =>
{
    CallConnection callConnection = GetConnection();
    RemoveParticipantOptions removeParticipantOptions = new RemoveParticipantOptions(new PhoneNumberIdentifier(pstnTarget))
    {
        OperationContext = "removePstnParticipantContext"
    };

    callConnection.RemoveParticipant(removeParticipantOptions);
    return Results.Ok();
}).WithTags("Add/Remove Participant APIs");

app.MapPost("/removeAcsParticipantAsync", async (string acsTarget, ILogger<Program> logger) =>
{
    CallConnection callConnection = GetConnection();
    RemoveParticipantOptions removeParticipantOptions = new RemoveParticipantOptions(new CommunicationUserIdentifier(acsTarget))
    {
        OperationContext = "removeAcsParticipantContext"
    };

    await callConnection.RemoveParticipantAsync(removeParticipantOptions);
    return Results.Ok();
}).WithTags("Add/Remove Participant APIs");

app.MapPost("/removeAcsParticipant", (string acsTarget, ILogger<Program> logger) =>
{
    CallConnection callConnection = GetConnection();
    RemoveParticipantOptions removeParticipantOptions = new RemoveParticipantOptions(new CommunicationUserIdentifier(acsTarget))
    {
        OperationContext = "removeAcsParticipantContext"
    };

    callConnection.RemoveParticipant(removeParticipantOptions);
    return Results.Ok();
}).WithTags("Add/Remove Participant APIs");

app.MapPost("/removeTeamsParticipantAsync", async (string teamsObjectId, ILogger<Program> logger) =>
{
    CallConnection callConnection = GetConnection();
    RemoveParticipantOptions removeParticipantOptions = new RemoveParticipantOptions(new MicrosoftTeamsUserIdentifier(teamsObjectId))
    {
        OperationContext = "removeTeamsParticipantContext"
    };

    await callConnection.RemoveParticipantAsync(removeParticipantOptions);
    return Results.Ok();
}).WithTags("Add/Remove Participant APIs");

app.MapPost("/removeTeamsParticipant", (string teamsObjectId, ILogger<Program> logger) =>
{
    CallConnection callConnection = GetConnection();
    RemoveParticipantOptions removeParticipantOptions = new RemoveParticipantOptions(new MicrosoftTeamsUserIdentifier(teamsObjectId))
    {
        OperationContext = "removeTeamsParticipantContext"
    };

    callConnection.RemoveParticipantAsync(removeParticipantOptions);
    return Results.Ok();
}).WithTags("Add/Remove Participant APIs");

app.MapPost("/cancelAddParticipantAsync", async (string invitationId, ILogger<Program> logger) =>
{
    CallConnection callConnection = GetConnection();

    CancelAddParticipantOperationOptions cancelAddParticipantOperationOptions = new CancelAddParticipantOperationOptions(invitationId)
    {
        OperationContext = "CancelAddingParticipantContext"
    };
    var result = await callConnection.CancelAddParticipantOperationAsync(cancelAddParticipantOperationOptions);

    return Results.Ok(result);
}).WithTags("Add/Remove Participant APIs");

app.MapPost("/cancelAddParticipant", (string invitationId, ILogger<Program> logger) =>
{
    CallConnection callConnection = GetConnection();
    CancelAddParticipantOperationOptions cancelAddParticipantOperationOptions = new CancelAddParticipantOperationOptions(invitationId)
    {
        OperationContext = "CancelAddingParticipantContext"
    };
    var result = callConnection.CancelAddParticipantOperationAsync(cancelAddParticipantOperationOptions);
    return Results.Ok(result);
}).WithTags("Add/Remove Participant APIs");

#endregion

# region Play Media with Single/Multiple Sources

app.MapPost("/playMediaToPstnTargetAsync", async (string pstnTarget, bool isMultipleSource, ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();

    TextSource textSource = new("Hi, this is test source played through play source thanks. Goodbye!.")
    {
        VoiceName = "en-US-NancyNeural"
    };

    PlayOptions playToOptions;

    List<CommunicationIdentifier> playTo = new() { new PhoneNumberIdentifier(pstnTarget) };

    if (isMultipleSource)
    {
        SsmlSource ssmlSource = new SsmlSource(
            "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"><voice name=\"en-US-JennyNeural\">Hi, this is ssml test played through ssml source thanks. Goodbye!</voice></speak>");

        FileSource fileSource = new FileSource(new Uri(fileSourceUri));

        playToOptions = new(playSources: [textSource, ssmlSource, fileSource], playTo)
        {
            OperationContext = "playToContext"
        };
    }
    else
    {
        playToOptions = new(playSource: textSource, playTo: playTo)
        {
            OperationContext = "playToContext"
        };
    }
    await callMedia.PlayAsync(playToOptions);

    return Results.Ok();
}).WithTags("Play Media Source APIs");

app.MapPost("/playMediaToPstnTarget", (string pstnTarget, bool isMultipleSource, ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();

    TextSource textSource = new("Hi, this is test source played through play source thanks. Goodbye!.")
    {
        VoiceName = "en-US-NancyNeural"
    };

    PlayOptions playToOptions;

    List<CommunicationIdentifier> playTo = new() { new PhoneNumberIdentifier(pstnTarget) };

    if (isMultipleSource)
    {
        SsmlSource ssmlSource = new SsmlSource(
            "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"><voice name=\"en-US-JennyNeural\">Hi, this is ssml test played through ssml source thanks. Goodbye!</voice></speak>");

        FileSource fileSource = new FileSource(new Uri(fileSourceUri));

        playToOptions = new(playSources: [textSource, ssmlSource, fileSource], playTo)
        {
            OperationContext = "playToContext"
        };
    }
    else
    {
        playToOptions = new(playSource: textSource, playTo: playTo)
        {
            OperationContext = "playToContext"
        };
    }
    callMedia.Play(playToOptions);

    return Results.Ok();
}).WithTags("Play Media APIs");

app.MapPost("/playMediaToAcsTargetAsync", async (string acsTarget, bool isMultipleSource, ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();

    TextSource textSource = new("Hi, this is test source played through play source thanks. Goodbye!.")
    {
        VoiceName = "en-US-NancyNeural"
    };

    PlayOptions playToOptions;

    List<CommunicationIdentifier> playTo = new List<CommunicationIdentifier> { new CommunicationUserIdentifier(acsTarget) };

    if (isMultipleSource)
    {
        SsmlSource ssmlSource = new SsmlSource(
            "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"><voice name=\"en-US-JennyNeural\">Hi, this is ssml test played through ssml source thanks. Goodbye!</voice></speak>");

        FileSource fileSource = new FileSource(new Uri(fileSourceUri));

        playToOptions = new(playSources: [textSource, ssmlSource, fileSource], playTo)
        {
            OperationContext = "playToContext"
        };
    }
    else
    {
        playToOptions = new(playSource: textSource, playTo: playTo)
        {
            OperationContext = "playToContext"
        };
    }
    await callMedia.PlayAsync(playToOptions);

    return Results.Ok();
}).WithTags("Play Media APIs");

app.MapPost("playMediaToAcsTarget", (string acsTarget, bool isMultipleSource, ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();

    TextSource textSource = new("Hi, this is test source played through play source thanks. Goodbye!.")
    {
        VoiceName = "en-US-NancyNeural"
    };

    PlayOptions playToOptions;

    List<CommunicationIdentifier> playTo = new List<CommunicationIdentifier> { new CommunicationUserIdentifier(acsTarget) };

    if (isMultipleSource)
    {
        SsmlSource ssmlSource = new SsmlSource(
            "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"><voice name=\"en-US-JennyNeural\">Hi, this is ssml test played through ssml source thanks. Goodbye!</voice></speak>");

        FileSource fileSource = new FileSource(new Uri(fileSourceUri));

        playToOptions = new(playSources: [textSource, ssmlSource, fileSource], playTo)
        {
            OperationContext = "playToContext"
        };
    }
    else
    {
        playToOptions = new(playSource: textSource, playTo: playTo)
        {
            OperationContext = "playToContext"
        };
    }
    callMedia.Play(playToOptions);

    return Results.Ok();
}).WithTags("Play Media APIs");

app.MapPost("/playMediaToTeamsTargetAsync", async (string teamsObjectId, bool isMultipleSource, ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();

    TextSource textSource = new("Hi, this is test source played through play source thanks. Goodbye!.")
    {
        VoiceName = "en-US-NancyNeural"
    };

    PlayOptions playToOptions;

    List<CommunicationIdentifier> playTo = new List<CommunicationIdentifier> { new MicrosoftTeamsUserIdentifier(teamsObjectId) };

    if (isMultipleSource)
    {
        SsmlSource ssmlSource = new SsmlSource(
            "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"><voice name=\"en-US-JennyNeural\">Hi, this is ssml test played through ssml source thanks. Goodbye!</voice></speak>");

        FileSource fileSource = new FileSource(new Uri(fileSourceUri));

        playToOptions = new(playSources: [textSource, ssmlSource, fileSource], playTo)
        {
            OperationContext = "playToContext"
        };
    }
    else
    {
        playToOptions = new(playSource: textSource, playTo: playTo)
        {
            OperationContext = "playToContext"
        };
    }
    await callMedia.PlayAsync(playToOptions);

    return Results.Ok();
}).WithTags("Play TextSource Media APIs");

app.MapPost("/playMediaToTeamsTarget", (string teamsObjectId, bool isMultipleSource, ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();

    TextSource textSource = new("Hi, this is test source played through play source thanks. Goodbye!.")
    {
        VoiceName = "en-US-NancyNeural"
    };

    PlayOptions playToOptions;

    List<CommunicationIdentifier> playTo = new List<CommunicationIdentifier> { new MicrosoftTeamsUserIdentifier(teamsObjectId) };

    if (isMultipleSource)
    {
        SsmlSource ssmlSource = new SsmlSource(
            "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"><voice name=\"en-US-JennyNeural\">Hi, this is ssml test played through ssml source thanks. Goodbye!</voice></speak>");

        FileSource fileSource = new FileSource(new Uri(fileSourceUri));

        playToOptions = new(playSources: [textSource, ssmlSource, fileSource], playTo)
        {
            OperationContext = "playToContext"
        };
    }
    else
    {
        playToOptions = new(playSource: textSource, playTo: playTo)
        {
            OperationContext = "playToContext"
        };
    }
    callMedia.Play(playToOptions);

    return Results.Ok();
}).WithTags("Play Media APIs");

app.MapPost("/playMediaToAllAsync", async (bool isMultipleSource, ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();

    TextSource textSource = new("Hi, this is test source played through play source thanks. Goodbye!.")
    {
        VoiceName = "en-US-NancyNeural"
    };

    PlayToAllOptions playToAllOptions;

    if (isMultipleSource)
    {
        SsmlSource ssmlSource = new SsmlSource(
            "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"><voice name=\"en-US-JennyNeural\">Hi, this is ssml test played through ssml source thanks. Goodbye!</voice></speak>");

        FileSource fileSource = new FileSource(new Uri(fileSourceUri));

        playToAllOptions = new(playSources: [textSource, ssmlSource, fileSource])
        {
            OperationContext = "playToAllContext"
        };
    }
    else
    {
        playToAllOptions = new(playSource: textSource)
        {
            OperationContext = "playToAllContext"
        };
    }

    await callMedia.PlayToAllAsync(playToAllOptions);

    return Results.Ok();
}).WithTags("Play Media APIs");

app.MapPost("/playMediaToAll", (bool isMultipleSource, ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();

    TextSource textSource = new("Hi, this is test source played through play source thanks. Goodbye!.")
    {
        VoiceName = "en-US-NancyNeural"
    };

    PlayToAllOptions playToAllOptions;

    if (isMultipleSource)
    {
        SsmlSource ssmlSource = new SsmlSource(
            "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"><voice name=\"en-US-JennyNeural\">Hi, this is ssml test played through ssml source thanks. Goodbye!</voice></speak>");

        FileSource fileSource = new FileSource(new Uri(fileSourceUri));

        playToAllOptions = new(playSources: [textSource, ssmlSource, fileSource])
        {
            OperationContext = "playToAllContext"
        };
    }
    else
    {
        playToAllOptions = new(playSource: textSource)
        {
            OperationContext = "playToAllContext"
        };
    }

    callMedia.PlayToAll(playToAllOptions);

    return Results.Ok();
}).WithTags("Play Media APIs");

app.MapPost("/playMediaBargeInAsync", async (bool isMultipleSource, ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();

    TextSource textSource = new TextSource("Hi, this is barge in test played through play source thanks. Goodbye!.")
    {
        VoiceName = "en-US-NancyNeural"
    };


    PlayToAllOptions playToAllOptions;

    if (isMultipleSource)
    {
        SsmlSource ssmlSource = new SsmlSource(
            "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"><voice name=\"en-US-JennyNeural\">Hi, this is ssml test played through ssml source thanks. Goodbye!</voice></speak>");

        FileSource fileSource = new FileSource(new Uri(fileSourceUri));

        playToAllOptions = new(playSources: [textSource, ssmlSource, fileSource])
        {
            OperationContext = "playToAllContext",
            InterruptCallMediaOperation = true
        };
    }
    else
    {
        playToAllOptions = new(playSource: textSource)
        {
            OperationContext = "playToAllContext",
            InterruptCallMediaOperation = true
        };
    }
    await callMedia.PlayToAllAsync(playToAllOptions);

    return Results.Ok();
}).WithTags("Play Media APIs");

#endregion 

#region Recognization

// pstn

app.MapPost("/recognizeDTMFAsync", async (string pstnTarget, ILogger<Program> logger) =>
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
        EndSilenceTimeout = TimeSpan.FromSeconds(15),
        EnableSentimentAnalysis = true,
        SpeechLanguages = new List<string> { "en-US","en-AU" }
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
                   EndSilenceTimeout = TimeSpan.FromSeconds(5),
                   EnableSentimentAnalysis = true,
                   SpeechLanguages = new List<string> { "en-US", "en-AU" }
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
            OperationContext = "ChoiceContext",
            EnableSentimentAnalysis = true,
            SpeechLanguages = new List<string> { "en-US" }
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

// acs
app.MapPost("/recognizeDTMFAtACSAsync", async (string acsTarget, ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    TextSource textSource = new TextSource("Hi, this is recognize test. please provide input thanks!.")
    {
        VoiceName = "en-US-NancyNeural"
    };

    var target = new CommunicationUserIdentifier(acsTarget);

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

app.MapPost("/recognizeDTMFAtACS", (string acsTarget, ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    TextSource textSource = new TextSource("Hi, this is recognize test. please provide input thanks!.")
    {
        VoiceName = "en-US-NancyNeural"
    };

    var target = new CommunicationUserIdentifier(acsTarget);

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

app.MapPost("/recognizeSpeechAtACSAsync", async (string acsTarget, ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    TextSource textSource = new TextSource("Hi, this is recognize test. please provide input thanks!.")
    {
        VoiceName = "en-US-NancyNeural"
    };

    var target = new CommunicationUserIdentifier(acsTarget);


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

app.MapPost("/recognizeSpeechAtACS", (string acsTarget, ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    TextSource textSource = new TextSource("Hi, this is recognize test. please provide input thanks!.")
    {
        VoiceName = "en-US-NancyNeural"
    };

    var target = new CommunicationUserIdentifier(acsTarget);
    //var target = new PhoneNumberIdentifier(acsTarget);

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

app.MapPost("/recognizeSpeechOrDtmfAtACSAsync", async (string acsTarget, ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    TextSource textSource = new TextSource("Hi, this is recognize test. please provide input thanks!.")
    {
        VoiceName = "en-US-NancyNeural"
    };

    var target = new CommunicationUserIdentifier(acsTarget);
    //var target = new PhoneNumberIdentifier(acsTarget);

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

app.MapPost("/recognizeSpeechOrDtmfAtACS", (string acsTarget, ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    TextSource textSource = new TextSource("Hi, this is recognize test. please provide input thanks!.")
    {
        VoiceName = "en-US-NancyNeural"
    };

    var target = new CommunicationUserIdentifier(acsTarget);
    //var target = new PhoneNumberIdentifier(acsTarget);

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

app.MapPost("/recognizeChoiceAtACSAsync", async (string acsTarget, ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    TextSource textSource = new TextSource("Hi, this is recognize test. please provide input thanks!.")
    {
        VoiceName = "en-US-NancyNeural"
    };

    var target = new CommunicationUserIdentifier(acsTarget);
    //var target = new PhoneNumberIdentifier(acsTarget);


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

app.MapPost("/recognizeChoiceAtACS", (string acsTarget, ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    TextSource textSource = new TextSource("Hi, this is recognize test. please provide input thanks!.")
    {
        VoiceName = "en-US-NancyNeural"
    };

    var target = new CommunicationUserIdentifier(acsTarget);
    //var target = new PhoneNumberIdentifier(acsTarget);


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

app.MapPost("/sendDTMFTonesAsync", async (string pstnTarget, ILogger<Program> logger) =>
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

app.MapPost("/startContinuousDTMFTonesAsync", async (string pstnTarget, ILogger<Program> logger) =>
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

app.MapPost("/stopContinuousDTMFTonesAsync", async (string pstnTarget, ILogger<Program> logger) =>
{
    CommunicationIdentifier target = new PhoneNumberIdentifier(pstnTarget);

    CallMedia callMedia = GetCallMedia();

    await callMedia.StopContinuousDtmfRecognitionAsync(target);
    return Results.Ok();
}).WithTags("Send or Start DTMF APIs");

app.MapPost("/stopContinuousDTMFTones", (string pstnTarget, ILogger<Program> logger) =>
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
/*
app.MapPost("/interrupAudioAndAnnounceAsync", async (string pstnTarget, ILogger<Program> logger) =>
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

*/
app.MapPost("/unholdParticipantAsync", async (string pstnTarget, ILogger<Program> logger) =>
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
    PlayOptions playToOptions = new PlayOptions(textSource, playTo);

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

# region Mute Particiapant

app.MapPost("/muteAcsParticipantAsync", async (string acsTarget, ILogger<Program> logger) =>
{
    CommunicationIdentifier target = new CommunicationUserIdentifier(acsTarget);

    CallConnection callConnection = GetConnection();

    await callConnection.MuteParticipantAsync(target);

    return Results.Ok();
}).WithTags("Mute Participant APIs");

app.MapPost("/muteAcsParticipant", (string acsTarget, ILogger<Program> logger) =>
{
    CommunicationIdentifier target = new CommunicationUserIdentifier(acsTarget);

    CallConnection callConnection = GetConnection();

    callConnection.MuteParticipant(target);

    return Results.Ok();
}).WithTags("Mute Participant APIs");

#endregion

# region Media streaming

app.MapPost("/createCallToPstnWithMediaStreamingAsync", async (string targetPhoneNumber, bool isEnableBidirectional, bool isPcm24kMono, bool isMixed, bool isStartMediaStreaming, ILogger<Program> logger) =>
{
    PhoneNumberIdentifier target = new PhoneNumberIdentifier(targetPhoneNumber);
    PhoneNumberIdentifier caller = new PhoneNumberIdentifier(acsPhoneNumber);


    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    eventCallbackUri = callbackUri;
    CallInvite callInvite = new CallInvite(target, caller);
    var websocketUri = callbackUriHost.Replace("https", "wss") + "/ws";
    MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(
        isMixed ? MediaStreamingAudioChannel.Mixed : MediaStreamingAudioChannel.Unmixed);
    mediaStreamingOptions.TransportUri = new Uri(websocketUri);
    mediaStreamingOptions.EnableBidirectional = isEnableBidirectional;
    mediaStreamingOptions.AudioFormat = isPcm24kMono ? AudioFormat.Pcm24KMono : AudioFormat.Pcm16KMono;
    mediaStreamingOptions.EnableDtmfTones = true;
    mediaStreamingOptions.StartMediaStreaming = isStartMediaStreaming;
    
    var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
    {
        CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
        MediaStreamingOptions = mediaStreamingOptions
    };

    CreateCallResult createCallResult = await client.CreateCallAsync(createCallOptions);

    logger.LogInformation($"Created async pstn media streaming call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
}).WithTags("Media streaming APIs");

app.MapPost("/createCallToPstnWithMediaStreaming", (string targetPhoneNumber, bool isEnableBidirectional, bool isPcm24kMono, bool isMixed, bool isStartMediaStreaming, ILogger<Program> logger) =>
{
    PhoneNumberIdentifier target = new PhoneNumberIdentifier(targetPhoneNumber);
    PhoneNumberIdentifier caller = new PhoneNumberIdentifier(acsPhoneNumber);


    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    eventCallbackUri = callbackUri;
    CallInvite callInvite = new CallInvite(target, caller);
    var websocketUri = callbackUriHost.Replace("https", "wss") + "/ws";


    MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(
        isMixed ? MediaStreamingAudioChannel.Mixed : MediaStreamingAudioChannel.Unmixed);
    mediaStreamingOptions.TransportUri = new Uri(websocketUri);
    mediaStreamingOptions.EnableBidirectional = isEnableBidirectional;
    mediaStreamingOptions.AudioFormat = isPcm24kMono ? AudioFormat.Pcm24KMono : AudioFormat.Pcm16KMono;
    mediaStreamingOptions.EnableDtmfTones = true;
    mediaStreamingOptions.StartMediaStreaming = isStartMediaStreaming;


    var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
    {
        CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
        MediaStreamingOptions = mediaStreamingOptions
    };

    CreateCallResult createCallResult = client.CreateCall(createCallOptions);

    logger.LogInformation($"Created async pstn media streaming call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
}).WithTags("Media streaming APIs");

app.MapPost("/createCallToAcsWithMediaStreamingAsync", async (string acsTarget, bool isEnableBidirectional, bool isPcm24kMono, bool isMixed, bool isStartMediaStreaming, ILogger<Program> logger) =>
{
    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    eventCallbackUri = callbackUri;
    CallInvite callInvite = new CallInvite(new CommunicationUserIdentifier(acsTarget));
    var websocketUri = callbackUriHost.Replace("https", "wss") + "/ws";

    MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(
       isMixed ? MediaStreamingAudioChannel.Mixed : MediaStreamingAudioChannel.Unmixed);
    mediaStreamingOptions.TransportUri = new Uri(websocketUri);
    mediaStreamingOptions.EnableBidirectional = isEnableBidirectional;
    mediaStreamingOptions.AudioFormat = isPcm24kMono ? AudioFormat.Pcm24KMono : AudioFormat.Pcm16KMono;
    mediaStreamingOptions.EnableDtmfTones = true;
    mediaStreamingOptions.StartMediaStreaming = isStartMediaStreaming;


    var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
    {
        CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
        MediaStreamingOptions = mediaStreamingOptions
    };

    CreateCallResult createCallResult = await client.CreateCallAsync(createCallOptions);

    logger.LogInformation($"Created async acs call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
}).WithTags("Media streaming APIs");

app.MapPost("/createCallToAcsWithMediaStreaming", (string acsTarget, bool isEnableBidirectional, bool isPcm24kMono, bool isMixed, bool isStartMediaStreaming, ILogger<Program> logger) =>
{
    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    eventCallbackUri = callbackUri;
    CallInvite callInvite = new CallInvite(new CommunicationUserIdentifier(acsTarget));
    var websocketUri = callbackUriHost.Replace("https", "wss") + "/ws";

    MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(
       isMixed ? MediaStreamingAudioChannel.Mixed : MediaStreamingAudioChannel.Unmixed);
    mediaStreamingOptions.TransportUri = new Uri(websocketUri);
    mediaStreamingOptions.EnableBidirectional = isEnableBidirectional;
    mediaStreamingOptions.AudioFormat = isPcm24kMono ? AudioFormat.Pcm24KMono : AudioFormat.Pcm16KMono;
    mediaStreamingOptions.EnableDtmfTones = true;
    mediaStreamingOptions.StartMediaStreaming = isStartMediaStreaming;


    var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
    {
        CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
        MediaStreamingOptions = mediaStreamingOptions
    };

    CreateCallResult createCallResult = client.CreateCall(createCallOptions);

    logger.LogInformation($"Created acs call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
}).WithTags("Media streaming APIs");

app.MapPost("/createCallToTeamsWithMediaStreamingAsync", async (string teamsObjectId, bool isEnableBidirectional, bool isPcm24kMono, bool isMixed, bool isStartMediaStreaming, ILogger<Program> logger) =>
{
    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    eventCallbackUri = callbackUri;
    CallInvite callInvite = new CallInvite(new MicrosoftTeamsUserIdentifier(teamsObjectId));
    var websocketUri = callbackUriHost.Replace("https", "wss") + "/ws";

    MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(
       isMixed ? MediaStreamingAudioChannel.Mixed : MediaStreamingAudioChannel.Unmixed);
    mediaStreamingOptions.TransportUri = new Uri(websocketUri);
    mediaStreamingOptions.EnableBidirectional = isEnableBidirectional;
    mediaStreamingOptions.AudioFormat = isPcm24kMono ? AudioFormat.Pcm24KMono : AudioFormat.Pcm16KMono;
    mediaStreamingOptions.EnableDtmfTones = true;
    mediaStreamingOptions.StartMediaStreaming = isStartMediaStreaming;


    var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
    {
        CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
        MediaStreamingOptions = mediaStreamingOptions
    };

    CreateCallResult createCallResult = await client.CreateCallAsync(createCallOptions);

    logger.LogInformation($"Created async teams call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
}).WithTags("Media streaming APIs");

app.MapPost("/createCallToTeamsWithMediaStreaming", (string teamsObjectId, bool isEnableBidirectional, bool isPcm24kMono, bool isMixed, bool isStartMediaStreaming, ILogger<Program> logger) =>
{
    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    eventCallbackUri = callbackUri;
    CallInvite callInvite = new CallInvite(new MicrosoftTeamsUserIdentifier(teamsObjectId));
    var websocketUri = callbackUriHost.Replace("https", "wss") + "/ws";

    MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(
       isMixed ? MediaStreamingAudioChannel.Mixed : MediaStreamingAudioChannel.Unmixed);
    mediaStreamingOptions.TransportUri = new Uri(websocketUri);
    mediaStreamingOptions.EnableBidirectional = isEnableBidirectional;
    mediaStreamingOptions.AudioFormat = isPcm24kMono ? AudioFormat.Pcm24KMono : AudioFormat.Pcm16KMono;
    mediaStreamingOptions.EnableDtmfTones = true;
    mediaStreamingOptions.StartMediaStreaming = isStartMediaStreaming;


    var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
    {
        CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
        MediaStreamingOptions = mediaStreamingOptions
    };

    CreateCallResult createCallResult = client.CreateCall(createCallOptions);

    logger.LogInformation($"Created teams call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
}).WithTags("Media streaming APIs");


app.MapPost("/startMediaStreamingAsync", async (ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    await callMedia.StartMediaStreamingAsync();
    return Results.Ok();
}).WithTags("Media streaming APIs");

app.MapPost("/startMediaStreaming", (ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    callMedia.StartMediaStreaming();
    return Results.Ok();
}).WithTags("Media streaming APIs");

app.MapPost("/stopMediaStreamingAsync", async (ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    await callMedia.StopMediaStreamingAsync();
    return Results.Ok();
}).WithTags("Media streaming APIs");

app.MapPost("/stopMediaStreaming", (ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    callMedia.StopMediaStreaming();
    return Results.Ok();
}).WithTags("Media streaming APIs");

app.MapPost("/startMediaStreamingWithOptionsAsync", async (ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    StartMediaStreamingOptions startMediaStreamingOptions = new()
    {
        OperationContext = "StartMediaStreamingContext",
        OperationCallbackUri = eventCallbackUri
    };
    await callMedia.StartMediaStreamingAsync(startMediaStreamingOptions);
    return Results.Ok();
}).WithTags("Media streaming APIs");

app.MapPost("/startMediaStreamingWithOptions", (ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    StartMediaStreamingOptions startMediaStreamingOptions = new()
    {
        OperationContext = "StartMediaStreamingContext",
        OperationCallbackUri = eventCallbackUri
    };
    callMedia.StartMediaStreaming(startMediaStreamingOptions);
    return Results.Ok();
}).WithTags("Media streaming APIs");

app.MapPost("/stopMediaStreamingWithOptionsAsync", async (ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    StopMediaStreamingOptions stopMediaStreamingOptions = new()
    {
        OperationContext = "StopMediaStreamingContext",
        OperationCallbackUri = eventCallbackUri
    };
    await callMedia.StopMediaStreamingAsync(stopMediaStreamingOptions);
    return Results.Ok();
}).WithTags("Media streaming APIs");

app.MapPost("/stopMediaStreamingWithOptions", (ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    StopMediaStreamingOptions stopMediaStreamingOptions = new()
    {
        OperationContext = "StopMediaStreamingContext",
        OperationCallbackUri = eventCallbackUri
    };
    callMedia.StopMediaStreaming(stopMediaStreamingOptions);
    return Results.Ok();
}).WithTags("Media streaming APIs");

#endregion

#region Transcription

app.MapPost("/createCallToPstnWithTranscriptionAsync", async (string targetPhoneNumber, bool isStartTranscription, ILogger<Program> logger) =>
{
    PhoneNumberIdentifier target = new PhoneNumberIdentifier(targetPhoneNumber);
    PhoneNumberIdentifier caller = new PhoneNumberIdentifier(acsPhoneNumber);


    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    eventCallbackUri = callbackUri;
    CallInvite callInvite = new CallInvite(target, caller);
    var websocketUri = callbackUriHost.Replace("https", "wss") + "/ws";

    TranscriptionOptions transcriptionOptions = new TranscriptionOptions("en-US");
    transcriptionOptions.TransportUri = new Uri(websocketUri);
    transcriptionOptions.EnableIntermediateResults = false;
    transcriptionOptions.StartTranscription = isStartTranscription;
    transcriptionOptions.EnableSentimentAnalysis = true;
    transcriptionOptions.PiiRedactionOptions = new PiiRedactionOptions()
    {
        Enable = true,
        RedactionType = RedactionType.MaskWithCharacter
    };
    transcriptionOptions.Locales = new List<string> { "en-US", "hi-IN" };
    transcriptionOptions.SummarizationOptions = new SummarizationOptions()
    {
        Locale = "en-US",
        //EnableEndCallSummary = true
    };

    var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
    {
        CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
        TranscriptionOptions = transcriptionOptions,
        EnableLoopbackAudio = false
    };
    

    CreateCallResult createCallResult = await client.CreateCallAsync(createCallOptions);

    logger.LogInformation($"Created async pstn transcription call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
}).WithTags("Transcription APIs");

app.MapPost("/createCallToPstnWithTranscription", (string targetPhoneNumber, bool isStartTranscription, ILogger<Program> logger) =>
{
    PhoneNumberIdentifier target = new PhoneNumberIdentifier(targetPhoneNumber);
    PhoneNumberIdentifier caller = new PhoneNumberIdentifier(acsPhoneNumber);

    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    eventCallbackUri = callbackUri;
    CallInvite callInvite = new CallInvite(target, caller);

    var websocketUri = callbackUriHost.Replace("https", "wss") + "/ws";

    TranscriptionOptions transcriptionOptions = new TranscriptionOptions();
    transcriptionOptions.TransportUri = new Uri(websocketUri);
    transcriptionOptions.EnableIntermediateResults = false;
    transcriptionOptions.StartTranscription = isStartTranscription;

    var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
    {
        CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
        TranscriptionOptions = transcriptionOptions
    };

    CreateCallResult createCallResult = client.CreateCall(createCallOptions);

    logger.LogInformation($"Created pstn transcription call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
}).WithTags("Transcription APIs");

// start transcription
app.MapPost("/createCallToAcsWithTranscriptionAsync", async (string acsTarget, bool isStartTranscription, ILogger<Program> logger) =>
{
    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    eventCallbackUri = callbackUri;
    CallInvite callInvite = new CallInvite(new CommunicationUserIdentifier(acsTarget));
    var websocketUri = callbackUriHost.Replace("https", "wss") + "/ws";
    
    TranscriptionOptions transcriptionOptions = new TranscriptionOptions();
    transcriptionOptions.TransportUri = new Uri(websocketUri);
    transcriptionOptions.EnableIntermediateResults = false;
    transcriptionOptions.StartTranscription = isStartTranscription;

    var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
    {
        CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
        TranscriptionOptions = transcriptionOptions
    };

    CreateCallResult createCallResult = await client.CreateCallAsync(createCallOptions);

    logger.LogInformation($"Created async acs transcription call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
}).WithTags("Transcription APIs");

app.MapPost("/createCallToAcsWithTranscription", (string acsTarget, bool isStartTranscription, ILogger<Program> logger) =>
{
    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    eventCallbackUri = callbackUri;
    CallInvite callInvite = new CallInvite(new CommunicationUserIdentifier(acsTarget));
    var websocketUri = callbackUriHost.Replace("https", "wss") + "/ws";

    TranscriptionOptions transcriptionOptions = new TranscriptionOptions();
    transcriptionOptions.TransportUri = new Uri(websocketUri);
    transcriptionOptions.EnableIntermediateResults = false;
    transcriptionOptions.StartTranscription = isStartTranscription;

    var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
    {
        CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
        TranscriptionOptions = transcriptionOptions
    };

    CreateCallResult createCallResult = client.CreateCall(createCallOptions);

    logger.LogInformation($"Created acs transcription call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
}).WithTags("Transcription APIs");

app.MapPost("/createCallToTeamsWithTranscriptionAsync", async (string teamsObjectId, bool isStartTranscription, ILogger<Program> logger) =>
{
    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    eventCallbackUri = callbackUri;
    CallInvite callInvite = new CallInvite(new MicrosoftTeamsUserIdentifier(teamsObjectId));
    var websocketUri = callbackUriHost.Replace("https", "wss") + "/ws";

    TranscriptionOptions transcriptionOptions = new TranscriptionOptions();
    transcriptionOptions.TransportUri = new Uri(websocketUri);
    transcriptionOptions.EnableIntermediateResults = false;
    transcriptionOptions.StartTranscription = isStartTranscription;

    var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
    {
        CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
        TranscriptionOptions = transcriptionOptions
    };

    CreateCallResult createCallResult = await client.CreateCallAsync(createCallOptions);

    logger.LogInformation($"Created async teams transcription call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
}).WithTags("Transcription APIs");

app.MapPost("/createCallToTeamsWithTranscription", (string teamsObjectId, bool isStartTranscription, ILogger<Program> logger) =>
{
    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    eventCallbackUri = callbackUri;
    CallInvite callInvite = new CallInvite(new MicrosoftTeamsUserIdentifier(teamsObjectId));
    var websocketUri = callbackUriHost.Replace("https", "wss") + "/ws";

    TranscriptionOptions transcriptionOptions = new TranscriptionOptions();
    transcriptionOptions.TransportUri = new Uri(websocketUri);
    transcriptionOptions.EnableIntermediateResults = false;
    transcriptionOptions.StartTranscription = isStartTranscription;

    var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
    {
        CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
        TranscriptionOptions = transcriptionOptions
    };

    CreateCallResult createCallResult = client.CreateCall(createCallOptions);

    logger.LogInformation($"Created teams transcription call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
}).WithTags("Transcription APIs");

app.MapPost("/startTranscriptionAsync", async (ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    await callMedia.StartTranscriptionAsync();
    return Results.Ok();
}).WithTags("Transcription APIs");

app.MapPost("/startTranscription", (ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    callMedia.StartTranscription();
    return Results.Ok();
}).WithTags("Transcription APIs");

app.MapPost("/updateTranscriptionWithOptionsAsync", async (string locale, ILogger<Program> logger) =>
{
    UpdateTranscriptionOptions updateTranscriptionOptions = new UpdateTranscriptionOptions()
    {
        OperationContext = "UpdateTranscriptionContext",
        Locale = "en-CA",
        PiiRedactionOptions = new PiiRedactionOptions()
        {
            Enable = true,
            RedactionType = RedactionType.MaskWithCharacter
        },
        EnableSentimentAnalysis = true,
        SummarizationOptions = new SummarizationOptions()
        {
            Locale = "en-US",
            EnableEndCallSummary = false
        }
    };
    CallMedia callMedia = GetCallMedia();
    await callMedia.UpdateTranscriptionAsync(updateTranscriptionOptions);
    return Results.Ok();
}).WithTags("Transcription APIs");

app.MapPost("/updateTranscriptionWithOptions", (string locale, ILogger<Program> logger) =>
{
    UpdateTranscriptionOptions updateTranscriptionOptions = new UpdateTranscriptionOptions()
    {
        OperationContext = "UpdateTranscriptionContext"
    };
    CallMedia callMedia = GetCallMedia();
    callMedia.UpdateTranscription(updateTranscriptionOptions);
    return Results.Ok();
}).WithTags("Transcription APIs");

app.MapPost("/stopTranscriptionAsync", async (ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    await callMedia.StopTranscriptionAsync();
    return Results.Ok();
}).WithTags("Transcription APIs");

app.MapPost("/stopTranscription", (ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    callMedia.StopTranscription();
    return Results.Ok();
}).WithTags("Transcription APIs");

app.MapPost("/startTranscriptionWithOptionsAsync", async (ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    StartTranscriptionOptions startTranscriptionOptions = new StartTranscriptionOptions()
    {
        OperationContext = "StartTranscriptionContext",
        //Locale = "en-us",
        //PiiRedactionOptions = new PiiRedactionOptions()
        //{
        //    Enable = true,
        //    RedactionType = RedactionType.MaskWithCharacter
        //},
        //EnableSentimentAnalysis = true,
        //Locales = new List<string> { "en-US", "en-AU" },
        //SummarizationOptions = new SummarizationOptions()
        //{
        //    Locale = "en-US",
        //    EnableEndCallSummary = false
        //},

    };
    await callMedia.StartTranscriptionAsync(startTranscriptionOptions);
    return Results.Ok();
}).WithTags("Transcription APIs");

app.MapPost("/startTranscriptionWithOptions", (ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    StartTranscriptionOptions startTranscriptionOptions = new StartTranscriptionOptions()
    {
        OperationContext = "StartTranscriptionContext",
        Locale = "en-us",
        PiiRedactionOptions = new PiiRedactionOptions()
        {
            Enable = true,
            RedactionType = RedactionType.MaskWithCharacter
        },
        EnableSentimentAnalysis = true,
        //Locales = new List<string> { "en-US", "en-AU" },
        SummarizationOptions = new SummarizationOptions()
        {
            Locale = "en-US",
            EnableEndCallSummary = false
        },
        
    };
    callMedia.StartTranscription(startTranscriptionOptions);
    return Results.Ok();
}).WithTags("Transcription APIs");

app.MapPost("/stopTranscriptionWithOptionsAsync", async (ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    StopTranscriptionOptions stopTranscriptionOptions = new StopTranscriptionOptions()
    {
        OperationContext = "StopTranscriptionContext",
    };
    
    await callMedia.StopTranscriptionAsync(stopTranscriptionOptions);
    return Results.Ok();
}).WithTags("Transcription APIs");

app.MapPost("/stopTranscriptionWithOptions", (ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    StopTranscriptionOptions stopTranscriptionOptions = new StopTranscriptionOptions()
    {
        OperationContext = "StopTranscriptionContext",
    };
    callMedia.StopTranscription(stopTranscriptionOptions);
    return Results.Ok();
}).WithTags("Transcription APIs");

#endregion

#region Transfer Call

app.MapPost("/transferCallToPstnParticipantAsync", async (string pstnTransferTarget, string pstnTarget, ILogger<Program> logger) =>
{
    CallConnection callConnection = GetConnection();

    TransferToParticipantOptions transferToParticipantOptions = new TransferToParticipantOptions(new PhoneNumberIdentifier(pstnTransferTarget))
    {
        OperationContext = "TransferCallContext",
        Transferee = new PhoneNumberIdentifier(pstnTarget),
    };

    await callConnection.TransferCallToParticipantAsync(transferToParticipantOptions);
    return Results.Ok();
}).WithTags("Transfer Call APIs");

app.MapPost("/transferCallToPstnParticipant", (string pstnTransferTarget, string pstnTarget, ILogger<Program> logger) =>
{
    CallConnection callConnection = GetConnection();

    TransferToParticipantOptions transferToParticipantOptions = new TransferToParticipantOptions(new PhoneNumberIdentifier(pstnTransferTarget))
    {
        OperationContext = "TransferCallContext",
        Transferee = new PhoneNumberIdentifier(pstnTarget),
    };

    callConnection.TransferCallToParticipant(transferToParticipantOptions);
    return Results.Ok();
}).WithTags("Transfer Call APIs");

#endregion

#region Get Participant

app.MapPost("/getPstnParticipantAsync", async (string pstnTarget, ILogger<Program> logger) =>
{
    CallConnection callConnection = GetConnection();

    CallParticipant participant = await callConnection.GetParticipantAsync(new PhoneNumberIdentifier(pstnTarget));

    if (participant != null)
    {
        Console.WriteLine("Participant:-->" + participant.Identifier.RawId.ToString());
        Console.WriteLine("Is Participant on hold:-->" + participant.IsOnHold);
        Console.WriteLine("Is Participant on muted:-->" + participant.IsMuted);
    }
    return Results.Ok();
}).WithTags("Get Participant APIs");

app.MapPost("/getPstnParticipant", (string pstnTarget, ILogger<Program> logger) =>
{
    CallConnection callConnection = GetConnection();

    CallParticipant participant = callConnection.GetParticipant(new PhoneNumberIdentifier(pstnTarget));

    if (participant != null)
    {
        Console.WriteLine("Participant:-->" + participant.Identifier.RawId.ToString());
        Console.WriteLine("Is Participant on hold:-->" + participant.IsOnHold);
        Console.WriteLine("Is Participant on muted:-->" + participant.IsMuted);
    }
    return Results.Ok();
}).WithTags("Get Participant APIs");

app.MapPost("/getAcsParticipantAsync", async (string acsTarget, ILogger<Program> logger) =>
{
    CallConnection callConnection = GetConnection();

    CallParticipant participant = await callConnection.GetParticipantAsync(new CommunicationUserIdentifier(acsTarget));

    if (participant != null)
    {
        Console.WriteLine("Participant:-->" + participant.Identifier.RawId.ToString());
        Console.WriteLine("Is Participant on hold:-->" + participant.IsOnHold);
    }
    return Results.Ok();
}).WithTags("Get Participant APIs");

app.MapPost("/getAcsParticipant", (string acsTarget, ILogger<Program> logger) =>
{
    CallConnection callConnection = GetConnection();

    CallParticipant participant = callConnection.GetParticipant(new CommunicationUserIdentifier(acsTarget));

    if (participant != null)
    {
        Console.WriteLine("Participant:-->" + participant.Identifier.RawId.ToString());
        Console.WriteLine("Is Participant on hold:-->" + participant.IsOnHold);
    }
    return Results.Ok();
}).WithTags("Get Participant APIs");

app.MapPost("/getTeamsParticipantAsync", async (string teamsObjectId, ILogger<Program> logger) =>
{
    CallConnection callConnection = GetConnection();

    CallParticipant participant = await callConnection.GetParticipantAsync(new MicrosoftTeamsUserIdentifier(teamsObjectId));

    if (participant != null)
    {
        Console.WriteLine("Participant:-->" + participant.Identifier.RawId.ToString());
        Console.WriteLine("Is Participant on hold:-->" + participant.IsOnHold);
    }
    return Results.Ok();
}).WithTags("Get Participant APIs");

app.MapPost("/getTeamsParticipant", (string teamsObjectId, ILogger<Program> logger) =>
{
    CallConnection callConnection = GetConnection();

    CallParticipant participant = callConnection.GetParticipant(new MicrosoftTeamsUserIdentifier(teamsObjectId));

    if (participant != null)
    {
        Console.WriteLine("Participant:-->" + participant.Identifier.RawId.ToString());
        Console.WriteLine("Is Participant on hold:-->" + participant.IsOnHold);
    }
    return Results.Ok();
}).WithTags("Get Participant APIs");

app.MapPost("/getParticipantListAsync", async (ILogger<Program> logger) =>
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
    return Results.Ok();
}).WithTags("Get Participant APIs");

app.MapPost("/getParticipantList", (ILogger<Program> logger) =>
{
    CallConnection callConnection = GetConnection();

    var list = callConnection.GetParticipants();

    foreach (var participant in list.Value)
    {
        Console.WriteLine("----------------------------------------------------------------------");
        Console.WriteLine("Participant:-->" + participant.Identifier.RawId.ToString());
        Console.WriteLine("Is Participant on hold:-->" + participant.IsOnHold);
        Console.WriteLine("----------------------------------------------------------------------");
    }
    return Results.Ok();
}).WithTags("Get Participant APIs");

#endregion

#region Recording

app.MapPost("/startRecordingWithVideoMp4MixedAsync", async (bool isRecordingWithCallConnectionId, bool isPauseOnStart, ILogger<Program> logger) =>
{
    CallConnectionProperties callConnectionProperties = GetCallConnectionProperties();
    var serverCallId = callConnectionProperties.ServerCallId;
    //CallLocator callLocator = new ServerCallLocator(serverCallId);
    CallLocator callLocator = new RoomCallLocator("99495491359806362");
    var recordingOptions = new StartRecordingOptions(callLocator);
    recordingOptions.RecordingContent = RecordingContent.AudioVideo;
    recordingOptions.RecordingFormat = RecordingFormat.Mp4;
    recordingOptions.RecordingChannel = RecordingChannel.Mixed;
    recordingOptions.RecordingStateCallbackUri = eventCallbackUri;
    recordingOptions.PauseOnStart = isPauseOnStart ? true : false;
    recordingFileFormat = "mp4";
    var recordingResult = await client.GetCallRecording().StartAsync(recordingOptions);
    recordingId = recordingResult.Value.RecordingId;
    logger.LogInformation($"Recording started. RecordingId: {recordingId}");
    return Results.Ok();
}).WithTags("Recording APIs");

app.MapPost("/startRecordingWithVideoMp4Mixed", (bool isRecordingWithCallConnectionId, bool isPauseOnStart, ILogger<Program> logger) =>
{
    CallConnectionProperties callConnectionProperties = GetCallConnectionProperties();
    var serverCallId = callConnectionProperties.ServerCallId;
    //CallLocator callLocator = new ServerCallLocator(serverCallId);
    CallLocator callLocator = new RoomCallLocator("99497984749597482");
    var recordingOptions = new StartRecordingOptions(callLocator);
    recordingOptions.RecordingContent = RecordingContent.AudioVideo;
    recordingOptions.RecordingFormat = RecordingFormat.Mp4;
    recordingOptions.RecordingChannel = RecordingChannel.Mixed;
    recordingOptions.RecordingStateCallbackUri = eventCallbackUri;
    recordingOptions.PauseOnStart = isPauseOnStart ? true : false;
    recordingFileFormat = "mp4";

    var recordingResult = client.GetCallRecording().Start(recordingOptions);
    recordingId = recordingResult.Value.RecordingId;
    logger.LogInformation($"Recording started. RecordingId: {recordingId}");
    return Results.Ok();
}).WithTags("Recording APIs");

app.MapPost("/startRecordingWithAudioMp3MixedAsync", async (bool isRecordingWithCallConnectionId, bool isPauseOnStart, ILogger<Program> logger) =>
{
    CallConnectionProperties callConnectionProperties = GetCallConnectionProperties();
    var serverCallId = callConnectionProperties.ServerCallId;
    CallLocator callLocator = new ServerCallLocator(serverCallId);
    var recordingOptions = new StartRecordingOptions(callLocator);
    recordingOptions.RecordingContent = RecordingContent.Audio;
    recordingOptions.RecordingFormat = RecordingFormat.Mp3;
    recordingOptions.RecordingChannel = RecordingChannel.Mixed;
    recordingOptions.RecordingStateCallbackUri = eventCallbackUri;
    recordingOptions.PauseOnStart = isPauseOnStart ? true : false;
    recordingFileFormat = "mp3";

    var recordingResult = await client.GetCallRecording().StartAsync(recordingOptions);
    recordingId = recordingResult.Value.RecordingId;
    logger.LogInformation($"Recording started. RecordingId: {recordingId}");
    return Results.Ok();
}).WithTags("Recording APIs");

app.MapPost("/startRecordingWithAudioMp3Mixed", (bool isRecordingWithCallConnectionId, bool isPauseOnStart, ILogger<Program> logger) =>
{
    CallConnectionProperties callConnectionProperties = GetCallConnectionProperties();
    var serverCallId = callConnectionProperties.ServerCallId;
    CallLocator callLocator = new ServerCallLocator(serverCallId);
    var recordingOptions = new StartRecordingOptions(callLocator);
    recordingOptions.RecordingContent = RecordingContent.Audio;
    recordingOptions.RecordingFormat = RecordingFormat.Mp3;
    recordingOptions.RecordingChannel = RecordingChannel.Mixed;
    recordingOptions.RecordingStateCallbackUri = eventCallbackUri;
    recordingOptions.PauseOnStart = isPauseOnStart ? true : false;
    recordingFileFormat = "mp3";

    var recordingResult = client.GetCallRecording().Start(recordingOptions);
    recordingId = recordingResult.Value.RecordingId;
    logger.LogInformation($"Recording started. RecordingId: {recordingId}");
    return Results.Ok();
}).WithTags("Recording APIs");

app.MapPost("/startRecordingWithAudioMp3UnMixedAsync", async (bool isRecordingWithCallConnectionId, bool isPauseOnStart, ILogger<Program> logger) =>
{
    CallConnectionProperties callConnectionProperties = GetCallConnectionProperties();
    var serverCallId = callConnectionProperties.ServerCallId;
    CallLocator callLocator = new ServerCallLocator(serverCallId);
    var recordingOptions = new StartRecordingOptions(callLocator);
    recordingOptions.RecordingContent = RecordingContent.Audio;
    recordingOptions.RecordingFormat = RecordingFormat.Mp3;
    recordingOptions.RecordingChannel = RecordingChannel.Unmixed;
    recordingOptions.RecordingStateCallbackUri = eventCallbackUri;
    recordingOptions.PauseOnStart = isPauseOnStart ? true : false;
    recordingFileFormat = "mp3";

    var recordingResult = await client.GetCallRecording().StartAsync(recordingOptions);
    recordingId = recordingResult.Value.RecordingId;
    logger.LogInformation($"Recording started. RecordingId: {recordingId}");
    return Results.Ok();
}).WithTags("Recording APIs");

app.MapPost("/startRecordingWithAudioMp3Unmixed", (bool isRecordingWithCallConnectionId, bool isPauseOnStart, ILogger<Program> logger) =>
{
    CallConnectionProperties callConnectionProperties = GetCallConnectionProperties();
    var serverCallId = callConnectionProperties.ServerCallId;
    CallLocator callLocator = new ServerCallLocator(serverCallId);
    var recordingOptions = new StartRecordingOptions(callLocator);
    recordingOptions.RecordingContent = RecordingContent.Audio;
    recordingOptions.RecordingFormat = RecordingFormat.Mp3;
    recordingOptions.RecordingChannel = RecordingChannel.Unmixed;
    recordingOptions.RecordingStateCallbackUri = eventCallbackUri;
    recordingOptions.PauseOnStart = isPauseOnStart ? true : false;
    recordingFileFormat = "mp3";

    var recordingResult = client.GetCallRecording().Start(recordingOptions);
    recordingId = recordingResult.Value.RecordingId;
    logger.LogInformation($"Recording started. RecordingId: {recordingId}");
    return Results.Ok();
}).WithTags("Recording APIs");

app.MapPost("/startRecordingWithAudioWavUnMixedAsync", async (bool isRecordingWithCallConnectionId, bool isPauseOnStart, ILogger<Program> logger) =>
{
    CallConnectionProperties callConnectionProperties = GetCallConnectionProperties();
    var serverCallId = callConnectionProperties.ServerCallId;
    CallLocator callLocator = new ServerCallLocator(serverCallId);
    var recordingOptions = new StartRecordingOptions(callLocator);
    recordingOptions.RecordingContent = RecordingContent.Audio;
    recordingOptions.RecordingFormat = RecordingFormat.Wav;
    recordingOptions.RecordingChannel = RecordingChannel.Unmixed;
    recordingOptions.RecordingStateCallbackUri = eventCallbackUri;
    recordingOptions.PauseOnStart = isPauseOnStart ? true : false;
    recordingFileFormat = "wav";

    var recordingResult = await client.GetCallRecording().StartAsync(recordingOptions);
    recordingId = recordingResult.Value.RecordingId;
    logger.LogInformation($"Recording started. RecordingId: {recordingId}");
    return Results.Ok();
}).WithTags("Recording APIs");

app.MapPost("/startRecordingWithAudioWavUnmixed", (bool isRecordingWithCallConnectionId, bool isPauseOnStart, ILogger<Program> logger) =>
{
    CallConnectionProperties callConnectionProperties = GetCallConnectionProperties();
    var serverCallId = callConnectionProperties.ServerCallId;
    CallLocator callLocator = new ServerCallLocator(serverCallId);
    var recordingOptions = new StartRecordingOptions(callLocator);
    recordingOptions.RecordingContent = RecordingContent.Audio;
    recordingOptions.RecordingFormat = RecordingFormat.Wav;
    recordingOptions.RecordingChannel = RecordingChannel.Unmixed;
    recordingOptions.RecordingStateCallbackUri = eventCallbackUri;
    recordingOptions.PauseOnStart = isPauseOnStart ? true : false;
    recordingFileFormat = "wav";

    var recordingResult = client.GetCallRecording().Start(recordingOptions);
    recordingId = recordingResult.Value.RecordingId;
    logger.LogInformation($"Recording started. RecordingId: {recordingId}");
    return Results.Ok();
}).WithTags("Recording APIs");



app.MapPost("/pauseRecordingAsync", async () =>
{
    await client.GetCallRecording().PauseAsync(recordingId);
    return Results.Ok();
}).WithTags("Recording APIs");

app.MapPost("/pauseRecording", () =>
{
    client.GetCallRecording().Pause(recordingId);
    return Results.Ok();
}).WithTags("Recording APIs");

app.MapPost("/resumeRecordingAsync", async () =>
{
    await client.GetCallRecording().ResumeAsync(recordingId);
    return Results.Ok();
}).WithTags("Recording APIs");

app.MapPost("/resumeRecording", () =>
{
    client.GetCallRecording().Resume(recordingId);
    return Results.Ok();
}).WithTags("Recording APIs");

app.MapPost("/stopRecordingAsync", async () =>
{
    await client.GetCallRecording().StopAsync(recordingId);
    return Results.Ok();
}).WithTags("Recording APIs");

app.MapPost("/stopRecording", () =>
{
    client.GetCallRecording().Stop(recordingId);
    return Results.Ok();
}).WithTags("Recording APIs");

app.MapGet("/downloadRecording", (ILogger<Program> logger) =>
{
    if (!string.IsNullOrEmpty(recordingLocation))
    {
        string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        var date = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"Recording_{date}.{recordingFileFormat}";
        client.GetCallRecording().DownloadTo(new Uri(recordingLocation), $"{downloadsPath}\\{fileName}");
    }
    else
    {
        logger.LogError("Recording is not available");
    }
    return Results.Ok();
}).WithTags("Recording APIs");

#endregion

#region Cancel All Media Operations

app.MapPost("/cancelAllMediaOperationAsync", async (ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    await callMedia.CancelAllMediaOperationsAsync();
    return Results.Ok();
}).WithTags("Cancel All Media Opertation APIs");

app.MapPost("/cancelAllMediaOperation", (ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    callMedia.CancelAllMediaOperations();
    return Results.Ok();
}).WithTags("Cancel All Media Opertation APIs");

#endregion

#region Summarize call
app.MapPost("/summarizeCallAsync", async (ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    SummarizeCallOptions summarizeCallOptions = new SummarizeCallOptions()
    {
        OperationContext = "SummarizeCallContext",
        SummarizationOptions = new SummarizationOptions()
        {
            Locale = "en-us",
        }
    };
    await callMedia.SummarizeCallAsync(summarizeCallOptions);
    Console.WriteLine("Summarize api executed");
    return Results.Ok();
}).WithTags("Summarize call APIs");

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

//app.UseStaticFiles(new StaticFileOptions
//{
//    FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "audio")),
//    RequestPath = "/audio"
//});


app.Run();

