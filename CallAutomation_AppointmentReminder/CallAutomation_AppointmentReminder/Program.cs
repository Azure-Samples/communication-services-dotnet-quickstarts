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
using System.Linq;
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
        foreach (var target in identities)
        {
            if (!string.IsNullOrEmpty(target))
            {
                TargetIdentity = target;
                //var target = new PhoneNumberIdentifier(indentity);
                CallInvite? callInvite = null;
                var identifierKind = GetIdentifierKind(target);
                if (identifierKind == CommunicationIdentifierKind.PhoneIdentity)
                {
                    callInvite = new CallInvite(new PhoneNumberIdentifier(target), acsAcquiredNumber);
                }
                else if (identifierKind == CommunicationIdentifierKind.UserIdentity)
                {
                    callInvite = new CallInvite(new CommunicationUserIdentifier(target));
                }
                var createCallOption = new CreateCallOptions(callInvite, new Uri(callConfiguration.Value.CallbackEventUri));
                var response = await callAutomationClient.CreateCallAsync(createCallOption).ConfigureAwait(false);
                logger.LogInformation($"Reponse from create call: {response.GetRawResponse()}" +
                    $"CallConnection Id : {response.Value.CallConnection.CallConnectionId}");
            }

        }

    }
    else
    {
        TargetIdentity = callConfiguration.Value.TargetPhoneNumber;
        if (!string.IsNullOrEmpty(TargetIdentity))
        {
            var identifierKind = GetIdentifierKind(TargetIdentity);
            CallInvite? callInvite = null;
            if (identifierKind == CommunicationIdentifierKind.PhoneIdentity)
            {
                callInvite = new CallInvite(new PhoneNumberIdentifier(TargetIdentity), acsAcquiredNumber);
            }

            else if (identifierKind == CommunicationIdentifierKind.UserIdentity)
            {
                callInvite = new CallInvite(new CommunicationUserIdentifier(TargetIdentity));
            }
            var createCallOption = new CreateCallOptions(callInvite, new Uri(callConfiguration.Value.CallbackEventUri));
            var response = await callAutomationClient.CreateCallAsync(createCallOption).ConfigureAwait(false);
            logger.LogInformation($"Reponse from create call: {response.GetRawResponse()}" +
                $"CallConnection Id : {response.Value.CallConnection.CallConnectionId}");
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
            foreach (var Participantindentity in Participants)
            {
                CallInvite? callInvite = null;
                if (!string.IsNullOrEmpty(Participantindentity))
                {
                    var identifierKind = GetIdentifierKind(Participantindentity);
                    if (identifierKind == CommunicationIdentifierKind.PhoneIdentity)
                    {
                        callInvite = new CallInvite(new PhoneNumberIdentifier(Participantindentity), new PhoneNumberIdentifier(callConfiguration.Value.SourcePhoneNumber));
                    }

                    else if (identifierKind == CommunicationIdentifierKind.UserIdentity)
                    {
                        callInvite = new CallInvite(new CommunicationUserIdentifier(Participantindentity));
                    }

                    var addParticipantOptions = new AddParticipantOptions(callInvite);
                    var response = await callConnection.AddParticipantAsync(addParticipantOptions);
                    var playSource = new FileSource(new Uri(callConfiguration.Value.AppBaseUri + callConfiguration.Value.AddParticipant));
                    await callConnectionMedia.PlayToAllAsync(playSource, new PlayOptions { OperationContext = "addParticipant", Loop = false });
                    await Task.Delay(TimeSpan.FromSeconds(10));
                    

                    logger.LogInformation($"AddParticipant event received for call connection id: {@event.CallConnectionId}" + $" Correlation id: {@event.CorrelationId}");
                    logger.LogInformation($"Add participant call : {response.Value.Participant}" + $"  Status of call :{response.GetRawResponse().Status}"
                        + $"  participant ID: {response.Value.Participant.Identifier}" + $" participat is muted : {response.Value.Participant.IsMuted}"+$"  Call Connection Properties: {callConnection.GetCallConnectionProperties()}");
                     

                }

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
        if (@event is AddParticipantSucceeded )
        {            
            
            IReadOnlyList<CallParticipant> participantsList = callConnection.GetParticipantsAsync().Result.Value;
            foreach (var Participant in participantsList)
            {
                logger.LogInformation($"participant ID :{Participant.Identifier.RawId}");
            }
            logger.LogInformation($"Total participants in the call : {participantsList.Count}");

            //await callConnection.HangUpAsync(false);
            //logger.LogInformation($"CA is hangup the call." + $"Information of Call:{callConnection.GetCallConnectionProperties()}");

            List<CallParticipant> participantsToRemoveAll = (await callConnection.GetParticipantsAsync()).Value.ToList();

            foreach (CallParticipant participantToRemove in participantsToRemoveAll)
            {
                //await callConnection.HangUpAsync(false);
                //logger.LogInformation($"CA is hangup the call." + $"Information of Call:{callConnection.GetCallConnectionProperties()}");

                // await Task.Delay(TimeSpan.FromSeconds(30));
                var Plist = callConfiguration.Value.AddParticipantNumber;
                if (Plist.Contains(participantToRemove.Identifier.ToString()))
                {
                    CommunicationIdentifier RemoveParticipants = null;
                    var RemoveId = participantToRemove.Identifier;
                    var identifierKind = GetIdentifierKind(RemoveId.RawId);
                    if (identifierKind == CommunicationIdentifierKind.PhoneIdentity)
                    {
                        RemoveParticipants = new PhoneNumberIdentifier(participantToRemove.Identifier.ToString());
                    }

                    else if (identifierKind == CommunicationIdentifierKind.UserIdentity)
                    {
                        RemoveParticipants = new CommunicationUserIdentifier(RemoveId.RawId);
                    }
                    var RemoveParticipant = new RemoveParticipantOptions(participantToRemove.Identifier);
                    await callConnection.RemoveParticipantAsync(RemoveParticipant);

                }

            }




        }
        if (@event is AddParticipantFailed)
        {
            AddParticipantFailed addParticipantFailed = (AddParticipantFailed)@event;           
            logger.LogInformation($"Add participant failed RawId:{addParticipantFailed.Participant.RawId}"+$"Result Information:{addParticipantFailed.ResultInformation.Message}");
            
        }

        if (@event is RemoveParticipantSucceeded)
        {
            RemoveParticipantSucceeded RemoveParticipantSucceeded = (RemoveParticipantSucceeded)@event;

            logger.LogInformation($"Remove Participant Succeeded RawId : {RemoveParticipantSucceeded.Participant.RawId}");
        }
        if (@event is RemoveParticipantFailed)
        {
            RemoveParticipantFailed removeParticipantFailed = (RemoveParticipantFailed)@event;
            logger.LogInformation($"Remove participant failed RawId:{removeParticipantFailed.Participant.RawId}");
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
