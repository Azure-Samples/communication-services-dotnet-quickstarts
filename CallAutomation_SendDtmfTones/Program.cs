using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid.SystemEvents;
using Azure.Messaging.EventGrid;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Nodes;
using Newtonsoft.Json;

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
        var calleePhonenumber = builder.Configuration["calleePhonenumber"];

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
                        logger.LogInformation("CALLEE: SubscriptionValidationEvent");
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
                    if (toPhoneNumber == calleePhonenumber)
                    {
                        logger.LogInformation("incoming call");
                        var answerCallResult = await callAutomationClient.AnswerCallAsync(incomingCallContext, new Uri(callbackUriHost + "/api/incomingCallCallbacks"));
                        logger.LogInformation("CALLEE: AnswerCallAsync result: {answerCallResult}, correlationId={correlationId}", answerCallResult, correlationId);
                    }
                    else
                    {
                        logger.LogInformation("CALLEE: incoming call, phone number not found");
                    }
                }
                else
                {
                    logger.LogInformation("CALLEE: incoming call, not a phone number");
                }
            }
            return Results.Ok();
        });

        app.MapPost("/api/incomingCallCallbacks", async (CloudEvent[] cloudEvents, ILogger<Program> logger) =>
        {
            foreach (var cloudEvent in cloudEvents)
            {
                CallAutomationEventBase acsEvent = CallAutomationEventParser.Parse(cloudEvent);
                logger.LogInformation("CALLEE: Received event {eventName}: {event}", acsEvent?.GetType().Name, JsonConvert.SerializeObject(acsEvent));
                var callConnectionId = acsEvent?.CallConnectionId;

                if (acsEvent is CallConnected)
                {
                    // Start continuous DTMF recognition
                    var continuousDtmfRecognitionOptions = new ContinuousDtmfRecognitionOptions(new PhoneNumberIdentifier(callerPhonenumber))
                    {
                        OperationContext = "dtmf-reco-on-c2"
                    };
                    var startContinuousDtmfRecognitionAsyncResult = await callAutomationClient.GetCallConnection(callConnectionId)
                        .GetCallMedia()
                        .StartContinuousDtmfRecognitionAsync(continuousDtmfRecognitionOptions);
                    logger.LogInformation("CALLEE: StartContinuousDtmfRecognitionAsync, status: {status}", startContinuousDtmfRecognitionAsyncResult.Status);
                }
                if (acsEvent is ContinuousDtmfRecognitionToneReceived continuousDtmfRecognitionToneReceived)
                {
                    logger.LogInformation("CALLEE: ******************************************************************************");
                    logger.LogInformation("CALLEE: ***");
                    logger.LogInformation("CALLEE: *** Tone detected: sequenceId={sequenceId}, tone={tone}",
                        continuousDtmfRecognitionToneReceived.SequenceId,
                        continuousDtmfRecognitionToneReceived.Tone);
                    logger.LogInformation("CALLEE: ***");
                    logger.LogInformation("CALLEE: ******************************************************************************");

                    if (continuousDtmfRecognitionToneReceived.Tone.Equals(DtmfTone.Pound))
                    {
                        // Stop continuous DTMF recognition
                        var continuousDtmfRecognitionOptions = new ContinuousDtmfRecognitionOptions(new PhoneNumberIdentifier(callerPhonenumber))
                        {
                            OperationContext = "dtmf-reco-on-c2"
                        };
                        var stopContinuousDtmfRecognitionAsyncResult = await callAutomationClient.GetCallConnection(callConnectionId)
                            .GetCallMedia()
                            .StopContinuousDtmfRecognitionAsync(continuousDtmfRecognitionOptions);
                        logger.LogInformation("CALLEE: StopContinuousDtmfRecognitionAsync, status: {status}", stopContinuousDtmfRecognitionAsyncResult.Status);
                    }
                }
                if (acsEvent is ContinuousDtmfRecognitionToneFailed continuousDtmfRecognitionToneFailed)
                {
                    logger.LogInformation("CALLEE: Start continuous DTMF recognition failed, result={result}, context={context}",
                        continuousDtmfRecognitionToneFailed.ResultInformation?.Message,
                        continuousDtmfRecognitionToneFailed.OperationContext);
                }
                if (acsEvent is ContinuousDtmfRecognitionStopped continuousDtmfRecognitionStopped)
                {
                    logger.LogInformation("CALLEE: Continuous DTMF recognition stopped, context={context}", continuousDtmfRecognitionStopped.OperationContext);
                }
            }
            return Results.Ok();
        }).Produces(StatusCodes.Status200OK);

        app.MapGet("/outboundCall", async (ILogger<Program> logger) =>
        {
            var target = new PhoneNumberIdentifier(calleePhonenumber);
            var caller = new PhoneNumberIdentifier(callerPhonenumber);
            var callInvite = new CallInvite(target, caller);
            var createCallResult = await callAutomationClient.CreateCallAsync(callInvite, new Uri(callbackUriHost + "/api/outboundCallCallbacks"));
            logger.LogInformation("CALLER: CreateCallAsync result: {createCallResult}", createCallResult);
            return Results.Redirect("/index.html");
        });

        app.MapPost("/api/outboundCallCallbacks", async (CloudEvent[] cloudEvents, ILogger<Program> logger) =>
        {
            foreach (var cloudEvent in cloudEvents)
            {
                CallAutomationEventBase acsEvent = CallAutomationEventParser.Parse(cloudEvent);
                logger.LogInformation("CALLER: eceived event {eventName}: {event}", acsEvent?.GetType().Name, JsonConvert.SerializeObject(acsEvent));
                var callConnectionId = acsEvent?.CallConnectionId;

                if (acsEvent is CallConnected callConnected)
                {
                    logger.LogInformation("CALLER: ******************************************************************************");
                    logger.LogInformation("CALLER: ***");
                    logger.LogInformation("CALLER: *** Send DTMF tones 1 2 3 #");
                    logger.LogInformation("CALLER: ***");
                    logger.LogInformation("CALLER: ******************************************************************************");
                    var tones = new DtmfTone[] { DtmfTone.One, DtmfTone.Two, DtmfTone.Three, DtmfTone.Pound };
                    var sendDtmfTonesOptions = new SendDtmfTonesOptions(tones, new PhoneNumberIdentifier(calleePhonenumber))
                    {
                        OperationContext = "dtmfs-to-ivr"
                    };
                    var sendDtmfAsyncResult = await callAutomationClient.GetCallConnection(callConnectionId)
                        .GetCallMedia()
                        .SendDtmfTonesAsync(sendDtmfTonesOptions);
                    logger.LogInformation("CALLER: SendDtmfAsync result: {sendDtmfAsyncResult}", sendDtmfAsyncResult);
                }
                if (acsEvent is SendDtmfTonesCompleted sendDtmfCompleted)
                {
                    logger.LogInformation("CALLER: Send dtmf succeeded, context={context}", sendDtmfCompleted.OperationContext);
                    if (sendDtmfCompleted.OperationContext.Equals("dtmfs-to-ivr"))
                    {
                        logger.LogInformation("CALLER: ******************************************************************************");
                        logger.LogInformation("CALLER: ***");
                        logger.LogInformation("CALLER: *** Send DTMF tones 4 5");
                        logger.LogInformation("CALLER: ***");
                        logger.LogInformation("CALLER: ******************************************************************************");
                        var tones = new DtmfTone[] { DtmfTone.Four, DtmfTone.Five };
                        var sendDtmfTonesOptions = new SendDtmfTonesOptions(tones, new PhoneNumberIdentifier(calleePhonenumber))
                        {
                            OperationContext = "dtmfs-to-ivr-2"
                        };
                        var sendDtmfAsyncResult = await callAutomationClient.GetCallConnection(callConnectionId)
                            .GetCallMedia()
                            .SendDtmfTonesAsync(sendDtmfTonesOptions);
                        logger.LogInformation("CALLER: SendDtmfAsync result: {sendDtmfAsyncResult}", sendDtmfAsyncResult);
                    }
                }
                if (acsEvent is SendDtmfTonesFailed sendDtmfFailed)
                {
                    logger.LogInformation("CALLER: Send dtmf failed: result={result}, context={context}",
                        sendDtmfFailed.ResultInformation?.Message, sendDtmfFailed.OperationContext);
                }
            }
            return Results.Ok();
        }).Produces(StatusCodes.Status200OK);

        app.UseStaticFiles();

        app.Run();
    }
}
