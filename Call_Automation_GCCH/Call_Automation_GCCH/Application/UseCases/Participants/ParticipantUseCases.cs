using Call_Automation_GCCH.Core.Common;
using Call_Automation_GCCH.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Call_Automation_GCCH.Application.UseCases.Participants;

public class AddParticipantUseCase
{
    private readonly IParticipantService _participantService;
    private readonly ILogger<AddParticipantUseCase> _logger;

    public AddParticipantUseCase(IParticipantService participantService, ILogger<AddParticipantUseCase> logger)
    {
        _participantService = participantService;
        _logger = logger;
    }

    public async Task<Result<string>> ExecuteAsync(string callConnectionId, string participantId, bool isPstn, string? operationContext = null, int? invitationTimeout = null)
    {
        try
        {
            _logger.LogInformation("Adding participant {Participant} to call {CallConnectionId}", participantId, callConnectionId);

            if (string.IsNullOrWhiteSpace(callConnectionId))
                return Result<string>.Failure("Call connection ID is required");

            if (string.IsNullOrWhiteSpace(participantId))
                return Result<string>.Failure("Participant ID is required");

            var result = await _participantService.AddParticipantAsync(callConnectionId, participantId, isPstn, operationContext, invitationTimeout);
            return Result<string>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding participant");
            return Result<string>.Failure($"Failed to add participant: {ex.Message}");
        }
    }
}

public class RemoveParticipantUseCase
{
    private readonly IParticipantService _participantService;
    private readonly ILogger<RemoveParticipantUseCase> _logger;

    public RemoveParticipantUseCase(IParticipantService participantService, ILogger<RemoveParticipantUseCase> logger)
    {
        _participantService = participantService;
        _logger = logger;
    }

    public async Task<Result<string>> ExecuteAsync(string callConnectionId, string participantId, bool isPstn, string? operationContext = null)
    {
        try
        {
            _logger.LogInformation("Removing participant {Participant} from call {CallConnectionId}", participantId, callConnectionId);

            if (string.IsNullOrWhiteSpace(callConnectionId))
                return Result<string>.Failure("Call connection ID is required");

            if (string.IsNullOrWhiteSpace(participantId))
                return Result<string>.Failure("Participant ID is required");

            var result = await _participantService.RemoveParticipantAsync(callConnectionId, participantId, isPstn, operationContext);
            return Result<string>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing participant");
            return Result<string>.Failure($"Failed to remove participant: {ex.Message}");
        }
    }
}

public class GetParticipantUseCase
{
    private readonly IParticipantService _participantService;
    private readonly ILogger<GetParticipantUseCase> _logger;

    public GetParticipantUseCase(IParticipantService participantService, ILogger<GetParticipantUseCase> logger)
    {
        _participantService = participantService;
        _logger = logger;
    }

    public async Task<Result<ParticipantInfo>> ExecuteAsync(string callConnectionId, string participantId, bool isPstn)
    {
        try
        {
            _logger.LogInformation("Getting participant {Participant} from call {CallConnectionId}", participantId, callConnectionId);

            if (string.IsNullOrWhiteSpace(callConnectionId))
                return Result<ParticipantInfo>.Failure("Call connection ID is required");

            if (string.IsNullOrWhiteSpace(participantId))
                return Result<ParticipantInfo>.Failure("Participant ID is required");

            var result = await _participantService.GetParticipantAsync(callConnectionId, participantId, isPstn);
            return Result<ParticipantInfo>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting participant");
            return Result<ParticipantInfo>.Failure($"Failed to get participant: {ex.Message}");
        }
    }
}

public class GetAllParticipantsUseCase
{
    private readonly IParticipantService _participantService;
    private readonly ILogger<GetAllParticipantsUseCase> _logger;

    public GetAllParticipantsUseCase(IParticipantService participantService, ILogger<GetAllParticipantsUseCase> logger)
    {
        _participantService = participantService;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<ParticipantInfo>>> ExecuteAsync(string callConnectionId)
    {
        try
        {
            _logger.LogInformation("Getting all participants from call {CallConnectionId}", callConnectionId);

            if (string.IsNullOrWhiteSpace(callConnectionId))
                return Result<IEnumerable<ParticipantInfo>>.Failure("Call connection ID is required");

            var result = await _participantService.GetAllParticipantsAsync(callConnectionId);
            return Result<IEnumerable<ParticipantInfo>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting participants");
            return Result<IEnumerable<ParticipantInfo>>.Failure($"Failed to get participants: {ex.Message}");
        }
    }
}

public class MuteParticipantUseCase
{
    private readonly IParticipantService _participantService;
    private readonly ILogger<MuteParticipantUseCase> _logger;

    public MuteParticipantUseCase(IParticipantService participantService, ILogger<MuteParticipantUseCase> logger)
    {
        _participantService = participantService;
        _logger = logger;
    }

    public async Task<Result<string>> ExecuteAsync(string callConnectionId, string participantId, bool isPstn)
    {
        try
        {
            _logger.LogInformation("Muting participant {Participant} in call {CallConnectionId}", participantId, callConnectionId);

            if (string.IsNullOrWhiteSpace(callConnectionId))
                return Result<string>.Failure("Call connection ID is required");

            if (string.IsNullOrWhiteSpace(participantId))
                return Result<string>.Failure("Participant ID is required");

            var result = await _participantService.MuteParticipantAsync(callConnectionId, participantId, isPstn);
            return Result<string>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error muting participant");
            return Result<string>.Failure($"Failed to mute participant: {ex.Message}");
        }
    }
}

public class CancelAddParticipantUseCase
{
    private readonly IParticipantService _participantService;
    private readonly ILogger<CancelAddParticipantUseCase> _logger;

    public CancelAddParticipantUseCase(IParticipantService participantService, ILogger<CancelAddParticipantUseCase> logger)
    {
        _participantService = participantService;
        _logger = logger;
    }

    public async Task<Result<string>> ExecuteAsync(string callConnectionId, string invitationId, string? operationContext = null)
    {
        try
        {
            _logger.LogInformation("Cancelling add participant invitation {InvitationId} in call {CallConnectionId}", invitationId, callConnectionId);

            if (string.IsNullOrWhiteSpace(callConnectionId))
                return Result<string>.Failure("Call connection ID is required");

            if (string.IsNullOrWhiteSpace(invitationId))
                return Result<string>.Failure("Invitation ID is required");

            var result = await _participantService.CancelAddParticipantAsync(callConnectionId, invitationId, operationContext);
            return Result<string>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling add participant");
            return Result<string>.Failure($"Failed to cancel add participant: {ex.Message}");
        }
    }
}
