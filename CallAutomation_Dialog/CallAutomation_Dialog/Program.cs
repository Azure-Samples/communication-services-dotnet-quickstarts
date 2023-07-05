using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using CallAutomation_Dialog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var callConfigurationSection = builder.Configuration.GetSection(nameof(CallConfiguration));
builder.Services.Configure<CallConfiguration>(callConfigurationSection);

var client = new CallAutomationClient(builder.Configuration["CallConfiguration:ConnectionString"]);
var baseUri = Environment.GetEnvironmentVariable("VS_TUNNEL_URL")?.TrimEnd('/');
if (string.IsNullOrEmpty(baseUri))
{
    baseUri = builder.Configuration["BaseUri"];
}

var app = builder.Build();

app.MapPost("/api/incomingCall", async (
    [FromBody] EventGridEvent[] eventGridEvents,
    IOptions<CallConfiguration> callConfiguration,
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
        var calleeId = (string)(jsonObject["to"]["rawId"]);
        var incomingCallContext = (string)jsonObject["incomingCallContext"];
        var callbackUri = new Uri(baseUri + $"/api/calls/{Guid.NewGuid()}?callerId={callerId}&calleeId={calleeId}");

        AnswerCallResult answerCallResult = await client.AnswerCallAsync(incomingCallContext, callbackUri);
    }
    return Results.Ok();
});

app.MapPost("/api/calls/{contextId}", async (
    [FromBody] CloudEvent[] cloudEvents,
    [FromRoute] string contextId,
    [Required] string callerId,
    [Required] string calleeId,
    IOptions<CallConfiguration> callConfiguration,
    ILogger<Program> logger) =>
{
    foreach (var cloudEvent in cloudEvents)
    {
        CallAutomationEventBase @event = CallAutomationEventParser.Parse(cloudEvent);
        logger.LogInformation($"Event received: {JsonConvert.SerializeObject(cloudEvent)}");

        var callConnection = client.GetCallConnection(@event.CallConnectionId);
        var callDialog = callConnection.GetCallDialog();

        if (callConnection == null || callDialog == null)
        {
            return Results.BadRequest($"Call objects failed to get for connection id {@event.CallConnectionId}.");
        }

        if (@event is CallConnected)
        {
            Dictionary<string, object> dialogContext = new Dictionary<string, object>();

            //Initiate start dialog as call connected event is received
            logger.LogInformation($"CallConnected event received for call connection id: {@event.CallConnectionId}");

            string botAppId;

            if (calleeId.StartsWith("4:", StringComparison.OrdinalIgnoreCase))
            {
                botAppId = callConfiguration.Value.BotRouting.ContainsKey(calleeId.Substring("4:".Length).Trim()) ? callConfiguration.Value.BotRouting[calleeId.Substring("4:".Length).Trim()] : callConfiguration.Value.DefaultBotId;
            }
            else
            {
                botAppId = callConfiguration.Value.BotRouting.ContainsKey(calleeId) ? callConfiguration.Value.BotRouting[calleeId] : callConfiguration.Value.DefaultBotId;
            }

            var dialogOptions = new StartDialogOptions(DialogInputType.PowerVirtualAgents, botAppId, dialogContext)
            {
                OperationContext = "DialogStart"
            };

            await callDialog.StartDialogAsync(dialogOptions);

        }
        if (@event is DialogStarted { OperationContext: "DialogStart" })
        {
            //Verify the start of dialog here 
        }
        if (@event is DialogFailed { OperationContext: "DialogStart" })
        {

        }
        if (@event is DialogTransfer)
        {
            var transferEvent = (DialogTransfer)@event;
            await callDialog.StopDialogAsync(transferEvent.DialogId);
            await callConnection.TransferCallToParticipantAsync(new PhoneNumberIdentifier(transferEvent.TransferDestination));
        }
        if (@event is DialogHangup)
        {
            var hangupEvent = (DialogHangup)@event;

            //Stop the dialog
            await callDialog.StopDialogAsync(hangupEvent.DialogId);

            //Hang up the call for everyone
            await callConnection.HangUpAsync(true);
        }
        if (@event is DialogConsent { OperationContext: "DialogStart" })
        {

        }
        if (@event is DialogCompleted { OperationContext: "DialogStop" })
        {

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

app.UseHttpsRedirection();
app.Run();
