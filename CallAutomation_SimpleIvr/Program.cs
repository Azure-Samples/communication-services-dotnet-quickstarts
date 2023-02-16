using Azure.Communication;	
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var client = new CallAutomationClient(builder.Configuration["ConnectionString"]);
var callbackUriBase = builder.Configuration["CallbackUriBase"];

var app = builder.Build();
app.MapPost("/api/incomingCall", async (
    [FromBody] EventGridEvent[] eventGridEvents,
    ILogger<Program> logger) =>
{
    foreach (var eventGridEvent in eventGridEvents)
    {
        logger.LogInformation($"Incoming Call event received : {JsonConvert.SerializeObject(eventGridEvent)}");
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
        var jsonObject = JsonNode.Parse(eventGridEvent.Data).AsObject();
        var callerId = (string)(jsonObject["from"]["rawId"]);
        var incomingCallContext = (string)jsonObject["incomingCallContext"];
        var callbackUri = new Uri(callbackUriBase + $"/api/calls/{Guid.NewGuid()}?callerId={callerId}");

        AnswerCallResult answerCallResult = await client.AnswerCallAsync(incomingCallContext, callbackUri);
    }
    return Results.Ok();
});

app.MapPost("/api/calls/{contextId}", async (
    [FromBody] CloudEvent[] cloudEvents,
    [FromRoute] string contextId,
    [Required] string callerId,
    ILogger<Program> logger) =>
{
    var audioBaseUrl = builder.Configuration["CallbackUriBase"];
    var audioPlayOptions = new PlayOptions() { OperationContext = "SimpleIVR", Loop = false };

    foreach (var cloudEvent in cloudEvents)
    {
        CallAutomationEventBase @event = CallAutomationEventParser.Parse(cloudEvent);
        logger.LogInformation($"Event received: {JsonConvert.SerializeObject(@event)}");
        if (@event is CallConnected)
        {
            // Start call recording
            var serverCallId = client.GetCallConnection(@event.CallConnectionId).GetCallConnectionProperties().Value.ServerCallId;
            StartRecordingOptions startRecordingOptions = new StartRecordingOptions(new ServerCallLocator(serverCallId));
            _ = Task.Run(async () => await client.GetCallRecording().StartRecordingAsync(startRecordingOptions));


            // Start recognize prompt - play audio and recognize 1-digit DTMF input
            var recognizeOptions =
                new CallMediaRecognizeDtmfOptions(CommunicationIdentifier.FromRawId(callerId), maxTonesToCollect: 1)
                {
                    InterruptPrompt = true,
                    InterToneTimeout = TimeSpan.FromSeconds(10),
                    InitialSilenceTimeout = TimeSpan.FromSeconds(5),
                    Prompt = new FileSource(new Uri(audioBaseUrl + builder.Configuration["MainMenuAudio"])),
                    OperationContext = "MainMenu"
                };
            await client.GetCallConnection(@event.CallConnectionId)
                .GetCallMedia()
                .StartRecognizingAsync(recognizeOptions);
        }
        if (@event is RecognizeCompleted { OperationContext: "MainMenu" })
        {
            var recognizeCompleted = (RecognizeCompleted)@event;

            if (recognizeCompleted.CollectTonesResult.Tones[0] == DtmfTone.One)
            {
                PlaySource salesAudio = new FileSource(new Uri(audioBaseUrl + builder.Configuration["SalesAudio"]));
                await client.GetCallConnection(@event.CallConnectionId).GetCallMedia().PlayToAllAsync(salesAudio, audioPlayOptions);
            }
            else if (recognizeCompleted.CollectTonesResult.Tones[0] == DtmfTone.Two)
            {
                PlaySource marketingAudio = new FileSource(new Uri(audioBaseUrl + builder.Configuration["MarketingAudio"]));
                await client.GetCallConnection(@event.CallConnectionId).GetCallMedia().PlayToAllAsync(marketingAudio, audioPlayOptions);
            }
            else if (recognizeCompleted.CollectTonesResult.Tones[0] == DtmfTone.Three)
            {
                PlaySource customerCareAudio = new FileSource(new Uri(audioBaseUrl + builder.Configuration["CustomerCareAudio"]));
                await client.GetCallConnection(@event.CallConnectionId).GetCallMedia().PlayToAllAsync(customerCareAudio, audioPlayOptions);
            }
            else if (recognizeCompleted.CollectTonesResult.Tones[0] == DtmfTone.Four)
            {
                PlaySource agentAudio = new FileSource(new Uri(audioBaseUrl + builder.Configuration["AgentAudio"]));
                audioPlayOptions.OperationContext = "AgentConnect";
                await client.GetCallConnection(@event.CallConnectionId).GetCallMedia().PlayToAllAsync(agentAudio, audioPlayOptions);

                var addParticipantOptions = new AddParticipantsOptions(new List<CommunicationIdentifier>()
                        {
                        new PhoneNumberIdentifier(builder.Configuration["ParticipantToAdd"])
                        });
                addParticipantOptions.SourceCallerId = new PhoneNumberIdentifier(builder.Configuration["ACSAlternatePhoneNumber"]);

                await client.GetCallConnection(@event.CallConnectionId).AddParticipantsAsync(addParticipantOptions);

            }
            else if (recognizeCompleted.CollectTonesResult.Tones[0] == DtmfTone.Five)
            {
                // Hangup for everyone
                await client.GetCallConnection(@event.CallConnectionId).HangUpAsync(true);
            }
            else
            {
                PlaySource invalidAudio = new FileSource(new Uri(audioBaseUrl + builder.Configuration["InvalidAudio"]));
                await client.GetCallConnection(@event.CallConnectionId).GetCallMedia().PlayToAllAsync(invalidAudio, audioPlayOptions);
            }
        }
        if (@event is RecognizeFailed { OperationContext: "MainMenu" })
        {

            // play invalid audio
            await client.GetCallConnection(@event.CallConnectionId).GetCallMedia().PlayToAllAsync(new FileSource(new Uri(audioBaseUrl + builder.Configuration["InvalidAudio"])), new PlayOptions() { Loop = false });
            await client.GetCallConnection(@event.CallConnectionId).HangUpAsync(true);
        }
        if (@event is PlayCompleted { OperationContext: "SimpleIVR" })
        {
            await client.GetCallConnection(@event.CallConnectionId).HangUpAsync(true);
        }
        if (@event is PlayFailed { OperationContext: "SimpleIVR" })
        {
            await client.GetCallConnection(@event.CallConnectionId).HangUpAsync(true);
        }
    }
    return Results.Ok();
}).Produces(StatusCodes.Status200OK);

app.MapPost("api/recording", async (
    [FromBody] EventGridEvent[] eventGridEvents) =>
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
        }

        if (eventData is AcsRecordingFileStatusUpdatedEventData acsRecordingFileStatusUpdatedEventData)
        {
            var recordingDownloadUri = new Uri(acsRecordingFileStatusUpdatedEventData.RecordingStorageInfo.RecordingChunks[0].ContentLocation);
            var downloadRespose = await client.GetCallRecording().DownloadStreamingAsync(recordingDownloadUri);

            string filePath = $".\\recording\\{acsRecordingFileStatusUpdatedEventData.RecordingStorageInfo.RecordingChunks[0].DocumentId}.mp4";
            using (Stream streamToReadFrom = downloadRespose.Value)
            {
                using (Stream streamToWriteTo = System.IO.File.Open(filePath, FileMode.Create))
                {
                    await streamToReadFrom.CopyToAsync(streamToWriteTo);
                    await streamToWriteTo.FlushAsync();
                }
            }
        }
    }
    return Results.Ok();
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
           Path.Combine(builder.Environment.ContentRootPath, "audio")),
    RequestPath = "/audio"
});

app.UseAuthorization();

app.MapControllers();

app.Run();
