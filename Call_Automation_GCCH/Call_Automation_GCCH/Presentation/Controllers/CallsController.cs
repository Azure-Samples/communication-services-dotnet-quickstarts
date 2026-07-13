using Call_Automation_GCCH.Application.UseCases.Calls;
using Call_Automation_GCCH.Core.Entities;
using Call_Automation_GCCH.Models;
using Microsoft.AspNetCore.Mvc;

namespace Call_Automation_GCCH.Presentation.Controllers;

/// <summary>
/// API Controller for Call Management - Clean Architecture Implementation
/// Presentation Layer: Handles HTTP requests/responses and delegates to use cases
/// </summary>
[ApiController]
[Route("api/v2/calls")]
[Produces("application/json")]
[Tags("Call Management (Clean Architecture)")]
public class CallsController : ControllerBase
{
    private readonly CreateCallUseCase _createCallUseCase;
    private readonly CreateGroupCallUseCase _createGroupCallUseCase;
    private readonly TransferCallUseCase _transferCallUseCase;
    private readonly HangupCallUseCase _hangupCallUseCase;
    private readonly ILogger<CallsController> _logger;

    public CallsController(
        CreateCallUseCase createCallUseCase,
        CreateGroupCallUseCase createGroupCallUseCase,
        TransferCallUseCase transferCallUseCase,
        HangupCallUseCase hangupCallUseCase,
        ILogger<CallsController> logger)
    {
        _createCallUseCase = createCallUseCase;
        _createGroupCallUseCase = createGroupCallUseCase;
        _transferCallUseCase = transferCallUseCase;
        _hangupCallUseCase = hangupCallUseCase;
        _logger = logger;
    }

    /// <summary>
    /// Creates an outbound call with optional transcription, media streaming, and AI features
    /// </summary>
    /// <param name="request">Call configuration including target, options, and features</param>
    /// <response code="200">Call created successfully</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("create")]
    [ProducesResponseType(typeof(CallConnectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCall([FromBody] CreateCallWithOptionsRequest request)
    {
        _logger.LogInformation("POST /api/v2/calls/create - Target: {Target}", request.Target);

        // Map request to domain entity
        var callOptions = MapToCallOptions(request);

        // Execute use case
        var result = await _createCallUseCase.ExecuteAsync(callOptions);

        // Map result to HTTP response
        return result.IsSuccess
            ? Ok(MapToResponse(result.Data!))
            : BadRequest(new { error = result.ErrorMessage });
    }

    /// <summary>
    /// Creates a group call with multiple participants
    /// </summary>
    [HttpPost("create-group")]
    [ProducesResponseType(typeof(CallConnectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateGroupCall([FromBody] CreateGroupCallWithOptionsRequest request)
    {
        _logger.LogInformation("POST /api/v2/calls/create-group - Targets: {Count}", request.Targets?.Count ?? 0);

        if (request.Targets == null || request.Targets.Count == 0)
        {
            return BadRequest(new { error = "At least one target is required" });
        }

        // Map request to domain entity
        var callOptions = new CallOptions
        {
            OperationContext = request.OperationContext,
            Transcription = request.TranscriptionOptions != null ? new TranscriptionConfiguration
            {
                Locale = request.TranscriptionOptions.Locale ?? "en-US",
                StartTranscription = request.TranscriptionOptions.StartTranscription,
                EnableIntermediateResults = request.TranscriptionOptions.EnableIntermediateResults
            } : null,
            MediaStreaming = request.MediaStreamingOptions != null ? new MediaStreamingConfiguration
            {
                StartMediaStreaming = request.MediaStreamingOptions.StartMediaStreaming,
                MediaStreamingAudioChannel = request.MediaStreamingOptions.MediaStreamingAudioChannel ?? "Mixed",
                EnableBidirectional = request.MediaStreamingOptions.EnableBidirectional,
                AudioFormat = request.MediaStreamingOptions.AudioFormat ?? "Pcm16KMono"
            } : null,
            CallIntelligence = request.CallIntelligenceOptions != null ? new CallIntelligenceConfiguration
            {
                CognitiveServicesEndpoint = request.CallIntelligenceOptions.CognitiveServicesEndpoint
            } : null
        };

        // Execute use case
        var result = await _createGroupCallUseCase.ExecuteAsync(request.Targets, callOptions);

        // Map result to HTTP response
        return result.IsSuccess
            ? Ok(MapToResponse(result.Data!))
            : BadRequest(new { error = result.ErrorMessage });
    }

    /// <summary>
    /// Transfers an active call to another participant
    /// </summary>
    [HttpPost("transfer")]
    [ProducesResponseType(typeof(CallConnectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TransferCall([FromBody] TransferCallRequest request)
    {
        _logger.LogInformation("POST /api/v2/calls/transfer - CallId: {CallId}", request.CallConnectionId);

        // Execute use case
        var result = await _transferCallUseCase.ExecuteAsync(
            request.CallConnectionId,
            request.TransferTarget,
            request.Transferee,
            request.IsPstn);

        // Map result to HTTP response
        return result.IsSuccess
            ? Ok(MapToResponse(result.Data!))
            : BadRequest(new { error = result.ErrorMessage });
    }

    /// <summary>
    /// Terminates an active call
    /// </summary>
    [HttpPost("hangup")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Hangup([FromBody] HangupRequest request)
    {
        _logger.LogInformation("POST /api/v2/calls/hangup - CallId: {CallId}", request.CallConnectionId);

        if (string.IsNullOrWhiteSpace(request.CallConnectionId))
        {
            return BadRequest(new { error = "Call Connection ID is required" });
        }

        // Execute use case
        var result = await _hangupCallUseCase.ExecuteAsync(request.CallConnectionId, request.ForEveryone);

        // Map result to HTTP response
        return result.IsSuccess
            ? Ok(new { success = true, message = "Call terminated successfully" })
            : BadRequest(new { error = result.ErrorMessage });
    }

    // ============================================================================
    // PRIVATE MAPPING METHODS
    // ============================================================================

    private CallOptions MapToCallOptions(CreateCallWithOptionsRequest request)
    {
        return new CallOptions
        {
            Target = request.Target,
            IsPstn = request.IsPstn,
            OperationContext = request.OperationContext,
            Transcription = request.TranscriptionOptions != null ? new TranscriptionConfiguration
            {
                Locale = request.TranscriptionOptions.Locale ?? "en-US",
                StartTranscription = request.TranscriptionOptions.StartTranscription,
                EnableIntermediateResults = request.TranscriptionOptions.EnableIntermediateResults
            } : null,
            MediaStreaming = request.MediaStreamingOptions != null ? new MediaStreamingConfiguration
            {
                StartMediaStreaming = request.MediaStreamingOptions.StartMediaStreaming,
                MediaStreamingAudioChannel = request.MediaStreamingOptions.MediaStreamingAudioChannel ?? "Mixed",
                EnableBidirectional = request.MediaStreamingOptions.EnableBidirectional,
                AudioFormat = request.MediaStreamingOptions.AudioFormat ?? "Pcm16KMono"
            } : null,
            CallIntelligence = request.CallIntelligenceOptions != null ? new CallIntelligenceConfiguration
            {
                CognitiveServicesEndpoint = request.CallIntelligenceOptions.CognitiveServicesEndpoint
            } : null
        };
    }

    private CallConnectionResponse MapToResponse(CallConnection callConnection)
    {
        return new CallConnectionResponse
        {
            CallConnectionId = callConnection.CallConnectionId,
            CorrelationId = callConnection.CorrelationId,
            Status = callConnection.Status
        };
    }
}

// ============================================================================
// REQUEST DTOs
// ============================================================================

public class HangupRequest
{
    public string? CallConnectionId { get; set; }
    public bool ForEveryone { get; set; } = false;
}
