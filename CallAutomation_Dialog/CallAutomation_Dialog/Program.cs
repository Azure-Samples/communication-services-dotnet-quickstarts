using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Identity;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using CallAutomation_Dialog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;

var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var callConfigurationSection = builder.Configuration.GetSection(nameof(CallConfiguration));
builder.Services.Configure<CallConfiguration>(callConfigurationSection);

var client = new CallAutomationClient(
    new Uri(builder.Configuration["CallConfiguration:PmaEndpoint"]), 
    builder.Configuration["CallConfiguration:ConnectionString"]);

var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();
var baseUri = builder.Configuration["BaseUri"]?.TrimEnd('/');
if (string.IsNullOrEmpty(baseUri))
{
    logger.LogError("BaseUri is not configured in appsettings.json. Please ensure BaseUri is set in the configuration.");
    throw new InvalidOperationException("BaseUri configuration is missing. Please check appsettings.json.");
}

var app = builder.Build();

// Configure path base for sub-application deployment
app.UsePathBase("/dialogsite");

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
        var callbackUri = new Uri(baseUri + $"/dialogsite/api/calls/{Guid.NewGuid()}?callerId={callerId}&calleeId={calleeId}");

        AnswerCallOptions options = new AnswerCallOptions(incomingCallContext, callbackUri)
        {

        };
        AnswerCallResult answerCallResult = await client.AnswerCallAsync(options);
    }
    return Results.Ok();
});

app.MapPost("/api/calls/{contextId}", async (
    [FromBody] CloudEvent[] cloudEvents,
    [FromRoute] string contextId,
    [Required] string callerId,
    [Required] string calleeId,
    IOptions<CallConfiguration> callConfiguration,
    ILogger <Program> logger) =>
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
            logger.LogInformation($"CorrelationId: {@event.CorrelationId}");

            string botAppId;

            if (calleeId.StartsWith("4:", StringComparison.OrdinalIgnoreCase))
            {
                botAppId = callConfiguration.Value.BotRouting.ContainsKey(calleeId.Substring("4:".Length).Trim()) ? callConfiguration.Value.BotRouting[calleeId.Substring("4:".Length).Trim()] : callConfiguration.Value.DefaultBotId;
            } 
            else
            {
                botAppId = callConfiguration.Value.BotRouting.ContainsKey(calleeId) ? callConfiguration.Value.BotRouting[calleeId] : callConfiguration.Value.DefaultBotId;
            }

            var dialogOptions = new StartDialogOptions(Guid.NewGuid().ToString(), new PowerVirtualAgentsDialog(botAppId, dialogContext))
            {
                OperationContext = "DialogStart"
            };

            var response = await callDialog.StartDialogAsync(dialogOptions);

            logger.LogInformation($"Response: {response.GetRawResponse().Content.ToString()}");

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
    app.UseSwagger(c =>
    {
        c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
        {
            // Set the server URL to include the path base
            swaggerDoc.Servers = new List<Microsoft.OpenApi.Models.OpenApiServer>
            {
                new Microsoft.OpenApi.Models.OpenApiServer { Url = "/dialogsite" }
            };
        });
    });
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/dialogsite/swagger/v1/swagger.json", "CallAutomation Dialog API V1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.Run();
