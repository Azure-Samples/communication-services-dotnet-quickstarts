using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;

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
var callbackUriHost = Environment.GetEnvironmentVariable("VS_TUNNEL_URL")?.TrimEnd('/');

// This will be set by fileStatus endpoints
string recordingLocation = "";

string recordingId = "";

CallAutomationClient callAutomationClient = new CallAutomationClient(acsConnectionString);
builder.Services.AddSingleton(callAutomationClient);
var app = builder.Build();

app.MapPost("/api/outboundCall", async (ILogger<Program> logger) =>
{
    PhoneNumberIdentifier target = new PhoneNumberIdentifier(targetPhonenumber);
    PhoneNumberIdentifier caller = new PhoneNumberIdentifier(acsPhonenumber);
    CallInvite callInvite = new CallInvite(target, caller);
    CreateCallResult createCallResult = await callAutomationClient.CreateCallAsync(callInvite, new Uri(callbackUriHost + "/api/callbacks")).ConfigureAwait(false);

    logger.LogInformation($"Created call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
});

app.MapPost("/api/callbacks", async (CloudEvent[] cloudEvents, ILogger<Program> logger) =>
{
    foreach (var cloudEvent in cloudEvents)
    {
        CallAutomationEventBase @event = CallAutomationEventParser.Parse(cloudEvent);
        logger.LogInformation($"{@event?.GetType().Name} event received for call connection id: {@event?.CallConnectionId}");
        var callConnection = callAutomationClient.GetCallConnection(@event.CallConnectionId);
        var callMedia = callConnection.GetCallMedia();

        if (@event is CallConnected)
        {
            logger.LogInformation($"Start Recording...");
            CallLocator callLocator = new ServerCallLocator(@event.ServerCallId);
            var recordingResult = await callAutomationClient.GetCallRecording().StartAsync(new StartRecordingOptions(callLocator));
            recordingId = recordingResult.Value.RecordingId;

            // prepare recognize tones
            CallMediaRecognizeDtmfOptions callMediaRecognizeDtmfOptions = new CallMediaRecognizeDtmfOptions(new PhoneNumberIdentifier(targetPhonenumber), maxTonesToCollect: 1);
            callMediaRecognizeDtmfOptions.Prompt = new FileSource(new Uri(callbackUriHost + "/audio/MainMenu.wav"));
            callMediaRecognizeDtmfOptions.InterruptPrompt = true;
            callMediaRecognizeDtmfOptions.InitialSilenceTimeout = TimeSpan.FromSeconds(5);
            callMediaRecognizeDtmfOptions.InterToneTimeout = TimeSpan.FromSeconds(10);
            callMediaRecognizeDtmfOptions.StopTones = new List<DtmfTone> { DtmfTone.Pound, DtmfTone.Asterisk };

            // Send request to recognize tones
            await callMedia.StartRecognizingAsync(callMediaRecognizeDtmfOptions);
        }
        if (@event is RecognizeCompleted)
        {
            // Play audio once recognition is completed sucessfully
            var recognizeCompleted = (RecognizeCompleted)@event;
            string selectedTone = ((DtmfResult)recognizeCompleted.RecognizeResult).ConvertToString();

            switch (selectedTone)
            {
                case "1":
                    await callMedia.PlayToAllAsync(new FileSource(new Uri(callbackUriHost + "/audio/Confirmed.wav")));
                    break;

                case "2":
                    await callMedia.PlayToAllAsync(new FileSource(new Uri(callbackUriHost + "/audio/Goodbye.wav")));         
                    break;

                default:
                    //invalid tone
                    await callMedia.PlayToAllAsync(new FileSource(new Uri(callbackUriHost + "/audio/Invalid.wav")));
                    break;
            }
        }
        if (@event is RecognizeFailed)
        {
            logger.LogInformation($"RecognizeFailed event received for call connection id: {@event.CallConnectionId}");
            var recognizeFailedEvent = (RecognizeFailed)@event;

            // Check for time out, and then play audio message
            if (recognizeFailedEvent.ReasonCode.Equals(MediaEventReasonCode.RecognizeInitialSilenceTimedOut))
            {
                await callMedia.PlayToAllAsync(new FileSource(new Uri(callbackUriHost + "/audio/Timeout.wav")));
            }
        }
        if ((@event is PlayCompleted) || (@event is PlayFailed))
        {
            logger.LogInformation($"Stop recording and terminating call.");
            callAutomationClient.GetCallRecording().Stop(recordingId);
            await callConnection.HangUpAsync(true);
        }
    }
    return Results.Ok();
}).Produces(StatusCodes.Status200OK);

app.MapPost("/filestatus", ([FromBody] EventGridEvent[] eventGridEvents, ILogger<Program> logger) =>
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

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "audio")),
    RequestPath = "/audio"
});

app.UseHttpsRedirection();
app.Run();
