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
const string MainMenu ="Say confirm or cancel to proceed further";
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

CallAutomationClient callAutomationClient = new CallAutomationClient(new Uri("https://x-pma-uswe-07.plat.skype.com"), acsConnectionString);
var app = builder.Build();

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

        logger.LogInformation($"CORRELATION ID : {parsedEvent.CorrelationId}");

        var callMedia = callConnection.GetCallMedia();

        if (parsedEvent is CallConnected callConnected)
        {
            logger.LogInformation($"CallConnected event Received.");
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
           
        }
        
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

app.MapPost("/connectApi", async (ILogger<Program> logger) =>
{
    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    CallLocator callLocator = new RoomCallLocator("99431062370875888");
    //CallLocator callLocator = new GroupCallLocator("29228d3e-040e-4656-a70e-890ab4e173e5");
    //CallLocator callLocator = new ServerCallLocator("aHR0cHM6Ly9hcGkuZmxpZ2h0cHJveHkuc2t5cGUuY29tL2FwaS92Mi9jcC9jb252LWpwd2UtMDUtcHJvZC1ha3MuY29udi5za3lwZS5jb20vY29udi81bnRQMDIxeDdFYWdObUJEOHlpZ3hBP2k9MTAtNjAtMTItMTExJmU9NjM4NTQ1OTgzMDA1MTQxNjUw");
    ConnectCallOptions connectCallOptions = new ConnectCallOptions(callLocator, callbackUri)
    {
        CallIntelligenceOptions = new CallIntelligenceOptions()
        {
            CognitiveServicesEndpoint = new Uri(cognitiveServiceEndpoint)
        }
    };


    ConnectCallResult result = await callAutomationClient.ConnectCallAsync(connectCallOptions);
    logger.LogInformation($"CALL CONNECTION ID : {result.CallConnectionProperties.CallConnectionId}");

    var callConnection = callAutomationClient.GetCallConnection(result.CallConnectionProperties.CallConnectionId);
    //CommunicationUserIdentifier target = new CommunicationUserIdentifier("8:acs:19ae37ff-1a44-4e19-aade-198eedddbdf2_00000020-f97d-9169-28c5-593a0d004d5f");
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

    //// prepare dtmf tones
    //var recognizeOptions = GetMediaRecognizeDTMFOptions(dtmfPrompt, targetPhonenumber, string.Empty);

    //// prepare Speech tones
    //var recognizeOptions = GetMediaRecognizeSpeechOptions(MainMenu, targetPhonenumber, string.Empty);

    //// prepare Speech or dtmf tones
    //var recognizeOptions = GetMediaRecognizeSpeechOrDtmfOptions("Say conform or press one two three on you keypad", targetPhonenumber, string.Empty);

    // prepare Choice options for pstn user
    var recognizeOptions = GetMediaRecognizeChoiceOptions(MainMenu, targetPhonenumber, string.Empty);

    //// prepare Choice options for ACS user
    //var recognizeOptions = GetMediaRecognizeChoiceOptions(MainMenu, participants.Value.LastOrDefault()?.Identifier.ToString(), string.Empty);

    // Send request to recognize tones
    callMedia.StartRecognizing(recognizeOptions);

    await Task.Delay(5000);
    await HandlePlayAsync(callMedia, "Connect API play started test", participants.Value.FirstOrDefault()?.Identifier);

    #region Mute participant
    //var muteParticipant = participants.Value[1]?.Identifier;
    //var muteResponse = await callConnection.MuteParticipantAsync(muteParticipant);

    //if (muteResponse.GetRawResponse().Status == 200)
    //{
    //    logger.LogInformation("Participant is muted. Waiting for confirmation...");
    //    var participant = await callConnection.GetParticipantAsync(muteParticipant);
    //    logger.LogInformation($"Is participant muted: {participant.Value.IsMuted}");
    //    logger.LogInformation("Mute participant test completed.");
    //}
    #endregion
    //Start Continuous Dtmf
    await StartContinuousDtmfAsync(callMedia);
    await Task.Delay(5000);

    //Stop Continuous Dtmf
    await StopContinuousDtmfAsync(callMedia);

    #region Recording
    StartRecordingOptions recordingOptions = new StartRecordingOptions(new ServerCallLocator(callConnection.GetCallConnectionProperties().Value.ServerCallId))
    {
        RecordingContent = RecordingContent.Audio,
        RecordingChannel = RecordingChannel.Unmixed,
        RecordingFormat = RecordingFormat.Wav
    };
    var recordingTask = await callAutomationClient.GetCallRecording().StartAsync(recordingOptions);
    var recordingId = recordingTask.Value.RecordingId;
    logger.LogInformation($"Call recording id--> {recordingId}");
    await Task.Delay(5000);

    await callAutomationClient.GetCallRecording().PauseAsync(recordingId);
    logger.LogInformation($"Recording is Paused.");
    var recordingState = await callAutomationClient.GetCallRecording().GetStateAsync(recordingId);
    string state = recordingState.Value.RecordingState.ToString();
    logger.LogInformation($"Recording Status:->  {state}");

    await Task.Delay(5000);
    await callAutomationClient.GetCallRecording().ResumeAsync(recordingId);
    logger.LogInformation($"Recording is resumed.");
    recordingState = await callAutomationClient.GetCallRecording().GetStateAsync(recordingId);
    state = recordingState.Value.RecordingState.ToString();
    logger.LogInformation($"Recording Status:->  {state}");

    await Task.Delay(5000);
    await callAutomationClient.GetCallRecording().StopAsync(recordingId);
    logger.LogInformation($"Recording is Stopped.");
    #endregion

    //Remove participant
    var removeParticipant = await callConnection.RemoveParticipantAsync(target);

    //HangUp call
    await callConnection.HangUpAsync(true);

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


CallMediaRecognizeChoiceOptions GetMediaRecognizeChoiceOptions(string content, string targetParticipant, string context = "")
{
    var playSource = new TextSource(content) { VoiceName = SpeechToTextVoice };
    var recognizeOptions =
        new CallMediaRecognizeChoiceOptions(targetParticipant: new PhoneNumberIdentifier(targetParticipant), GetChoices())
        {
            InterruptCallMediaOperation = false,
            InterruptPrompt = false,
            InitialSilenceTimeout = TimeSpan.FromSeconds(10),
            Prompt = playSource,
            //PlayPrompts = playSources,
            OperationContext = context
        };

    return recognizeOptions;
}

CallMediaRecognizeDtmfOptions GetMediaRecognizeDTMFOptions(string content, string targetParticipant, string context = "")
{
    var playSource = new TextSource(content) { VoiceName = SpeechToTextVoice };
   
    var recognizeOptions =
                new CallMediaRecognizeDtmfOptions(
                    targetParticipant: new PhoneNumberIdentifier(targetParticipant), maxTonesToCollect: 8)
                {
                    InterruptPrompt = false,
                    InterToneTimeout = TimeSpan.FromSeconds(5),
                    OperationContext = context,
                    InitialSilenceTimeout = TimeSpan.FromSeconds(15),
                    Prompt = playSource
                };
    return recognizeOptions;
}

CallMediaRecognizeSpeechOptions GetMediaRecognizeSpeechOptions(string content, string targetParticipant, string context = "")
{
    var playSource = new TextSource(content) { VoiceName = SpeechToTextVoice };
    
    var recognizeOptions =
                new CallMediaRecognizeSpeechOptions(
                    targetParticipant: new PhoneNumberIdentifier(targetParticipant))
                {
                    InterruptPrompt = false,
                    OperationContext = context,
                    InitialSilenceTimeout = TimeSpan.FromSeconds(15),
                    Prompt = playSource,
                    EndSilenceTimeout = TimeSpan.FromSeconds(15)
                };
    return recognizeOptions;
}

CallMediaRecognizeSpeechOrDtmfOptions GetMediaRecognizeSpeechOrDtmfOptions(string content, string targetParticipant, string context = "")
{
    var playSource = new TextSource(content) { VoiceName = SpeechToTextVoice };
   
    var recognizeOptions =
                new CallMediaRecognizeSpeechOrDtmfOptions(
                    targetParticipant: new PhoneNumberIdentifier(targetParticipant), maxTonesToCollect: 8)
                {
                    InterruptPrompt = false,
                    OperationContext = context,
                    InitialSilenceTimeout = TimeSpan.FromSeconds(15),
                    Prompt = playSource,
                    EndSilenceTimeout = TimeSpan.FromSeconds(5)
                };
    return recognizeOptions;
}

async Task StartContinuousDtmfAsync(CallMedia callMedia)
{
    await callMedia.StartContinuousDtmfRecognitionAsync(new PhoneNumberIdentifier(targetPhonenumber));
    Console.WriteLine("Continuous Dtmf recognition started. Press one on dialpad.");
}

async Task StopContinuousDtmfAsync(CallMedia callMedia)
{
    await callMedia.StopContinuousDtmfRecognitionAsync(new PhoneNumberIdentifier(targetPhonenumber));
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
