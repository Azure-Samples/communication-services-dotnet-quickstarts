using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Microsoft.Extensions.FileProviders;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Your ACS resource connection string
var acsConnectionString = "";

// Your ACS resource phone number will act as source number to start outbound call
var acsPhonenumber = "";

// Target phone number you want to receive the call.
var targetPhonenumber = "";

// Base url of the app
var callbackUriHost = "";

// Your cognitive service endpoint
var cognitiveServiceEndpoint = "";

// (Optional) User Id of the target teams user you want to receive the call.
var targetTeamsUserId = "<TARGET_TEAMS_USER_ID>";

// text to play
const string SpeechToTextVoice = "en-US-NancyNeural";
const string MainMenu =
    """ 
    Hello this is Contoso Bank, we’re calling in regard to your appointment tomorrow 
    at 9am to open a new account. Please say confirm if this time is still suitable for you or say cancel 
    if you would like to cancel this appointment.
    """;
const string ConfirmedText = "Thank you for confirming your appointment tomorrow at 9am, we look forward to meeting with you.";
const string CancelText = """
Your appointment tomorrow at 9am has been cancelled. Please call the bank directly 
if you would like to rebook for another date and time.
""";
const string CustomerQueryTimeout = "I’m sorry I didn’t receive a response, please try again.";
const string NoResponse = "I didn't receive an input, we will go ahead and confirm your appointment. Goodbye";
const string InvalidAudio = "I’m sorry, I didn’t understand your response, please try again.";
const string ConfirmChoiceLabel = "Confirm";
const string RetryContext = "retry";

CallAutomationClient callAutomationClient = new CallAutomationClient(new Uri("https://jpwe-01.sdf.pma.teams.microsoft.com/"),acsConnectionString);
var app = builder.Build();

app.MapPost("/outboundCall", async (ILogger<Program> logger) =>
{
    PhoneNumberIdentifier target = new PhoneNumberIdentifier(targetPhonenumber);
    PhoneNumberIdentifier caller = new PhoneNumberIdentifier(acsPhonenumber);
    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");

    CallInvite callInvite = new CallInvite(target, caller);

    //CallInvite callInvite = new CallInvite(new CommunicationUserIdentifier("8:acs:19ae37ff-1a44-4e19-aade-198eedddbdf2_00000021-e4b4-87f5-2c8a-08482200c685"));

    MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(
        new Uri("wss://m84qr1x3-8081.inc1.devtunnels.ms/ws"),
        MediaStreamingContent.Audio,
        MediaStreamingAudioChannel.Unmixed,
        MediaStreamingTransport.Websocket,
        false
       );

    //TranscriptionOptions transcriptionOptions = new TranscriptionOptions(
    //    new Uri("wss://m84qr1x3-8081.inc1.devtunnels.ms/ws"),
    //    "en-US",
    //    false,
    //    TranscriptionTransport.Websocket
    //    );

    //MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(
    //    new Uri("wss://m84qr1x3-8081.inc1.devtunnels.ms/ws"),
    //    MediaStreamingContent.Audio,
    //    MediaStreamingAudioChannel.Unmixed,
    //    MediaStreamingTransport.Websocket

    //    );

    var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
    {
        CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServiceEndpoint) },
        MediaStreamingOptions = mediaStreamingOptions
        //TranscriptionOptions = transcriptionOptions
    };

    CreateCallResult createCallResult = await callAutomationClient.CreateCallAsync(createCallOptions);

    logger.LogInformation($"Created call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
});

app.MapPost("/createGroupCall", async (ILogger<Program> logger) =>
{
    MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(
        new Uri("wss://m84qr1x3-8081.inc1.devtunnels.ms/ws"),
        MediaStreamingContent.Audio,
        MediaStreamingAudioChannel.Unmixed,
        MediaStreamingTransport.Websocket,
        false
       );

    TranscriptionOptions transcriptionOptions = new TranscriptionOptions(
        new Uri("wss://m84qr1x3-8081.inc1.devtunnels.ms/ws"),
        "en-US",
        false,
        TranscriptionTransport.Websocket
        );

    //MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(
    //    new Uri("wss://m84qr1x3-8081.inc1.devtunnels.ms/ws"),
    //    MediaStreamingTransport.Websocket,
    //    MediaStreamingContent.Audio,
    //    MediaStreamingAudioChannel.Unmixed,
    //    true
    //    );
    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    IEnumerable<CommunicationIdentifier> targets = new List<CommunicationIdentifier>()
    {
        new PhoneNumberIdentifier(targetPhonenumber),
        //new CommunicationUserIdentifier("8:acs:19ae37ff-1a44-4e19-aade-198eedddbdf2_00000021-d590-cf12-d68a-084822003366")

    };
    var createGroupCallOptions = new CreateGroupCallOptions(targets, callbackUri)
    {
        CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri("https://abc.com") },
        SourceCallerIdNumber = new PhoneNumberIdentifier(acsPhonenumber),
        //MediaStreamingOptions = mediaStreamingOptions,
        TranscriptionOptions = transcriptionOptions
    };

    CreateCallResult createCallResult = await callAutomationClient.CreateGroupCallAsync(createGroupCallOptions);

    logger.LogInformation($"Created group call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
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

        var callConnection = callAutomationClient.GetCallConnection(parsedEvent.CallConnectionId);
        var callMedia = callConnection.GetCallMedia();
        //CallConnectionProperties properties = callConnection.GetCallConnectionProperties();
        var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
        if (parsedEvent is CallConnected callConnected)
        {
            // (Optional) Add a Microsoft Teams user to the call.  Uncomment the below snippet to enable Teams Interop scenario.
            // await callConnection.AddParticipantAsync(
            //     new CallInvite(new MicrosoftTeamsUserIdentifier(targetTeamsUserId))
            //     {
            //         SourceDisplayName = "Jack (Contoso Tech Support)"
            //     });

            CallConnectionProperties properties = callConnection.GetCallConnectionProperties();

            logger.LogInformation("CORRELATION ID:-->" + properties.CorrelationId);
            //logger.LogInformation("MEDIA STREMAING SUBSCRIPTION ID:-->" + properties.MediaSubscriptionId);
            logger.LogInformation("MEDIA STREMAING SUBSCRIPTION STATE:-->" + properties.MediaStreamingSubscription.State);
            logger.LogInformation("TRANSCRIPTION SUBSCRIPTION STATE:-->" + properties.TranscriptionSubscription.State);
            //var testCallBackUri = new Uri(new Uri("https://m84qr1x3-8081.inc1.devtunnels.ms"), "/api/callbacks");

            //StartMediaStreamingOptions options = new StartMediaStreamingOptions()
            //{
            //    //OperationCallbackUri = new Uri("https://localhost.com"),
            //    OperationCallbackUri = callbackUri,
            //    OperationContext = "startMediaStreamingContext"
            //};

            //callMedia.StartMediaStreaming(options);
            //callMedia.StartMediaStreaming();

            //await callMedia.StartMediaStreamingAsync(options);
            //await callMedia.StartMediaStreamingAsync();

            //**********************************************************

            StartTranscriptionOptions startTranscriptionOptions = new StartTranscriptionOptions()
            {
                Locale = "en-US",
                OperationContext = "StartTranscriptionContext"
            };

            await callMedia.StartTranscriptionAsync(startTranscriptionOptions);

            //callMedia.StartTranscription(startTranscriptionOptions);

            //await callMedia.StartTranscriptionAsync();

            //callMedia.StartTranscription();

            logger.LogInformation("Fetching recognize options...");

            // prepare recognize tones
            var recognizeOptions = GetMediaRecognizeChoiceOptions(MainMenu, targetPhonenumber);

            //logger.LogInformation("Recognizing options...");

            // Send request to recognize tones
            await callMedia.StartRecognizingAsync(recognizeOptions);

            //await HandlePlayAsync(callMedia, MainMenu);


            //PhoneNumberIdentifier pstnParticipant = new PhoneNumberIdentifier("+918793489556");
            //PhoneNumberIdentifier caller = new PhoneNumberIdentifier(acsPhonenumber);
            //CallInvite callInvite = new CallInvite(pstnParticipant, caller);
            //AddParticipantOptions pstnParticipantOptions = new AddParticipantOptions(callInvite)
            //{
            //    InvitationTimeoutInSeconds = 30,
            //    OperationContext = "pstnUserContext"
            //};
            //await callConnection.AddParticipantAsync(pstnParticipantOptions);


            //CallInvite callInvite = new CallInvite(new CommunicationUserIdentifier("8:acs:19ae37ff-1a44-4e19-aade-198eedddbdf2_00000021-e4b4-87f5-2c8a-08482200c685"));
            //AddParticipantOptions acsParticipantOptions = new AddParticipantOptions(callInvite)
            //{
            //    InvitationTimeoutInSeconds = 30,
            //    OperationContext = "acsUserContext"
            //};
            //await callConnection.AddParticipantAsync(acsParticipantOptions);

        }
        else if (parsedEvent is RecognizeCompleted recognizeCompleted)
        {
            //StopMediaStreamingOptions options = new StopMediaStreamingOptions()
            //{
            //    OperationCallbackUri = callbackUri,
            //    OperationContext = "StopMediaStreamingContext"
            //};

            UpdateTranscriptionOptions updateTranscriptionOptions = new UpdateTranscriptionOptions("en-AU")
            {
                OperationContext = "UpdateTranscriptionContext"
            };

            //callMedia.UpdateTranscription(updateTranscriptionOptions);
            //callMedia.UpdateTranscription("en-AU");

            await callMedia.UpdateTranscriptionAsync(updateTranscriptionOptions);
            //await callMedia.UpdateTranscriptionAsync("en-AU");

            //await Task.Delay(2000);

            //StopTranscriptionOptions stopTranscriptionOptions = new StopTranscriptionOptions()
            //{
            //    OperationContext = "StopTranscriptionOption"
            //};

            //await callMedia.StopTranscriptionAsync(stopTranscriptionOptions);

            //await Task.Delay(2000);

            //callMedia.StopMediaStreaming(options);
            //callMedia.StopMediaStreaming();
            //await callMedia.StopMediaStreamingAsync(options);
            //await callMedia.StopMediaStreamingAsync();

            //await Task.Delay(2000);

            //await callMedia.StartTranscriptionAsync();

            await Task.Delay(2000);

            //await callMedia.UpdateTranscriptionAsync("en-AU");

            var choiceResult = recognizeCompleted.RecognizeResult as ChoiceResult;
            var labelDetected = choiceResult?.Label;
            var phraseDetected = choiceResult?.RecognizedPhrase;
            // If choice is detected by phrase, choiceResult.RecognizedPhrase will have the phrase detected, 
            // If choice is detected using dtmf tone, phrase will be null 
            logger.LogInformation("Recognize completed succesfully, labelDetected={labelDetected}, phraseDetected={phraseDetected}", labelDetected, phraseDetected);
            var textToPlay = labelDetected.Equals(ConfirmChoiceLabel, StringComparison.OrdinalIgnoreCase) ? ConfirmedText : CancelText;
            //await Task.Delay(5000);
            //await callMedia.StartMediaStreamingAsync();

            await HandlePlayAsync(callMedia, textToPlay);
        }
        else if (parsedEvent is RecognizeFailed { OperationContext: RetryContext })
        {
            logger.LogError("Encountered error during recognize, operationContext={context}", RetryContext);
            await HandlePlayAsync(callMedia, NoResponse);
        }
        else if (parsedEvent is RecognizeFailed recognizeFailedEvent)
        {
            var resultInformation = recognizeFailedEvent.ResultInformation;
            logger.LogError("Encountered error during recognize, message={msg}, code={code}, subCode={subCode}",
                resultInformation?.Message,
                resultInformation?.Code,
                resultInformation?.SubCode);

            var reasonCode = recognizeFailedEvent.ReasonCode;
            string replyText = reasonCode switch
            {
                var _ when reasonCode.Equals(MediaEventReasonCode.RecognizePlayPromptFailed) ||
                reasonCode.Equals(MediaEventReasonCode.RecognizeInitialSilenceTimedOut) => CustomerQueryTimeout,
                var _ when reasonCode.Equals(MediaEventReasonCode.RecognizeIncorrectToneDetected) => InvalidAudio,
                _ => CustomerQueryTimeout,
            };

            var recognizeOptions = GetMediaRecognizeChoiceOptions(replyText, targetPhonenumber, RetryContext);
            await callMedia.StartRecognizingAsync(recognizeOptions);
        }
        else if ((parsedEvent is MediaStreamingStarted mediaStreamingStarted))
        {
            var properties = callAutomationClient.GetCallConnection(mediaStreamingStarted.CallConnectionId).GetCallConnectionProperties();

            //logger.LogInformation("************ SUBSCRIPTION ID-->" + properties.Value.MediaStreamingSubscription.Id);
            //logger.LogInformation("************ STATE-->" + properties.Value.MediaStreamingSubscription.State.Value.ToString());
            //logger.LogInformation("************ CONTENT TYPE-->" + properties.Value.MediaStreamingSubscription.SubscribedContentTypes[0].ToString());

            logger.LogInformation("************"+JsonSerializer.Serialize(mediaStreamingStarted));

            logger.LogInformation("MediaStreaming started");
            logger.LogInformation(mediaStreamingStarted.OperationContext);
            logger.LogInformation(string.Format("Media Streaming status:-{0}", mediaStreamingStarted.MediaStreamingUpdate.MediaStreamingStatus));
            logger.LogInformation(string.Format("Media Streaming status details:-{0}", mediaStreamingStarted.MediaStreamingUpdate.MediaStreamingStatusDetails));
            logger.LogInformation(string.Format("Media Streaming content type:-{0}", mediaStreamingStarted.MediaStreamingUpdate.ContentType));
        }
        else if ((parsedEvent is MediaStreamingStopped mediaStreamingStopped))
        {
            //logger.LogInformation("MediaStreaming stopped");
            //var properties = callAutomationClient.GetCallConnection(mediaStreamingStopped.CallConnectionId).GetCallConnectionProperties();
            //logger.LogInformation("************ SUBSCRIPTION ID-->" + properties.Value.MediaStreamingSubscription.Id);
            //logger.LogInformation("************ STATE-->" + properties.Value.MediaStreamingSubscription.State.Value.ToString());
            //logger.LogInformation("************ CONTENT TYPE-->" + properties.Value.MediaStreamingSubscription.SubscribedContentTypes[0].ToString());

            logger.LogInformation(mediaStreamingStopped.OperationContext);
            logger.LogInformation(string.Format("Media Streaming status:-{0}", mediaStreamingStopped.MediaStreamingUpdate.MediaStreamingStatus));
            logger.LogInformation(string.Format("Media Streaming status details:-{0}", mediaStreamingStopped.MediaStreamingUpdate.MediaStreamingStatusDetails));
            logger.LogInformation(string.Format("Media Streaming content type:-{0}", mediaStreamingStopped.MediaStreamingUpdate.ContentType));
        }
        else if ((parsedEvent is MediaStreamingFailed mediaStreamingFailed))
        {
            logger.LogInformation("MediaStreaming Failed");
            logger.LogInformation(JsonSerializer.Serialize(parsedEvent));
        }
        else if ((parsedEvent is AddParticipantSucceeded addParticipantSucceeded))
        {
            if (!string.IsNullOrEmpty(addParticipantSucceeded.OperationContext) && addParticipantSucceeded.OperationContext.Equals("pstnUserContext"))
            {
                Console.WriteLine("Pstn user Added");
                await HandlePlayAsync(callMedia, ConfirmedText);
            }
            else
            {
                Console.WriteLine("Acs user Added");
                await HandlePlayAsync(callMedia, ConfirmedText);
            }
        }
        else if (parsedEvent is TranscriptionStarted transcriptionStarted)
        {
            var properties = callAutomationClient.GetCallConnection(transcriptionStarted.CallConnectionId).GetCallConnectionProperties();
            logger.LogInformation(transcriptionStarted.OperationContext);
            logger.LogInformation("************ SUBSCRIPTION ID-->" + properties.Value.TranscriptionSubscription.Id);
            logger.LogInformation("************ STATE-->" + properties.Value.TranscriptionSubscription.State.Value.ToString());
            logger.LogInformation("************ RESULT STATEs-->" + properties.Value.TranscriptionSubscription.SubscribedResultStates[0].ToString());

            logger.LogInformation(JsonSerializer.Serialize(parsedEvent));

            logger.LogInformation(string.Format("Transcription status:-{0}", transcriptionStarted.TranscriptionUpdate.TranscriptionStatus));
            logger.LogInformation(string.Format("Transcription status details:-{0}", transcriptionStarted.TranscriptionUpdate.TranscriptionStatusDetails));
            
        }
        else if (parsedEvent is TranscriptionStopped transcriptionStopped)
        {
            var properties = callAutomationClient.GetCallConnection(transcriptionStopped.CallConnectionId).GetCallConnectionProperties();
            logger.LogInformation(transcriptionStopped.OperationContext);
            logger.LogInformation("************ SUBSCRIPTION ID-->" + properties.Value.TranscriptionSubscription.Id);
            logger.LogInformation("************ STATE-->" + properties.Value.TranscriptionSubscription.State.Value.ToString());
            logger.LogInformation("************ RESULT STATE-->" + properties.Value.TranscriptionSubscription.SubscribedResultStates[0].ToString());

            logger.LogInformation(JsonSerializer.Serialize(parsedEvent));
            logger.LogInformation(string.Format("Transcription status:-{0}", transcriptionStopped.TranscriptionUpdate.TranscriptionStatus));
            logger.LogInformation(string.Format("Transcription status details:-{0}", transcriptionStopped.TranscriptionUpdate.TranscriptionStatusDetails));
        }
        else if (parsedEvent is TranscriptionUpdated transcriptionUpdated)
        {
            var properties = callAutomationClient.GetCallConnection(transcriptionUpdated.CallConnectionId).GetCallConnectionProperties();
            logger.LogInformation(transcriptionUpdated.OperationContext);
            logger.LogInformation("************ SUBSCRIPTION ID-->" + properties.Value.TranscriptionSubscription.Id);
            logger.LogInformation("************ STATE-->" + properties.Value.TranscriptionSubscription.State.Value.ToString());
            logger.LogInformation("************ RESULT STATE-->" + properties.Value.TranscriptionSubscription.SubscribedResultStates[0].ToString());

            logger.LogInformation(JsonSerializer.Serialize(parsedEvent));
            logger.LogInformation(string.Format("Transcription status:-{0}", transcriptionUpdated.TranscriptionUpdate.TranscriptionStatus));
            logger.LogInformation(string.Format("Transcription status details:-{0}", transcriptionUpdated.TranscriptionUpdate.TranscriptionStatusDetails));
        }
        else if (parsedEvent is TranscriptionFailed transcriptionFailed)
        {
            logger.LogInformation("Transcription Failed");
            logger.LogInformation(JsonSerializer.Serialize(parsedEvent));
        }
        else if ((parsedEvent is PlayCompleted) || (parsedEvent is PlayFailed))
        {
            logger.LogInformation($"PLAY COMPLETED....");
            //await Task.Delay(5000);
            //StopMediaStreamingOptions options = new StopMediaStreamingOptions()
            //{
            //    OperationCallbackUri = callbackUri,
            //    //OperationContext = "StopMediaStreamingContext"
            //};

            //callMedia.StopMediaStreaming(options);
            //callMedia.StopMediaStreaming();
            //await callMedia.StopMediaStreamingAsync(options);
            //await callMedia.StopMediaStreamingAsync();

            StopTranscriptionOptions stopTranscriptionOptions = new StopTranscriptionOptions()
            {
                OperationContext = "StopTranscriptionOption",
            };

            //callMedia.StopTranscription(stopTranscriptionOptions);

            //callMedia.StopTranscription();

            await callMedia.StopTranscriptionAsync(stopTranscriptionOptions);

            //await callMedia.StopTranscriptionAsync();

            logger.LogInformation($"Terminating call.");
            await Task.Delay(3000);
            await callConnection.HangUpAsync(true);
        }
    }
    return Results.Ok();
}).Produces(StatusCodes.Status200OK);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

CallMediaRecognizeChoiceOptions GetMediaRecognizeChoiceOptions(string content, string targetParticipant, string context = "")
{
    var playSource = new TextSource(content) { VoiceName = SpeechToTextVoice };
    var fileSource = new FileSource(new Uri(callbackUriHost + "/audio/MainMenu.wav"));
    var smmlSource = new SsmlSource("<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"><voice name=\"en-US-JennyNeural\">Hello this is SSML recognition test please confirm or cancel to proceed further.</voice></speak>");
    var recognizeOptions =
        new CallMediaRecognizeChoiceOptions(targetParticipant: new PhoneNumberIdentifier(targetParticipant), GetChoices())
        {
            InterruptCallMediaOperation = false,
            InterruptPrompt = false,
            InitialSilenceTimeout = TimeSpan.FromSeconds(10),
            Prompt = smmlSource,
            OperationContext = context
        };

    return recognizeOptions;
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

async Task HandlePlayAsync(CallMedia callConnectionMedia, string text)
{
    Console.WriteLine($"Playing text to customer: {text}.");

    // Play goodbye message
    var GoodbyePlaySource = new TextSource(text)
    {
        VoiceName = "en-US-NancyNeural"
    };

    var smmlSource = new SsmlSource("<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"><voice name=\"en-US-JennyNeural\">Thank you for confirming your appointment tomorrow. Goodbye!</voice></speak>");

       //await callConnectionMedia.PlayToAllAsync(GoodbyePlaySource);
       //await callConnectionMedia.PlayToAllAsync(new FileSource(new Uri(callbackUriHost + "/audio/Confirmed.wav")));
    await callConnectionMedia.PlayToAllAsync(smmlSource);
    //await callConnectionMedia.PlayAsync(GoodbyePlaySource, new List<CommunicationIdentifier>() { new PhoneNumberIdentifier(targetPhonenumber) });
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "audio")),
    RequestPath = "/audio"
});
app.Run();
