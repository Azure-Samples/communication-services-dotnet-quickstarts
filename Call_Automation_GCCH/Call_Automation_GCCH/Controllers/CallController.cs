using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Communication;
using Azure.Communication.CallAutomation;
using Call_Automation_GCCH.Models;
using Call_Automation_GCCH.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Call_Automation_GCCH.Controllers
{
    [ApiController]
    [Route("api/calls")]
    [Produces("application/json")]
    public class CallController : ControllerBase
    {
        private readonly CallAutomationService _service;
        private readonly ILogger<CallController> _logger;
        private readonly ConfigurationRequest _config;

        public CallController(
            CallAutomationService service,
            ILogger<CallController> logger,
            IOptions<ConfigurationRequest> configOptions)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = configOptions.Value ?? throw new ArgumentNullException(nameof(configOptions));
        }

        //
        // CREATE CALL (ACS or PSTN)
        //

        [HttpPost("createCall")]
        [Tags("Outbound Call APIs")]
        public IActionResult CreateCall(
            string target,
            bool isPstn = false)
            => HandleCreateCall(target, isPstn, async: false).Result;

        [HttpPost("createCallAsync")]
        [Tags("Outbound Call APIs")]
        public Task<IActionResult> CreateCallAsync(
            string target,
            bool isPstn = false)
            => HandleCreateCall(target, isPstn, async: true);

        //
        // TRANSFER CALL
        //

        [HttpPost("transferCall")]
        [Tags("Transfer Call APIs")]
        public IActionResult TransferCall(
            string callConnectionId,
            string transferTarget,
            string transferee,
            bool isPstn = false)
            => HandleTransferCall(callConnectionId, transferTarget, transferee, isPstn, async: false).Result;

        [HttpPost("transferCallAsync")]
        [Tags("Transfer Call APIs")]
        public Task<IActionResult> TransferCallAsync(
            string callConnectionId,
            string transferTarget,
            string transferee,
            bool isPstn = false)
            => HandleTransferCall(callConnectionId, transferTarget, transferee, isPstn, async: true);

        //
        // HANG UP
        //

        [HttpPost("hangup")]
        [Tags("Disconnect call APIs")]
        public IActionResult Hangup(
            string callConnectionId,
            bool isForEveryone)
            => HandleHangup(callConnectionId, isForEveryone, async: false).Result;

        [HttpPost("hangupAsync")]
        [Tags("Disconnect call APIs")]
        public Task<IActionResult> HangupAsync(
            string callConnectionId,
            bool isForEveryone)
            => HandleHangup(callConnectionId, isForEveryone, async: true);

        //
        // GROUP CALL (PSTN or ACS if you like—you could extend to both)
        //
        [HttpPost("createGroupCallAsync")]
        [Tags("Group Call APIs")]
        public Task<IActionResult> CreateGroupCallAsync(
            [FromQuery] string targets)
            => HandleGroupCall(targets, async: true);

        [HttpPost("createGroupCall")]
        [Tags("Group Call APIs")]
        public Task<IActionResult> CreateGroupCall(
            [FromQuery] string targets)
            => HandleGroupCall(targets, async: false);

        // You could add a sync version if you really need it...

        //
        // ========  HELPERS  ========
        //

        private async Task<IActionResult> HandleCreateCall(
            string target,
            bool isPstn,
            bool async)
        {
            if (string.IsNullOrEmpty(target))
                return BadRequest("Target is required");

            var idType = isPstn ? "PSTN" : "ACS";
            _logger.LogInformation($"Starting {(async ? "async " : "")}create {idType} call to {target}");

            try
            {
                // Build identifier & invite
                var callbackUri = new Uri(new Uri(_config.CallbackUriHost), "/api/callbacks");
                CallInvite invite = isPstn
                    ? new CallInvite(new PhoneNumberIdentifier(target),
                                     new PhoneNumberIdentifier(_config.AcsPhoneNumber))
                    : new CallInvite(new CommunicationUserIdentifier(target));

                var options = new CreateCallOptions(invite, callbackUri)
                {
                     CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(_config.CognitiveServiceEndpoint) },
                };

                // Call SDK
                CreateCallResult result = async
                    ? await _service.GetCallAutomationClient().CreateCallAsync(options)
                    : _service.GetCallAutomationClient().CreateCall(options);

                var props = result.CallConnectionProperties;
                _logger.LogInformation(
                    $"Created {idType} call: ConnId={props.CallConnectionId}, CorrId={props.CorrelationId}, Status={props.CallConnectionState}");

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = props.CallConnectionId,
                    CorrelationId = props.CorrelationId,
                    Status = props.CallConnectionState.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating {idType} call");
                return Problem($"Failed to create {idType} call: {ex.Message}");
            }
        }

        private async Task<IActionResult> HandleTransferCall(
            string callConnectionId,
            string transferTarget,
            string transferee,
            bool isPstn,
            bool async)
        {
            if (string.IsNullOrEmpty(callConnectionId))
                return BadRequest("callConnectionId is required");

            var idType = isPstn ? "PSTN" : "ACS";
            _logger.LogInformation($"Starting {(async ? "async " : "")}transfer {idType} call: {transferTarget} → {transferee}");

            try
            {
                var connection = _service.GetCallConnection(callConnectionId);
                var correlationId = _service.GetCallConnectionProperties(callConnectionId).CorrelationId;

                TransferToParticipantOptions options;
                if (isPstn)
                {
                    // PSTN → PSTN
                    options = new TransferToParticipantOptions(new PhoneNumberIdentifier(transferTarget))
                    {
                        OperationContext = "TransferCallContext",
                        Transferee = new PhoneNumberIdentifier(transferee)
                    };
                }
                else
                {
                    // ACS → ACS
                    options = new TransferToParticipantOptions(new CommunicationUserIdentifier(transferTarget))
                    {
                        OperationContext = "TransferCallContext",
                        Transferee = new CommunicationUserIdentifier(transferee)
                    };
                }

                // Call SDK
                Response<TransferCallToParticipantResult> resp = async
                    ? await connection.TransferCallToParticipantAsync(options)
                    : connection.TransferCallToParticipant(options);

                _logger.LogInformation(
                    $"Transfer complete. CallConnId={callConnectionId}, CorrId={correlationId}, Status={resp.GetRawResponse().Status}");

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = resp.GetRawResponse().Status.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error transferring {idType} call");
                return Problem($"Failed to transfer {idType} call: {ex.Message}");
            }
        }

        private async Task<IActionResult> HandleHangup(
            string callConnectionId,
            bool isForEveryone,
            bool async)
        {
            if (string.IsNullOrEmpty(callConnectionId))
                return BadRequest("callConnectionId is required");

            _logger.LogInformation($"Starting {(async ? "async " : "")}hangup for {callConnectionId}");

            try
            {
                var connection = _service.GetCallConnection(callConnectionId);
                var correlationId = _service.GetCallConnectionProperties(callConnectionId).CorrelationId;

                var resp = async
                    ? await connection.HangUpAsync(isForEveryone)
                    : connection.HangUp(isForEveryone);

                _logger.LogInformation(
                    $"Hangup complete. ConnId={callConnectionId}, CorrId={correlationId}, Status={resp.Status}");

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = resp.Status.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hanging up call");
                return Problem($"Failed to hang up call: {ex.Message}");
            }
        }

        private async Task<IActionResult> HandleGroupCall(
            string targets,
            bool async)
        {
            if (string.IsNullOrEmpty(targets))
                return BadRequest("Targets parameter is required");

            var targetList = targets.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                   .Select(t => t.Trim())
                                   .Where(t => !string.IsNullOrWhiteSpace(t))
                                   .ToList();

            if (targetList.Count == 0)
                return BadRequest("At least one target is required");

            _logger.LogInformation($"Starting {(async ? "async " : "")}group call to {string.Join(", ", targetList)}");

            try
            {
                // Build identifiers based on format
                var idList = new List<CommunicationIdentifier>();

                foreach (var target in targetList)
                {
                    if (target.StartsWith("8:"))
                    {
                        // ACS participant
                        idList.Add(new CommunicationUserIdentifier(target));
                    }
                    else
                    {
                        // PSTN participant
                        if (!target.StartsWith("+"))
                            return BadRequest($"PSTN number '{target}' must include country code (e.g., +1 for US)");

                        idList.Add(new PhoneNumberIdentifier(target));
                    }
                }

                var callbackUri = new Uri(new Uri(_config.CallbackUriHost), "/api/callbacks");
                var sourceCallerId = new PhoneNumberIdentifier(_config.AcsPhoneNumber);

                var createGroupOpts = new CreateGroupCallOptions(idList, callbackUri)
                {
                    SourceCallerIdNumber = sourceCallerId,
                    CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(_config.CognitiveServiceEndpoint) },
                    // ... any media/transcription options you need
                };

                CreateCallResult result;
                if (async)
                    result = await _service.GetCallAutomationClient().CreateGroupCallAsync(createGroupOpts);
                else
                    result = _service.GetCallAutomationClient().CreateGroupCall(createGroupOpts);

                var props = result.CallConnectionProperties;
                _logger.LogInformation(
                    $"Group call created. ConnId={props.CallConnectionId}, CorrId={props.CorrelationId}");

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = props.CallConnectionId,
                    CorrelationId = props.CorrelationId,
                    Status = props.CallConnectionState.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating group call");
                return Problem($"Failed to create group call: {ex.Message}");
            }
        }
    }
}




#region Outbound Call to PSTN
/**********************************************************************************************
app.MapPost("/outboundCallToPstnAsync", async (string targetPhoneNumber, ILogger<Program> logger) =>
{
    try
    {
        PhoneNumberIdentifier target = new PhoneNumberIdentifier(targetPhoneNumber);
        PhoneNumberIdentifier caller = new PhoneNumberIdentifier(acsPhoneNumber);

        var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
        eventCallbackUri = callbackUri;
        CallInvite callInvite = new CallInvite(target, caller);

        var createCallOptions = new CreateCallOptions(callInvite, callbackUri)    
        {
        // ACS GCCH Phase 2
        // CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },

        };

        CreateCallResult createCallResult = await client.CreateCallAsync(createCallOptions);

        logger.LogInformation($"Created async pstn call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
        LogCollector.Log($"Created async pstn call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
        return Results.Ok(new { CallConnectionId = createCallResult.CallConnectionProperties.CallConnectionId, Status = "Succeeded" });
    }
    catch (Exception ex)
    {
        LogCollector.Log($"Error creating outbound call: {ex.Message}");
        return Results.Problem($"Failed to create outbound call: {ex.Message}");
    }
}).WithTags("Create Outbound Call APIs");

app.MapPost("/outboundCallToPstn", (string targetPhoneNumber, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation($"Starting outbound call to PSTN: {targetPhoneNumber}");
        LogCollector.Log($"Starting outbound call to PSTN: {targetPhoneNumber}");

        PhoneNumberIdentifier target = new PhoneNumberIdentifier(targetPhoneNumber);
        PhoneNumberIdentifier caller = new PhoneNumberIdentifier(acsPhoneNumber);

        var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
        eventCallbackUri = callbackUri;
        CallInvite callInvite = new CallInvite(target, caller);

        var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
        {
            // ACS GCCH Phase 2
            // CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
        };

        logger.LogInformation("Initiating CreateCall operation");
        LogCollector.Log("Initiating CreateCall operation");

        CreateCallResult createCallResult = client.CreateCall(createCallOptions);

        logger.LogInformation($"Created call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
        LogCollector.Log($"Created call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
        return Results.Ok(new { CallConnectionId = createCallResult.CallConnectionProperties.CallConnectionId, Status = "Succeeded" });
    }
    catch (Exception ex)
    {
        logger.LogInformation($"Error creating outbound call: {ex.Message}");
        LogCollector.Log($"Error creating outbound call: {ex.Message}");
        return Results.Problem($"Failed to create outbound call: {ex.Message}");
    }
}).WithTags("Create Outbound Call APIs");
*/
#endregion
#region Outbound Call to Teams
/**********************************************************************************************
app.MapPost("/outboundCallToTeamsAsync", async (string teamsObjectId, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation($"Starting async outbound call to Teams user: {teamsObjectId}");
        LogCollector.Log($"Starting async outbound call to Teams user: {teamsObjectId}");

        var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
        eventCallbackUri = callbackUri;
        CallInvite callInvite = new CallInvite(new MicrosoftTeamsUserIdentifier(teamsObjectId));

        var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
        {
            // ACS GCCH Phase 2
            // CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
        };

        logger.LogInformation("Initiating CreateCallAsync operation");
        LogCollector.Log("Initiating CreateCallAsync operation");

        CreateCallResult createCallResult = await client.CreateCallAsync(createCallOptions);

        logger.LogInformation($"Created async teams call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
        LogCollector.Log($"Created async teams call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
        return Results.Ok(new { CallConnectionId = createCallResult.CallConnectionProperties.CallConnectionId, Status = "Succeeded" });
    }
    catch (Exception ex)
    {
        logger.LogInformation($"Error creating outbound Teams call: {ex.Message}");
        LogCollector.Log($"Error creating outbound Teams call: {ex.Message}");
        return Results.Problem($"Failed to create outbound Teams call: {ex.Message}");
    }
}).WithTags("Create Outbound Call APIs");

app.MapPost("/outboundCallToTeams", (string teamsObjectId, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation($"Starting outbound call to Teams user: {teamsObjectId}");
        LogCollector.Log($"Starting outbound call to Teams user: {teamsObjectId}");

        var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
        eventCallbackUri = callbackUri;
        CallInvite callInvite = new CallInvite(new MicrosoftTeamsUserIdentifier(teamsObjectId));

        var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
        {
            // ACS GCCH Phase 2
            // CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
        };

        logger.LogInformation("Initiating CreateCall operation");
        LogCollector.Log("Initiating CreateCall operation");

        CreateCallResult createCallResult = client.CreateCall(createCallOptions);

        logger.LogInformation($"Created teams call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
        LogCollector.Log($"Created teams call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
        return Results.Ok(new { CallConnectionId = createCallResult.CallConnectionProperties.CallConnectionId, Status = "Succeeded" });
    }
    catch (Exception ex)
    {
        logger.LogInformation($"Error creating outbound Teams call: {ex.Message}");
        LogCollector.Log($"Error creating outbound Teams call: {ex.Message}");
        return Results.Problem($"Failed to create outbound Teams call: {ex.Message}");
    }
}).WithTags("Create Outbound Call APIs");
*/

#endregion
#region Group Call with pstn phone number
/*
app.MapPost("/createGroupCallAsync", async (string targetPhoneNumber, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation($"Starting async group call to: {targetPhoneNumber}");
        LogCollector.Log($"Starting async group call to: {targetPhoneNumber}");

        PhoneNumberIdentifier target = new PhoneNumberIdentifier(targetPhoneNumber);
        PhoneNumberIdentifier sourceCallerId = new PhoneNumberIdentifier(acsPhoneNumber);

        var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
        eventCallbackUri = callbackUri;
        var websocketUri = callbackUriHost.Replace("https", "wss") + "/ws";
        MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(new Uri(websocketUri), MediaStreamingContent.Audio,
            MediaStreamingAudioChannel.Unmixed, MediaStreamingTransport.Websocket, false);
        TranscriptionOptions transcriptionOptions = new TranscriptionOptions(new Uri(websocketUri), TranscriptionTransport.Websocket,
            "en-us", false);

        IEnumerable<CommunicationIdentifier> targets = new List<CommunicationIdentifier>()
        {
            target
        };

        var createGroupCallOptions = new CreateGroupCallOptions(targets, callbackUri)
        {
            // ACS GCCH Phase 2
            // CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
            SourceCallerIdNumber = sourceCallerId,
            MediaStreamingOptions = mediaStreamingOptions,
            TranscriptionOptions = transcriptionOptions
        };

        logger.LogInformation("Initiating CreateGroupCallAsync operation");
        LogCollector.Log("Initiating CreateGroupCallAsync operation");

        CreateCallResult createCallResult = await client.CreateGroupCallAsync(createGroupCallOptions);

        logger.LogInformation($"Created async group call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
        LogCollector.Log($"Created async group call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
        return Results.Ok(new { CallConnectionId = createCallResult.CallConnectionProperties.CallConnectionId, Status = "Succeeded" });
    }
    catch (Exception ex)
    {
        logger.LogInformation($"Error creating group call: {ex.Message}");
        LogCollector.Log($"Error creating group call: {ex.Message}");
        return Results.Problem($"Failed to create group call: {ex.Message}");
    }
}).WithTags("Create Group Call APIs");

app.MapPost("/createGroupCall", (string targetPhoneNumber, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation($"Starting group call to: {targetPhoneNumber}");
        LogCollector.Log($"Starting group call to: {targetPhoneNumber}");

        PhoneNumberIdentifier target = new PhoneNumberIdentifier(targetPhoneNumber);
        PhoneNumberIdentifier sourceCallerId = new PhoneNumberIdentifier(acsPhoneNumber);

        var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
        eventCallbackUri = callbackUri;
        var websocketUri = callbackUriHost.Replace("https", "wss") + "/ws";
        MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(new Uri(websocketUri), MediaStreamingContent.Audio,
            MediaStreamingAudioChannel.Unmixed, MediaStreamingTransport.Websocket, false);
        TranscriptionOptions transcriptionOptions = new TranscriptionOptions(new Uri(websocketUri), TranscriptionTransport.Websocket,
            "en-us", false);

        IEnumerable<CommunicationIdentifier> targets = new List<CommunicationIdentifier>()
        {
            target
        };

        var createGroupCallOptions = new CreateGroupCallOptions(targets, callbackUri)
        {
            // ACS GCCH Phase 2
            // CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
            SourceCallerIdNumber = sourceCallerId,
            MediaStreamingOptions = mediaStreamingOptions,
            TranscriptionOptions = transcriptionOptions
        };

        logger.LogInformation("Initiating CreateGroupCall operation");
        LogCollector.Log("Initiating CreateGroupCall operation");

        CreateCallResult createCallResult = client.CreateGroupCall(createGroupCallOptions);

        logger.LogInformation($"Created group call with id: {createCallResult.CallConnectionProperties.CallConnectionId}");
        LogCollector.Log($"Created group call with id: {createCallResult.CallConnectionProperties.CallConnectionId}");
        return Results.Ok(new { CallConnectionId = createCallResult.CallConnectionProperties.CallConnectionId, Status = "Succeeded" });
    }
    catch (Exception ex)
    {
        logger.LogInformation($"Error creating group call: {ex.Message}");
        LogCollector.Log($"Error creating group call: {ex.Message}");
        return Results.Problem($"Failed to create group call: {ex.Message}");
    }
}).WithTags("Create Group Call APIs");
*/
#endregion
#region Transfer Call
/*
app.MapPost("/transferCallToPstnParticipantAsync", async (string callConnectionId, string pstnTransferTarget, string pstnTarget, ILogger<Program> logger) =>
{
    try
    {
        CallConnection callConnection = GetConnection(callConnectionId);

        TransferToParticipantOptions transferToParticipantOptions = new TransferToParticipantOptions(new PhoneNumberIdentifier(pstnTransferTarget))
        {
            OperationContext = "TransferCallContext",
            Transferee = new PhoneNumberIdentifier(pstnTarget),
        };

        await callConnection.TransferCallToParticipantAsync(transferToParticipantOptions);
        logger.LogInformation($"Call transferred successfully. CallConnectionId: {callConnectionId}");
        LogCollector.Log($"Call transferred successfully. CallConnectionId: {callConnectionId}");
        return Results.Ok();
    }
    catch (Exception ex)
    {
        logger.LogError($"Error transferring call: {ex.Message}. CallConnectionId: {callConnectionId}");
        LogCollector.Log($"Error transferring call: {ex.Message}. CallConnectionId: {callConnectionId}");
        return Results.Problem($"Failed to transfer call: {ex.Message}. CallConnectionId: {callConnectionId}");
    }
}).WithTags("Transfer Call APIs");

app.MapPost("/transferCallToPstnParticipant", (string callConnectionId, string pstnTransferTarget, string pstnTarget, ILogger<Program> logger) =>
{
    try
    {
        CallConnection callConnection = GetConnection(callConnectionId);

        TransferToParticipantOptions transferToParticipantOptions = new TransferToParticipantOptions(new PhoneNumberIdentifier(pstnTransferTarget))
        {
            OperationContext = "TransferCallContext",
            Transferee = new PhoneNumberIdentifier(pstnTarget),
        };

        callConnection.TransferCallToParticipant(transferToParticipantOptions);
        logger.LogInformation($"Call transferred successfully. CallConnectionId: {callConnectionId}");
        LogCollector.Log($"Call transferred successfully. CallConnectionId: {callConnectionId}");
        return Results.Ok();
    }
    catch (Exception ex)
    {
        logger.LogError($"Error transferring call: {ex.Message}. CallConnectionId: {callConnectionId}");
        LogCollector.Log($"Error transferring call: {ex.Message}. CallConnectionId: {callConnectionId}");
        return Results.Problem($"Failed to transfer call: {ex.Message}. CallConnectionId: {callConnectionId}");
    }
}).WithTags("Transfer Call APIs");
*/
#endregion