using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var client = new CallAutomationClient(builder.Configuration["ConnectionString"]);
var callbackUriBase = builder.Configuration["CallbackUriBase"]; // i.e. https://someguid.ngrok.io

var app = builder.Build();
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
    [Required] string callerId) =>
{
    foreach (var cloudEvent in cloudEvents)
    {
        CallAutomationEventBase @event = CallAutomationEventParser.Parse(cloudEvent);
        if (@event is CallConnected)
        {

            PlaySource promptMessageUri = new FileSource(new Uri(builder.Configuration["CallbackUriBase"] + builder.Configuration["PromptMessageFile"]));
            // play audio then recognize 1-digit DTMF input with pound (#) stop tone

            // Call recording
            var serverCallId = client.GetCallConnection(@event.CallConnectionId).GetCallConnectionProperties().Value.ServerCallId;
            StartRecordingOptions startRecordingOptions = new StartRecordingOptions(new ServerCallLocator(serverCallId));
            _ = Task.Run(async () => await client.GetCallRecording().StartRecordingAsync(startRecordingOptions));


            // Start recognize prompt
            var recognizeOptions =
                new CallMediaRecognizeDtmfOptions(CommunicationIdentifier.FromRawId(callerId), maxTonesToCollect: 1)
                {
                    InterruptPrompt = true,
                    InterToneTimeout = TimeSpan.FromSeconds(10),
                    InitialSilenceTimeout = TimeSpan.FromSeconds(5),
                    Prompt = promptMessageUri,
                    StopTones = new[] { DtmfTone.Pound },
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
                // this RecognizeCompleted correlates to the previous action as per the OperationContext value
                var addParticipantOptions = new AddParticipantsOptions(new List<CommunicationIdentifier>()
                        {
                        new PhoneNumberIdentifier(builder.Configuration["ParticipantToAdd"])
                        });
                addParticipantOptions.SourceCallerId = new PhoneNumberIdentifier(builder.Configuration["ACSAlternatePhoneNumber"]);

                await client.GetCallConnection(@event.CallConnectionId).AddParticipantsAsync(addParticipantOptions);
            }
            else
            {
                // Hangup for everyone
                await client.GetCallConnection(@event.CallConnectionId).HangUpAsync(true);
            }

        }
        if (@event is RecognizeFailed { OperationContext: "MainMenu" })
        {

            // play invalid audio
            PlaySource invalidMessageUri = new FileSource(new Uri(builder.Configuration["CallbackUriBase"] + builder.Configuration["InvlidMessageFile"]));
            await client.GetCallConnection(@event.CallConnectionId).GetCallMedia().PlayToAllAsync(invalidMessageUri, new PlayOptions() { Loop = false });

            await client.GetCallConnection(@event.CallConnectionId).HangUpAsync(true);
        }
    }
    return Results.Ok();
}).Produces(StatusCodes.Status200OK);

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
