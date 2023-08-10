using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Your ACS resource connection string
        var acsConnectionString = "<ACS_CONNECTION_STRING>";
        
        // Your ACS resource phone number will act as source number to start outbound call
        var acsPhonenumber = "<ACS_PHONE_NUMBER>";

        // Target phone number you want to receive the call.
        var c2Target = "<TARGET_PHONE_NUMBER>";

        // Base url of the app
        var callbackUriHost = "<CALLBACK_URI_HOST_WITH_PROTOCOL>";


        var callAutomationClient = new CallAutomationClient(acsConnectionString);
        var app = builder.Build();

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
                logger.LogInformation("Received event {eventName} for call connection id {callConnectionId}", acsEvent?.GetType().Name, acsEvent?.CallConnectionId);
                var callConnectionId = acsEvent?.CallConnectionId;

                if (acsEvent is CallConnected)
                {
                    // Start continuous DTMF recognition
                    var startContinuousDtmfRecognitionAsyncResult = await callAutomationClient.GetCallConnection(callConnectionId)
                        .GetCallMedia()
                        .StartContinuousDtmfRecognitionAsync(new PhoneNumberIdentifier(c2Target), "dtmf-reco-on-c2");
                    logger.LogInformation("StartContinuousDtmfRecognitionAsync result: {startContinuousDtmfRecognitionAsyncResult}", startContinuousDtmfRecognitionAsyncResult);
                }
                if (acsEvent is ContinuousDtmfRecognitionToneReceived continuousDtmfRecognitionToneReceived)
                {
                    logger.LogInformation("Tone detected: sequenceId={sequenceId}, tone={tone}, context={context}",
                        continuousDtmfRecognitionToneReceived.ToneInfo.SequenceId,
                        continuousDtmfRecognitionToneReceived.ToneInfo.Tone,
                        continuousDtmfRecognitionToneReceived.OperationContext);

                    // Stop continuous DTMF recognition
                    var stopContinuousDtmfRecognitionAsyncResult = await callAutomationClient.GetCallConnection(callConnectionId)
                        .GetCallMedia()
                        .StopContinuousDtmfRecognitionAsync(new PhoneNumberIdentifier(c2Target), "dtmf-reco-on-c2");
                    logger.LogInformation("StopContinuousDtmfRecognitionAsync result: {stopContinuousDtmfRecognitionAsyncResult}", stopContinuousDtmfRecognitionAsyncResult);
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

        app.UseStaticFiles();

        app.Run();
    }
}