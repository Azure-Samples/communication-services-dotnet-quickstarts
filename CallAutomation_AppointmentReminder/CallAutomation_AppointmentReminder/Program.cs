using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using CallAutomation_AppointmentReminder;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using System.Collections;
using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Fetch configuration and add call automation as singleton service
var callConfigurationSection = builder.Configuration.GetSection(nameof(CallConfiguration));
builder.Services.Configure<CallConfiguration>(callConfigurationSection);
builder.Services.AddSingleton(new CallAutomationClient(callConfigurationSection["ConnectionString"]));

var app = builder.Build();
var TargetIdentity = "";
var sourceIdentity = await app.ProvisionAzureCommunicationServicesIdentity(callConfigurationSection["ConnectionString"]);

// Api to initiate out bound call
app.MapPost("/api/call", async ([Required] string targetNo, CallAutomationClient callAutomationClient, IOptions<CallConfiguration> callConfiguration, ILogger<Program> logger) =>
{
    var acsAcquiredNumber = new PhoneNumberIdentifier(callConfiguration.Value.SourcePhoneNumber);
    if (!string.IsNullOrEmpty(targetNo))
    {
        var identities = targetNo.Split(';');
        foreach (var indentity in identities)
        {
            TargetIdentity = indentity;
            var target = new PhoneNumberIdentifier(indentity);
            var callInvite = new CallInvite(target, acsAcquiredNumber);

            var createCallOption = new CreateCallOptions(callInvite, new Uri(callConfiguration.Value.CallbackEventUri));

            var response = await callAutomationClient.CreateCallAsync(createCallOption).ConfigureAwait(false);

            logger.LogInformation($"Reponse from create call: {response.GetRawResponse()}" +
                $"CallConnection Id : {response.Value.CallConnection.CallConnectionId}");
        }

    }
    else
    {
        TargetIdentity = callConfiguration.Value.TargetPhoneNumber;
        var target = new PhoneNumberIdentifier(TargetIdentity);
        var callInvite = new CallInvite(target, acsAcquiredNumber);

        var createCallOption = new CreateCallOptions(callInvite, new Uri(callConfiguration.Value.CallbackEventUri));

        var response = await callAutomationClient.CreateCallAsync(createCallOption).ConfigureAwait(false);

        logger.LogInformation($"Reponse from create call: {response.GetRawResponse()}" +
            $"CallConnection Id : {response.Value.CallConnection.CallConnectionId}");
    }

});

//api to handle call back events
app.MapPost("/api/callbacks", async (CloudEvent[] cloudEvents, CallAutomationClient callAutomationClient, IOptions<CallConfiguration> callConfiguration, ILogger<Program> logger) =>
{
    foreach (var cloudEvent in cloudEvents)
    {
        logger.LogInformation($"Event received: {JsonConvert.SerializeObject(cloudEvent)}");

        CallAutomationEventBase @event = CallAutomationEventParser.Parse(cloudEvent);
        var callConnection = callAutomationClient.GetCallConnection(@event.CallConnectionId);
        var callConnectionMedia = callConnection.GetCallMedia();
        if (@event is CallConnected)
        {
            //Initiate recognition as call connected event is received
            logger.LogInformation($"CallConnected event received for call connection id: {@event.CallConnectionId}");
            var recognizeOptions =
            new CallMediaRecognizeDtmfOptions(CommunicationIdentifier.FromRawId(TargetIdentity), maxTonesToCollect: 1)
            {
                InterruptPrompt = true,
                InterToneTimeout = TimeSpan.FromSeconds(10),
                InitialSilenceTimeout = TimeSpan.FromSeconds(5),
                Prompt = new FileSource(new Uri(callConfiguration.Value.AppBaseUri + callConfiguration.Value.AppointmentReminderMenuAudio)),
                OperationContext = "AppointmentReminderMenu"
            };

            //Start recognition 
            await callConnectionMedia.StartRecognizingAsync(recognizeOptions);
        }
        if (@event is RecognizeCompleted { OperationContext: "AppointmentReminderMenu" })
        {
            // Play audio once recognition is completed sucessfully
            logger.LogInformation($"RecognizeCompleted event received for call connection id: {@event.CallConnectionId}");
            var recognizeCompletedEvent = (RecognizeCompleted)@event;
            var toneDetected = ((CollectTonesResult)recognizeCompletedEvent.RecognizeResult).Tones[0];
            if (toneDetected == DtmfTone.Three)
            {

                var target = callConfiguration.Value.TargetParticipant;
                var Participants = target.Split(';');

                foreach (var Participantindentity in Participants)
                {
                    var Participanttarget = new PhoneNumberIdentifier(Participantindentity);
                    var callInvite = new CallInvite(Participanttarget, new PhoneNumberIdentifier(callConfiguration.Value.SourcePhoneNumber));
                    var addParticipantOptions = new AddParticipantOptions(callInvite);
                    var response = await callConnection.AddParticipantAsync(addParticipantOptions);

                    logger.LogInformation($"Addparticipant call: {response.Value.Participant}" + $"Addparticipant call: {response.Value.Participant}"
                        + $"get response fron participat : {response.GetRawResponse}");
                    Thread.Sleep(10);

                }
                //to remove first Participant
                Thread.Sleep(10);
                var RemoveParticipant = new RemoveParticipantOptions(new PhoneNumberIdentifier(Participants[0]));
                var RemoveParticipantResult = await callConnection.RemoveParticipantAsync(RemoveParticipant);
                //to remove Second Participant
                Thread.Sleep(10);
                RemoveParticipant = new RemoveParticipantOptions(new PhoneNumberIdentifier(Participants[1]));
                RemoveParticipantResult = await callConnection.RemoveParticipantAsync(RemoveParticipant);

            }

            var playSource = Utils.GetAudioForTone(toneDetected, callConfiguration);

            // Play audio for dtmf response
            await callConnectionMedia.PlayToAllAsync(playSource, new PlayOptions { OperationContext = "ResponseToDtmf", Loop = false });
        }
        if (@event is RecognizeFailed { OperationContext: "AppointmentReminderMenu" })
        {
            logger.LogInformation($"RecognizeFailed event received for call connection id: {@event.CallConnectionId}");
            var recognizeFailedEvent = (RecognizeFailed)@event;

            // Check for time out, and then play audio message
            if (recognizeFailedEvent.ReasonCode.Equals(ReasonCode.RecognizeInitialSilenceTimedOut))
            {
                logger.LogInformation($"Recognition timed out for call connection id: {@event.CallConnectionId}");
                var playSource = new FileSource(new Uri(callConfiguration.Value.AppBaseUri + callConfiguration.Value.TimedoutAudio));

                //Play audio for time out
                await callConnectionMedia.PlayToAllAsync(playSource, new PlayOptions { OperationContext = "ResponseToDtmf", Loop = false });
            }
        }
        if (@event is PlayCompleted { OperationContext: "ResponseToDtmf" })
        {
            logger.LogInformation($"PlayCompleted event received for call connection id: {@event.CallConnectionId}");
            await callConnection.HangUpAsync(forEveryone: true);
        }
        if (@event is PlayFailed { OperationContext: "ResponseToDtmf" })
        {
            logger.LogInformation($"PlayFailed event received for call connection id: {@event.CallConnectionId}");
            await callConnection.HangUpAsync(forEveryone: true);
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
app.Run();
