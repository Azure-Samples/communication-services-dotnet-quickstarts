using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using CallAutomation_AppointmentReminder;
using Microsoft.Extensions.FileProviders;
using static CallAutomation_AppointmentReminder.Logger;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var sourceIdentity = await Identity.CreateUser(builder.Configuration["ConnectionString"]);
var phoneNumberToAddToCall = builder.Configuration["TargetPhoneNumber"];
builder.Services.AddSingleton(new CallAutomationClient(builder.Configuration["ConnectionString"]));
builder.Services.AddSingleton(new CallConfiguration(builder.Configuration["ConnectionString"],
    sourceIdentity.Id,
    builder.Configuration["ApplicationPhoneNumber"],
    builder.Configuration["AppBaseUri"]));

var app = builder.Build();

app.MapPost("/api/call", async (CallAutomationClient callAutomationClient, CallConfiguration callConfiguration) =>
{
    var source = new CallSource(new CommunicationUserIdentifier(callConfiguration.SourceIdentity))
    {
        CallerId = new PhoneNumberIdentifier(callConfiguration.SourcePhoneNumber)
    };
    var target = new PhoneNumberIdentifier(phoneNumberToAddToCall);

    var createCallOption = new CreateCallOptions(source,
        new List<CommunicationIdentifier>() { target },
        new Uri(callConfiguration.CallbackEventUri));

    var response = await callAutomationClient.CreateCallAsync(createCallOption).ConfigureAwait(false);

    Logger.LogMessage(MessageType.INFORMATION, $"Reponse from create call: {response.GetRawResponse()}" +
        $"CallConnection Id : {response.Value.CallConnection.CallConnectionId}");
});

app.MapPost("/api/callbacks", async (CloudEvent[] cloudEvents, CallAutomationClient callAutomationClient, CallConfiguration callConfiguration) =>
{
    // handle callbacks
    foreach (var cloudEvent in cloudEvents)
    {
        CallAutomationEventBase @event = CallAutomationEventParser.Parse(cloudEvent);
        if (@event is CallConnected)
        {
            Logger.LogMessage(MessageType.INFORMATION, $"CallConnected event recieved for callconnetion id: {@event.CallConnectionId}");

            var recognizeOptions =
            new CallMediaRecognizeDtmfOptions(CommunicationIdentifier.FromRawId(phoneNumberToAddToCall), maxTonesToCollect: 1)
            {
                InterruptPrompt = true,
                InterToneTimeout = TimeSpan.FromSeconds(10),
                InitialSilenceTimeout = TimeSpan.FromSeconds(5),
                Prompt = new FileSource(new Uri(callConfiguration.AppBaseUri + Constants.AppointmentReminderMenuAudio)),
                OperationContext = "AppointmentReminderMenu"
            };
            await callAutomationClient.GetCallConnection(@event.CallConnectionId)
                .GetCallMedia()
                .StartRecognizingAsync(recognizeOptions);

        }
        if (@event is RecognizeCompleted { OperationContext: "AppointmentReminderMenu" })
        {
            Logger.LogMessage(MessageType.INFORMATION, $"RecognizeCompleted event recieved for callconnetion id: {@event.CallConnectionId}");

            var recognizeEvent = (RecognizeCompleted)@event;
            var toneDetected = recognizeEvent.CollectTonesResult.Tones[0];
            FileSource playSource;

            if (toneDetected.Equals(DtmfTone.One))
            {
                playSource = new FileSource(new Uri(callConfiguration.AppBaseUri + Constants.AppointmentConfirmedAudio));
            }
            else if (toneDetected.Equals(DtmfTone.Two))
            {
                playSource = new FileSource(new Uri(callConfiguration.AppBaseUri + Constants.AppointmentCancelledAudio));
            }
            else // Invalid Dtmf tone
            {
                playSource = new FileSource(new Uri(callConfiguration.AppBaseUri + Constants.InvalidToneAudio));
            }

            // Play audio for dtmf response
            await callAutomationClient.GetCallConnection(@event.CallConnectionId)
                .GetCallMedia()
                .PlayToAllAsync(playSource, new PlayOptions { OperationContext = "ResponseToDtmf", Loop = false });
        }
        if (@event is RecognizeFailed { OperationContext: "AppointmentReminderMenu" })
        {
            Logger.LogMessage(MessageType.INFORMATION, $"RecognizeFailed event recieved for callconnetion id: {@event.CallConnectionId}");

            var playSource = new FileSource(new Uri(callConfiguration.AppBaseUri + Constants.InvalidToneAudio));
            await callAutomationClient.GetCallConnection(@event.CallConnectionId)
                .GetCallMedia()
                .PlayToAllAsync(playSource);
        }
        if (@event is PlayCompleted { OperationContext: "ResponseToDtmf" })
        {
            Logger.LogMessage(MessageType.INFORMATION, $"PlayCompleted event recieved for callconnetion id: {@event.CallConnectionId}");

            await callAutomationClient.GetCallConnection(@event.CallConnectionId).HangUpAsync(forEveryone: true);
        }
        if (@event is PlayFailed { OperationContext: "ResponseToDtmf" })
        {
            Logger.LogMessage(MessageType.INFORMATION, $"PlayFailed event recieved for callconnetion id: {@event.CallConnectionId}");

            await callAutomationClient.GetCallConnection(@event.CallConnectionId).HangUpAsync(forEveryone: true);
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

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
