using Azure;
using Azure.Communication;
using Azure.Communication.CallAutomation;
using Call_Automation_GCCH.Core.Interfaces;
using Call_Automation_GCCH.Services;
using Microsoft.Extensions.Logging;

namespace Call_Automation_GCCH.Infrastructure.Services;

/// <summary>
/// Infrastructure layer implementation of IParticipantService
/// Delegates to existing ICallAutomationService for Azure SDK operations
/// </summary>
public class AzureParticipantService : IParticipantService
{
    private readonly ICallAutomationService _callAutomationService;
    private readonly ILogger<AzureParticipantService> _logger;
    private readonly string _sourcePhoneNumber;

    public AzureParticipantService(
        ICallAutomationService callAutomationService,
        ILogger<AzureParticipantService> logger,
        IConfiguration configuration)
    {
        _callAutomationService = callAutomationService ?? throw new ArgumentNullException(nameof(callAutomationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sourcePhoneNumber = configuration["CommunicationSettings:AcsPhoneNumber"] ?? string.Empty;
    }

    public async Task<string> AddParticipantAsync(
        string callConnectionId,
        string participantId,
        bool isPstn,
        string? operationContext = null,
        int? invitationTimeout = null)
    {
        _logger.LogInformation("Adding participant {Participant} to call {CallConnectionId}",
            participantId, callConnectionId);

        var callConnection = _callAutomationService.GetCallConnection(callConnectionId);

        CallInvite invite = isPstn
            ? new CallInvite(new PhoneNumberIdentifier(participantId), new PhoneNumberIdentifier(_sourcePhoneNumber))
            : new CallInvite(new CommunicationUserIdentifier(participantId));

        var addParticipantOptions = new AddParticipantOptions(invite)
        {
            OperationContext = operationContext
        };

        if (invitationTimeout.HasValue)
        {
            addParticipantOptions.InvitationTimeoutInSeconds = invitationTimeout.Value;
        }

        var response = await callConnection.AddParticipantAsync(addParticipantOptions);
        return operationContext ?? "ParticipantAdded";
    }

    public async Task<string> RemoveParticipantAsync(
        string callConnectionId,
        string participantId,
        bool isPstn,
        string? operationContext = null)
    {
        _logger.LogInformation("Removing participant {Participant} from call {CallConnectionId}",
            participantId, callConnectionId);

        var callConnection = _callAutomationService.GetCallConnection(callConnectionId);

        var participant = isPstn
            ? (CommunicationIdentifier)new PhoneNumberIdentifier(participantId)
            : new CommunicationUserIdentifier(participantId);

        var removeParticipantOptions = new RemoveParticipantOptions(participant)
        {
            OperationContext = operationContext
        };

        var response = await callConnection.RemoveParticipantAsync(removeParticipantOptions);
        return operationContext ?? "ParticipantRemoved";
    }

    public async Task<ParticipantInfo> GetParticipantAsync(
        string callConnectionId,
        string participantId,
        bool isPstn)
    {
        _logger.LogInformation("Getting participant {Participant} from call {CallConnectionId}",
            participantId, callConnectionId);

        var callConnection = _callAutomationService.GetCallConnection(callConnectionId);

        var participant = isPstn
            ? (CommunicationIdentifier)new PhoneNumberIdentifier(participantId)
            : new CommunicationUserIdentifier(participantId);

        var response = await callConnection.GetParticipantAsync(participant);

        return new ParticipantInfo
        {
            RawId = participantId,
            IsOnHold = response.Value.IsOnHold,
            IsMuted = response.Value.IsMuted
        };
    }

    public async Task<IEnumerable<ParticipantInfo>> GetAllParticipantsAsync(string callConnectionId)
    {
        _logger.LogInformation("Getting all participants from call {CallConnectionId}", callConnectionId);

        var callConnection = _callAutomationService.GetCallConnection(callConnectionId);
        var response = await callConnection.GetParticipantsAsync();
        var participants = new List<ParticipantInfo>();

        foreach (var participant in response.Value)
        {
            participants.Add(new ParticipantInfo
            {
                RawId = participant.Identifier.RawId,
                IsOnHold = participant.IsOnHold,
                IsMuted = participant.IsMuted
            });
        }

        return participants;
    }

    public async Task<string> MuteParticipantAsync(
        string callConnectionId,
        string participantId,
        bool isPstn)
    {
        _logger.LogInformation("Muting participant {Participant} in call {CallConnectionId}",
            participantId, callConnectionId);

        var callConnection = _callAutomationService.GetCallConnection(callConnectionId);

        var participant = isPstn
            ? (CommunicationIdentifier)new PhoneNumberIdentifier(participantId)
            : new CommunicationUserIdentifier(participantId);

        var response = await callConnection.MuteParticipantAsync(participant);
        return "ParticipantMuted";
    }

    public async Task<string> CancelAddParticipantAsync(
        string callConnectionId,
        string invitationId,
        string? operationContext = null)
    {
        _logger.LogInformation("Cancelling add participant invitation {InvitationId} in call {CallConnectionId}",
            invitationId, callConnectionId);

        var callConnection = _callAutomationService.GetCallConnection(callConnectionId);

        var options = new CancelAddParticipantOperationOptions(invitationId)
        {
            OperationContext = operationContext
        };

        var response = await callConnection.CancelAddParticipantOperationAsync(options);
        return operationContext ?? "AddParticipantCancelled";
    }

    public async Task AddParticipantAsync(string callConnectionId, string participantId)
    {
        await AddParticipantAsync(callConnectionId, participantId, false);
    }

    public async Task RemoveParticipantAsync(string callConnectionId, string participantId)
    {
        await RemoveParticipantAsync(callConnectionId, participantId, false);
    }

    public async Task MuteParticipantAsync(string callConnectionId, string participantId)
    {
        await MuteParticipantAsync(callConnectionId, participantId, false);
    }
}
