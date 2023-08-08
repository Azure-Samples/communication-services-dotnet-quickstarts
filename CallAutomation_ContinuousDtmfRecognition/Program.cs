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
        var targetPhonenumber = "<TARGET_PHONE_NUMBER>";

        // Base url of the app
        var callbackUriHost = "<CALLBACK_URI_HOST_WITH_PROTOCOL>";

        var callAutomationClient = new CallAutomationClient(acsConnectionString);
        var app = builder.Build();

        app.MapGet("/outboundCall", async (ILogger<Program> logger) =>
        {
            var target = new PhoneNumberIdentifier(targetPhonenumber);
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
                CallAutomationEventBase parsedEvent = CallAutomationEventParser.Parse(cloudEvent);
                logger.LogInformation("Received event {eventName} for call connection id {callConnectionId}", parsedEvent?.GetType().Name, parsedEvent?.CallConnectionId);
                var callConnection = callAutomationClient.GetCallConnection(parsedEvent?.CallConnectionId);
                var callMedia = callConnection.GetCallMedia();

                if (parsedEvent is CallConnected)
                {
                    // Start continuous DTMF recognition
                    var targetParticipant = new PhoneNumberIdentifier(targetPhonenumber);
                    var operationContext = "Customer";
                    var startContinuousDtmfRecognitionAsyncResult = await callMedia.StartContinuousDtmfRecognitionAsync(targetParticipant, operationContext);
                    logger.LogInformation("StartContinuousDtmfRecognitionAsync result: {startContinuousDtmfRecognitionAsyncResult}", startContinuousDtmfRecognitionAsyncResult);
                }
                else if (parsedEvent is ContinuousDtmfRecognitionToneReceived continuousDtmfRecognitionToneReceived)
                {
                    logger.LogInformation("DTMF tone received: {tone}", continuousDtmfRecognitionToneReceived.ToneInfo.Tone.ToChar());
                    await callConnection.HangUpAsync(true);
                }
                else if (parsedEvent is ContinuousDtmfRecognitionToneFailed continuousDtmfRecognitionToneFailed)
                {
                    logger.LogInformation("StartContinuousDtmfRecognitionAsync failed with ResultInformation: {resultInformation}", continuousDtmfRecognitionToneFailed.ResultInformation);
                    await callConnection.HangUpAsync(true);
                }
            }
            return Results.Ok();
        }).Produces(StatusCodes.Status200OK);

        app.UseStaticFiles();

        app.Run();
    }
}
