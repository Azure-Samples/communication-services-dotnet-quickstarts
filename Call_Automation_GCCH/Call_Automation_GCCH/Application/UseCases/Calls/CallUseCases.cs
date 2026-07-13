using Call_Automation_GCCH.Core.Common;
using Call_Automation_GCCH.Core.Entities;
using Call_Automation_GCCH.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Call_Automation_GCCH.Application.UseCases.Calls;

/// <summary>
/// Use Case: Create an outbound call
/// Application layer - orchestrates business logic
/// </summary>
public class CreateCallUseCase
{
    private readonly ICallService _callService;
    private readonly ILogger<CreateCallUseCase> _logger;
    private readonly string _callbackUri;

    public CreateCallUseCase(
        ICallService callService,
        ILogger<CreateCallUseCase> logger,
        string callbackUri)
    {
        _callService = callService;
        _logger = logger;
        _callbackUri = callbackUri;
    }

    public async Task<Result<CallConnection>> ExecuteAsync(CallOptions options)
    {
        try
        {
            _logger.LogInformation("Creating call to target: {Target}, IsPSTN: {IsPSTN}", 
                options.Target, options.IsPstn);

            // Validation
            if (string.IsNullOrWhiteSpace(options.Target))
            {
                return Result<CallConnection>.Failure("Target is required");
            }

            // Execute business logic
            var callConnection = await _callService.CreateCallAsync(options, _callbackUri);

            _logger.LogInformation("Call created successfully. CallConnectionId: {CallConnectionId}", 
                callConnection.CallConnectionId);

            return Result<CallConnection>.Success(callConnection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create call");
            return Result<CallConnection>.Failure($"Failed to create call: {ex.Message}");
        }
    }
}

/// <summary>
/// Use Case: Create a group call
/// </summary>
public class CreateGroupCallUseCase
{
    private readonly ICallService _callService;
    private readonly ILogger<CreateGroupCallUseCase> _logger;
    private readonly string _callbackUri;

    public CreateGroupCallUseCase(
        ICallService callService,
        ILogger<CreateGroupCallUseCase> logger,
        string callbackUri)
    {
        _callService = callService;
        _logger = logger;
        _callbackUri = callbackUri;
    }

    public async Task<Result<CallConnection>> ExecuteAsync(List<string> targets, CallOptions? options = null)
    {
        try
        {
            _logger.LogInformation("Creating group call with {Count} participants", targets.Count);

            // Validation
            if (targets == null || targets.Count == 0)
            {
                return Result<CallConnection>.Failure("At least one target is required");
            }

            // Execute business logic
            var callConnection = await _callService.CreateGroupCallAsync(targets, _callbackUri, options);

            _logger.LogInformation("Group call created successfully. CallConnectionId: {CallConnectionId}", 
                callConnection.CallConnectionId);

            return Result<CallConnection>.Success(callConnection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create group call");
            return Result<CallConnection>.Failure($"Failed to create group call: {ex.Message}");
        }
    }
}

/// <summary>
/// Use Case: Transfer a call
/// </summary>
public class TransferCallUseCase
{
    private readonly ICallService _callService;
    private readonly ILogger<TransferCallUseCase> _logger;

    public TransferCallUseCase(ICallService callService, ILogger<TransferCallUseCase> logger)
    {
        _callService = callService;
        _logger = logger;
    }

    public async Task<Result<CallConnection>> ExecuteAsync(
        string callConnectionId, 
        string targetParticipant,
        string? transferee = null,
        bool isPstn = true)
    {
        try
        {
            _logger.LogInformation("Transferring call {CallConnectionId} to {Target}", 
                callConnectionId, targetParticipant);

            // Validation
            if (string.IsNullOrWhiteSpace(callConnectionId))
            {
                return Result<CallConnection>.Failure("Call connection ID is required");
            }

            if (string.IsNullOrWhiteSpace(targetParticipant))
            {
                return Result<CallConnection>.Failure("Target participant is required");
            }

            // Execute business logic
            var result = await _callService.TransferCallAsync(callConnectionId, targetParticipant, transferee, isPstn);

            _logger.LogInformation("Call transferred successfully");

            return Result<CallConnection>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to transfer call");
            return Result<CallConnection>.Failure($"Failed to transfer call: {ex.Message}");
        }
    }
}

/// <summary>
/// Use Case: Hangup a call
/// </summary>
public class HangupCallUseCase
{
    private readonly ICallService _callService;
    private readonly ILogger<HangupCallUseCase> _logger;

    public HangupCallUseCase(ICallService callService, ILogger<HangupCallUseCase> logger)
    {
        _callService = callService;
        _logger = logger;
    }

    public async Task<Result<bool>> ExecuteAsync(string callConnectionId, bool forEveryone = false)
    {
        try
        {
            _logger.LogInformation("Hanging up call {CallConnectionId}, ForEveryone: {ForEveryone}", 
                callConnectionId, forEveryone);

            // Validation
            if (string.IsNullOrWhiteSpace(callConnectionId))
            {
                return Result<bool>.Failure("Call connection ID is required");
            }

            // Execute business logic
            var success = await _callService.HangupCallAsync(callConnectionId, forEveryone);

            _logger.LogInformation("Call hangup completed");

            return Result<bool>.Success(success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to hangup call");
            return Result<bool>.Failure($"Failed to hangup call: {ex.Message}");
        }
    }
}
