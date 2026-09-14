using Call_Automation_GCCH.Core.Common;
using Call_Automation_GCCH.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Call_Automation_GCCH.Application.UseCases.Recordings;

public class StartRecordingUseCase
{
    private readonly IRecordingService _recordingService;
    private readonly ILogger<StartRecordingUseCase> _logger;

    public StartRecordingUseCase(IRecordingService recordingService, ILogger<StartRecordingUseCase> logger)
    {
        _recordingService = recordingService;
        _logger = logger;
    }

    public async Task<Result<RecordingInfo>> ExecuteAsync(string callConnectionId, RecordingConfiguration? options = null)
    {
        try
        {
            _logger.LogInformation("Starting recording for call {CallConnectionId}", callConnectionId);

            if (string.IsNullOrWhiteSpace(callConnectionId))
                return Result<RecordingInfo>.Failure("Call connection ID is required");

            var result = await _recordingService.StartRecordingAsync(callConnectionId, options);
            return Result<RecordingInfo>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting recording");
            return Result<RecordingInfo>.Failure($"Failed to start recording: {ex.Message}");
        }
    }
}

public class PauseRecordingUseCase
{
    private readonly IRecordingService _recordingService;
    private readonly ILogger<PauseRecordingUseCase> _logger;

    public PauseRecordingUseCase(IRecordingService recordingService, ILogger<PauseRecordingUseCase> logger)
    {
        _recordingService = recordingService;
        _logger = logger;
    }

    public async Task<Result<string>> ExecuteAsync(string recordingId)
    {
        try
        {
            _logger.LogInformation("Pausing recording {RecordingId}", recordingId);

            if (string.IsNullOrWhiteSpace(recordingId))
                return Result<string>.Failure("Recording ID is required");

            var result = await _recordingService.PauseRecordingAsync(recordingId);
            return Result<string>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing recording");
            return Result<string>.Failure($"Failed to pause recording: {ex.Message}");
        }
    }
}

public class ResumeRecordingUseCase
{
    private readonly IRecordingService _recordingService;
    private readonly ILogger<ResumeRecordingUseCase> _logger;

    public ResumeRecordingUseCase(IRecordingService recordingService, ILogger<ResumeRecordingUseCase> logger)
    {
        _recordingService = recordingService;
        _logger = logger;
    }

    public async Task<Result<string>> ExecuteAsync(string recordingId)
    {
        try
        {
            _logger.LogInformation("Resuming recording {RecordingId}", recordingId);

            if (string.IsNullOrWhiteSpace(recordingId))
                return Result<string>.Failure("Recording ID is required");

            var result = await _recordingService.ResumeRecordingAsync(recordingId);
            return Result<string>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming recording");
            return Result<string>.Failure($"Failed to resume recording: {ex.Message}");
        }
    }
}

public class StopRecordingUseCase
{
    private readonly IRecordingService _recordingService;
    private readonly ILogger<StopRecordingUseCase> _logger;

    public StopRecordingUseCase(IRecordingService recordingService, ILogger<StopRecordingUseCase> logger)
    {
        _recordingService = recordingService;
        _logger = logger;
    }

    public async Task<Result<string>> ExecuteAsync(string recordingId)
    {
        try
        {
            _logger.LogInformation("Stopping recording {RecordingId}", recordingId);

            if (string.IsNullOrWhiteSpace(recordingId))
                return Result<string>.Failure("Recording ID is required");

            var result = await _recordingService.StopRecordingAsync(recordingId);
            return Result<string>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping recording");
            return Result<string>.Failure($"Failed to stop recording: {ex.Message}");
        }
    }
}

public class GetRecordingStateUseCase
{
    private readonly IRecordingService _recordingService;
    private readonly ILogger<GetRecordingStateUseCase> _logger;

    public GetRecordingStateUseCase(IRecordingService recordingService, ILogger<GetRecordingStateUseCase> logger)
    {
        _recordingService = recordingService;
        _logger = logger;
    }

    public async Task<Result<RecordingStateInfo>> ExecuteAsync(string recordingId)
    {
        try
        {
            _logger.LogInformation("Getting state for recording {RecordingId}", recordingId);

            if (string.IsNullOrWhiteSpace(recordingId))
                return Result<RecordingStateInfo>.Failure("Recording ID is required");

            var result = await _recordingService.GetRecordingStateAsync(recordingId);
            return Result<RecordingStateInfo>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recording state");
            return Result<RecordingStateInfo>.Failure($"Failed to get recording state: {ex.Message}");
        }
    }
}

public class DeleteRecordingUseCase
{
    private readonly IRecordingService _recordingService;
    private readonly ILogger<DeleteRecordingUseCase> _logger;

    public DeleteRecordingUseCase(IRecordingService recordingService, ILogger<DeleteRecordingUseCase> logger)
    {
        _recordingService = recordingService;
        _logger = logger;
    }

    public async Task<Result<string>> ExecuteAsync(string recordingLocation)
    {
        try
        {
            _logger.LogInformation("Deleting recording at {Location}", recordingLocation);

            if (string.IsNullOrWhiteSpace(recordingLocation))
                return Result<string>.Failure("Recording location is required");

            var result = await _recordingService.DeleteRecordingAsync(recordingLocation);
            return Result<string>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting recording");
            return Result<string>.Failure($"Failed to delete recording: {ex.Message}");
        }
    }
}

public class DownloadRecordingUseCase
{
    private readonly IRecordingService _recordingService;
    private readonly ILogger<DownloadRecordingUseCase> _logger;

    public DownloadRecordingUseCase(IRecordingService recordingService, ILogger<DownloadRecordingUseCase> logger)
    {
        _recordingService = recordingService;
        _logger = logger;
    }

    public async Task<Result<byte[]>> ExecuteAsync(string downloadLocation)
    {
        try
        {
            _logger.LogInformation("=== DOWNLOAD RECORDING INITIATED ===");
            _logger.LogInformation("Download Location: {Location}", downloadLocation);

            if (string.IsNullOrWhiteSpace(downloadLocation))
            {
                var errorMsg = "Download location is required";
                _logger.LogError("Validation Error: {Error}", errorMsg);
                return Result<byte[]>.Failure(errorMsg);
            }

            _logger.LogInformation("Validating download location URL: {Location}", downloadLocation);

            // Validate URI format
            if (!Uri.TryCreate(downloadLocation, UriKind.Absolute, out var uri))
            {
                var errorMsg = $"Invalid download location URI format: {downloadLocation}";
                _logger.LogError("URI Validation Error: {Error}", errorMsg);
                return Result<byte[]>.Failure(errorMsg);
            }

            _logger.LogInformation("Starting async download from service layer...");
            var result = await _recordingService.DownloadRecordingAsync(downloadLocation);

            _logger.LogInformation("Download completed successfully. Downloaded {ByteCount} bytes", result?.Length ?? 0);
            _logger.LogInformation("=== DOWNLOAD RECORDING COMPLETED SUCCESSFULLY ===");

            return Result<byte[]>.Success(result);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(ex, "Download operation was cancelled after {Time}ms");
            return Result<byte[]>.Failure($"Download was cancelled: {ex.Message}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error during download. Status Code: {StatusCode}. Response: {Response}", 
                ex.StatusCode, ex.Message);
            return Result<byte[]>.Failure($"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "=== CRITICAL ERROR DURING DOWNLOAD ===");
            _logger.LogError("Exception Type: {ExceptionType}", ex.GetType().Name);
            _logger.LogError("Exception Message: {Message}", ex.Message);
            _logger.LogError("Stack Trace: {StackTrace}", ex.StackTrace);
            if (ex.InnerException != null)
            {
                _logger.LogError("Inner Exception: {InnerException}", ex.InnerException.Message);
            }
            return Result<byte[]>.Failure($"Failed to download recording: {ex.Message}");
        }
    }
}
