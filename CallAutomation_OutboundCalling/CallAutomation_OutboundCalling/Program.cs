using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// Your ACS resource connection string
var acsConnectionString = "<ACS_CONNECTION_STRING>";

// Your ACS resource phone number will act as source number to start outbound call
var acsPhonenumber = "<ACS_PHONE_NUMBER>";

// Target phone number you want to receive the call.
var targetPhonenumber = "<TARGET_PHONE_NUMBER>";

// Base url of the app
var callbackUriHost = "<CALLBACK_URI_HOST_WITH_PROTOCOL>";

// Your cognitive service endpoint
var cognitiveServiceEndpoint = "<COGNITIVE_SERVICE_ENDPOINT>";

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

CallAutomationClient callAutomationClient = new CallAutomationClient(new Uri(""), acsConnectionString);
var app = builder.Build();

app.MapPost("/outboundCall", async (ILogger<Program> logger) =>
{
    PhoneNumberIdentifier target = new PhoneNumberIdentifier(targetPhonenumber);
    PhoneNumberIdentifier caller = new PhoneNumberIdentifier(acsPhonenumber);
    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    CallInvite callInvite = new CallInvite(target, caller);
    var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
    {
        CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServiceEndpoint) },
        //MediaStreamingOptions = new MediaStreamingOptions(new Uri("wss://866a-103-180-73-34.ngrok-free.app"), MediaStreamingTransport.Websocket, MediaStreamingContent.Audio, MediaStreamingAudioChannel.Mixed) { StartMediaStreaming = true },
        //TranscriptionOptions = new TranscriptionOptions(new Uri("wss://866a-103-180-73-34.ngrok-free.app"), TranscriptionTransport.Websocket, "", true),
    };

    CreateCallResult createCallResult = await callAutomationClient.CreateCallAsync(createCallOptions);

    logger.LogInformation($"Created call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
    //logger.LogInformation($"Answered By: {createCallResult.CallConnectionProperties.AnsweredBy}");
    //logger.LogInformation($"Media Streaming: {createCallResult.CallConnectionProperties.MediaStreamingSubscription}");
    //logger.LogInformation($"Media Streaming: {createCallResult.CallConnectionProperties.TranscriptionSubscription}");
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

        logger.LogInformation($"CALL CONNECTION ID : {parsedEvent.CallConnectionId}");
        logger.LogInformation($"CORRELATION ID : {parsedEvent.CorrelationId}");

        var callMedia = callConnection.GetCallMedia();

        if (parsedEvent is CallConnected callConnected)
        {
            logger.LogInformation("Fetching recognize options...");

            // prepare recognize tones
            var recognizeOptions = GetMediaRecognizeChoiceOptions(MainMenu, targetPhonenumber);

            logger.LogInformation("Recognizing options...");

            // Send request to recognize tones
            await callMedia.StartRecognizingAsync(recognizeOptions);
            await HandlePlayAsync(callMedia, "Play Started");
        }
        else if (parsedEvent is RecognizeCompleted recognizeCompleted)
        {
            var choiceResult = recognizeCompleted.RecognizeResult as ChoiceResult;
            var labelDetected = choiceResult?.Label;
            var phraseDetected = choiceResult?.RecognizedPhrase;
            // If choice is detected by phrase, choiceResult.RecognizedPhrase will have the phrase detected, 
            // If choice is detected using dtmf tone, phrase will be null 
            logger.LogInformation("Recognize completed succesfully, labelDetected={labelDetected}, phraseDetected={phraseDetected}", labelDetected, phraseDetected);
            var textToPlay = labelDetected.Equals(ConfirmChoiceLabel, StringComparison.OrdinalIgnoreCase) ? ConfirmedText : CancelText;

            await HandlePlayAsync(callMedia, textToPlay);

            PhoneNumberIdentifier target = new PhoneNumberIdentifier(targetPhonenumber);
            var options = new HoldOptions(target)
            {
                OperationCallbackUri = new Uri(callbackUriHost)
            };
            var a = await callMedia.HoldAsync(target);
            logger.LogInformation("Hold target user completed succesfully");
            var parti = await callConnection.GetParticipantAsync(target);
            var isHold = parti.Value.IsOnHold;

            var b = await callMedia.UnholdAsync(target);
            var partic = await callConnection.GetParticipantAsync(target);
            var isHolds = partic.Value.IsOnHold;
            logger.LogInformation("Un Hold target user completed succesfully");

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
        else if ((parsedEvent is PlayStarted))
        {
            logger.LogInformation($"Play started event triggered.");
        }
        else if ((parsedEvent is PlayCompleted) || (parsedEvent is PlayFailed))
        {
            logger.LogInformation($"Terminating call.");
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
    var playSources = new List<PlaySource>() { new TextSource("Recognize Prompt One") { VoiceName = SpeechToTextVoice }, new TextSource("Recognize Prompt Two") { VoiceName = SpeechToTextVoice }, new TextSource(content) { VoiceName = SpeechToTextVoice } };

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
    
    PlayToAllOptions playOptions = new PlayToAllOptions(GoodbyePlaySource)
    {
        InterruptCallMediaOperation = false,
        OperationCallbackUri = new Uri(callbackUriHost),
        Loop = false
    };
    
    await callConnectionMedia.PlayToAllAsync(playOptions);
    //var interrupt = new TextSource("Interrupt prompt message")
    //{
    //    VoiceName = "en-US-NancyNeural"
    //};

    //PlayToAllOptions playInterrupt = new PlayToAllOptions(interrupt)
    //{
    //    InterruptCallMediaOperation = true,
    //    OperationCallbackUri = new Uri(callbackUriHost),
    //    Loop = false
    //};
    //await callConnectionMedia.PlayToAllAsync(playInterrupt);
}

app.Run();
