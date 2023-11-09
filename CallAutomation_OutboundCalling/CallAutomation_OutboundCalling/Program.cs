using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;

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

// This will be set by fileStatus endpoints
string recordingLocation = "";

string recordingId = "";

// text to play
const string SpeechToTextVoice = "en-US-NancyNeural";
const string MainMenu =
    """ 
    Hello this is Contoso Bank, we’re calling in regard to your appointment tomorrow 
    at 9am to open a new account. Please confirm if this time is still suitable for you or if you would like to cancel. 
    This call is recorded for quality purposes.
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
const string CancelChoiceLabel = "Cancel";
const string RetryContext = "retry";

CallAutomationClient callAutomationClient = new CallAutomationClient(acsConnectionString);
var app = builder.Build();

app.MapPost("/outboundCall", async (ILogger<Program> logger) =>
{
    PhoneNumberIdentifier target = new PhoneNumberIdentifier(targetPhonenumber);
    PhoneNumberIdentifier caller = new PhoneNumberIdentifier(acsPhonenumber);
    var callbackUri = new Uri(callbackUriHost + "/api/callbacks");
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
        CallAutomationEventBase parsedEvent = CallAutomationEventParser.Parse(cloudEvent);
        logger.LogInformation(
                    "Received call event: {type}, callConnectionID: {connId}, serverCallId: {serverId}",
                    parsedEvent.GetType(),
                    parsedEvent.CallConnectionId,
                    parsedEvent.ServerCallId);

        var callConnection = callAutomationClient.GetCallConnection(parsedEvent.CallConnectionId);
        var callMedia = callConnection.GetCallMedia();

        if (parsedEvent is CallConnected callConnected)
        {
            logger.LogInformation($"Start Recording...");
            CallLocator callLocator = new ServerCallLocator(parsedEvent.ServerCallId);
            var recordingResult = await callAutomationClient.GetCallRecording().StartAsync(new StartRecordingOptions(callLocator));
            recordingId = recordingResult.Value.RecordingId;

            var choices = GetChoices();

            // prepare recognize tones
            var recognizeOptions = GetMediaRecognizeChoiceOptions(MainMenu, targetPhonenumber, choices);

            // Send request to recognize tones
            await callMedia.StartRecognizingAsync(recognizeOptions);
        }
        if (parsedEvent is RecognizeCompleted recognizeCompleted)
        {
            var choiceResult = recognizeCompleted.RecognizeResult as ChoiceResult;
            var labelDetected = choiceResult?.Label;
            var phraseDetected = choiceResult?.RecognizedPhrase;
            // If choice is detected by phrase, choiceResult.RecognizedPhrase will have the phrase detected, 
            // If choice is detected using dtmf tone, phrase will be null 
            logger.LogInformation("Recognize completed succesfully, labelDetected={labelDetected}, phraseDetected={phraseDetected}", labelDetected, phraseDetected);
            var textToPlay = labelDetected.Equals(ConfirmChoiceLabel, StringComparison.OrdinalIgnoreCase) ? ConfirmedText : CancelText;

            await HandlePlayAsync(callMedia, textToPlay);
        }
        if (parsedEvent is RecognizeFailed recognizeFailedEvent)
        {
            var context = recognizeFailedEvent.OperationContext;
            if (!string.IsNullOrWhiteSpace(context) && context.Equals(RetryContext, StringComparison.OrdinalIgnoreCase))
            {
                await HandlePlayAsync(callMedia, NoResponse);
            }
            else
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

                var recognizeOptions = GetMediaRecognizeChoiceOptions(replyText, targetPhonenumber, GetChoices(), RetryContext);
                await callMedia.StartRecognizingAsync(recognizeOptions);
            }
        }
        if ((parsedEvent is PlayCompleted) || (parsedEvent is PlayFailed))
        {
            logger.LogInformation($"Stop recording and terminating call.");
            callAutomationClient.GetCallRecording().Stop(recordingId);
            await callConnection.HangUpAsync(true);
        }
    }
    return Results.Ok();
}).Produces(StatusCodes.Status200OK);

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
    callAutomationClient.GetCallRecording().DownloadTo(new Uri(recordingLocation), "testfile.wav");
    return Results.Ok();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

CallMediaRecognizeChoiceOptions GetMediaRecognizeChoiceOptions(string content, string targetParticipant, List<RecognitionChoice> choices, string context = "")
{
    var playSource = new TextSource(content) { VoiceName = SpeechToTextVoice };

    var recognizeOptions =
        new CallMediaRecognizeChoiceOptions(targetParticipant: new PhoneNumberIdentifier(targetParticipant), choices)
        {
            InterruptCallMediaOperation = false,
            InterruptPrompt = false,
            InitialSilenceTimeout = TimeSpan.FromSeconds(10),
            Prompt = playSource,
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
    // Play goodbye message
    var GoodbyePlaySource = new TextSource(text)
    {
        VoiceName = "en-US-NancyNeural"
    };

    await callConnectionMedia.PlayToAllAsync(GoodbyePlaySource);
}

app.Run();
