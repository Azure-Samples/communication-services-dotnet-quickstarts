using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;
using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;

var client = new CallAutomationClient("<ACS_CONNECTION_STRING>");

var builder = WebApplication.CreateBuilder(args);
var callbackUriBase = builder.Configuration["VS_TUNNEL_URL"];
Console.WriteLine($"CALLBACK URI: {callbackUriBase}");
var app = builder.Build();
var serverCallId = "";

#region Handle Incoming Call

app.MapPost("/api/incomingCall", async (
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

        // Handle incoming call event
        var jsonObject = JsonNode.Parse(eventGridEvent.Data).AsObject();
        var callerId = (string)(jsonObject["from"]["rawId"]);
        serverCallId = (string)(jsonObject["serverCallId"]);
        var incomingCallContext = (string)jsonObject["incomingCallContext"];
        var callbackUri = new Uri(callbackUriBase + $"api/calls/{Guid.NewGuid()}?callerId={callerId}");

        await client.AnswerCallAsync(incomingCallContext, callbackUri);
    }
    return Results.Ok();
});

#endregion

#region Handle Call Connected

app.MapPost("/api/calls/{contextId}", async (
    [FromBody] CloudEvent[] cloudEvents,
    [FromRoute] string contextId,
    [Required] string callerId) =>
{
    foreach (var cloudEvent in cloudEvents)
    {
        CallAutomationEventBase @event = CallAutomationEventParser.Parse(cloudEvent);
        
        if (@event is CallConnected callConnected)
        {
            // Option 1: Record unmixed audio and automatically set participant ordering based on audio first detected
            await client.GetCallRecording().StartRecordingAsync(
                new StartRecordingOptions(new ServerCallLocator(serverCallId))
                {
                    RecordingChannel = RecordingChannel.Unmixed,

                });

            // Option 2: Record unmixed audio and manually set the PSTN participant to be the first channel in the recording
            // var participants = await client.GetCallConnection(callConnected.CallConnectionId).GetParticipantsAsync();
            // var caller = participants.Value.FirstOrDefault(p => p.Identifier is PhoneNumberIdentifier).Identifier;
            //
            // await client.GetCallRecording().StartRecordingAsync(
            //     new StartRecordingOptions(new ServerCallLocator(serverCallId))
            //     {
            //         RecordingChannel = RecordingChannel.Unmixed,
            //         AudioChannelParticipantOrdering = { caller }
            //     });

            await client.GetCallConnection(@event.CallConnectionId)
                .GetCallMedia()
                .PlayToAllAsync(new FileSource(new Uri($"{callbackUriBase}audio/intro.wav")));
        }

    }
    return Results.Ok();
}).Produces(StatusCodes.Status200OK);

#endregion

#region Download Recording

app.MapPost("/api/recordingDone", async ([FromBody] EventGridEvent[] eventGridEvents) =>
{
    foreach (var eventGridEvent in eventGridEvents)
    {
        // Handle system events
        if (eventGridEvent.TryGetSystemEventData(out object eventData))
        {
            // Handle the subscription validation event
            if (eventData is SubscriptionValidationEventData subscriptionValidationEventData)
            {
                var responseData = new SubscriptionValidationResponse
                {
                    ValidationResponse = subscriptionValidationEventData.ValidationCode
                };
                return Results.Ok(responseData);
            }
        }

        // Handle recording status updated event - download recording to local folder
        if (eventData is AcsRecordingFileStatusUpdatedEventData acsRecordingFileStatusUpdatedEventData)
        {
            var recordingDownloadUri = new Uri(acsRecordingFileStatusUpdatedEventData.RecordingStorageInfo.RecordingChunks[0].ContentLocation);
            await using var fileStream = File.Create("unmixed_recording.wav");
            await client.GetCallRecording().DownloadToAsync(recordingDownloadUri, fileStream);
        }
    }
    return Results.Ok();
});

#endregion

#region Sentiment Analysis

app.MapGet("/api/sentimentAnalysis", async ([FromQuery] string filePath) => await CognitiveService.AnalyzeSentiment(filePath));

#endregion

#region Transcription

app.MapGet("/api/speechToText", async ([FromQuery] string filePath) => await CognitiveService.SpeechToText(filePath));

#endregion

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "audio")),
    RequestPath = "/audio"
});

app.Run();