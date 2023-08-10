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
