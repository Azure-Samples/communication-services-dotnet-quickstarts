using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var client = new CallAutomationClient(builder.Configuration["ConnectionString"]);
var baseUri = Environment.GetEnvironmentVariable("VS_TUNNEL_URL")?.TrimEnd('/');
if (string.IsNullOrEmpty(baseUri))
{
    baseUri = builder.Configuration["BaseUri"];
}

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
        var callbackUri = new Uri(baseUri + $"/api/calls/{Guid.NewGuid()}?callerId={callerId}");

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
    var audioPlayOptions = new PlayOptions() { OperationContext = "SimpleIVR", Loop = false };

    foreach (var cloudEvent in cloudEvents)
    {
        CallAutomationEventBase @event = CallAutomationEventParser.Parse(cloudEvent);
        logger.LogInformation($"Event received: {JsonConvert.SerializeObject(@event)}");

        var callConnection = client.GetCallConnection(@event.CallConnectionId);
        var callMedia = callConnection?.GetCallMedia();

        if (callConnection == null || callMedia == null)
        {
            return Results.BadRequest($"Call objects failed to get for connection id {@event.CallConnectionId}.");
        }

        if (@event is CallConnected)
        {
            // Start recognize prompt - play audio and recognize 1-digit DTMF input
            var recognizeOptions =
                new CallMediaRecognizeDtmfOptions(CommunicationIdentifier.FromRawId(callerId), maxTonesToCollect: 1)
                {
                    InterruptPrompt = true,
                    InterToneTimeout = TimeSpan.FromSeconds(10),
                    InitialSilenceTimeout = TimeSpan.FromSeconds(5),
                    Prompt = new FileSource(new Uri(baseUri + builder.Configuration["MainMenuAudio"])),
                    OperationContext = "MainMenu"
                };
            await callMedia.StartRecognizingAsync(recognizeOptions);
        }
        if (@event is RecognizeCompleted { OperationContext: "MainMenu" })
        {
            var recognizeCompleted = (RecognizeCompleted)@event;
            CollectTonesResult collectedTones = (CollectTonesResult)recognizeCompleted.RecognizeResult;

            if (collectedTones.Tones[0] == DtmfTone.One)
            {
                PlaySource salesAudio = new FileSource(new Uri(baseUri + builder.Configuration["SalesAudio"]));
                await callMedia.PlayToAllAsync(salesAudio, audioPlayOptions);
            }
            else if (collectedTones.Tones[0] == DtmfTone.Two)
            {
                PlaySource marketingAudio = new FileSource(new Uri(baseUri + builder.Configuration["MarketingAudio"]));
                await callMedia.PlayToAllAsync(marketingAudio, audioPlayOptions);
            }
            else if (collectedTones.Tones[0] == DtmfTone.Three)
            {
                PlaySource customerCareAudio = new FileSource(new Uri(baseUri + builder.Configuration["CustomerCareAudio"]));
                await callMedia.PlayToAllAsync(customerCareAudio, audioPlayOptions);
            }
            else if (collectedTones.Tones[0] == DtmfTone.Four)
            {
                PlaySource agentAudio = new FileSource(new Uri(baseUri + builder.Configuration["AgentAudio"]));
                audioPlayOptions.OperationContext = "AgentConnect";
                await callMedia.PlayToAllAsync(agentAudio, audioPlayOptions);
            }
            else if (collectedTones.Tones[0] == DtmfTone.Five)
            {
                // Hangup for everyone
                await callConnection.HangUpAsync(true);
            }
            else
            {
                PlaySource invalidAudio = new FileSource(new Uri(baseUri + builder.Configuration["InvalidAudio"]));
                await callMedia.PlayToAllAsync(invalidAudio, audioPlayOptions);
            }
        }
        if (@event is RecognizeFailed { OperationContext: "MainMenu" })
        {
            // play invalid audio
            await callMedia.PlayToAllAsync(new FileSource(new Uri(baseUri + builder.Configuration["InvalidAudio"])), audioPlayOptions);
        }
        if (@event is PlayCompleted)
        {
            if (@event.OperationContext == "AgentConnect")
            {


                var participantToAdd = new PhoneNumberIdentifier(builder.Configuration["ParticipantToAdd"]);
                var callInvite = new CallInvite(participantToAdd, new PhoneNumberIdentifier(builder.Configuration["ACSAlternatePhoneNumber"]));
                var addParticipantOptions = new AddParticipantOptions(callInvite);
                var response = await callConnection.AddParticipantAsync(addParticipantOptions);
            }
            if (@event.OperationContext == "SimpleIVR")
            {
                await callConnection.HangUpAsync(true);
            }
        }
        if (@event is PlayFailed)
        {
            logger.LogInformation($"PlayFailed Event: {JsonConvert.SerializeObject(@event)}");
            await callConnection.HangUpAsync(true);
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
