using Call_Automation_GCCH.Application.UseCases.Media;
using Call_Automation_GCCH.Presentation.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Call_Automation_GCCH.Presentation.Controllers;

/// <summary>
/// API Controller for Media Management - Clean Architecture Implementation
/// Presentation Layer: Handles HTTP requests/responses and delegates to use cases
/// </summary>
[ApiController]
[Route("api/v2/media")]
[Produces("application/json")]
[Tags("Media Management (Clean Architecture)")]
public class MediaController : ControllerBase
{
    private readonly PlayMediaUseCase _playMediaUseCase;
    private readonly PlayToAllUseCase _playToAllUseCase;
    private readonly HoldParticipantUseCase _holdParticipantUseCase;
    private readonly UnholdParticipantUseCase _unholdParticipantUseCase;
    private readonly StartMediaStreamingUseCase _startMediaStreamingUseCase;
    private readonly StopMediaStreamingUseCase _stopMediaStreamingUseCase;
    private readonly CancelAllMediaOperationsUseCase _cancelAllMediaOperationsUseCase;
    private readonly StartTranscriptionUseCase _startTranscriptionUseCase;
    private readonly StopTranscriptionUseCase _stopTranscriptionUseCase;
    private readonly UpdateTranscriptionUseCase _updateTranscriptionUseCase;
    private readonly ILogger<MediaController> _logger;

    public MediaController(
        PlayMediaUseCase playMediaUseCase,
        PlayToAllUseCase playToAllUseCase,
        HoldParticipantUseCase holdParticipantUseCase,
        UnholdParticipantUseCase unholdParticipantUseCase,
        StartMediaStreamingUseCase startMediaStreamingUseCase,
        StopMediaStreamingUseCase stopMediaStreamingUseCase,
        CancelAllMediaOperationsUseCase cancelAllMediaOperationsUseCase,
        StartTranscriptionUseCase startTranscriptionUseCase,
        StopTranscriptionUseCase stopTranscriptionUseCase,
        UpdateTranscriptionUseCase updateTranscriptionUseCase,
        ILogger<MediaController> logger)
    {
        _playMediaUseCase = playMediaUseCase;
        _playToAllUseCase = playToAllUseCase;
        _holdParticipantUseCase = holdParticipantUseCase;
        _unholdParticipantUseCase = unholdParticipantUseCase;
        _startMediaStreamingUseCase = startMediaStreamingUseCase;
        _stopMediaStreamingUseCase = stopMediaStreamingUseCase;
        _cancelAllMediaOperationsUseCase = cancelAllMediaOperationsUseCase;
        _startTranscriptionUseCase = startTranscriptionUseCase;
        _stopTranscriptionUseCase = stopTranscriptionUseCase;
        _updateTranscriptionUseCase = updateTranscriptionUseCase;
        _logger = logger;
    }

    [HttpPost("play")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Play([FromBody] PlayMediaRequest request)
    {
        _logger.LogInformation("POST /api/v2/media/play - CallConnectionId: {CallConnectionId}", request.CallConnectionId);

        var result = await _playMediaUseCase.ExecuteAsync(
            request.CallConnectionId,
            request.TargetParticipant,
            request.IsPstn,
            request.AudioFileUrl,
            request.TextToPlay,
            request.Loop,
            request.InterruptCallMediaOperation,
            request.OperationContext);

        return result.IsSuccess
            ? Ok(new { message = result.Data })
            : BadRequest(new { error = result.ErrorMessage });
    }

    [HttpPost("play-to-all")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PlayToAll([FromBody] PlayToAllRequest request)
    {
        _logger.LogInformation("POST /api/v2/media/play-to-all - CallConnectionId: {CallConnectionId}", request.CallConnectionId);

        var result = await _playToAllUseCase.ExecuteAsync(
            request.CallConnectionId,
            request.AudioFileUrl,
            request.TextToPlay,
            request.Loop,
            request.InterruptCallMediaOperation,
            request.OperationContext);

        return result.IsSuccess
            ? Ok(new { message = result.Data })
            : BadRequest(new { error = result.ErrorMessage });
    }

    [HttpPost("hold")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Hold([FromBody] MediaHoldRequest request)
    {
        _logger.LogInformation("POST /api/v2/media/hold - CallConnectionId: {CallConnectionId}, Participant: {Participant}",
            request.CallConnectionId, request.ParticipantId);

        var result = await _holdParticipantUseCase.ExecuteAsync(
            request.CallConnectionId,
            request.ParticipantId,
            request.IsPstn,
            request.PlaySourceId,
            request.OperationContext);

        return result.IsSuccess
            ? Ok(new { message = result.Data })
            : BadRequest(new { error = result.ErrorMessage });
    }

    [HttpPost("unhold")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Unhold([FromBody] MediaUnholdRequest request)
    {
        _logger.LogInformation("POST /api/v2/media/unhold - CallConnectionId: {CallConnectionId}, Participant: {Participant}",
            request.CallConnectionId, request.ParticipantId);

        var result = await _unholdParticipantUseCase.ExecuteAsync(
            request.CallConnectionId,
            request.ParticipantId,
            request.IsPstn,
            request.OperationContext);

        return result.IsSuccess
            ? Ok(new { message = result.Data })
            : BadRequest(new { error = result.ErrorMessage });
    }

    [HttpPost("streaming/start")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StartMediaStreaming([FromBody] MediaStreamingRequest request)
    {
        _logger.LogInformation("POST /api/v2/media/streaming/start - CallConnectionId: {CallConnectionId}", request.CallConnectionId);

        var result = await _startMediaStreamingUseCase.ExecuteAsync(request.CallConnectionId, request.OperationContext);

        return result.IsSuccess
            ? Ok(new { message = result.Data })
            : BadRequest(new { error = result.ErrorMessage });
    }

    [HttpPost("streaming/stop")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StopMediaStreaming([FromBody] MediaStreamingRequest request)
    {
        _logger.LogInformation("POST /api/v2/media/streaming/stop - CallConnectionId: {CallConnectionId}", request.CallConnectionId);

        var result = await _stopMediaStreamingUseCase.ExecuteAsync(request.CallConnectionId, request.OperationContext);

        return result.IsSuccess
            ? Ok(new { message = result.Data })
            : BadRequest(new { error = result.ErrorMessage });
    }

    [HttpPost("cancel-all")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelAllMediaOperations([FromBody] CancelMediaRequest request)
    {
        _logger.LogInformation("POST /api/v2/media/cancel-all - CallConnectionId: {CallConnectionId}", request.CallConnectionId);

        var result = await _cancelAllMediaOperationsUseCase.ExecuteAsync(request.CallConnectionId);

        return result.IsSuccess
            ? Ok(new { message = result.Data })
            : BadRequest(new { error = result.ErrorMessage });
    }

    [HttpPost("transcription/start")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StartTranscription([FromBody] TranscriptionRequest request)
    {
        _logger.LogInformation("POST /api/v2/media/transcription/start - CallConnectionId: {CallConnectionId}", request.CallConnectionId);

        var result = await _startTranscriptionUseCase.ExecuteAsync(
            request.CallConnectionId,
            request.Locale ?? "en-US",
            request.OperationContext);

        return result.IsSuccess
            ? Ok(new { message = result.Data })
            : BadRequest(new { error = result.ErrorMessage });
    }

    [HttpPost("transcription/stop")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StopTranscription([FromBody] TranscriptionRequest request)
    {
        _logger.LogInformation("POST /api/v2/media/transcription/stop - CallConnectionId: {CallConnectionId}", request.CallConnectionId);

        var result = await _stopTranscriptionUseCase.ExecuteAsync(request.CallConnectionId, request.OperationContext);

        return result.IsSuccess
            ? Ok(new { message = result.Data })
            : BadRequest(new { error = result.ErrorMessage });
    }

    [HttpPost("transcription/update")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateTranscription([FromBody] TranscriptionRequest request)
    {
        _logger.LogInformation("POST /api/v2/media/transcription/update - CallConnectionId: {CallConnectionId}", request.CallConnectionId);

        if (string.IsNullOrWhiteSpace(request.Locale))
        {
            return BadRequest(new { error = "Locale is required for update" });
        }

        var result = await _updateTranscriptionUseCase.ExecuteAsync(
            request.CallConnectionId,
            request.Locale,
            request.OperationContext);

        return result.IsSuccess
            ? Ok(new { message = result.Data })
            : BadRequest(new { error = result.ErrorMessage });
    }
}
