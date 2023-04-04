using Azure;
using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using CallAutomation_AppointmentReminder;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Security.Principal;
using System.Text.RegularExpressions;

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
CommunicationIdentifierKind GetIdentifierKind(string participantnumber)
{
    //checks the identity type returns as string
    return Regex.Match(participantnumber, Constants.userIdentityRegex, RegexOptions.IgnoreCase).Success ? CommunicationIdentifierKind.UserIdentity :
          Regex.Match(participantnumber, Constants.phoneIdentityRegex, RegexOptions.IgnoreCase).Success ? CommunicationIdentifierKind.PhoneIdentity :
          CommunicationIdentifierKind.UnknownIdentity;
}

// Api to initiate out bound call
app.MapPost("/api/call", async ([Required] string targetNo, CallAutomationClient callAutomationClient, IOptions<CallConfiguration> callConfiguration, ILogger<Program> logger) =>
{

    var acsAcquiredNumber = new PhoneNumberIdentifier(callConfiguration.Value.SourcePhoneNumber);
    if (!string.IsNullOrEmpty(targetNo))
    {
        var identities = targetNo.Split(';');
        foreach (var indentity in identities)
        {
            if (!string.IsNullOrEmpty(indentity))
            {
                TargetIdentity = indentity;
                //var target = new PhoneNumberIdentifier(indentity);

                var identifierKind = GetIdentifierKind(indentity);
                if (identifierKind == CommunicationIdentifierKind.PhoneIdentity)
                {
                    var target = new PhoneNumberIdentifier(indentity);
                    var callInvite = new CallInvite(target, acsAcquiredNumber);
                    var createCallOption = new CreateCallOptions(callInvite, new Uri(callConfiguration.Value.CallbackEventUri));
                    var response = await callAutomationClient.CreateCallAsync(createCallOption).ConfigureAwait(false);
                    logger.LogInformation($"Reponse from create call: {response.GetRawResponse()}" +
                        $"CallConnection Id : {response.Value.CallConnection.CallConnectionId}");
                }

                else if (identifierKind == CommunicationIdentifierKind.UserIdentity)
                {
                    var targets = new CommunicationUserIdentifier(indentity);
                    var callInvite = new CallInvite(targets);
                    var createCallOption = new CreateCallOptions(callInvite, new Uri(callConfiguration.Value.CallbackEventUri));
                    var response = await callAutomationClient.CreateCallAsync(createCallOption).ConfigureAwait(false);
                    logger.LogInformation($"Reponse from create call: {response.GetRawResponse()}" +
                        $"CallConnection Id : {response.Value.CallConnection.CallConnectionId}");
                }
            }

        }

    }
    else
    {
        TargetIdentity = callConfiguration.Value.TargetPhoneNumber;
        if (!string.IsNullOrEmpty(TargetIdentity))
        {
            var identifierKind = GetIdentifierKind(TargetIdentity);
            if (identifierKind == CommunicationIdentifierKind.PhoneIdentity)
            {
                var target = new PhoneNumberIdentifier(TargetIdentity);
                var callInvite = new CallInvite(target, acsAcquiredNumber);
                var createCallOption = new CreateCallOptions(callInvite, new Uri(callConfiguration.Value.CallbackEventUri));
                var response = await callAutomationClient.CreateCallAsync(createCallOption).ConfigureAwait(false);
                logger.LogInformation($"Reponse from create call: {response.GetRawResponse()}" +
                    $"CallConnection Id : {response.Value.CallConnection.CallConnectionId}");
            }

            else if (identifierKind == CommunicationIdentifierKind.UserIdentity)
            {
                var targets = new CommunicationUserIdentifier(TargetIdentity);
                var callInvite = new CallInvite(targets);
                var createCallOption = new CreateCallOptions(callInvite, new Uri(callConfiguration.Value.CallbackEventUri));
                var response = await callAutomationClient.CreateCallAsync(createCallOption).ConfigureAwait(false);
                logger.LogInformation($"Reponse from create call: {response.GetRawResponse()}" +
                    $"CallConnection Id : {response.Value.CallConnection.CallConnectionId}");
            }
        }
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
            logger.LogInformation($"CallConnected event received for call connection id: {@event.CallConnectionId}" + $" Correlation id: {@event.CorrelationId}");
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
            logger.LogInformation($"RecognizeCompleted event received for call connection id: {@event.CallConnectionId}" + $" Correlation id: {@event.CorrelationId}");
            var recognizeCompletedEvent = (RecognizeCompleted)@event;
            var toneDetected = ((CollectTonesResult)recognizeCompletedEvent.RecognizeResult).Tones[0];
            if (toneDetected == DtmfTone.Three)
            {

                var playSource = Utils.GetAudioForTone(toneDetected, callConfiguration);
                // Play audio for dtmf response
                await callConnectionMedia.PlayToAllAsync(playSource, new PlayOptions { OperationContext = "AgentConnect", Loop = false });

            }
            else
            {

                var playSource = Utils.GetAudioForTone(toneDetected, callConfiguration);
                // Play audio for dtmf response
                await callConnectionMedia.PlayToAllAsync(playSource, new PlayOptions { OperationContext = "ResponseToDtmf", Loop = false });
            }
        }
        if (@event is PlayCompleted { OperationContext: "AgentConnect" })
        {
            var target = callConfiguration.Value.AddParticipantNumber;
            var Participants = target.Split(';');
            var count = 0;
            foreach (var Participantindentity in Participants)
            {

                if (!string.IsNullOrEmpty(Participantindentity))
                {
                    var identifierKind = GetIdentifierKind(Participantindentity);
                    if (identifierKind == CommunicationIdentifierKind.PhoneIdentity)
                    {
                        var Participanttarget = new PhoneNumberIdentifier(Participantindentity);
                        var callInvite = new CallInvite(Participanttarget, new PhoneNumberIdentifier(callConfiguration.Value.SourcePhoneNumber));
                        var addParticipantOptions = new AddParticipantOptions(callInvite);
                        var response = await callConnection.AddParticipantAsync(addParticipantOptions);
                        logger.LogInformation($"AddParticipant event received for call connection id: {@event.CallConnectionId}" + $" Correlation id: {@event.CorrelationId}");
                        var playSource = new FileSource(new Uri(callConfiguration.Value.AppBaseUri + callConfiguration.Value.AddParticipant));
                        // Play audio for dtmf response
                        await callConnectionMedia.PlayToAllAsync(playSource, new PlayOptions { OperationContext = "addParticipant", Loop = false });

                        TimeSpan InterToneTimeout = TimeSpan.FromSeconds(10);
                        TimeSpan InitialSilenceTimeout = TimeSpan.FromSeconds(5);
                        logger.LogInformation($"Addparticipant call: {response.Value.Participant}" + $"  Addparticipant ID: {Participantindentity}"
                             + $"  get response fron participat : {response.GetRawResponse}");
                    }

                    else if (identifierKind == CommunicationIdentifierKind.UserIdentity)
                    {
                        var Participanttarget = new CommunicationUserIdentifier(Participantindentity);
                        var callInvite = new CallInvite(Participanttarget);
                        var addParticipantOptions = new AddParticipantOptions(callInvite);
                        var response = await callConnection.AddParticipantAsync(addParticipantOptions);
                        logger.LogInformation($"AddParticipant event received for call connection id: {@event.CallConnectionId}" + $" Correlation id: {@event.CorrelationId}");
                        var playSource = new FileSource(new Uri(callConfiguration.Value.AppBaseUri + callConfiguration.Value.AddParticipant));
                        // Play audio for dtmf response
                        await callConnectionMedia.PlayToAllAsync(playSource, new PlayOptions { OperationContext = "addParticipant", Loop = false });
                        TimeSpan InterToneTimeout = TimeSpan.FromSeconds(10);
                        TimeSpan InitialSilenceTimeout = TimeSpan.FromSeconds(5);
                        logger.LogInformation($"Addparticipant call: {response.Value.Participant}" + $"  Addparticipant ID: {Participantindentity}"
                             + $"  get response fron participat : {response.GetRawResponse}");
                    }
                }
                count++;
            }
            logger.LogInformation($"List of Participants: {count}" + $"  participant ids :{await callConnection.GetParticipantsAsync()}");
            // logger.LogInformation($"List of Participants: {count}" + $"List of Participant ID's: {target}");
            //to remove first Participant
            if (Participants.Length >= 2)
            {
                for (var i = 0; i < 2; i++)
                {
                    if (!string.IsNullOrEmpty(Participants[i]))
                    {
                        var identifierKind = GetIdentifierKind(Participants[i]);
                        if (identifierKind == CommunicationIdentifierKind.PhoneIdentity)
                        {
                            var Participanttarget = new PhoneNumberIdentifier(Participants[i]);
                            var RemoveParticipant = new RemoveParticipantOptions(Participanttarget);
                            var RemoveParticipantResult = await callConnection.RemoveParticipantAsync(RemoveParticipant);
                            logger.LogInformation($"RemoveParticipant event received for call connection id: {@event.CallConnectionId}" + $" Correlation id: {@event.CorrelationId}");
                            var playSource = new FileSource(new Uri(callConfiguration.Value.AppBaseUri + callConfiguration.Value.RemoveParticipant));
                            // Play audio for dtmf response
                            await callConnectionMedia.PlayToAllAsync(playSource, new PlayOptions { OperationContext = "RemoveParticipant", Loop = false });
                            logger.LogInformation($"Removeparticipant call: {Participants[i]}"
                                    + $"get response fron participat : {RemoveParticipantResult.GetRawResponse}");
                        }

                        else if (identifierKind == CommunicationIdentifierKind.UserIdentity)
                        {
                            var Participanttarget = new CommunicationUserIdentifier(Participants[i]);
                            var RemoveParticipant = new RemoveParticipantOptions(Participanttarget);
                            var RemoveParticipantResult = await callConnection.RemoveParticipantAsync(RemoveParticipant);
                            logger.LogInformation($"RemoveParticipant event received for call connection id: {@event.CallConnectionId}" + $" Correlation id: {@event.CorrelationId}");
                            var playSource = new FileSource(new Uri(callConfiguration.Value.AppBaseUri + callConfiguration.Value.RemoveParticipant));
                            // Play audio for dtmf response
                            await callConnectionMedia.PlayToAllAsync(playSource, new PlayOptions { OperationContext = "RemoveParticipant", Loop = false });
                            logger.LogInformation($"Removeparticipant call: {Participants[i]}"
                                    + $"get response fron participat : {RemoveParticipantResult.GetRawResponse}");
                        }
                    }
                }
            }
            if (@event is PlayCompleted { OperationContext: "addParticipant" })
            {
                logger.LogInformation($"PlayCompleted event received for Add Participant Call connection id: {@event.CallConnectionId}" + $" Correlation id: {@event.CorrelationId}");
                await callConnection.HangUpAsync(forEveryone: true);
            }
            if (@event is PlayFailed { OperationContext: "addParticipant" })
            {
                logger.LogInformation($"PlayFailed event received for Add Participantv Call connection id: {@event.CallConnectionId}" + $" Correlation id: {@event.CorrelationId}");
                await callConnection.HangUpAsync(forEveryone: true);
            }
            if (@event is PlayCompleted { OperationContext: "RemoveParticipant" })
            {
                logger.LogInformation($"PlayCompleted event received for Remove Participant call connection id: {@event.CallConnectionId}" + $" Correlation id: {@event.CorrelationId}");
                await callConnection.HangUpAsync(forEveryone: true);
            }
            if (@event is PlayFailed { OperationContext: "RemoveParticipant" })
            {
                logger.LogInformation($"PlayFailed event received for Remove Participant call connection id: {@event.CallConnectionId}" + $" Correlation id: {@event.CorrelationId}");
                await callConnection.HangUpAsync(forEveryone: true);
            }
        }
        if (@event is RecognizeFailed { OperationContext: "AppointmentReminderMenu" })
        {
            logger.LogInformation($"RecognizeFailed event received for call connection id: {@event.CallConnectionId}" + $" Correlation id: {@event.CorrelationId}");
            var recognizeFailedEvent = (RecognizeFailed)@event;

            // Check for time out, and then play audio message
            if (recognizeFailedEvent.ReasonCode.Equals(ReasonCode.RecognizeInitialSilenceTimedOut))
            {
                logger.LogInformation($"Recognition timed out for call connection id: {@event.CallConnectionId}" + $" Correlation id: {@event.CorrelationId}");
                var playSource = new FileSource(new Uri(callConfiguration.Value.AppBaseUri + callConfiguration.Value.TimedoutAudio));

                //Play audio for time out
                await callConnectionMedia.PlayToAllAsync(playSource, new PlayOptions { OperationContext = "ResponseToDtmf", Loop = false });
            }
        }

        if (@event is PlayCompleted { OperationContext: "ResponseToDtmf" })
        {
            logger.LogInformation($"PlayCompleted event received for call connection id: {@event.CallConnectionId}" + $" Correlation id: {@event.CorrelationId}");
            await callConnection.HangUpAsync(forEveryone: true);
        }
        if (@event is PlayFailed { OperationContext: "ResponseToDtmf" })
        {
            logger.LogInformation($"PlayFailed event received for call connection id: {@event.CallConnectionId}" + $" Correlation id: {@event.CorrelationId}");
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
public enum CommunicationIdentifierKind
{
    PhoneIdentity,
    UserIdentity,
    UnknownIdentity

}
public class Constants
{
    public const string userIdentityRegex = @"8:acs:[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}_[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}";
    public const string phoneIdentityRegex = @"^\+\d{10,14}$";

}
