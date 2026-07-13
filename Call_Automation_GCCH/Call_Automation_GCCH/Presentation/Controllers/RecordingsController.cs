using Call_Automation_GCCH.Application.UseCases.Recordings;
using Call_Automation_GCCH.Core.Interfaces;
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
    private readonly ILogger<RecordingsController> _logger;

    public RecordingsController(
        StartRecordingUseCase startRecordingUseCase,
        PauseRecordingUseCase pauseRecordingUseCase,
        ResumeRecordingUseCase resumeRecordingUseCase,
        StopRecordingUseCase stopRecordingUseCase,
        GetRecordingStateUseCase getRecordingStateUseCase,
        DeleteRecordingUseCase deleteRecordingUseCase,
        DownloadRecordingUseCase downloadRecordingUseCase,
        ILogger<RecordingsController> logger)
    {
        _startRecordingUseCase = startRecordingUseCase;
        _pauseRecordingUseCase = pauseRecordingUseCase;
        _resumeRecordingUseCase = resumeRecordingUseCase;
        _stopRecordingUseCase = stopRecordingUseCase;
        _getRecordingStateUseCase = getRecordingStateUseCase;
        _deleteRecordingUseCase = deleteRecordingUseCase;
        _downloadRecordingUseCase = downloadRecordingUseCase;
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
        _logger.LogInformation("POST /api/v2/recordings/download - Location: {Location}", request.DownloadLocation);

        var result = await _downloadRecordingUseCase.ExecuteAsync(request.DownloadLocation);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        return File(result.Data!, "application/octet-stream", "recording.mp3");
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
