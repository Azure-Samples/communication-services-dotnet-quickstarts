using Call_Automation_GCCH.Application.UseCases.Participants;
using Call_Automation_GCCH.Core.Interfaces;
using Call_Automation_GCCH.Models;
using Call_Automation_GCCH.Presentation.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Call_Automation_GCCH.Presentation.Controllers;

[ApiController]
[Route("api/v2/participants")]
[Produces("application/json")]
[Tags("Participant Management (Clean Architecture)")]
public class ParticipantsController : ControllerBase
{
    private readonly AddParticipantUseCase _addParticipantUseCase;
    private readonly RemoveParticipantUseCase _removeParticipantUseCase;
    private readonly GetParticipantUseCase _getParticipantUseCase;
    private readonly GetAllParticipantsUseCase _getAllParticipantsUseCase;
    private readonly MuteParticipantUseCase _muteParticipantUseCase;
    private readonly CancelAddParticipantUseCase _cancelAddParticipantUseCase;
    private readonly ILogger<ParticipantsController> _logger;

    public ParticipantsController(
        AddParticipantUseCase addParticipantUseCase,
        RemoveParticipantUseCase removeParticipantUseCase,
        GetParticipantUseCase getParticipantUseCase,
        GetAllParticipantsUseCase getAllParticipantsUseCase,
        MuteParticipantUseCase muteParticipantUseCase,
        CancelAddParticipantUseCase cancelAddParticipantUseCase,
        ILogger<ParticipantsController> logger)
    {
        _addParticipantUseCase = addParticipantUseCase;
        _removeParticipantUseCase = removeParticipantUseCase;
        _getParticipantUseCase = getParticipantUseCase;
        _getAllParticipantsUseCase = getAllParticipantsUseCase;
        _muteParticipantUseCase = muteParticipantUseCase;
        _cancelAddParticipantUseCase = cancelAddParticipantUseCase;
        _logger = logger;
    }

    [HttpPost("add")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddParticipant([FromBody] AddParticipantRequest request)
    {
        _logger.LogInformation("POST /api/v2/participants/add - CallConnectionId: {CallConnectionId}", request.CallConnectionId);

        var result = await _addParticipantUseCase.ExecuteAsync(
            request.CallConnectionId,
            request.ParticipantId,
            request.IsPstn,
            request.OperationContext,
            request.InvitationTimeoutInSeconds);

        return result.IsSuccess
            ? Ok(new { message = result.Data })
            : BadRequest(new { error = result.ErrorMessage });
    }

    [HttpPost("remove")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemoveParticipant([FromBody] RemoveParticipantRequest request)
    {
        _logger.LogInformation("POST /api/v2/participants/remove - CallConnectionId: {CallConnectionId}", request.CallConnectionId);

        var result = await _removeParticipantUseCase.ExecuteAsync(
            request.CallConnectionId,
            request.ParticipantId,
            request.IsPstn,
            request.OperationContext);

        return result.IsSuccess
            ? Ok(new { message = result.Data })
            : BadRequest(new { error = result.ErrorMessage });
    }

    [HttpGet("{callConnectionId}/all")]
    [ProducesResponseType(typeof(IEnumerable<ParticipantInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAllParticipants(string callConnectionId)
    {
        _logger.LogInformation("GET /api/v2/participants/{callConnectionId}/all", callConnectionId);

        var result = await _getAllParticipantsUseCase.ExecuteAsync(callConnectionId);

        return result.IsSuccess
            ? Ok(result.Data)
            : BadRequest(new { error = result.ErrorMessage });
    }

    [HttpGet("{callConnectionId}/{participantId}")]
    [ProducesResponseType(typeof(ParticipantInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetParticipant(string callConnectionId, string participantId, [FromQuery] bool isPstn = false)
    {
        _logger.LogInformation("GET /api/v2/participants/{callConnectionId}/{participantId}", callConnectionId, participantId);

        var result = await _getParticipantUseCase.ExecuteAsync(callConnectionId, participantId, isPstn);

        return result.IsSuccess
            ? Ok(result.Data)
            : BadRequest(new { error = result.ErrorMessage });
    }

    [HttpPost("mute")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> MuteParticipant([FromBody] MuteParticipantRequest request)
    {
        _logger.LogInformation("POST /api/v2/participants/mute - CallConnectionId: {CallConnectionId}", request.CallConnectionId);

        var result = await _muteParticipantUseCase.ExecuteAsync(
            request.CallConnectionId,
            request.ParticipantId,
            request.IsPstn);

        return result.IsSuccess
            ? Ok(new { message = result.Data })
            : BadRequest(new { error = result.ErrorMessage });
    }

    [HttpPost("cancel-add")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelAddParticipant([FromBody] CancelAddParticipantRequest request)
    {
        _logger.LogInformation("POST /api/v2/participants/cancel-add - CallConnectionId: {CallConnectionId}", request.CallConnectionId);

        var result = await _cancelAddParticipantUseCase.ExecuteAsync(
            request.CallConnectionId,
            request.InvitationId,
            request.OperationContext);

        return result.IsSuccess
            ? Ok(new { message = result.Data })
            : BadRequest(new { error = result.ErrorMessage });
    }
}
