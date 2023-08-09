using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Microsoft.Extensions.Logging;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Your ACS resource connection string
        var acsConnectionString = "<ACS_CONNECTION_STRING>";
        
        // Your ACS resource phone number will act as source number to start outbound call
        var acsPhonenumber = "<ACS_PHONE_NUMBER>";

        // Target phone number you want to receive the call.
        var targetPhonenumber = "<TARGET_PHONE_NUMBER>";

        // Base url of the app
        var callbackUriHost = "<CALLBACK_URI_HOST_WITH_PROTOCOL>";

        // Cognitive Service endpoint URI
        var cognitiveServiceEndpoint = "<COGNITIVE_SERVICE_ENDPOINT_URI>";

        var audioUri = callbackUriHost + "/prompt.wav";
        var playSourceCacheId = "1";


        var operation = "PlayFile";
        var playSourceType = "TextWithKind";

        var callAutomationClient = new CallAutomationClient(acsConnectionString);
        var app = builder.Build();

        app.MapGet("/outboundCall", async (ILogger<Program> logger) =>
        {
            var target = new PhoneNumberIdentifier(targetPhonenumber);
            var caller = new PhoneNumberIdentifier(acsPhonenumber);
            var callInvite = new CallInvite(target, caller);
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
                CallAutomationEventBase parsedEvent = CallAutomationEventParser.Parse(cloudEvent);
                logger.LogInformation("Received event {eventName} for call connection id {callConnectionId}", parsedEvent?.GetType().Name, parsedEvent?.CallConnectionId);
                var callConnection = callAutomationClient.GetCallConnection(parsedEvent?.CallConnectionId);
                var callMedia = callConnection.GetCallMedia();

                if (parsedEvent is CallConnected)
                {
                    if (operation == "PlayFile")
                    {
                        var playSource = new FileSource(new Uri(audioUri));
                        var playTo = new List<CommunicationIdentifier> { new PhoneNumberIdentifier(targetPhonenumber) };
                        var playAsyncResult = await callMedia.PlayAsync(playSource, playTo);
                        logger.LogInformation("PlayAsync result: {playAsyncResult}", playAsyncResult);
                    }
                    else if (operation == "PlayTextWithKind")
                    {
                        String textToPlay = "Welcome to Contoso";

                        //you can provide SourceLocale and VoiceKind to select an appropriate voice
                        var playSource = new TextSource(textToPlay, "en-US", VoiceKind.Female);
                        var playTo = new List<CommunicationIdentifier> { new PhoneNumberIdentifier(targetPhonenumber) };
                        var playAsyncResult = await callMedia.PlayAsync(playSource, playTo);
                        logger.LogInformation("PlayAsync result: {playAsyncResult}", playAsyncResult);
                    }
                    else if (operation == "PlayTextWithVoice")
                    {
                        String textToPlay = "Welcome to Contoso";

                        //you can provide VoiceName to select a specific voice
                        var playSource = new TextSource(textToPlay, "en-US-ElizabethNeural");
                        var playTo = new List<CommunicationIdentifier> { new PhoneNumberIdentifier(targetPhonenumber) };
                        var playAsyncResult = await callMedia.PlayAsync(playSource, playTo);
                        logger.LogInformation("PlayAsync result: {playAsyncResult}", playAsyncResult);
                    }
                    else if (operation == "PlaySSML")
                    {
                        String ssmlToPlay = "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"><voice name=\"en-US-JennyNeural\">Hello World!</voice></speak>";

                        var playSource = new SsmlSource(ssmlToPlay);
                        var playTo = new List<CommunicationIdentifier> { new PhoneNumberIdentifier(targetPhonenumber) };
                        var playAsyncResult = await callMedia.PlayAsync(playSource, playTo);
                        logger.LogInformation("PlayAsync result: {playAsyncResult}", playAsyncResult);
                    }
                    else if (operation == "PlayToAllAsync")
                    {
                        String textToPlay = "Welcome to Contoso";
                        var playSource = new TextSource(textToPlay, "en-US-ElizabethNeural");
                        var playToAllAsyncResult = await callMedia.PlayToAllAsync(playSource);
                        logger.LogInformation("PlayToAllAsync result: {playToAllAsyncResult}", playToAllAsyncResult);
                    }
                    else if (operation == "PlayLoop")
                    {
                        String textToPlay = "Welcome to Contoso";
                        var playSource = new TextSource(textToPlay, "en-US-ElizabethNeural");
                        var playOptions = new PlayToAllOptions(playSource)
                        {
                            Loop = true,
                            OperationContext = "PlayAudio"
                        };
                        var playToAllAsyncResult = await callMedia.PlayToAllAsync(playOptions);
                        logger.LogInformation("PlayToAllAsync result: {playToAllAsyncResult}", playToAllAsyncResult);
                    }
                    else if (operation == "PlayWithCache")
                    {
                        var playSource = new FileSource(new Uri(audioUri))
                        {
                            PlaySourceCacheId = playSourceCacheId
                        };
                        var playTo = new List<CommunicationIdentifier> { new PhoneNumberIdentifier(targetPhonenumber) };
                        var playAsyncResult = await callMedia.PlayAsync(playSource, playTo);
                        logger.LogInformation("PlayAsync result: {playAsyncResult}", playAsyncResult);
                    }
                    else if (operation == "CancelMedia")
                    {
                        var cancelResult = await callMedia.CancelAllMediaOperationsAsync();
                        logger.LogInformation("CancelAllMediaOperationsAsync result: {cancelResult}", cancelResult);
                    }
                    else if (operation == "RecognizeDTMF")
                    {
                        var targetParticipant = new PhoneNumberIdentifier(targetPhonenumber);
                        var maxTonesToCollect = 3;
                        String textToPlay = "Welcome to Contoso";
                        var playSource = new TextSource(textToPlay);
                        var recognizeOptions = new CallMediaRecognizeDtmfOptions(targetParticipant, maxTonesToCollect)
                        {
                            InterruptCallMediaOperation = true,
                            InitialSilenceTimeout = TimeSpan.FromSeconds(30),
                            Prompt = playSource,
                            InterToneTimeout = TimeSpan.FromSeconds(5),
                            InterruptPrompt = true,
                            StopTones = new DtmfTone[] { DtmfTone.Pound },
                        };
                        var recognizeResult = await callMedia.StartRecognizingAsync(recognizeOptions);
                        logger.LogInformation("StartRecognizingAsync result: {recognizeResult}", recognizeResult);
                    }
                    else if (operation == "RecognizeChoice")
                    {
                        var targetParticipant = new PhoneNumberIdentifier(targetPhonenumber);
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
                        var playSource = new TextSource(textToPlay);
                        var recognizeOptions = new CallMediaRecognizeChoiceOptions(targetParticipant, choices)
                        {
                            InterruptPrompt = true,
                            InitialSilenceTimeout = TimeSpan.FromSeconds(30),
                            Prompt = playSource,
                            OperationContext = "AppointmentReminderMenu"
                        };
                        var recognizeResult = await callMedia.StartRecognizingAsync(recognizeOptions);
                        logger.LogInformation("StartRecognizingAsync result: {recognizeResult}", recognizeResult);
                    }
                    else if (operation == "RecognizeSpeech")
                    {
                        var targetParticipant = new PhoneNumberIdentifier(targetPhonenumber);
                        String textToPlay = "Hi, how can I help you today?";
                        var playSource = new TextSource(textToPlay);
                        var recognizeOptions = new CallMediaRecognizeSpeechOptions(targetParticipant)
                        {
                            InterruptCallMediaOperation = true,
                            Prompt = playSource,
                            EndSilenceTimeout = TimeSpan.FromMilliseconds(1000),
                            OperationContext = "OpenQuestionSpeech"
                        };
                        var recognizeResult = await callMedia.StartRecognizingAsync(recognizeOptions);
                        logger.LogInformation("StartRecognizingAsync result: {recognizeResult}", recognizeResult);
                    }

                }
                else if (parsedEvent is PlayCompleted playCompleted)
                {
                    logger.LogInformation("Play completed successfully, context={context}", playCompleted.OperationContext);
                }
                else if (parsedEvent is PlayFailed playFailed)
                {
                    logger.LogInformation("Play failed, ResultInformation: {resultInformation}, context={context}", playFailed.ResultInformation, playFailed.OperationContext);
                    await callConnection.HangUpAsync(true);
                }
                else if (parsedEvent is PlayCanceled playCanceled)
                {
                    logger.LogInformation("Play canceled");
                }
                else if (parsedEvent is RecognizeCompleted recognizeCompleted)
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
                else if (parsedEvent is RecognizeFailed recognizeFailed)
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
                }
            }
            return Results.Ok();
        }).Produces(StatusCodes.Status200OK);

        app.UseStaticFiles();

        app.Run();
    }
}
