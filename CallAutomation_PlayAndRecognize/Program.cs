using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Your ACS resource connection string
        var acsConnectionString = "<ACS_CONNECTION_STRING>";

        // Base url of the app
        var callbackUriHost = "<CALLBACK_URI_HOST_WITH_PROTOCOL>";

        // Cognitive Service endpoint URI
        var cognitiveServiceEndpoint = "<COGNITIVE_SERVICE_ENDPOINT_URI>";

        // Whether to use a phone number or ACS user ID
        var usePhone = false;

        // When usePhone is false: target ACS user id you want to receive the call.
        var targetUserId = "<TARGET_USER_ID>";

        // When usePhone is true: ACS resource phone number will act as source number to start outbound call
        var acsPhonenumber = "<ACS_PHONE_NUMBER>";

        // When usePhone is true: target phone number you want to receive the call.
        var targetPhonenumber = "<TARGET_PHONE_NUMBER>";

        var operation = "RecognizeSpeechOrDtmf";


        CommunicationIdentifier targetParticipant;
        CallInvite callInvite;
        if (usePhone)
        {
            var targetPhoneNumberIdentifier = new PhoneNumberIdentifier(targetPhonenumber);
            targetParticipant = targetPhoneNumberIdentifier;
            var callerPhoneNumberIdentifier = new PhoneNumberIdentifier(acsPhonenumber);
            callInvite = new CallInvite(targetPhoneNumberIdentifier, callerPhoneNumberIdentifier);
        }
        else
        {
            var targetCommunicationUserIdentifier = new CommunicationUserIdentifier(targetUserId);
            targetParticipant = targetCommunicationUserIdentifier;
            callInvite = new CallInvite(targetCommunicationUserIdentifier);
        }
        var audioUri = callbackUriHost + "/prompt.wav";


        var callAutomationClient = new CallAutomationClient(acsConnectionString);
        var app = builder.Build();

        app.MapGet("/outboundCall", async (ILogger<Program> logger) =>
        {
            var createCallOptions = new CreateCallOptions(callInvite, new Uri(callbackUriHost + "/api/callbacks"))
            {
                CognitiveServicesEndpoint = new Uri(cognitiveServiceEndpoint)
            };
            var createCallResult = await callAutomationClient.CreateCallAsync(createCallOptions);
            logger.LogInformation("CreateCallAsync result: {createCallResult}", createCallResult);
            return Results.Redirect("/index.html");
        });

        app.MapPost("/api/callbacks", async (CloudEvent[] cloudEvents, ILogger<Program> logger) =>
        {
            foreach (var cloudEvent in cloudEvents)
            {
                CallAutomationEventBase acsEvent = CallAutomationEventParser.Parse(cloudEvent);
                logger.LogInformation("Received event {eventName} for call connection id {callConnectionId}", acsEvent?.GetType().Name, acsEvent?.CallConnectionId);
                var callConnectionId = acsEvent?.CallConnectionId;

                if (acsEvent is CallConnected)
                {
                    if (operation == "PlayFile")
                    {
                        var playSource = new FileSource(new Uri(audioUri));
                        var playTo = new List<CommunicationIdentifier> { targetParticipant };
                        var playAsyncResult = await callAutomationClient.GetCallConnection(callConnectionId).GetCallMedia().PlayAsync(playSource, playTo);
                        logger.LogInformation("PlayAsync result: {playAsyncResult}", playAsyncResult);
                    }
                    else if (operation == "PlayTextWithKind")
                    {
                        String textToPlay = "Welcome to Contoso";

                        // Provide SourceLocale and VoiceKind to select an appropriate voice. SourceLocale or VoiceName needs to be provided.
                        var playSource = new TextSource(textToPlay, "en-US", VoiceKind.Female);
                        var playTo = new List<CommunicationIdentifier> { targetParticipant };
                        var playAsyncResult = await callAutomationClient.GetCallConnection(callConnectionId).GetCallMedia().PlayAsync(playSource, playTo);
                        logger.LogInformation("PlayAsync result: {playAsyncResult}", playAsyncResult);
                    }
                    else if (operation == "PlayTextWithVoice")
                    {
                        String textToPlay = "Welcome to Contoso";

                        // Provide VoiceName to select a specific voice. SourceLocale or VoiceName needs to be provided.
                        var playSource = new TextSource(textToPlay, "en-US-ElizabethNeural");
                        var playTo = new List<CommunicationIdentifier> { targetParticipant };
                        var playAsyncResult = await callAutomationClient.GetCallConnection(callConnectionId).GetCallMedia().PlayAsync(playSource, playTo);
                        logger.LogInformation("PlayAsync result: {playAsyncResult}", playAsyncResult);
                    }
                    else if (operation == "PlaySSML")
                    {
                        String ssmlToPlay = "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"><voice name=\"en-US-JennyNeural\">Hello World!</voice></speak>";

                        var playSource = new SsmlSource(ssmlToPlay);
                        var playTo = new List<CommunicationIdentifier> { targetParticipant };
                        var playResult = await callAutomationClient.GetCallConnection(callConnectionId)
                            .GetCallMedia()
                            .PlayAsync(playSource, playTo);
                        logger.LogInformation("PlayAsync result: {playAsyncResult}", playResult);
                    }
                    else if (operation == "PlayToAllAsync")
                    {
                        String textToPlay = "Welcome to Contoso";
                        var playSource = new TextSource(textToPlay, "en-US-ElizabethNeural");
                        var playResult = await callAutomationClient.GetCallConnection(callConnectionId)
                            .GetCallMedia()
                            .PlayToAllAsync(playSource);
                        logger.LogInformation("PlayToAllAsync result: {playToAllAsyncResult}", playResult);
                    }
                    else if (operation == "PlayLoop")
                    {
                        String textToPlay = "Welcome to Contoso";
                        var playSource = new TextSource(textToPlay, "en-US-ElizabethNeural");
                        var playOptions = new PlayToAllOptions(playSource)
                        {
                            Loop = true
                        };
                        var playResult = await callAutomationClient.GetCallConnection(callConnectionId)
                            .GetCallMedia()
                            .PlayToAllAsync(playOptions);
                        logger.LogInformation("PlayToAllAsync result: {playToAllAsyncResult}", playResult);
                    }
                    else if (operation == "PlayWithCache")
                    {
                        var playTo = new List<CommunicationIdentifier> { targetParticipant };
                        var playSource = new FileSource(new Uri(audioUri))
                        {
                            PlaySourceCacheId = "<playSourceId>"
                        };
                        var playResult = await callAutomationClient.GetCallConnection(callConnectionId)
                            .GetCallMedia()
                            .PlayAsync(playSource, playTo);
                        logger.LogInformation("PlayAsync result: {playAsyncResult}", playResult);
                    }
                    else if (operation == "CancelMedia")
                    {
                        var cancelResult = await callAutomationClient.GetCallConnection(callConnectionId)
                            .GetCallMedia()
                            .CancelAllMediaOperationsAsync();
                        logger.LogInformation("CancelAllMediaOperationsAsync result: {cancelResult}", cancelResult);
                    }
                    else if (operation == "RecognizeDTMF")
                    {
                        var maxTonesToCollect = 3;
                        String textToPlay = "Welcome to Contoso, please enter 3 DTMF.";
                        var playSource = new TextSource(textToPlay, "en-US-ElizabethNeural");
                        var recognizeOptions = new CallMediaRecognizeDtmfOptions(targetParticipant, maxTonesToCollect)
                        {
                            InitialSilenceTimeout = TimeSpan.FromSeconds(30),
                            Prompt = playSource,
                            InterToneTimeout = TimeSpan.FromSeconds(5),
                            InterruptPrompt = true,
                            StopTones = new DtmfTone[] { DtmfTone.Pound },
                        };
                        var recognizeResult = await callAutomationClient.GetCallConnection(callConnectionId)
                            .GetCallMedia()
                            .StartRecognizingAsync(recognizeOptions);
                        logger.LogInformation("StartRecognizingAsync result: {recognizeResult}", recognizeResult);
                    }
                    else if (operation == "RecognizeChoice")
                    {
                        var choices = new List<RecognitionChoice>
{
    new RecognitionChoice("Confirm", new List<string> { "Confirm", "First", "One"})
    {
        Tone = DtmfTone.One
    },
    new RecognitionChoice("Cancel", new List<string> { "Cancel", "Second", "Two"})
    {
        Tone = DtmfTone.Two
    }
};
                        String textToPlay = "Hello, This is a reminder for your appointment at 2 PM, Say Confirm to confirm your appointment or Cancel to cancel the appointment. Thank you!";
                        var playSource = new TextSource(textToPlay, "en-US-ElizabethNeural");
                        var recognizeOptions = new CallMediaRecognizeChoiceOptions(targetParticipant, choices)
                        {
                            InterruptPrompt = true,
                            InitialSilenceTimeout = TimeSpan.FromSeconds(30),
                            Prompt = playSource,
                            OperationContext = "AppointmentReminderMenu"
                        };
                        var recognizeResult = await callAutomationClient.GetCallConnection(callConnectionId)
                            .GetCallMedia()
                            .StartRecognizingAsync(recognizeOptions);
                        logger.LogInformation("StartRecognizingAsync result: {recognizeResult}", recognizeResult);
                    }
                    else if (operation == "RecognizeSpeech")
                    {
                        String textToPlay = "Hi, how can I help you today?";
                        var playSource = new TextSource(textToPlay, "en-US-ElizabethNeural");
                        var recognizeOptions = new CallMediaRecognizeSpeechOptions(targetParticipant)
                        {
                            Prompt = playSource,
                            EndSilenceTimeout = TimeSpan.FromMilliseconds(1000),
                            OperationContext = "OpenQuestionSpeech"
                        };
                        var recognizeResult = await callAutomationClient.GetCallConnection(callConnectionId)
                            .GetCallMedia()
                            .StartRecognizingAsync(recognizeOptions);
                        logger.LogInformation("StartRecognizingAsync result: {recognizeResult}", recognizeResult);
                    }
                    else if (operation == "RecognizeSpeechOrDtmf")
                    {
                        var maxTonesToCollect = 1;
                        String textToPlay = "Hi, how can I help you today, you can press 0 to speak to an agent?";
                        var playSource = new TextSource(textToPlay, "en-US-ElizabethNeural");
                        var recognizeOptions = new CallMediaRecognizeSpeechOrDtmfOptions(targetParticipant, maxTonesToCollect)
                        {
                            Prompt = playSource,
                            EndSilenceTimeout = TimeSpan.FromMilliseconds(1000),
                            InitialSilenceTimeout = TimeSpan.FromSeconds(30),
                            InterruptPrompt = true,
                            OperationContext = "OpenQuestionSpeechOrDtmf"
                        };
                        var recognizeResult = await callAutomationClient.GetCallConnection(callConnectionId)
                            .GetCallMedia()
                            .StartRecognizingAsync(recognizeOptions);
                        logger.LogInformation("StartRecognizingAsync result: {recognizeResult}", recognizeResult);
                    }

                }
                if (acsEvent is PlayCompleted playCompleted)
                {
                    logger.LogInformation("Play completed successfully, context={context}", playCompleted.OperationContext);
                }
                if (acsEvent is PlayFailed playFailed)
                {
                    if (MediaEventReasonCode.PlayDownloadFailed.Equals(playFailed.ReasonCode))
                    {
                        logger.LogInformation("Play failed: download failed, context={context}", playFailed.OperationContext);
                    }
                    else if (MediaEventReasonCode.PlayInvalidFileFormat.Equals(playFailed.ReasonCode))
                    {
                        logger.LogInformation("Play failed: invalid file format, context={context}", playFailed.OperationContext);
                    }
                    else
                    {
                        logger.LogInformation("Play failed, result={result}, context={context}", playFailed.ResultInformation?.Message, playFailed.OperationContext);
                    }
                }
                if (acsEvent is PlayCanceled playCanceled)
                {
                    logger.LogInformation("Play canceled, context={context}", playCanceled.OperationContext);
                }
                if (acsEvent is RecognizeCompleted recognizeCompleted)
                {
                    switch (recognizeCompleted.RecognizeResult)
                    {
                        case DtmfResult dtmfResult:
                            //Take action for Recognition through DTMF
                            var tones = dtmfResult.Tones;
                            logger.LogInformation("Recognize completed succesfully, tones={tones}", tones);
                            break;
                        case ChoiceResult choiceResult:
                            // Take action for Recognition through Choices
                            var labelDetected = choiceResult.Label;
                            var phraseDetected = choiceResult.RecognizedPhrase;
                            // If choice is detected by phrase, choiceResult.RecognizedPhrase will have the phrase detected,
                            // If choice is detected using dtmf tone, phrase will be null
                            logger.LogInformation("Recognize completed succesfully, labelDetected={labelDetected}, phraseDetected={phraseDetected}", labelDetected, phraseDetected);
                            break;
                        case SpeechResult speechResult:
                            // Take action for Recognition through Choices
                            var text = speechResult.Speech;
                            logger.LogInformation("Recognize completed succesfully, text={text}", text);
                            break;
                        default:
                            logger.LogInformation("Recognize completed succesfully, recognizeResult={recognizeResult}", recognizeCompleted.RecognizeResult);
                            break;
                    }
                }
                if (acsEvent is RecognizeFailed recognizeFailed)
                {
                    if (MediaEventReasonCode.RecognizeInitialSilenceTimedOut.Equals(recognizeFailed.ReasonCode))
                    {
                        // Take action for time out
                        logger.LogInformation("Recognition failed: initial silencev time out");
                    }
                    else if (MediaEventReasonCode.RecognizeSpeechOptionNotMatched.Equals(recognizeFailed.ReasonCode))
                    {
                        // Take action for option not matched
                        logger.LogInformation("Recognition failed: speech option not matched");
                    }
                    else if (MediaEventReasonCode.RecognizeIncorrectToneDetected.Equals(recognizeFailed.ReasonCode))
                    {
                        // Take action for incorrect tone
                        logger.LogInformation("Recognition failed: incorrect tone detected");
                    }
                    else
                    {
                        logger.LogInformation("Recognition failed, result={result}, context={context}", recognizeFailed.ResultInformation?.Message, recognizeFailed.OperationContext);
                    }
                }
            }
            return Results.Ok();
        }).Produces(StatusCodes.Status200OK);

        app.UseStaticFiles();

        app.Run();
    }
}
