using Call_Automation_GCCH.Core.Common;
using Call_Automation_GCCH.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Call_Automation_GCCH.Application.UseCases.Media;

/// <summary>
/// Use Case: Play media to a specific participant
/// </summary>
public class PlayMediaUseCase
{
    private readonly IMediaService _mediaService;
    private readonly ILogger<PlayMediaUseCase> _logger;

    public PlayMediaUseCase(IMediaService mediaService, ILogger<PlayMediaUseCase> logger)
    {
        _mediaService = mediaService;
        _logger = logger;
    }

    public async Task<Result<string>> ExecuteAsync(
        string callConnectionId,
        string targetParticipant,
        bool isPstn,
        string? audioFileUrl = null,
        string? textToPlay = null,
        bool loop = false,
        bool interruptCallMediaOperation = false,
        string? operationContext = null)
    {
        try
        {
            _logger.LogInformation("Playing media to participant {Participant} in call {CallConnectionId}",
                targetParticipant, callConnectionId);

            if (string.IsNullOrWhiteSpace(callConnectionId))
            {
                return Result<string>.Failure("Call connection ID is required");
            }

            if (string.IsNullOrWhiteSpace(targetParticipant))
            {
                return Result<string>.Failure("Target participant is required");
            }

            if (string.IsNullOrWhiteSpace(audioFileUrl) && string.IsNullOrWhiteSpace(textToPlay))
            {
                return Result<string>.Failure("Either audio file URL or text to play is required");
            }

            var result = await _mediaService.PlayAsync(
                callConnectionId,
                targetParticipant,
                isPstn,
                audioFileUrl,
                textToPlay,
                loop,
                interruptCallMediaOperation,
                operationContext);

            _logger.LogInformation("Media play initiated successfully");
            return Result<string>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error playing media");
            return Result<string>.Failure($"Failed to play media: {ex.Message}");
        }
    }
}

/// <summary>
/// Use Case: Play media to all participants
/// </summary>
public class PlayToAllUseCase
{
    private readonly IMediaService _mediaService;
    private readonly ILogger<PlayToAllUseCase> _logger;

    public PlayToAllUseCase(IMediaService mediaService, ILogger<PlayToAllUseCase> logger)
    {
        _mediaService = mediaService;
        _logger = logger;
    }

    public async Task<Result<string>> ExecuteAsync(
        string callConnectionId,
        string? audioFileUrl = null,
        string? textToPlay = null,
        bool loop = false,
        bool interruptCallMediaOperation = false,
        string? operationContext = null)
    {
        try
        {
            _logger.LogInformation("Playing media to all participants in call {CallConnectionId}", callConnectionId);

            if (string.IsNullOrWhiteSpace(callConnectionId))
            {
                return Result<string>.Failure("Call connection ID is required");
            }

            if (string.IsNullOrWhiteSpace(audioFileUrl) && string.IsNullOrWhiteSpace(textToPlay))
            {
                return Result<string>.Failure("Either audio file URL or text to play is required");
            }

            var result = await _mediaService.PlayToAllAsync(
                callConnectionId,
                audioFileUrl,
                textToPlay,
                loop,
                interruptCallMediaOperation,
                operationContext);

            _logger.LogInformation("Media play to all initiated successfully");
            return Result<string>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error playing media to all");
            return Result<string>.Failure($"Failed to play media to all: {ex.Message}");
        }
    }
}

/// <summary>
/// Use Case: Hold a participant
/// </summary>
public class HoldParticipantUseCase
{
    private readonly IMediaService _mediaService;
    private readonly ILogger<HoldParticipantUseCase> _logger;

    public HoldParticipantUseCase(IMediaService mediaService, ILogger<HoldParticipantUseCase> logger)
    {
        _mediaService = mediaService;
        _logger = logger;
    }

    public async Task<Result<string>> ExecuteAsync(
        string callConnectionId,
        string participantId,
        bool isPstn,
        string? playSourceId = null,
        string? operationContext = null)
    {
        try
        {
            _logger.LogInformation("Holding participant {Participant} in call {CallConnectionId}",
                participantId, callConnectionId);

            if (string.IsNullOrWhiteSpace(callConnectionId))
            {
                return Result<string>.Failure("Call connection ID is required");
            }

            if (string.IsNullOrWhiteSpace(participantId))
            {
                return Result<string>.Failure("Participant ID is required");
            }

            var result = await _mediaService.HoldAsync(
                callConnectionId,
                participantId,
                isPstn,
                playSourceId,
                operationContext);

            _logger.LogInformation("Participant hold initiated successfully");
            return Result<string>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error holding participant");
            return Result<string>.Failure($"Failed to hold participant: {ex.Message}");
        }
    }
}

/// <summary>
/// Use Case: Unhold a participant
/// </summary>
public class UnholdParticipantUseCase
{
    private readonly IMediaService _mediaService;
    private readonly ILogger<UnholdParticipantUseCase> _logger;

    public UnholdParticipantUseCase(IMediaService mediaService, ILogger<UnholdParticipantUseCase> logger)
    {
        _mediaService = mediaService;
        _logger = logger;
    }

    public async Task<Result<string>> ExecuteAsync(
        string callConnectionId,
        string participantId,
        bool isPstn,
        string? operationContext = null)
    {
        try
        {
            _logger.LogInformation("Unholding participant {Participant} in call {CallConnectionId}",
                participantId, callConnectionId);

            if (string.IsNullOrWhiteSpace(callConnectionId))
            {
                return Result<string>.Failure("Call connection ID is required");
            }

            if (string.IsNullOrWhiteSpace(participantId))
            {
                return Result<string>.Failure("Participant ID is required");
            }

            var result = await _mediaService.UnholdAsync(
                callConnectionId,
                participantId,
                isPstn,
                operationContext);

            _logger.LogInformation("Participant unhold initiated successfully");
            return Result<string>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unholding participant");
            return Result<string>.Failure($"Failed to unhold participant: {ex.Message}");
        }
    }
}

/// <summary>
/// Use Case: Start media streaming
/// </summary>
public class StartMediaStreamingUseCase
{
    private readonly IMediaService _mediaService;
    private readonly ILogger<StartMediaStreamingUseCase> _logger;

    public StartMediaStreamingUseCase(IMediaService mediaService, ILogger<StartMediaStreamingUseCase> logger)
    {
        _mediaService = mediaService;
        _logger = logger;
    }

    public async Task<Result<string>> ExecuteAsync(string callConnectionId, string? operationContext = null)
    {
        try
        {
            _logger.LogInformation("Starting media streaming for call {CallConnectionId}", callConnectionId);

            if (string.IsNullOrWhiteSpace(callConnectionId))
            {
                return Result<string>.Failure("Call connection ID is required");
            }

            var result = await _mediaService.StartMediaStreamingAsync(callConnectionId, operationContext);

            _logger.LogInformation("Media streaming started successfully");
            return Result<string>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting media streaming");
            return Result<string>.Failure($"Failed to start media streaming: {ex.Message}");
        }
    }
}

/// <summary>
/// Use Case: Stop media streaming
/// </summary>
public class StopMediaStreamingUseCase
{
    private readonly IMediaService _mediaService;
    private readonly ILogger<StopMediaStreamingUseCase> _logger;

    public StopMediaStreamingUseCase(IMediaService mediaService, ILogger<StopMediaStreamingUseCase> logger)
    {
        _mediaService = mediaService;
        _logger = logger;
    }

    public async Task<Result<string>> ExecuteAsync(string callConnectionId, string? operationContext = null)
    {
        try
        {
            _logger.LogInformation("Stopping media streaming for call {CallConnectionId}", callConnectionId);

            if (string.IsNullOrWhiteSpace(callConnectionId))
            {
                return Result<string>.Failure("Call connection ID is required");
            }

            var result = await _mediaService.StopMediaStreamingAsync(callConnectionId, operationContext);

            _logger.LogInformation("Media streaming stopped successfully");
            return Result<string>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping media streaming");
            return Result<string>.Failure($"Failed to stop media streaming: {ex.Message}");
        }
    }
}

/// <summary>
/// Use Case: Cancel all media operations
/// </summary>
public class CancelAllMediaOperationsUseCase
{
    private readonly IMediaService _mediaService;
    private readonly ILogger<CancelAllMediaOperationsUseCase> _logger;

    public CancelAllMediaOperationsUseCase(IMediaService mediaService, ILogger<CancelAllMediaOperationsUseCase> logger)
    {
        _mediaService = mediaService;
        _logger = logger;
    }

    public async Task<Result<string>> ExecuteAsync(string callConnectionId)
    {
        try
        {
            _logger.LogInformation("Cancelling all media operations for call {CallConnectionId}", callConnectionId);

            if (string.IsNullOrWhiteSpace(callConnectionId))
            {
                return Result<string>.Failure("Call connection ID is required");
            }

            var result = await _mediaService.CancelAllMediaOperationsAsync(callConnectionId);

            _logger.LogInformation("All media operations cancelled successfully");
            return Result<string>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling media operations");
            return Result<string>.Failure($"Failed to cancel media operations: {ex.Message}");
        }
    }
}

/// <summary>
/// Use Case: Start transcription
/// </summary>
public class StartTranscriptionUseCase
{
    private readonly IMediaService _mediaService;
    private readonly ILogger<StartTranscriptionUseCase> _logger;

    public StartTranscriptionUseCase(IMediaService mediaService, ILogger<StartTranscriptionUseCase> logger)
    {
        _mediaService = mediaService;
        _logger = logger;
    }

    public async Task<Result<string>> ExecuteAsync(
        string callConnectionId,
        string locale = "en-US",
        string? operationContext = null)
    {
        try
        {
            _logger.LogInformation("Starting transcription for call {CallConnectionId} with locale {Locale}",
                callConnectionId, locale);

            if (string.IsNullOrWhiteSpace(callConnectionId))
            {
                return Result<string>.Failure("Call connection ID is required");
            }

            var result = await _mediaService.StartTranscriptionAsync(callConnectionId, locale, operationContext);

            _logger.LogInformation("Transcription started successfully");
            return Result<string>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting transcription");
            return Result<string>.Failure($"Failed to start transcription: {ex.Message}");
        }
    }
}

/// <summary>
/// Use Case: Stop transcription
/// </summary>
public class StopTranscriptionUseCase
{
    private readonly IMediaService _mediaService;
    private readonly ILogger<StopTranscriptionUseCase> _logger;

    public StopTranscriptionUseCase(IMediaService mediaService, ILogger<StopTranscriptionUseCase> logger)
    {
        _mediaService = mediaService;
        _logger = logger;
    }

    public async Task<Result<string>> ExecuteAsync(string callConnectionId, string? operationContext = null)
    {
        try
        {
            _logger.LogInformation("Stopping transcription for call {CallConnectionId}", callConnectionId);

            if (string.IsNullOrWhiteSpace(callConnectionId))
            {
                return Result<string>.Failure("Call connection ID is required");
            }

            var result = await _mediaService.StopTranscriptionAsync(callConnectionId, operationContext);

            _logger.LogInformation("Transcription stopped successfully");
            return Result<string>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping transcription");
            return Result<string>.Failure($"Failed to stop transcription: {ex.Message}");
        }
    }
}

/// <summary>
/// Use Case: Update transcription locale
/// </summary>
public class UpdateTranscriptionUseCase
{
    private readonly IMediaService _mediaService;
    private readonly ILogger<UpdateTranscriptionUseCase> _logger;

    public UpdateTranscriptionUseCase(IMediaService mediaService, ILogger<UpdateTranscriptionUseCase> logger)
    {
        _mediaService = mediaService;
        _logger = logger;
    }

    public async Task<Result<string>> ExecuteAsync(
        string callConnectionId,
        string locale,
        string? operationContext = null)
    {
        try
        {
            _logger.LogInformation("Updating transcription for call {CallConnectionId} to locale {Locale}",
                callConnectionId, locale);

            if (string.IsNullOrWhiteSpace(callConnectionId))
            {
                return Result<string>.Failure("Call connection ID is required");
            }

            if (string.IsNullOrWhiteSpace(locale))
            {
                return Result<string>.Failure("Locale is required");
            }

            var result = await _mediaService.UpdateTranscriptionAsync(callConnectionId, locale, operationContext);

            _logger.LogInformation("Transcription updated successfully");
            return Result<string>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating transcription");
            return Result<string>.Failure($"Failed to update transcription: {ex.Message}");
        }
    }
}
