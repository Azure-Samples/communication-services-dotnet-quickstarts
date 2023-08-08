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
                    // Send DTMF tones
                    var tones = new DtmfTone[] { DtmfTone.One, DtmfTone.Two, DtmfTone.Three };
                    var targetParticipant = new PhoneNumberIdentifier(targetPhonenumber);
                    var operationContext = "Consultant IVR";
                    var sendDtmfAsyncResult = await callMedia.SendDtmfTonesAsync(tones, targetParticipant, operationContext);
                    logger.LogInformation("SendDtmfAsync result: {sendDtmfAsyncResult}", sendDtmfAsyncResult);
                }
                else if (parsedEvent is SendDtmfTonesCompleted sendDtmfCompleted)
                {
                    logger.LogInformation("SendDtmf completed successfully");
                    await callConnection.HangUpAsync(true);
                }
                else if (parsedEvent is SendDtmfTonesFailed sendDtmfFailed)
                {
                    logger.LogInformation("SendDtmf failed with ResultInformation: {resultInformation}", sendDtmfFailed.ResultInformation);
                    await callConnection.HangUpAsync(true);
                }
            }
            return Results.Ok();
        }).Produces(StatusCodes.Status200OK);

        app.UseStaticFiles();

        app.Run();
    }
}
