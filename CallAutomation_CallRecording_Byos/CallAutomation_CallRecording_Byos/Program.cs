using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Your ACS resource connection string
var acsConnectionString = "<ACS_CONNECTION_STRINGS>";

var bringYouOwnStorageUrl = "<YOUR_STORAGE_CONTAINER_URL>>";

// Base url of the app
var callbackUriHost = "DEVTUNNEL_HOST_URL";

var recordingId = "";

CallAutomationClient callAutomationClient = new CallAutomationClient(acsConnectionString);
var app = builder.Build();

/* Route for Azure Communication Service eventgrid webhooks*/
app.MapPost("/api/events", async (EventGridEvent[] eventGridEvents, ILogger<Program> logger) =>
{
    foreach (var eventGridEvent in eventGridEvents)
    {
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

            if (eventData is AcsIncomingCallEventData incomingCallEventData)
            {
                logger.LogInformation($"Incoming Call event received : {JsonSerializer.Serialize(eventGridEvent)}");

                var callerId = incomingCallEventData?.FromCommunicationIdentifier?.RawId;
                var incomingCallContext = incomingCallEventData?.IncomingCallContext;
                var callbackUri = new Uri(callbackUriHost + $"/api/callbacks?callerId={callerId}");
                var options = new AnswerCallOptions(incomingCallContext, callbackUri);

                AnswerCallResult answerCallResult = await callAutomationClient.AnswerCallAsync(options);
                logger.LogInformation($"Answer call result callConnectionId: {answerCallResult.CallConnection.CallConnectionId}," +
                    $" correlationId: {answerCallResult.CallConnectionProperties.CorrelationId}");

                var callConnectionMedia = answerCallResult.CallConnection.GetCallMedia();
                //Use EventProcessor to process CallConnected event
                var answer_result = await answerCallResult.WaitForEventProcessorAsync();

                if (answer_result.IsSuccess)
                {
                    StartRecordingOptions recordingOptions = new StartRecordingOptions(new ServerCallLocator(answer_result.SuccessResult.ServerCallId))
                    {
                        RecordingContent = RecordingContent.Audio,
                        RecordingChannel = RecordingChannel.Unmixed,
                        RecordingFormat = RecordingFormat.Wav,
                        ExternalStorage = new BlobStorage(new Uri(bringYouOwnStorageUrl))
                    };

                    var recordingResult = await callAutomationClient.GetCallRecording().StartAsync(recordingOptions);
                    recordingId = recordingResult?.Value.RecordingId;
                    logger.LogInformation($"Call recording id--> {recordingId}," +
                        $" recording state--> {recordingResult?.Value.RecordingState}");

                    await HandleDtmfRecognizeAsync(callConnectionMedia, callerId, "/audio/MainMenu.wav", "mainmenu");

                }
                callAutomationClient.GetEventProcessor().AttachOngoingEventProcessor<RecognizeCompleted>(answerCallResult.CallConnection.CallConnectionId, async (recognizeCompletedEvent) =>
                {
                    logger.LogInformation($"Play completed event received for connection id: {recognizeCompletedEvent.CallConnectionId}");

                    // Play audio once recognition is completed sucessfully
                    string selectedTone = ((DtmfResult)recognizeCompletedEvent.RecognizeResult).ConvertToString();

                    await HandleMenu(selectedTone, recognizeCompletedEvent.OperationContext, callConnectionMedia,callerId);
                });
                callAutomationClient.GetEventProcessor().AttachOngoingEventProcessor<PlayCompleted>(answerCallResult.CallConnection.CallConnectionId, async (playCompletedEvent) =>
                {
                    logger.LogInformation($"Play completed event received for connection id: {playCompletedEvent.CallConnectionId}");
                    var callConnection = callAutomationClient.GetCallConnection(playCompletedEvent.CallConnectionId);
                    await callConnection.HangUpAsync(true);

                });
                callAutomationClient.GetEventProcessor().AttachOngoingEventProcessor<RecognizeFailed>(answerCallResult.CallConnection.CallConnectionId, async (recognizeFailedEvent) =>
                {
                    logger.LogInformation($"Play completed event received for connection id: {recognizeFailedEvent.CallConnectionId}");

                    // Check for time out, and then play audio message
                    if (recognizeFailedEvent.ReasonCode.Equals(MediaEventReasonCode.RecognizeInitialSilenceTimedOut))
                    {
                        await callConnectionMedia.PlayToAllAsync(new FileSource(new Uri(callbackUriHost + "/audio/Timeout.wav")));
                    }

                });
            }
            if (eventData is AcsRecordingFileStatusUpdatedEventData statusUpdated)
            {
                logger.LogInformation($"Recording file status event received : {JsonSerializer.Serialize(statusUpdated)}");
                var recordingLocation = statusUpdated.RecordingStorageInfo.RecordingChunks[0].ContentLocation;
                logger.LogInformation($"The recording location is : {recordingLocation}");
            }
        }
    }

    return Results.Ok();
}).Produces(StatusCodes.Status200OK);

app.MapPost("/api/callbacks", async (CloudEvent[] cloudEvents, ILogger<Program> logger) =>
{
    var eventProcessor = callAutomationClient.GetEventProcessor();
    foreach (var cloudEvent in cloudEvents)
    {
        CallAutomationEventBase parsedEvent = CallAutomationEventParser.Parse(cloudEvent);
        logger.LogInformation(
            "Received call event: {type}, callConnectionID: {connId}, serverCallId: {serverId}, chatThreadId: {chatThreadId}",
            parsedEvent.GetType(),
            parsedEvent.CallConnectionId,
            parsedEvent.ServerCallId,
            parsedEvent.OperationContext);
    }

    eventProcessor.ProcessEvents(cloudEvents);
    return Results.Ok();
}).Produces(StatusCodes.Status200OK);

async Task HandleMenu(string selectedTone, string eventContext, CallMedia callConnectionMedia, string callerId)
{
    if (eventContext.Equals("mainmenu"))
    {
        await HandleToneSelection(selectedTone, callConnectionMedia, callerId, "Paused", "paused");
    }
    else if (eventContext.Equals("paused"))
    {
        await HandleToneSelection(selectedTone, callConnectionMedia, callerId, "Resumed", "resumed");
    }
    else
    {
        await StopRecordingAndPlayGoodbye(callConnectionMedia);
    }
}

async Task HandleToneSelection(string selectedTone, CallMedia callConnectionMedia, string callerId, string fileName, string operationContext)
{
    switch (selectedTone)
    {
        case "1":
            await (operationContext == "paused" ? ResumeRecording() : PauseRecording());
            await HandleDtmfRecognizeAsync(callConnectionMedia, callerId, $"/audio/{fileName}.wav", operationContext);
            break;
        case "2":
            await StopRecordingAndPlayGoodbye(callConnectionMedia);
            break;
        default:
            await callConnectionMedia.PlayToAllAsync(new FileSource(new Uri(callbackUriHost + "/audio/Invalid.wav")));
            break;
    }
}

async Task PauseRecording()
{
    await callAutomationClient.GetCallRecording().PauseAsync(recordingId);
    await GetRecordingState(recordingId);
}

async Task ResumeRecording()
{
    await callAutomationClient.GetCallRecording().ResumeAsync(recordingId);
    await GetRecordingState(recordingId);
}

async Task StopRecordingAndPlayGoodbye(CallMedia callConnectionMedia)
{
    await callAutomationClient.GetCallRecording().StopAsync(recordingId);
    Console.WriteLine("Recording is Stopped.");
    await callConnectionMedia.PlayToAllAsync(new FileSource(new Uri(callbackUriHost + "/audio/Goodbye.wav")));
}

async Task<RecordingState> GetRecordingState(string recordingId)
{
    var result = await callAutomationClient.GetCallRecording().GetStateAsync(recordingId);
    var state = result?.Value.RecordingState.ToString();
    Console.WriteLine($"Recording Status:->  {state}");
    //logger.LogInformation($"Recording Type:-> {result.Value.RecordingType.ToString()}");
    return result.Value.RecordingState.Value;
}

async Task HandleDtmfRecognizeAsync(CallMedia callConnectionMedia, string callerId, string filePath, string context)
{
    // Play greeting message
    var playSource = new FileSource(new Uri(callbackUriHost + filePath));

    var recognizeOptions =
        new CallMediaRecognizeDtmfOptions(
            targetParticipant: CommunicationIdentifier.FromRawId(callerId), maxTonesToCollect: 8)
        {
            InterruptPrompt = false,
            InterToneTimeout = TimeSpan.FromSeconds(5),
            Prompt = playSource,
            OperationContext = context,
            InitialSilenceTimeout = TimeSpan.FromSeconds(15)
        };

    var recognize_result = await callConnectionMedia.StartRecognizingAsync(recognizeOptions);
}


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "audio")),
    RequestPath = "/audio"
});

app.Run();
