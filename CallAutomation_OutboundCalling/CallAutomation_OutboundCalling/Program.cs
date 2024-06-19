using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.Extensions.Logging;

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

// text to play
const string SpeechToTextVoice = "en-US-NancyNeural";
const string MainMenu = "Please say confirm or say cancel to proceed further.";
const string ConfirmedText = "Thank you for confirming your appointment tomorrow at 9am, we look forward to meeting with you.";
const string CancelText = """
Your appointment tomorrow at 9am has been cancelled. Please call the bank directly s
if you would like to rebook for another date and time.
""";
const string CustomerQueryTimeout = "I�m sorry I didn�t receive a response, please try again.";
const string NoResponse = "I didn't receive an input, we will go ahead and confirm your appointment. Goodbye";
const string InvalidAudio = "I�m sorry, I didn�t understand your response, please try again.";
const string ConfirmChoiceLabel = "Confirm";
const string RetryContext = "retry";
const string dtmfPrompt = "Thank you for the update. Please type  one two three four on your keypad to close call.";
string cancelLabel = "Cancel";

CallAutomationClient callAutomationClient = new CallAutomationClient(new Uri("<Pma>"), acsConnectionString);
var app = builder.Build();

app.MapPost("/outboundCall", async (ILogger<Program> logger) =>
{
    PhoneNumberIdentifier target = new PhoneNumberIdentifier(targetPhonenumber);
    PhoneNumberIdentifier caller = new PhoneNumberIdentifier(acsPhonenumber);
    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    CallInvite callInvite = new CallInvite(target, caller);
    
    MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(
       new Uri("wss://t9x01h42-5000.inc1.devtunnels.ms/ws"),
       MediaStreamingContent.Audio,
       MediaStreamingAudioChannel.Mixed,
       MediaStreamingTransport.Websocket,
       false);
    var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
    {
        CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServiceEndpoint) },
        //MediaStreamingOptions = mediaStreamingOptions,
        //TranscriptionOptions = new TranscriptionOptions(new Uri("wss://jg1tlgbv-5000.inc1.devtunnels.ms/ws"), "en-US", false,  TranscriptionTransport.Websocket)
    };
    CreateCallResult createCallResult = await callAutomationClient.CreateCallAsync(createCallOptions);

    logger.LogInformation($"Created call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
});
app.MapPost("/createPSTNCall", async (ILogger<Program> logger) =>
{
    PhoneNumberIdentifier target = new PhoneNumberIdentifier("+18332638155");
    PhoneNumberIdentifier caller = new PhoneNumberIdentifier("+18772119545");
    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    CallInvite callInvite = new CallInvite(target, caller);
    var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
    {
        CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServiceEndpoint) }
    };

    CreateCallResult createCallResult = await callAutomationClient.CreateCallAsync(createCallOptions);

    logger.LogInformation($"Created call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
});

app.MapPost("/api/callbacks", async (CloudEvent[] cloudEvents, ILogger<Program> logger) =>
{
    foreach (var cloudEvent in cloudEvents)
    {
        PhoneNumberIdentifier target = new PhoneNumberIdentifier(targetPhonenumber);
        CallAutomationEventBase parsedEvent = CallAutomationEventParser.Parse(cloudEvent);
        logger.LogInformation(
                    "Received call event: {type}, callConnectionID: {connId}, serverCallId: {serverId}",
                    parsedEvent.GetType(),
                    parsedEvent.CallConnectionId,
                    parsedEvent.ServerCallId);

        var callConnection = callAutomationClient.GetCallConnection(parsedEvent.CallConnectionId);
        var callMedia = callConnection.GetCallMedia();
        logger.LogInformation($"CALL CONNECTION ID ----> {parsedEvent.CallConnectionId}");
        logger.LogInformation($"CORRELATION ID ----> {parsedEvent.CorrelationId}");
        var connectionProperties = await callConnection.GetCallConnectionPropertiesAsync();
        logger.LogInformation($"ANSWERED FOR ----> {connectionProperties.Value.AnsweredFor}");

        

        if (parsedEvent is CallConnected callConnected)
        {
            #region Transfer Call
            //PhoneNumberIdentifier caller = new PhoneNumberIdentifier("+18772119545");
            //var transferOption = new TransferToParticipantOptions(target);
            ////transferOption.Transferee = caller;
            //transferOption.OperationContext = "transferCallContext";
            //transferOption.SourceCallerIdNumber = caller;

            //// Sending event to a non-default endpoint.
            //transferOption.OperationCallbackUri = new Uri(callbackUriHost);
            //TransferCallToParticipantResult result = await callConnection.TransferCallToParticipantAsync(transferOption);
            //logger.LogInformation($"Call Transfered successfully");
            //logger.LogInformation($"Media Streaming Subscription state ----> {connectionProperties.Value.MediaStreamingSubscription.State}");
            #endregion

            #region Media streaming
            //StartMediaStreamingOptions options = new StartMediaStreamingOptions()
            //{
            //    OperationCallbackUri = new Uri(callbackUriHost),
            //    OperationContext = "startMediaStreamingContext"
            //};
            //await callMedia.StartMediaStreamingAsync(options);
            //logger.LogInformation("Start Media Streaming.....");
            //connectionProperties = await callConnection.GetCallConnectionPropertiesAsync();
            //logger.LogInformation($"Media Streaming Subscription state ----> {connectionProperties.Value.MediaStreamingSubscription.State}");
            //await Task.Delay(5000);

            ////Stop Media Streaming
            //StopMediaStreamingOptions stopOptions = new StopMediaStreamingOptions()
            //{
            //    OperationCallbackUri = new Uri(callbackUriHost)
            //};

            //await callMedia.StopMediaStreamingAsync(stopOptions);
            //logger.LogInformation("Stop Media Streaming.....");
            //connectionProperties = await callConnection.GetCallConnectionPropertiesAsync();
            //logger.LogInformation($"Media Streaming Subscription state ----> {connectionProperties.Value.MediaStreamingSubscription.State}");
            //await Task.Delay(5000);

            ////Start media streaming
            //await callMedia.StartMediaStreamingAsync(options);
            //logger.LogInformation("Start Media Streaming.....");
            //connectionProperties = await callConnection.GetCallConnectionPropertiesAsync();
            //logger.LogInformation($"Media Streaming Subscription state ----> {connectionProperties.Value.MediaStreamingSubscription.State}");
            #endregion

            #region Transcription
            //StartTranscriptionOptions options = new StartTranscriptionOptions()
            //{
            //    OperationContext = "startMediaStreamingContext",
            //    //Locale = "en-US",
            //};
            //await callMedia.StartTranscriptionAsync(options);
            //logger.LogInformation("Start Transcription.....");
            //connectionProperties = await callConnection.GetCallConnectionPropertiesAsync();
            //logger.LogInformation($"Transcription Subscription state ----> {connectionProperties.Value.TranscriptionSubscription.State}");
            //await Task.Delay(5000);

            ////Stop Transcription
            //StopTranscriptionOptions stopOptions = new StopTranscriptionOptions()
            //{
            //    OperationContext = "stopTranscription"
            //};

            //await callMedia.StopTranscriptionAsync(stopOptions);
            //logger.LogInformation("Stop Transcription.....");
            //connectionProperties = await callConnection.GetCallConnectionPropertiesAsync();
            //logger.LogInformation($"Transcription Subscription state ----> {connectionProperties.Value.TranscriptionSubscription.State}");

            ////Start Transcription
            //await callMedia.StartTranscriptionAsync(options);
            //logger.LogInformation("Start Transcription.....");
            //connectionProperties = await callConnection.GetCallConnectionPropertiesAsync();
            //logger.LogInformation($"Transcription Subscription state ----> {connectionProperties.Value.TranscriptionSubscription.State}");
            //await Task.Delay(5000);
            #endregion

            logger.LogInformation("Fetching recognize options...");

            // prepare recognize tones Choice
            var recognizeOptions = GetMediaRecognizeChoiceOptions(MainMenu, targetPhonenumber);

            //// prepare recognize tones DTMF
            //var recognizeOptions = GetMediaRecognizeDTMFOptions(dtmfPrompt, targetPhonenumber);

            //// prepare recognize tones Speech
            //var recognizeOptions = GetMediaRecognizeSpeechOptions(dtmfPrompt, targetPhonenumber);

            //// prepare recognize tones Speech or dtmf
            //var recognizeOptions = GetMediaRecognizeSpeechOrDtmfOptions(dtmfPrompt, targetPhonenumber);

            logger.LogInformation("Recognizing options...");

            // Send request to recognize tones
            await callMedia.StartRecognizingAsync(recognizeOptions);
        }
        else if (parsedEvent is RecognizeCompleted recognizeCompleted)
        {
            var choiceResult = recognizeCompleted.RecognizeResult as ChoiceResult;
            var labelDetected = choiceResult?.Label;
            var phraseDetected = choiceResult?.RecognizedPhrase;
            // If choice is detected by phrase, choiceResult.RecognizedPhrase will have the phrase detected, 
            // If choice is detected using dtmf tone, phrase will be null 
            logger.LogInformation("Recognize completed succesfully, labelDetected={labelDetected}, phraseDetected={phraseDetected}", labelDetected, phraseDetected);
            //var textToPlay = labelDetected.Equals(ConfirmChoiceLabel, StringComparison.OrdinalIgnoreCase) ? ConfirmedText : CancelText;
            var textToPlay = "Recognized DTMF tone";

            #region Hold and Unhold
            ////Hold
            //var holdOptions = new HoldOptions(target) {
            //    OperationCallbackUri = new Uri(callbackUriHost),
            //    OperationContext = "holdPstnParticipant"
            //};
            //////hold participant with options
            ////var holdParticipant = await callMedia.HoldAsync(holdOptions);

            ////hold participant without options
            //var holdParticipant = await callMedia.HoldAsync(target);

            //var isParticipantHold = (await callConnection.GetParticipantAsync(target)).Value.IsOnHold;
            //logger.LogInformation($"Is participant on hold ----> {isParticipantHold}");
            ////Un-Hold
            //var unHoldOptions = new UnholdOptions(target)
            //{
            //    OperationContext = "UnHoldPstnParticipant"
            //};
            //////Un-Hold participant with options
            ////var UnHoldParticipant = await callMedia.UnholdAsync(unHoldOptions);

            ////Un-Hold participant without options
            //var UnHoldParticipant = await callMedia.UnholdAsync(target);
            //var isParticipantUnHold = (await callConnection.GetParticipantAsync(target)).Value.IsOnHold;
            //logger.LogInformation($"Is participant on hold ----> {isParticipantUnHold}");
            #endregion

            #region Media Streaming
            //StopMediaStreamingOptions options = new StopMediaStreamingOptions()
            //{
            //    OperationCallbackUri = new Uri(callbackUriHost)
            //};

            //await callMedia.StopMediaStreamingAsync(options);
            //logger.LogInformation("Stop Media Streaming.....");
            //connectionProperties = await callConnection.GetCallConnectionPropertiesAsync();
            //logger.LogInformation($"Media Streaming Subscription state ----> {connectionProperties.Value.MediaStreamingSubscription.State}");
            //await Task.Delay(5000);

            ////Start media streaming
            //StartMediaStreamingOptions startOptions = new StartMediaStreamingOptions()
            //{
            //    OperationCallbackUri = new Uri(callbackUriHost),
            //    OperationContext = "startMediaStreamingContext"
            //};
            //await callMedia.StartMediaStreamingAsync(startOptions);
            //logger.LogInformation("Start Media Streaming.....");
            //connectionProperties = await callConnection.GetCallConnectionPropertiesAsync();
            //logger.LogInformation($"Media Streaming Subscription state ----> {connectionProperties.Value.MediaStreamingSubscription.State}");
            //await Task.Delay(5000);

            //await callMedia.StopMediaStreamingAsync(options);
            //logger.LogInformation("Stop Media Streaming.....");
            //connectionProperties = await callConnection.GetCallConnectionPropertiesAsync();
            //logger.LogInformation($"Media Streaming Subscription state ----> {connectionProperties.Value.MediaStreamingSubscription.State}");
            #endregion

            #region Transcription
            //StopTranscriptionOptions stopOptions = new StopTranscriptionOptions()
            //{
            //    OperationContext = "stopTranscription"
            //};

            //await callMedia.StopTranscriptionAsync(stopOptions);
            //logger.LogInformation("Stop Transcription.....");
            //connectionProperties = await callConnection.GetCallConnectionPropertiesAsync();
            //logger.LogInformation($"Transcription Subscription state ----> {connectionProperties.Value.TranscriptionSubscription.State}");

            ////Start Transcription
            //StartTranscriptionOptions options = new StartTranscriptionOptions()
            //{
            //    OperationContext = "startMediaStreamingContext",
            //    //Locale = "en-US",
            //};
            //await callMedia.StartTranscriptionAsync(options);
            //logger.LogInformation("Start Transcription.....");
            //connectionProperties = await callConnection.GetCallConnectionPropertiesAsync();
            //logger.LogInformation($"Transcription Subscription state ----> {connectionProperties.Value.TranscriptionSubscription.State}");
            //await Task.Delay(5000);

            ////Stop Transcription
            //await callMedia.StopTranscriptionAsync(stopOptions);
            //logger.LogInformation("Stop Transcription.....");
            //connectionProperties = await callConnection.GetCallConnectionPropertiesAsync();
            //logger.LogInformation($"Transcription Subscription state ----> {connectionProperties.Value.TranscriptionSubscription.State}");
            #endregion

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
        else if ((parsedEvent is MediaStreamingStarted))
        {
            logger.LogInformation($"MediaStreamingStarted event triggered.");
        }
        else if ((parsedEvent is MediaStreamingStopped))
        {
            logger.LogInformation($"MediaStreamingStopped event triggered.");
        }
        else if ((parsedEvent is MediaStreamingFailed))
        {
            logger.LogInformation($"MediaStreamingFailed event triggered.");
        }
        else if ((parsedEvent is TranscriptionStarted))
        {
            logger.LogInformation($"TranscriptionStarted event triggered.");
        }
        else if ((parsedEvent is TranscriptionStopped))
        {
            logger.LogInformation($"TranscriptionStopped event triggered.");
        }
        else if ((parsedEvent is TranscriptionFailed))
        {
            logger.LogInformation($"TranscriptionFailed event triggered.");
        }
        else if ((parsedEvent is PlayCompleted) || (parsedEvent is PlayFailed))
        {
            logger.LogInformation($"Terminating call.");
            await callConnection.HangUpAsync(true);
        }
        else if (parsedEvent is PlayStarted)
        {
            logger.LogInformation($"PlayStarted event triggered.");
        }
        else if (parsedEvent is CallTransferAccepted)
        {
            logger.LogInformation($"CallTransferAccepted event triggered.");
        }
        else if (parsedEvent is CallTransferFailed)
        {
            logger.LogInformation($"CallTransferFailed event triggered.");
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
    #region Recognize prompt list
    ////Multiple TextSource prompt
    //var playSources = new List<PlaySource>() { new TextSource("recognize prompt one") { VoiceName = SpeechToTextVoice }, new TextSource("recognize prompt two") { VoiceName = SpeechToTextVoice }, new TextSource(content) { VoiceName = SpeechToTextVoice } };
    
    ////Multiple FileSource Prompts
    //var playSources = new List<PlaySource>() { new FileSource(new Uri("https://www2.cs.uic.edu/~i101/SoundFiles/StarWars3.wav")), new FileSource(new Uri("https://www2.cs.uic.edu/~i101/SoundFiles/preamble10.wav")) };

    ////Multiple TextSource prompt
    //var playSources = new List<PlaySource>() { new TextSource("recognize prompt one") { VoiceName = SpeechToTextVoice }, new FileSource(new Uri("https://www2.cs.uic.edu/~i101/SoundFiles/StarWars3.wav")), new TextSource(content) { VoiceName = SpeechToTextVoice } };

    ////Empty play sources
    var playSources = new List<PlaySource>() { };

    #endregion
    var recognizeOptions =
        new CallMediaRecognizeChoiceOptions(targetParticipant: new PhoneNumberIdentifier(targetParticipant), GetChoices())
        {
            InterruptCallMediaOperation = false,
            InterruptPrompt = false,
            InitialSilenceTimeout = TimeSpan.FromSeconds(10),
            //Prompt = playSource,
            PlayPrompts = playSources,
            OperationContext = context
        };

    return recognizeOptions;
}

CallMediaRecognizeDtmfOptions GetMediaRecognizeDTMFOptions(string content, string targetParticipant, string context = "")
{
    var playSource = new TextSource(content) { VoiceName = SpeechToTextVoice };
    #region Recognize prompt list
    ////Multiple TextSource prompt
    //var playSources = new List<PlaySource>() { new TextSource("recognize prompt one") { VoiceName = SpeechToTextVoice }, new TextSource("recognize prompt two") { VoiceName = SpeechToTextVoice }, new TextSource(content) { VoiceName = SpeechToTextVoice } };

    ////Multiple FileSource Prompts
    //var playSources = new List<PlaySource>() { new FileSource(new Uri("https://www2.cs.uic.edu/~i101/SoundFiles/StarWars3.wav")), new FileSource(new Uri("https://www2.cs.uic.edu/~i101/SoundFiles/preamble10.wav")) };

    ////Multiple TextSource and file source prompt
    var playSources = new List<PlaySource>() { new TextSource("recognize prompt one") { VoiceName = SpeechToTextVoice }, new FileSource(new Uri("https://www2.cs.uic.edu/~i101/SoundFiles/StarWars3.wav")), new TextSource(content) { VoiceName = SpeechToTextVoice } };

    ////Empty play sources
    //var playSources = new List<PlaySource>() { };

    #endregion
    
    var recognizeOptions =
                new CallMediaRecognizeDtmfOptions(
                    targetParticipant: new PhoneNumberIdentifier(targetParticipant), maxTonesToCollect: 8)
                {
                    InterruptPrompt = false,
                    InterToneTimeout = TimeSpan.FromSeconds(5),
                    OperationContext = context,
                    InitialSilenceTimeout = TimeSpan.FromSeconds(15),
                    PlayPrompts = playSources,
                };
    return recognizeOptions;
}


CallMediaRecognizeSpeechOptions GetMediaRecognizeSpeechOptions(string content, string targetParticipant, string context = "")
{
    var playSource = new TextSource(content) { VoiceName = SpeechToTextVoice };
    #region Recognize prompt list
    ////Multiple TextSource prompt
    //var playSources = new List<PlaySource>() { new TextSource("recognize prompt one") { VoiceName = SpeechToTextVoice }, new TextSource("recognize prompt two") { VoiceName = SpeechToTextVoice }, new TextSource(content) { VoiceName = SpeechToTextVoice } };

    ////Multiple FileSource Prompts
    //var playSources = new List<PlaySource>() { new FileSource(new Uri("https://www2.cs.uic.edu/~i101/SoundFiles/StarWars3.wav")), new FileSource(new Uri("https://www2.cs.uic.edu/~i101/SoundFiles/preamble10.wav")) };

    ////Multiple TextSource and file source prompt
    var playSources = new List<PlaySource>() { new TextSource("recognize prompt one") { VoiceName = SpeechToTextVoice }, new FileSource(new Uri("https://www2.cs.uic.edu/~i101/SoundFiles/StarWars3.wav")), new TextSource(content) { VoiceName = SpeechToTextVoice } };

    ////Empty play sources
    //var playSources = new List<PlaySource>() { };

    #endregion

    var recognizeOptions =
                new CallMediaRecognizeSpeechOptions(
                    targetParticipant: new PhoneNumberIdentifier(targetParticipant))
                {
                    InterruptPrompt = false,
                    OperationContext = context,
                    InitialSilenceTimeout = TimeSpan.FromSeconds(15),
                    PlayPrompts = playSources,
                    EndSilenceTimeout = TimeSpan.FromSeconds(15)
                };
    return recognizeOptions;
}

CallMediaRecognizeSpeechOrDtmfOptions GetMediaRecognizeSpeechOrDtmfOptions(string content, string targetParticipant, string context = "")
{
    var playSource = new TextSource(content) { VoiceName = SpeechToTextVoice };
    #region Recognize prompt list
    ////Multiple TextSource prompt
    //var playSources = new List<PlaySource>() { new TextSource("recognize prompt one") { VoiceName = SpeechToTextVoice }, new TextSource("recognize prompt two") { VoiceName = SpeechToTextVoice }, new TextSource(content) { VoiceName = SpeechToTextVoice } };

    ////Multiple FileSource Prompts
    //var playSources = new List<PlaySource>() { new FileSource(new Uri("https://www2.cs.uic.edu/~i101/SoundFiles/StarWars3.wav")), new FileSource(new Uri("https://www2.cs.uic.edu/~i101/SoundFiles/preamble10.wav")) };

    ////Multiple TextSource and file source prompt
    var playSources = new List<PlaySource>() { new TextSource("recognize prompt one") { VoiceName = SpeechToTextVoice }, new FileSource(new Uri("https://www2.cs.uic.edu/~i101/SoundFiles/StarWars3.wav")), new TextSource(content) { VoiceName = SpeechToTextVoice } };

    ////Empty play sources
    //var playSources = new List<PlaySource>() { };

    #endregion

    var recognizeOptions =
                new CallMediaRecognizeSpeechOrDtmfOptions(
                    targetParticipant: new PhoneNumberIdentifier(targetParticipant), maxTonesToCollect: 8)
                {
                    InterruptPrompt = false,
                    OperationContext = context,
                    InitialSilenceTimeout = TimeSpan.FromSeconds(15),
                    PlayPrompts = playSources,
                    EndSilenceTimeout = TimeSpan.FromSeconds(5)
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
    PhoneNumberIdentifier target = new PhoneNumberIdentifier(targetPhonenumber);
    var playTo = new List<CommunicationIdentifier> { target };
    // Play goodbye message
    var GoodbyePlaySource = new TextSource(text)
    {
        VoiceName = "en-US-NancyNeural"
    };

    PlayToAllOptions playOptions = new PlayToAllOptions(GoodbyePlaySource)
    {
        InterruptCallMediaOperation = false,
        OperationCallbackUri = new Uri(callbackUriHost),
        Loop = false
    };

    await callConnectionMedia.PlayToAllAsync(playOptions);

    //PlayOptions playOptions = new PlayOptions(GoodbyePlaySource, playTo)
    //{
    //    OperationCallbackUri = new Uri(callbackUriHost),
    //    Loop = false
    //};
    //await callConnectionMedia.PlayAsync(playOptions);
    //var interrupt = new TextSource("Interrupt prompt message")
    //{
    //    VoiceName = "en-US-NancyNeural"
    //};

    //PlayToAllOptions playInterrupt = new PlayToAllOptions(interrupt)
    //{
    //    InterruptCallMediaOperation = false,
    //    OperationCallbackUri = new Uri(callbackUriHost),
    //    Loop = false
    //};
    //await callConnectionMedia.PlayToAllAsync(playInterrupt);

    ////file source
    //var interruptFile = new FileSource(new Uri("https://www2.cs.uic.edu/~i101/SoundFiles/StarWars3.wav"));
    //PlayToAllOptions playInterrupt = new PlayToAllOptions(interruptFile)
    //{
    //    InterruptCallMediaOperation = true,
    //    OperationCallbackUri = new Uri(callbackUriHost),
    //    Loop = false
    //};
    //await callConnectionMedia.PlayToAllAsync(playInterrupt);

    //PlayOptions playOptions = new PlayOptions(interruptFile, playTo)
    //{
    //    OperationCallbackUri = new Uri(callbackUriHost),
    //    Loop = false
    //};
    //await callConnectionMedia.PlayAsync(playOptions);
}

app.Run();
