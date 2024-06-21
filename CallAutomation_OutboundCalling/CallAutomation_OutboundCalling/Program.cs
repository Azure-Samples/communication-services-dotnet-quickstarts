using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
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

// text to play
const string SpeechToTextVoice = "en-US-NancyNeural";
const string MainMenu =
    """ 
    Hello this is Contoso Bank, we re calling in regard to your appointment tomorrow 
    at 9am to open a new account. Please say confirm if this time is still suitable for you or say cancel 
    if you would like to cancel this appointment.
    """;
const string ConfirmedText = "Thank you for confirming your appointment tomorrow at 9am, we look forward to meeting with you.";
const string CancelText = """
Your appointment tomorrow at 9am has been cancelled. Please call the bank directly 
if you would like to rebook for another date and time.
""";
const string CustomerQueryTimeout = "I m sorry I didn t receive a response, please try again.";
const string NoResponse = "I didn't receive an input, we will go ahead and confirm your appointment. Goodbye";
const string InvalidAudio = "I m sorry, I didn t understand your response, please try again.";
const string ConfirmChoiceLabel = "Confirm";
const string RetryContext = "retry";
const string dtmfPrompt = "Thank you for the update. Please type  one two three four on your keypad to close call.";
string cancelLabel = "Cancel";

CallAutomationClient callAutomationClient = new CallAutomationClient(new Uri("<PMA>"), acsConnectionString);
var app = builder.Build();

app.MapPost("/outboundCall", async (ILogger<Program> logger) =>
{
    PhoneNumberIdentifier target = new PhoneNumberIdentifier(targetPhonenumber);
    PhoneNumberIdentifier caller = new PhoneNumberIdentifier(acsPhonenumber);
    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    CallInvite callInvite = new CallInvite(target, caller);

    //MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(
    //    new Uri("wss://a016-103-190-198-138.ngrok-free.app"),
    //    MediaStreamingTransport.Websocket,
    //    MediaStreamingContent.Audio,
    //    MediaStreamingAudioChannel.Mixed,
    //    false);
    var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
    {
        CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServiceEndpoint) },
        //MediaStreamingOptions = mediaStreamingOptions
        //TranscriptionOptions = new TranscriptionOptions(new Uri("wss://866a-103-180-73-34.ngrok-free.app"), TranscriptionTransport.Websocket, "", true),
    };

    CreateCallResult createCallResult = await callAutomationClient.CreateCallAsync(createCallOptions);

    logger.LogInformation($"Created call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
    logger.LogInformation($"Answered By: {createCallResult.CallConnectionProperties.AnsweredBy}");
    //logger.LogInformation($"Media Streaming: {createCallResult.CallConnectionProperties.MediaStreamingSubscription}");
    //logger.LogInformation($"Transcription Subscription: {createCallResult.CallConnectionProperties.TranscriptionSubscription}");
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

        //logger.LogInformation($"CALL CONNECTION ID : {parsedEvent.CallConnectionId}");
        logger.LogInformation($"CORRELATION ID : {parsedEvent.CorrelationId}");

        var callMedia = callConnection.GetCallMedia();

        if (parsedEvent is CallConnected callConnected)
        {
            //StartMediaStreamingOptions options = new StartMediaStreamingOptions()
            //{
            //    OperationCallbackUrl = callbackUriHost,
            //    OperationContext = "startMediaStreamingContext"
            //};

            //callMedia.StartMediaStreaming(options);

            //await callMedia.StartMediaStreamingAsync(options);
            //logger.LogInformation("Start Media Streaming.....");

            //logger.LogInformation("Fetching recognize options...");

            //// prepare recognize tones
            //var recognizeOptions = GetMediaRecognizeChoiceOptions(MainMenu, targetPhonenumber);

            //logger.LogInformation("Recognizing options...");

            //// Send request to recognize tones
            //await callMedia.StartRecognizingAsync(recognizeOptions);
            //await HandlePlayAllAsync(callMedia, "Play Started");

        }
        else if ((parsedEvent is SendDtmfTonesCompleted))
        {
            logger.LogInformation($"SendDtmfTonesCompleted Received.");
        }
        else if ((parsedEvent is SendDtmfTonesFailed))
        {
            logger.LogInformation($"SendDtmfTonesFailed Received.");
        }

        else if (parsedEvent is RecognizeCompleted recognizeCompleted)
        {
            //StopMediaStreamingOptions options = new StopMediaStreamingOptions()
            //{
            //    OperationCallbackUrl = callbackUriHost
            //};

            ////callMedia.StopMediaStreaming(options);
            //await callMedia.StopMediaStreamingAsync(options);
            //logger.LogInformation("Stop Media Streaming.....");

            var choiceResult = recognizeCompleted.RecognizeResult as ChoiceResult;
            var labelDetected = choiceResult?.Label;
            var phraseDetected = choiceResult?.RecognizedPhrase;
            // If choice is detected by phrase, choiceResult.RecognizedPhrase will have the phrase detected, 
            // If choice is detected using dtmf tone, phrase will be null 
            logger.LogInformation("Recognize completed succesfully, labelDetected={labelDetected}, phraseDetected={phraseDetected}", labelDetected, phraseDetected);
            var textToPlay = labelDetected.Equals(ConfirmChoiceLabel, StringComparison.OrdinalIgnoreCase) ? ConfirmedText : CancelText;

            await HandlePlayAllAsync(callMedia, textToPlay);

        }
        else if (parsedEvent is RecognizeFailed { OperationContext: RetryContext })
        {
            logger.LogError("Encountered error during recognize, operationContext={context}", RetryContext);
            await HandlePlayAllAsync(callMedia, NoResponse);
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

            await StartRecognizing(callMedia, replyText, targetPhonenumber, false, RetryContext);
            //await callMedia.StartRecognizingAsync(recognizeOptions);
        }
        ////else if ((parsedEvent is MediaStreamingStarted))
        //{
        //    logger.LogInformation($"MediaStreamingStarted started event triggered.");
        //}
        //else if ((parsedEvent is MediaStreamingStopped))
        //{
        //    logger.LogInformation($"MediaStreamingStopped started event triggered.");
        //}
        //else if ((parsedEvent is PlayStarted))
        //{
        //    logger.LogInformation($"Play started event triggered.");
        //}
        else if ((parsedEvent is PlayCompleted) || (parsedEvent is PlayFailed))
        {
            logger.LogInformation($"PlayCompleted or PlayFailed Received.");
            //await callConnection.HangUpAsync(true);
        }
        else if ((parsedEvent is AddParticipantSucceeded))
        {
            logger.LogInformation($"AddParticipantSucceeded Received.");
        }
        else if ((parsedEvent is AddParticipantFailed))
        {
            logger.LogInformation($"AddParticipantFailed Received.");
        }
        else if ((parsedEvent is RemoveParticipantSucceeded))
        {
            logger.LogInformation($"RemoveParticipantSucceeded Received.");
        }
        else if ((parsedEvent is RemoveParticipantFailed))
        {
            logger.LogInformation($"RemoveParticipantFailed Received.");
        }

        else if ((parsedEvent is ConnectFailed))
        {
            logger.LogInformation($"ConnectFailed Received.");
        }
        else if ((parsedEvent is CallDisconnected))
        {
            logger.LogInformation($"CallDisconnected Received.");
        }
    }
    return Results.Ok();
}).Produces(StatusCodes.Status200OK);

app.MapPost("/connectApi", async (string serverCallId, ILogger<Program> logger) =>
{
    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    CallLocator callLocator = new ServerCallLocator(serverCallId);
    ConnectCallOptions connectCallOptions = new ConnectCallOptions(callLocator, callbackUri)
    {
        CallIntelligenceOptions = new CallIntelligenceOptions()
        {
            CognitiveServicesEndpoint = new Uri(cognitiveServiceEndpoint)
        }
    };


    ConnectCallResult result = await callAutomationClient.ConnectCallAsync(connectCallOptions);
    logger.LogInformation($"CALL CONNECTION ID : {result.CallConnectionProperties.CallConnectionId}");
    //logger.LogInformation($"CORRELATION ID : {result.CallConnectionProperties.CorrelationId}");

    var callConnection = callAutomationClient.GetCallConnection(result.CallConnectionProperties.CallConnectionId);
    //CommunicationUserIdentifier target = new CommunicationUserIdentifier("8:acs:19ae37ff-1a44-4e19-aade-198eedddbdf2_00000020-67e3-e152-d68a-08482200cbb3");
    PhoneNumberIdentifier target = new PhoneNumberIdentifier(targetPhonenumber);
    PhoneNumberIdentifier caller = new PhoneNumberIdentifier(acsPhonenumber);
    CallInvite callInvite = new CallInvite(target, caller);
    //CallInvite callInvite = new CallInvite(target);

    var addParticipantOptions = new AddParticipantOptions(callInvite)
    {
        OperationContext = "addPstnUserContext",
        InvitationTimeoutInSeconds = 10,
    };


    //add participant
    var addParticipant = await callConnection.AddParticipantAsync(callInvite);
    logger.LogInformation($"Adding Participant to the call: {addParticipant.Value?.InvitationId}");

    ////Cancel Add Participant
    //var cancelAddParticipantOperationOptions = new CancelAddParticipantOperationOptions(addParticipant.Value.InvitationId)
    //{
    //    OperationContext = "operationContext",
    //    OperationCallbackUri = callbackUri
    //};
    //await callConnection.CancelAddParticipantOperationAsync(cancelAddParticipantOperationOptions);

    ////Tranfer Call
    //var transferOption = new TransferToParticipantOptions(target);
    //transferOption.Transferee = caller;
    //transferOption.OperationContext = "transferCallContext";

    //// Sending event to a non-default endpoint.
    //transferOption.OperationCallbackUri = callbackUri;
    //TransferCallToParticipantResult transferResult = await callConnection.TransferCallToParticipantAsync(transferOption);
    //logger.LogInformation($"Call Transfered successfully");

   
    //get participant
    var participants = await callConnection.GetParticipantsAsync();
    logger.LogInformation($"Participants Count in call: {participants.Value.Count}");
    logger.LogInformation($"Participants in call: {JsonSerializer.Serialize(participants.Value)}");

    var callMedia = callConnection.GetCallMedia();


    logger.LogInformation("Fetching recognize options...");

    // prepare recognize tones
    //await StartRecognizing(callMedia, MainMenu, targetPhonenumber, false, string.Empty);

    // prepare dtmf tones
    await StartRecognizing(callMedia, dtmfPrompt, targetPhonenumber, true, string.Empty);

    // Send request to recognize tones
    //await callMedia.StartRecognizing(recognizeOptions);
    //await HandlePlayAsync(callMedia, "Connect API play started test", participants.Value.FirstOrDefault().Identifier);
    //logger.LogInformation(" Play Audio to target participants completed event");

    #region Mute participant
    //var muteResponse = await callConnection.MuteParticipantAsync(participants.Value.LastOrDefault()?.Identifier);

    //if (muteResponse.GetRawResponse().Status == 200)
    //{
    //    logger.LogInformation("Participant is muted. Waiting for confirmation...");
    //    var participant = await callConnection.GetParticipantAsync(participants.Value.LastOrDefault()?.Identifier);
    //    logger.LogInformation($"Is participant muted: {participant.Value.IsMuted}");
    //    logger.LogInformation("Mute participant test completed.");
    //}
    #endregion

    //Remove participant
    //var removeParticipant = await callConnection.RemoveParticipantAsync(target);

    #region Recording
    //StartRecordingOptions recordingOptions = new StartRecordingOptions(new ServerCallLocator(serverCallId))
    //{
    //    RecordingContent = RecordingContent.Audio,
    //    RecordingChannel = RecordingChannel.Unmixed,
    //    RecordingFormat = RecordingFormat.Wav
    //};
    //var recordingTask = callAutomationClient.GetCallRecording().StartAsync(recordingOptions);
    //var recordingId = recordingTask.Result.Value.RecordingId;
    //logger.LogInformation($"Call recording id--> {recordingId}");
    //await Task.Delay(5000);

    //await callAutomationClient.GetCallRecording().PauseAsync(recordingId);
    //logger.LogInformation($"Recording is Paused.");
    //var recordingState = await callAutomationClient.GetCallRecording().GetStateAsync(recordingId);
    //string state = recordingState.Value.RecordingState.ToString();
    //logger.LogInformation($"Recording Status:->  {state}");

    //await Task.Delay(5000);
    //await callAutomationClient.GetCallRecording().ResumeAsync(recordingId);
    //logger.LogInformation($"Recording is resumed.");
    //recordingState = await callAutomationClient.GetCallRecording().GetStateAsync(recordingId);
    //state = recordingState.Value.RecordingState.ToString();
    //logger.LogInformation($"Recording Status:->  {state}");

    //await Task.Delay(5000);
    //await callAutomationClient.GetCallRecording().StopAsync(recordingId);
    //logger.LogInformation($"Recording is Stopped.");
    #endregion

    //HangUp call
    //await callConnection.HangUpAsync(true);

});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

async Task StartRecognizing(CallMedia callMedia, string content, string targetParticipant, bool dtmf, string context = "")
{
    var playSource = new TextSource(content) { VoiceName = SpeechToTextVoice };
    var playSources = new List<PlaySource>() { new TextSource("Recognize Prompt One") { VoiceName = SpeechToTextVoice }, new TextSource("Recognize Prompt Two") { VoiceName = SpeechToTextVoice }, new TextSource(content) { VoiceName = SpeechToTextVoice } };

    var recognizeChoiceOptions =
        new CallMediaRecognizeChoiceOptions(targetParticipant: new PhoneNumberIdentifier(targetParticipant), GetChoices())
        {
            InterruptCallMediaOperation = false,
            InterruptPrompt = false,
            InitialSilenceTimeout = TimeSpan.FromSeconds(10),
            Prompt = playSource,
            //PlayPrompts = playSources,
            OperationContext = context
        };

    var recognizeDtmfOptions =
   new CallMediaRecognizeDtmfOptions(
       targetParticipant: new PhoneNumberIdentifier(targetParticipant), 4)
   {
       InterruptPrompt = false,
       InitialSilenceTimeout = TimeSpan.FromSeconds(15),
       Prompt = playSource,
       OperationContext = "dtmfContext",
       InterToneTimeout = TimeSpan.FromSeconds(5)
   };

    CallMediaRecognizeOptions recognizeOptions = dtmf ? recognizeDtmfOptions : recognizeChoiceOptions;
    await callMedia.StartRecognizingAsync(recognizeOptions);
}
async Task StartContinuousDtmfAsync(CallMedia callMedia)
{
    await callMedia.StartContinuousDtmfRecognitionAsync(CommunicationIdentifier.FromRawId(targetPhonenumber));
    Console.WriteLine("Continuous Dtmf recognition started. Press one on dialpad.");
}

async Task StopContinuousDtmfAsync(CallMedia callMedia)
{
    await callMedia.StopContinuousDtmfRecognitionAsync(CommunicationIdentifier.FromRawId(targetPhonenumber));
    Console.WriteLine("Continuous Dtmf recognition stopped. Wait for sending dtmf tones.");
}

async Task StartSendingDtmfToneAsync(CallMedia callMedia)
{
    List<DtmfTone> tones = new List<DtmfTone>
        {
            DtmfTone.Zero,
            DtmfTone.One
        };

    await callMedia.SendDtmfTonesAsync(tones, CommunicationIdentifier.FromRawId(targetPhonenumber));
    Console.WriteLine("Send dtmf tones started. Respond over phone.");
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

async Task HandlePlayAsync(CallMedia callConnectionMedia, string text, CommunicationIdentifier target)
{
    Console.WriteLine($"Playing text to customer: {text}.");

    // Play goodbye message
    var GoodbyePlaySource = new TextSource(text)
    {
        VoiceName = "en-US-NancyNeural"
    };

    PlayToAllOptions playOptions = new PlayToAllOptions(GoodbyePlaySource)
    {
        //InterruptCallMediaOperation = false,
        OperationCallbackUri = new Uri(callbackUriHost),
        Loop = false,
    };

    var playSource = new FileSource(new Uri("https://www2.cs.uic.edu/~i101/SoundFiles/StarWars3.wav"));
    var playTo = new List<CommunicationIdentifier> { target };
    await callConnectionMedia.PlayAsync(playSource, playTo);

    //await callConnectionMedia.PlayToAllAsync(playOptions);
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

async Task HandlePlayAllAsync(CallMedia callConnectionMedia, string text)
{
    Console.WriteLine($"Playing text to customer: {text}.");

    // Play goodbye message
    var GoodbyePlaySource = new TextSource(text)
    {
        VoiceName = "en-US-NancyNeural"
    };

    PlayToAllOptions playOptions = new PlayToAllOptions(GoodbyePlaySource)
    {
        //InterruptCallMediaOperation = false,
        OperationCallbackUri = new Uri(callbackUriHost),
        Loop = false,
    };
    await callConnectionMedia.PlayToAllAsync(playOptions);
}

app.Run();
