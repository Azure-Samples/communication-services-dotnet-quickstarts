using Azure;
using Azure.AI.OpenAI;
using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Get ACS Connection String from appsettings.json
var acsConnectionString = builder.Configuration.GetValue<string>("AcsConnectionString");
ArgumentNullException.ThrowIfNullOrEmpty(acsConnectionString);

//Call Automation Client
var client = new CallAutomationClient(pmaEndpoint: new Uri("https://x-pma-uswe-04.plat.skype.com:6448"), connectionString: acsConnectionString);

//Grab the Cognitive Services endpoint from appsettings.json
var cognitiveServicesEndpoint = builder.Configuration.GetValue<string>("CognitiveServiceEndpoint");
ArgumentNullException.ThrowIfNullOrEmpty(cognitiveServicesEndpoint);

string helloPrompt = "Hello, thank you for calling Contoso Utility service.This call is being recorded for quality purpose.";
string timeoutSilencePrompt = "I’m sorry, I didn’t hear anything.";
string askDobPrompt = "Please type your date of birth in date, month, year format, for exmple 01011990";
string dobUpdatePrompt = "Thanks for updating the date of birth.";
string goodbyePrompt = "Thank you for calling! Goodbye";
string askDobRetryPrompt = "I’m sorry, Date of birth is in incorrect format. Please provide your date of birth in date, month, year in numeric format.";
string goodbyeContext = "Goodbye";
string helloContext = "HelloContext";
string askDobContext = "AskDobContext";
string dobReceivedContext = "DobReceivedContext";
string callTransferFailurePrompt = "It looks like all I can’t connect you to an agent right now, but we will get the next available agent to call you back as soon as possible.";
string transferFailedContext = "TransferFailed";
bool isTrasncriptionActive = false;
List<DtmfTone> NumberTones = new List<DtmfTone>()
{
    DtmfTone.Zero,
    DtmfTone.One,
    DtmfTone.Two,
    DtmfTone.Three,
    DtmfTone.Four,
    DtmfTone.Five,
    DtmfTone.Six,
    DtmfTone.Seven,
    DtmfTone.Eight,
    DtmfTone.Nine
};

var key = builder.Configuration.GetValue<string>("AzureOpenAIServiceKey");
ArgumentNullException.ThrowIfNullOrEmpty(key);

var endpoint = builder.Configuration.GetValue<string>("AzureOpenAIServiceEndpoint");
ArgumentNullException.ThrowIfNullOrEmpty(endpoint);

var ai_client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));

var transportUrl = builder.Configuration.GetValue<string>("TransportUrl");
ArgumentNullException.ThrowIfNullOrEmpty(transportUrl);

//Register and make CallAutomationClient accessible via dependency injection
builder.Services.AddSingleton(client);
builder.Services.AddSingleton(ai_client);
var app = builder.Build();

var devTunnelUri = builder.Configuration.GetValue<string>("DevTunnelUri");
ArgumentNullException.ThrowIfNullOrEmpty(devTunnelUri);

var agentPhoneNumber = builder.Configuration.GetValue<string>("AgentPhoneNumber");
ArgumentNullException.ThrowIfNullOrEmpty(devTunnelUri);
var maxTimeout = 2;

string recordingId = string.Empty;
string recordingLocation = string.Empty;

app.MapPost("/api/incomingCall", async (
    [FromBody] EventGridEvent[] eventGridEvents,
    ILogger<Program> logger) =>
{
    foreach (var eventGridEvent in eventGridEvents)
    {
        logger.LogInformation("Call event received:{eventType}", eventGridEvent.EventType);

        // Handle system events
        if (eventGridEvent.TryGetSystemEventData(out object eventData))
        {
            // Handle the subscription validation event.
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
            var callbackUri = new Uri(new Uri(devTunnelUri), $"/api/callbacks/{Guid.NewGuid()}?callerId={callerId}");
            logger.LogInformation("Incoming call corelationId: {corid} context: {incon}, Callback url: {callbackuri}, transport Url: {transurl}",
                incomingCallEventData.CorrelationId, incomingCallEventData.IncomingCallContext, callbackUri, transportUrl);

            TranscriptionOptions transcriptionOptions = new TranscriptionOptions(
                new Uri(transportUrl),
                TranscriptionTransport.Websocket,
                "en-CA", false);

            var options = new AnswerCallOptions(incomingCallEventData.IncomingCallContext, callbackUri)
            {
                CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
                // AzureCognitiveServicesEndpointUri = new Uri(cognitiveServicesEndpoint),
               // TranscriptionOptions = transcriptionOptions
            };

            AnswerCallResult answerCallResult = await client.AnswerCallAsync(options);
            var callConnectionMedia = answerCallResult.CallConnection.GetCallMedia();

            //Use EventProcessor to process CallConnected event
            var answer_result = await answerCallResult.WaitForEventProcessorAsync();
            if (answer_result.IsSuccess)
            {
                logger.LogInformation(
                "Received call event: {type}, callConnectionID: {connId}, serverCallId: {serverId}",
                answer_result.GetType(),
                answer_result.SuccessResult.CallConnectionId,
                answer_result.SuccessResult.ServerCallId);

                /* Start the recording */
                CallLocator callLocator = new ServerCallLocator(answer_result.SuccessResult.ServerCallId);
                var recordingResult = await client.GetCallRecording().StartAsync(new StartRecordingOptions(callLocator));
                recordingId = recordingResult.Value.RecordingId;
                logger.LogInformation("Recording started. RecordingId: {recid}", recordingId);

                /* Start the Transcription */
               // await InitiateTranscription(callConnectionMedia);
               //  Console.WriteLine("Transcription initiated.");

                /* Play hello prompt to user */
                await HandlePlayAsync(callConnectionMedia, helloPrompt, helloContext);
            }
            client.GetEventProcessor().AttachOngoingEventProcessor<PlayCompleted>(
                answerCallResult.CallConnection.CallConnectionId, async (playCompletedEvent) =>
            {
                logger.LogInformation(
                 "Received call event: {type}, callConnectionID: {connId}, serverCallId: {serverId}, chatThreadId: {chatThreadId}",
                 playCompletedEvent.GetType(),
                 playCompletedEvent.CallConnectionId,
                 playCompletedEvent.ServerCallId,
                 playCompletedEvent.OperationContext);

                if (playCompletedEvent.OperationContext == helloContext)
                {
                    await PauseOrStopTranscriptionAndRecording(callConnectionMedia, logger, false, recordingId);
                    await HandleDtmfRecognizeAsync(client.GetCallConnection(playCompletedEvent.CallConnectionId).GetCallMedia(), callerId, askDobPrompt, askDobContext);
                }
                else if (playCompletedEvent.OperationContext == dobReceivedContext)
                {
                    await ResumeTranscriptionAndRecording(callConnectionMedia, logger, recordingId);
                    CommunicationIdentifier transferDestination = new PhoneNumberIdentifier(agentPhoneNumber);
                    TransferCallToParticipantResult result = await answerCallResult.CallConnection.TransferCallToParticipantAsync(transferDestination);
                    logger.LogInformation($"Transfer call initiated: {result.OperationContext}");

                    // await HandleRecognizeAsync(callConnectionMedia, callerId, nextStepPrompt, nextStepContext);
                }
                else if (playCompletedEvent.OperationContext == goodbyeContext ||
                playCompletedEvent.OperationContext == transferFailedContext)
                {
                    await PauseOrStopTranscriptionAndRecording(callConnectionMedia, logger, false, recordingId);
                    await answerCallResult.CallConnection.HangUpAsync(true);
                }
            });

            client.GetEventProcessor().AttachOngoingEventProcessor<RecognizeCompleted>(
                answerCallResult.CallConnection.CallConnectionId, async (recognizeCompletedEvent) =>
            {
                logger.LogInformation(
                "Received call event: {type}, callConnectionID: {connId}, serverCallId: {serverId}, chatThreadId: {chatThreadId}",
                recognizeCompletedEvent.GetType(),
                recognizeCompletedEvent.CallConnectionId,
                recognizeCompletedEvent.ServerCallId,
                recognizeCompletedEvent.OperationContext);

                if (!string.IsNullOrEmpty(recognizeCompletedEvent.OperationContext)
                    && recognizeCompletedEvent.OperationContext.Equals(askDobContext)
                    && recognizeCompletedEvent.RecognizeResult is DtmfResult)
                {
                    var dtmfResult = recognizeCompletedEvent.RecognizeResult as DtmfResult;

                    //Take action for Recognition through DTMF 
                    var tones = dtmfResult?.Tones;
                    bool hasMatch = tones.Select(x => x)
                           .Intersect(NumberTones)
                           .Any();
                    if (hasMatch)
                    {
                        await HandlePlayAsync(callConnectionMedia, dobUpdatePrompt, dobReceivedContext);
                    }
                    else
                    {
                        await HandleDtmfRecognizeAsync(callConnectionMedia, callerId, askDobRetryPrompt, askDobContext);
                    }
                }
                //else
                //{
                //    var speech_result = recognizeCompletedEvent.RecognizeResult as SpeechResult;
                //    var chatGPTResponse = await GetChatGPTResponse(speech_result.Speech);
                //    logger.LogInformation($"Chat GPT response: {chatGPTResponse}");
                //    await HandleRecognizeAsync(callConnectionMedia, callerId, chatGPTResponse, "openAIResponse");
                //}
            });

            client.GetEventProcessor().AttachOngoingEventProcessor<CallTransferAccepted>(
                answerCallResult.CallConnection.CallConnectionId, async (callTransferAcceptedEvent) =>
            {
                logger.LogInformation(
               "Received call event: {type}, callConnectionID: {connId}, subCode: {sCode}, message: {mess}, context: {con}",
               callTransferAcceptedEvent.GetType(),
               callTransferAcceptedEvent.CallConnectionId,
               callTransferAcceptedEvent.ResultInformation?.SubCode,
               callTransferAcceptedEvent.ResultInformation?.Message,
               callTransferAcceptedEvent.OperationContext);
            });
            client.GetEventProcessor().AttachOngoingEventProcessor<CallTransferFailed>(answerCallResult.CallConnection.CallConnectionId, async (callTransferFailedEvent) =>
            {
                var resultInformation = callTransferFailedEvent.ResultInformation;
                logger.LogError("Encountered error during call transfer, message={msg}, code={code}, subCode={subCode}",
                    resultInformation?.Message,
                    resultInformation?.Code,
                    resultInformation?.SubCode);

                await HandlePlayAsync(callConnectionMedia, callTransferFailurePrompt, transferFailedContext);

            });
            client.GetEventProcessor().AttachOngoingEventProcessor<PlayFailed>(
                answerCallResult.CallConnection.CallConnectionId, async (playFailedEvent) =>
            {
                logger.LogInformation(
               "Received call event: {type}, callConnectionID: {connId}, subCode: {sCode}, message: {mess}, context: {con}",
               playFailedEvent.GetType(),
               playFailedEvent.CallConnectionId,
               playFailedEvent.ResultInformation?.SubCode,
               playFailedEvent.ResultInformation?.Message,
               playFailedEvent.OperationContext);

                await PauseOrStopTranscriptionAndRecording(callConnectionMedia, logger, true, recordingId);
                await answerCallResult.CallConnection.HangUpAsync(true);
            });

            client.GetEventProcessor().AttachOngoingEventProcessor<RecognizeFailed>(
                answerCallResult.CallConnection.CallConnectionId, async (recognizeFailedEvent) =>
            {
                logger.LogInformation(
                "Received call event: {type}, callConnectionID: {connId}, subCode: {sCode}, message: {mess}, context: {con}",
                recognizeFailedEvent.GetType(),
                recognizeFailedEvent.CallConnectionId,
                recognizeFailedEvent.ResultInformation?.SubCode,
                recognizeFailedEvent.ResultInformation?.Message,
                recognizeFailedEvent.OperationContext);

                if (MediaEventReasonCode.RecognizeInitialSilenceTimedOut.Equals(recognizeFailedEvent.ResultInformation?.SubCode.Value.ToString()) && maxTimeout > 0)
                {
                    logger.LogInformation("Retrying recognize...");
                    maxTimeout--;
                    await HandleRecognizeAsync(callConnectionMedia, callerId, timeoutSilencePrompt, askDobContext);
                }
                else
                {
                    logger.LogInformation("Playing goodbye message...");
                    await HandlePlayAsync(callConnectionMedia, goodbyePrompt, goodbyeContext);
                }
            });

            client.GetEventProcessor().AttachOngoingEventProcessor<TranscriptionStarted>(
                answerCallResult.CallConnection.CallConnectionId, async (transcriptionStarted) =>
                {
                    logger.LogInformation(
                    "Received transcription event: {type}, callConnectionID: {connId}, subCode: {sode}, message: {message}",
                    transcriptionStarted.GetType(),
                    transcriptionStarted.CallConnectionId,
                    transcriptionStarted?.ResultInformation?.SubCode,
                    transcriptionStarted?.ResultInformation?.Message);
                });

            client.GetEventProcessor().AttachOngoingEventProcessor<TranscriptionResumed>(
                answerCallResult.CallConnection.CallConnectionId, async (transcriptionResumed) =>
                {
                    logger.LogInformation(
                    "Received transcription event: {type}, callConnectionID: {connId}, subCode: {sode}, message: {message}",
                    transcriptionResumed.GetType(),
                    transcriptionResumed.CallConnectionId,
                    transcriptionResumed?.ResultInformation?.SubCode,
                    transcriptionResumed?.ResultInformation?.Message);
                });

            client.GetEventProcessor().AttachOngoingEventProcessor<TranscriptionStopped>(
                answerCallResult.CallConnection.CallConnectionId, async (transcriptionStopped) =>
                {
                    isTrasncriptionActive = false;
                    logger.LogInformation(
                   "Received transcription event: {type}, callConnectionID: {connId}, subCode: {sode}, message: {message}",
                   transcriptionStopped.GetType(),
                   transcriptionStopped.CallConnectionId,
                   transcriptionStopped?.ResultInformation?.SubCode,
                   transcriptionStopped?.ResultInformation?.Message);
                });

            client.GetEventProcessor().AttachOngoingEventProcessor<TranscriptionFailed>(
                answerCallResult.CallConnection.CallConnectionId, async (TranscriptionFailed) =>
                {
                    logger.LogInformation(
                     "Received transcription event: {type}, callConnectionID: {connId}, subCode: {sode}, message: {message}",
                     TranscriptionFailed.GetType(),
                     TranscriptionFailed.CallConnectionId,
                     TranscriptionFailed?.ResultInformation?.SubCode,
                     TranscriptionFailed?.ResultInformation?.Message);
                });
        }

    }
    return Results.Ok();
});

// api to handle call back events
app.MapPost("/api/callbacks/{contextId}", async (
    [FromBody] CloudEvent[] cloudEvents,
    [FromRoute] string contextId,
    [Required] string callerId,
    CallAutomationClient callAutomationClient,
    ILogger<Program> logger) =>
{
    var eventProcessor = client.GetEventProcessor();
    logger.LogInformation($"calback event type: {cloudEvents.FirstOrDefault()?.Type}");
    eventProcessor.ProcessEvents(cloudEvents);
    return Results.Ok();
});

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
    client.GetCallRecording().DownloadTo(new Uri(recordingLocation), "testfile.wav");
    return Results.Ok();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

async Task ResumeTranscriptionAndRecording(CallMedia callMedia, ILogger logger, string recordingId)
{
    await InitiateTranscription(callMedia);
    logger.LogInformation("Transcription reinitiated.");

    await client.GetCallRecording().ResumeAsync(recordingId);
    logger.LogInformation($"Recording resumed. RecordingId: {recordingId}");
}

async Task PauseOrStopTranscriptionAndRecording(CallMedia callMedia, ILogger logger, bool stopRecording, string recordingId)
{
    if (isTrasncriptionActive)
    {
        await callMedia.StopTranscriptionAsync();
        logger.LogInformation("Transcription stopped.");
    }

    if (stopRecording)
    {
        await client.GetCallRecording().StopAsync(recordingId);
        logger.LogInformation($"Recording stopped. RecordingId: {recordingId}");
    }
    else
    {
        await client.GetCallRecording().PauseAsync(recordingId);
        logger.LogInformation($"Recording paused. RecordingId: {recordingId}");
    }
}

async Task HandleChatResponse(string chatResponse, CallMedia callConnectionMedia, string callerId, ILogger logger, string context = "OpenAISample")
{
    var chatGPTResponseSource = new TextSource(chatResponse)
    {
        VoiceName = "en-US-NancyNeural"
    };

    var recognizeOptions =
        new CallMediaRecognizeSpeechOptions(
            targetParticipant: CommunicationIdentifier.FromRawId(callerId))
        {
            InterruptPrompt = false,
            InitialSilenceTimeout = TimeSpan.FromSeconds(15),
            Prompt = chatGPTResponseSource,
            OperationContext = context,
            EndSilenceTimeout = TimeSpan.FromMilliseconds(500)
        };

    var recognize_result = await callConnectionMedia.StartRecognizingAsync(recognizeOptions);
}

async Task<string> GetChatGPTResponse(string speech_input)
{
    return await GetChatCompletionsAsync(helloPrompt, speech_input);
}

async Task<string> GetChatCompletionsAsync(string systemPrompt, string userPrompt)
{
    var messages = new List<ChatMessage>()
    {
        new ChatMessage(ChatRole.System, systemPrompt),
        new ChatMessage(ChatRole.User, userPrompt),
    };

    var chatCompletionsOptions = new ChatCompletionsOptions(
        builder.Configuration.GetValue<string>("AzureOpenAIDeploymentModelName"),
        messages);

    var response = await ai_client.GetChatCompletionsAsync(chatCompletionsOptions);

    var response_content = response.Value.Choices[0].Message.Content;
    return response_content;
}

async Task HandleDtmfRecognizeAsync(CallMedia callConnectionMedia, string callerId, string message, string context)
{
    // Play greeting message
    var greetingPlaySource = new TextSource(message)
    {
        VoiceName = "en-US-NancyNeural"
    };

    var recognizeOptions =
        new CallMediaRecognizeDtmfOptions(
            targetParticipant: CommunicationIdentifier.FromRawId(callerId), maxTonesToCollect: 8)
        {
            InterruptPrompt = false,
            InterToneTimeout = TimeSpan.FromSeconds(5),
            Prompt = greetingPlaySource,
            OperationContext = context,
            InitialSilenceTimeout = TimeSpan.FromSeconds(15)
        };

    var recognize_result = await callConnectionMedia.StartRecognizingAsync(recognizeOptions);
}
async Task HandleRecognizeAsync(CallMedia callConnectionMedia, string callerId, string message, string context)
{
    // Play greeting message
    var greetingPlaySource = new TextSource(message)
    {
        VoiceName = "en-US-NancyNeural"
    };

    var recognizeOptions =
        new CallMediaRecognizeSpeechOptions(
            targetParticipant: CommunicationIdentifier.FromRawId(callerId))
        {
            InterruptPrompt = false,
            InitialSilenceTimeout = TimeSpan.FromSeconds(15),
            Prompt = greetingPlaySource,
            OperationContext = context,
            EndSilenceTimeout = TimeSpan.FromMilliseconds(500)
        };

    var recognize_result = await callConnectionMedia.StartRecognizingAsync(recognizeOptions);
}

async Task HandlePlayAsync(CallMedia callConnectionMedia, string textToPlay, string context)
{
    // Play message
    var playSource = new TextSource(textToPlay)
    {
        VoiceName = "en-US-NancyNeural"
    };

    var playOptions = new PlayToAllOptions(playSource) { OperationContext = context };
    await callConnectionMedia.PlayToAllAsync(playOptions);
}

async Task InitiateTranscription(CallMedia callConnectionMedia)
{
    StartTranscriptionOptions startTrasnscriptionOption = new StartTranscriptionOptions()
    {
        Locale = "en-US",
        OperationContext = "StartTranscript"
    };

    await callConnectionMedia.StartTranscriptionAsync(startTrasnscriptionOption);
    isTrasncriptionActive = true;
}
bool IsDateFormatValid(string input)
{
    int lastDotIndex = input.LastIndexOf('.');
    string dob = lastDotIndex != -1 ? input.Substring(0, lastDotIndex) : input;
    DateTime fromDateValue;
    var formats = new[] { "dd/MM/yyyy", "yyyy-MM-dd" };
    if (DateTime.TryParseExact(dob, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out fromDateValue))
    {
        return true;
    }
    else
    {
        return false;
    }
}

app.Run();