using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid.SystemEvents;
using Azure.Messaging.EventGrid;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Your ACS resource connection string
        var acsConnectionString = builder.Configuration["acsConnectionString"];

        // Base url of the app
        var callbackUriHost = builder.Configuration["callbackUriHost"];

        // ACS resource phone number that will act as caller number to create an outbound call
        var callerPhonenumber = builder.Configuration["callerPhonenumber"];

        // ACS resource phone number that will receive an inbound call.
        var targetPhonenumber = builder.Configuration["targetPhonenumber"];

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
                // logger.LogInformation("jsonObject={jsonObject}", jsonObject);
                var toId = (string)(jsonObject["to"]["rawId"]);
                var fromId = (string)(jsonObject["from"]["rawId"]);
                var incomingCallContext = (string)jsonObject["incomingCallContext"];
                var correlationId = (string)jsonObject["correlationId"];


                var toKind = (string)(jsonObject["to"]["kind"]);
                if (toKind.Equals("phoneNumber"))
                {
                    var toPhoneNumber = (string)(jsonObject["to"]["phoneNumber"]["value"]);
                    if (toPhoneNumber == targetPhonenumber)
                    {
                        var answerCallResult = await callAutomationClient.AnswerCallAsync(incomingCallContext, new Uri(callbackUriHost + "/api/incomingCallCallbacks"));
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

        app.MapPost("/api/incomingCallCallbacks", async (CloudEvent[] cloudEvents, ILogger<Program> logger) =>
        {
            foreach (var cloudEvent in cloudEvents)
            {
                CallAutomationEventBase acsEvent = CallAutomationEventParser.Parse(cloudEvent);
                logger.LogInformation("Received event {eventName} for call connection id {callConnectionId}, correlationId={correlationId}", acsEvent?.GetType().Name, acsEvent?.CallConnectionId, acsEvent?.CorrelationId);
                var callConnectionId = acsEvent?.CallConnectionId;

                if (acsEvent is CallConnected)
                {
                    // Start continuous DTMF recognition
                    var startContinuousDtmfRecognitionAsyncResult = await callAutomationClient.GetCallConnection(callConnectionId)
                        .GetCallMedia()
                        .StartContinuousDtmfRecognitionAsync(new PhoneNumberIdentifier(callerPhonenumber), "dtmf-reco-on-c2");
                    logger.LogInformation("StartContinuousDtmfRecognitionAsync, status: {status}", startContinuousDtmfRecognitionAsyncResult.Status);
                }
                if (acsEvent is ContinuousDtmfRecognitionToneReceived continuousDtmfRecognitionToneReceived)
                {
                    logger.LogInformation("******************************************************************************");
                    logger.LogInformation("***");
                    logger.LogInformation("*** Tone detected: sequenceId={sequenceId}, tone={tone}",
                        continuousDtmfRecognitionToneReceived.ToneInfo.SequenceId,
                        continuousDtmfRecognitionToneReceived.ToneInfo.Tone);
                    logger.LogInformation("***");
                    logger.LogInformation("******************************************************************************");

                    if (continuousDtmfRecognitionToneReceived.ToneInfo.Tone.Equals(DtmfTone.Pound))
                    {
                        // Stop continuous DTMF recognition
                        var stopContinuousDtmfRecognitionAsyncResult = await callAutomationClient.GetCallConnection(callConnectionId)
                            .GetCallMedia()
                            .StopContinuousDtmfRecognitionAsync(new PhoneNumberIdentifier(callerPhonenumber), "dtmf-reco-on-c2");
                        logger.LogInformation("StopContinuousDtmfRecognitionAsync, status: {status}", stopContinuousDtmfRecognitionAsyncResult.Status);
                    }
                }
                if (acsEvent is ContinuousDtmfRecognitionToneFailed continuousDtmfRecognitionToneFailed)
                {
                    logger.LogInformation("Start continuous DTMF recognition failed, result={result}, context={context}",
                        continuousDtmfRecognitionToneFailed.ResultInformation?.Message,
                        continuousDtmfRecognitionToneFailed.OperationContext);
                }
                if (acsEvent is ContinuousDtmfRecognitionStopped continuousDtmfRecognitionStopped)
                {
                    logger.LogInformation("Continuous DTMF recognition stopped, context={context}", continuousDtmfRecognitionStopped.OperationContext);
                }
            }
            return Results.Ok();
        }).Produces(StatusCodes.Status200OK);

        app.MapGet("/outboundCall", async (ILogger<Program> logger) =>
        {
            var target = new PhoneNumberIdentifier(targetPhonenumber);
            var caller = new PhoneNumberIdentifier(callerPhonenumber);
            var callInvite = new CallInvite(target, caller);
            var createCallResult = await callAutomationClient.CreateCallAsync(callInvite, new Uri(callbackUriHost + "/api/outboundCallCallbacks"));
            logger.LogInformation("CreateCallAsync result: {createCallResult}", createCallResult);
            return Results.Redirect("/index.html");
        });

        app.MapPost("/api/outboundCallCallbacks", async (CloudEvent[] cloudEvents, ILogger<Program> logger) =>
        {
            foreach (var cloudEvent in cloudEvents)
            {
                CallAutomationEventBase acsEvent = CallAutomationEventParser.Parse(cloudEvent);
                logger.LogInformation("Received event {eventName} for call connection id {callConnectionId}, correlationId={correlationId}", acsEvent?.GetType().Name, acsEvent?.CallConnectionId, acsEvent?.CorrelationId);
                var callConnectionId = acsEvent?.CallConnectionId;

                if (acsEvent is CallConnected callConnected)
                {
                    logger.LogInformation("******************************************************************************");
                    logger.LogInformation("***");
                    logger.LogInformation("*** Send DTMF tones 1 2 3 #");
                    logger.LogInformation("***");
                    logger.LogInformation("******************************************************************************");
                    var tones = new DtmfTone[] { DtmfTone.One, DtmfTone.Two, DtmfTone.Three, DtmfTone.Pound };
                    var sendDtmfAsyncResult = await callAutomationClient.GetCallConnection(callConnectionId)
                        .GetCallMedia()
                        .SendDtmfTonesAsync(tones, new PhoneNumberIdentifier(targetPhonenumber), "dtmfs-to-ivr");
                    logger.LogInformation("SendDtmfAsync result: {sendDtmfAsyncResult}", sendDtmfAsyncResult);
                }
                if (acsEvent is SendDtmfTonesCompleted sendDtmfCompleted)
                {
                    logger.LogInformation("Send dtmf succeeded, context={context}", sendDtmfCompleted.OperationContext);
                    if (sendDtmfCompleted.OperationContext.Equals("dtmfs-to-ivr"))
                    {
                        logger.LogInformation("******************************************************************************");
                        logger.LogInformation("***");
                        logger.LogInformation("*** Send DTMF tones 4 5");
                        logger.LogInformation("***");
                        logger.LogInformation("******************************************************************************");
                        var tones = new DtmfTone[] { DtmfTone.Four, DtmfTone.Five };
                        var sendDtmfAsyncResult = await callAutomationClient.GetCallConnection(callConnectionId)
                            .GetCallMedia()
                            .SendDtmfTonesAsync(tones, new PhoneNumberIdentifier(targetPhonenumber), "dtmfs-to-ivr-2");
                        logger.LogInformation("SendDtmfAsync result: {sendDtmfAsyncResult}", sendDtmfAsyncResult);
                    }
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
