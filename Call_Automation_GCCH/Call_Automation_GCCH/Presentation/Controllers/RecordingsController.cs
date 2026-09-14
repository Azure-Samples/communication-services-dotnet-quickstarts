using Call_Automation_GCCH.Application.UseCases.Recordings;
using Call_Automation_GCCH.Core.Interfaces;
using Call_Automation_GCCH.Services;
using Microsoft.AspNetCore.Mvc;

namespace Call_Automation_GCCH.Presentation.Controllers;

[ApiController]
[Route("api/v2/recordings")]
[Produces("application/json")]
[Tags("Recording Management (Clean Architecture)")]
public class RecordingsController : ControllerBase
{
    private readonly StartRecordingUseCase _startRecordingUseCase;
    private readonly PauseRecordingUseCase _pauseRecordingUseCase;
    private readonly ResumeRecordingUseCase _resumeRecordingUseCase;
    private readonly StopRecordingUseCase _stopRecordingUseCase;
    private readonly GetRecordingStateUseCase _getRecordingStateUseCase;
    private readonly DeleteRecordingUseCase _deleteRecordingUseCase;
    private readonly DownloadRecordingUseCase _downloadRecordingUseCase;
    private readonly IRecordingHistoryService _recordingHistory;
    private readonly ILogger<RecordingsController> _logger;

    public RecordingsController(
        StartRecordingUseCase startRecordingUseCase,
        PauseRecordingUseCase pauseRecordingUseCase,
        ResumeRecordingUseCase resumeRecordingUseCase,
        StopRecordingUseCase stopRecordingUseCase,
        GetRecordingStateUseCase getRecordingStateUseCase,
        DeleteRecordingUseCase deleteRecordingUseCase,
        DownloadRecordingUseCase downloadRecordingUseCase,
        IRecordingHistoryService recordingHistory,
        ILogger<RecordingsController> logger)
    {
        _startRecordingUseCase = startRecordingUseCase;
        _pauseRecordingUseCase = pauseRecordingUseCase;
        _resumeRecordingUseCase = resumeRecordingUseCase;
        _stopRecordingUseCase = stopRecordingUseCase;
        _getRecordingStateUseCase = getRecordingStateUseCase;
        _deleteRecordingUseCase = deleteRecordingUseCase;
        _downloadRecordingUseCase = downloadRecordingUseCase;
        _recordingHistory = recordingHistory;
        _logger = logger;
    }

    [HttpPost("start")]
    [ProducesResponseType(typeof(RecordingInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StartRecording([FromBody] StartRecordingRequest request)
    {
        _logger.LogInformation("POST /api/v2/recordings/start - CallConnectionId: {CallConnectionId}", request.CallConnectionId);

        var result = await _startRecordingUseCase.ExecuteAsync(request.CallConnectionId, request.Options);

        return result.IsSuccess
            ? Ok(result.Data)
            : BadRequest(new { error = result.ErrorMessage });
    }

    [HttpPost("pause")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PauseRecording([FromBody] RecordingActionRequest request)
    {
        _logger.LogInformation("POST /api/v2/recordings/pause - RecordingId: {RecordingId}", request.RecordingId);

        var result = await _pauseRecordingUseCase.ExecuteAsync(request.RecordingId);

        return result.IsSuccess
            ? Ok(new { message = result.Data })
            : BadRequest(new { error = result.ErrorMessage });
    }

    [HttpPost("resume")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResumeRecording([FromBody] RecordingActionRequest request)
    {
        _logger.LogInformation("POST /api/v2/recordings/resume - RecordingId: {RecordingId}", request.RecordingId);

        var result = await _resumeRecordingUseCase.ExecuteAsync(request.RecordingId);

        return result.IsSuccess
            ? Ok(new { message = result.Data })
            : BadRequest(new { error = result.ErrorMessage });
    }

    [HttpPost("stop")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StopRecording([FromBody] RecordingActionRequest request)
    {
        _logger.LogInformation("POST /api/v2/recordings/stop - RecordingId: {RecordingId}", request.RecordingId);

        var result = await _stopRecordingUseCase.ExecuteAsync(request.RecordingId);

        return result.IsSuccess
            ? Ok(new { message = result.Data })
            : BadRequest(new { error = result.ErrorMessage });
    }

    [HttpGet("{recordingId}/state")]
    [ProducesResponseType(typeof(RecordingStateInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRecordingState(string recordingId)
    {
        _logger.LogInformation("GET /api/v2/recordings/{recordingId}/state", recordingId);

        var result = await _getRecordingStateUseCase.ExecuteAsync(recordingId);

        return result.IsSuccess
            ? Ok(result.Data)
            : BadRequest(new { error = result.ErrorMessage });
    }

    [HttpDelete("delete")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteRecording([FromBody] DeleteRecordingRequest request)
    {
        _logger.LogInformation("DELETE /api/v2/recordings/delete - Location: {Location}", request.RecordingLocation);

        var result = await _deleteRecordingUseCase.ExecuteAsync(request.RecordingLocation);

        return result.IsSuccess
            ? Ok(new { message = result.Data })
            : BadRequest(new { error = result.ErrorMessage });
    }

    [HttpPost("download")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DownloadRecording([FromBody] DownloadRecordingRequest request)
    {
        _logger.LogInformation("=== POST /api/v2/recordings/download - CONTROLLER ENTRY ===");
        _logger.LogInformation("Client IP: {ClientIP}", HttpContext.Connection.RemoteIpAddress);
        _logger.LogInformation("Request Location: {Location}", request?.DownloadLocation);
        _logger.LogInformation("Content-Length Expected: {ContentLength}", HttpContext.Request.ContentLength);

        if (request == null || string.IsNullOrWhiteSpace(request.DownloadLocation))
        {
            _logger.LogError("Invalid request: Location is null or empty");
            return BadRequest(new { error = "Download location is required" });
        }

        try
        {
            _logger.LogInformation("Calling DownloadRecordingUseCase...");
            var result = await _downloadRecordingUseCase.ExecuteAsync(request.DownloadLocation);

            if (!result.IsSuccess)
            {
                _logger.LogError("UseCase returned failure: {Error}", result.ErrorMessage);
                return BadRequest(new { error = result.ErrorMessage });
            }

            if (result.Data == null || result.Data.Length == 0)
            {
                _logger.LogError("Downloaded data is null or empty");
                return BadRequest(new { error = "Downloaded recording is empty" });
            }

            _logger.LogInformation("Returning file response with {ByteCount} bytes", result.Data.Length);
            _logger.LogInformation("=== DOWNLOAD RESPONSE PREPARED ===");

            return File(result.Data, "application/octet-stream", "recording.mp3");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "=== CRITICAL ERROR IN DOWNLOAD CONTROLLER ===");
            _logger.LogError("Exception Type: {ExceptionType}", ex.GetType().Name);
            _logger.LogError("Exception Message: {Message}", ex.Message);
            return BadRequest(new { error = $"Download failed: {ex.Message}" });
        }
    }

    /// <summary>
    /// Gets the last known recording location captured from the AcsRecordingFileStatusUpdated event.
    /// </summary>
    [HttpGet("last-location")]
    [ProducesResponseType(typeof(CapturedRecordingInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetLastRecordingLocation()
    {
        _logger.LogInformation("GET /api/v2/recordings/last-location");
        var last = _recordingHistory.GetLast();
        if (last == null)
        {
            return NotFound(new
            {
                message = "No recording location captured yet. Trigger a recording and wait for the AcsRecordingFileStatusUpdatedEvent to arrive via Event Grid.",
                hint = "Start a recording via POST /api/v2/recordings/start, then wait for it to stop. The location will be captured automatically."
            });
        }
        return Ok(last);
    }

    /// <summary>
    /// Gets the full history of recording locations captured from events.
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(IEnumerable<CapturedRecordingInfo>), StatusCodes.Status200OK)]
    public IActionResult GetRecordingHistory()
    {
        _logger.LogInformation("GET /api/v2/recordings/history");
        var history = _recordingHistory.GetAll();
        return Ok(new
        {
            totalCount = history.Count,
            recordings = history
        });
    }

    /// <summary>
    /// Downloads the file that was auto-downloaded when the AcsRecordingFileStatusUpdated event fired.
    /// </summary>
    [HttpGet("download-local")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DownloadLocalRecording()
    {
        _logger.LogInformation("GET /api/v2/recordings/download-local");
        var last = _recordingHistory.GetLast();
        if (last == null || string.IsNullOrEmpty(last.LocalFilePath) || !System.IO.File.Exists(last.LocalFilePath))
        {
            return NotFound(new { message = "No auto-downloaded recording file found. Check /api/v2/recordings/last-location for details." });
        }

        var bytes = System.IO.File.ReadAllBytes(last.LocalFilePath);
        var fileName = Path.GetFileName(last.LocalFilePath);
        return File(bytes, "application/octet-stream", fileName);
    }

    /// <summary>
    /// Clears the recording history in memory.
    /// </summary>
    [HttpPost("history/clear")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult ClearRecordingHistory()
    {
        _logger.LogInformation("POST /api/v2/recordings/history/clear");
        _recordingHistory.Clear();
        return Ok(new { message = "Recording history cleared", timestamp = DateTime.UtcNow });
    }
}

public class StartRecordingRequest
{
    public string CallConnectionId { get; set; } = string.Empty;
    public RecordingConfiguration? Options { get; set; }
}

public class RecordingActionRequest
{
    public string RecordingId { get; set; } = string.Empty;
}

public class DeleteRecordingRequest
{
    public string RecordingLocation { get; set; } = string.Empty;
}

public class DownloadRecordingRequest
{
    public string DownloadLocation { get; set; } = string.Empty;
}
