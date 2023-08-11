using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid.SystemEvents;
using Azure.Messaging.EventGrid;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Nodes;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Your ACS resource connection string
        var acsConnectionString = builder.Configuration["acsConnectionString"];

        // Base url of the app
        var callbackUriHost = builder.Configuration["callbackUriHost"];

        // ACS resource phone number will act as source number to start outbound call
        var acsPhonenumber = builder.Configuration["acsPhonenumber"];

        // Target phone number you want to receive the call.
        var c2Target = builder.Configuration["targetPhonenumber"];

        var callAutomationClient = new CallAutomationClient(acsConnectionString);
        var app = builder.Build();

        app.MapPost("/api/incomingCall", async (
            [FromBody] EventGridEvent[] eventGridEvents,
            ILogger<Program> logger) =>
        {
            foreach (var eventGridEvent in eventGridEvents)
            {
                // Handle system events
                if (eventGridEvent.TryGetSystemEventData(out object eventData))
                {
                    // Handle the subscription validation event.
                    if (eventData is SubscriptionValidationEventData subscriptionValidationEventData)
                    {
                        logger.LogInformation("SubscriptionValidationEvent");
                        var responseData = new SubscriptionValidationResponse
                        {
                            ValidationResponse = subscriptionValidationEventData.ValidationCode
                        };
                        return Results.Ok(responseData);
                    }
                }

                var jsonObject = JsonNode.Parse(eventGridEvent.Data)?.AsObject();
                logger.LogInformation("jsonObject={jsonObject}", jsonObject);
                var toId = (string)(jsonObject["to"]["rawId"]);
                var fromId = (string)(jsonObject["from"]["rawId"]);
                var incomingCallContext = (string)jsonObject["incomingCallContext"];
                var correlationId = (string)jsonObject["correlationId"];


                var toKind = (string)(jsonObject["to"]["kind"]);
                if (toKind.Equals("phoneNumber"))
                {
                    var toPhoneNumber = (string)(jsonObject["to"]["phoneNumber"]["value"]);
                    if (toPhoneNumber == acsPhonenumber)
                    {
                        var answerCallResult = await callAutomationClient.AnswerCallAsync(incomingCallContext, new Uri(callbackUriHost + "/api/callbacks"));
                        logger.LogInformation("AnswerCallAsync result: {answerCallResult}, correlationId={correlationId}", answerCallResult, correlationId);
                    }
                    else
                    {
                        logger.LogInformation("incoming call, phone number not found");
                    }
                }
                else
                {
                    logger.LogInformation("incoming call, not a phone number");
                }
            }
            return Results.Ok();
        });

        app.MapGet("/outboundCall", async (ILogger<Program> logger) =>
        {
            var target = new PhoneNumberIdentifier(c2Target);
            var caller = new PhoneNumberIdentifier(acsPhonenumber);
            var callInvite = new CallInvite(target, caller);
            var createCallResult = await callAutomationClient.CreateCallAsync(callInvite, new Uri(callbackUriHost + "/api/callbacks"));
            logger.LogInformation("CreateCallAsync result: {createCallResult}", createCallResult);
            return Results.Redirect("/index.html");
        });

        app.MapPost("/api/callbacks", async (CloudEvent[] cloudEvents, ILogger<Program> logger) =>
        {
            foreach (var cloudEvent in cloudEvents)
            {
                CallAutomationEventBase acsEvent = CallAutomationEventParser.Parse(cloudEvent);
                logger.LogInformation("Received event {eventName} for call connection id {callConnectionId}, correlationId={correlationId}", acsEvent?.GetType().Name, acsEvent?.CallConnectionId, acsEvent?.CorrelationId);
                var callConnectionId = acsEvent?.CallConnectionId;

                if (acsEvent is CallConnected)
                {
                    // Send DTMF tones
                    var tones = new DtmfTone[] { DtmfTone.One, DtmfTone.Two, DtmfTone.Three, DtmfTone.Pound };
                    var sendDtmfAsyncResult = await callAutomationClient.GetCallConnection(callConnectionId)
                        .GetCallMedia()
                        .SendDtmfTonesAsync(tones, new PhoneNumberIdentifier(c2Target), "dtmfs-to-ivr");
                    logger.LogInformation("SendDtmfAsync result: {sendDtmfAsyncResult}", sendDtmfAsyncResult);
                }
                if (acsEvent is SendDtmfTonesCompleted sendDtmfCompleted)
                {
                    logger.LogInformation("Send dtmf succeeded, context={context}", sendDtmfCompleted.OperationContext);
                }
                if (acsEvent is SendDtmfTonesFailed sendDtmfFailed)
                {
                    logger.LogInformation("Send dtmf failed: result={result}, context={context}",
                        sendDtmfFailed.ResultInformation?.Message, sendDtmfFailed.OperationContext);
                }
            }
            return Results.Ok();
        }).Produces(StatusCodes.Status200OK);

        app.UseStaticFiles();

        app.Run();
    }
}
