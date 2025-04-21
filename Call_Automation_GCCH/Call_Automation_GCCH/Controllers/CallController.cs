using System;
using System.Threading.Tasks;
using Azure.Communication;
using Azure.Communication.CallAutomation;
using Call_Automation_GCCH.Models;
using Call_Automation_GCCH.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
        private readonly ConfigurationRequest _config; // final, bound object

        public CallController(
            CallAutomationService service, 
            ILogger<CallController> logger, IOptions<ConfigurationRequest> configOptions)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = configOptions.Value ?? throw new ArgumentNullException(nameof(configOptions));
        }

        /// <summary>
        /// Creates an outbound call to an ACS user
        /// </summary>
        /// <param name="acsTarget">The ACS user identifier to call</param>
        /// <returns>Call connection information</returns>
        [HttpPost("/outboundCallToAcs")]
        [Tags("Outbound Call APIs")]
        public IActionResult CreateOutboundCallToAcs(string acsTarget)
        {
            try
            {
                if (string.IsNullOrEmpty(acsTarget))
                {
                    return BadRequest("ACS Target ID is required");
                }

                _logger.LogInformation($"Starting outbound call to ACS user: {acsTarget}");

                var callbackUri = new Uri(new Uri(_config.CallbackUriHost), "/api/callbacks");
                Uri _eventCallbackUri = callbackUri;
                
                var callInvite = new CallInvite(new CommunicationUserIdentifier(acsTarget));
                var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
                {
                    // ACS GCCH Phase 2
                    // CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
                };
                _logger.LogInformation("Initiating CreateCall operation");

                CreateCallResult createCallResult = _service.GetCallAutomationClient().CreateCall(createCallOptions);

                var connectionId = createCallResult.CallConnectionProperties.CallConnectionId;
                var correlationId = createCallResult.CallConnectionProperties.CorrelationId;
                var callStatus = createCallResult.CallConnectionProperties.CallConnectionState.ToString();
                
                _logger.LogInformation($"Created ACS call with connection id: {connectionId}, correlation id: {correlationId}, status: {callStatus}");

                return Ok(new CallConnectionResponse 
                { 
                    CallConnectionId = connectionId,
                    CorrelationId = correlationId,
                    Status = callStatus 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating outbound ACS call: {ex.Message}");
                return Problem($"Failed to create outbound ACS call: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates an outbound call to an ACS user asynchronously
        /// </summary>
        /// <param name="acsTarget">The ACS user identifier to call</param>
        /// <returns>Call connection information</returns>
        [HttpPost("/outboundCallToAcsAsync")]
        [Tags("Outbound Call APIs")]
        public async Task<IActionResult> CreateOutboundCallToAcsAsync(string acsTarget)
        {
            try
            {
                if (string.IsNullOrEmpty(acsTarget))
                {
                    return BadRequest("ACS Target ID is required");
                }

                _logger.LogInformation($"Starting async outbound call to ACS user: {acsTarget}");

                var callbackUri = new Uri(new Uri(_config.CallbackUriHost), "/api/callbacks");
                Uri _eventCallbackUri = callbackUri;

                _logger.LogInformation($"Created async ACS call with Callback Uri: {callbackUri}");
                var callInvite = new CallInvite(new CommunicationUserIdentifier(acsTarget));
                var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
                {
                    // ACS GCCH Phase 2
                    // CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
                };
                _logger.LogInformation("Initiating CreateCallAsync operation");

                CreateCallResult createCallResult = await _service.GetCallAutomationClient().CreateCallAsync(createCallOptions);

                var connectionId = createCallResult.CallConnectionProperties.CallConnectionId;
                var correlationId = createCallResult.CallConnectionProperties.CorrelationId;
                var callStatus = createCallResult.CallConnectionProperties.CallConnectionState.ToString();
                
                _logger.LogInformation($"Created async ACS call with connection id: {connectionId}, correlation id: {correlationId}, status: {callStatus}");

                return Ok(new CallConnectionResponse 
                { 
                    CallConnectionId = connectionId,
                    CorrelationId = correlationId,
                    Status = callStatus 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating outbound ACS call: {ex.Message}");
                return Problem($"Failed to create outbound ACS call: {ex.Message}");
            }
        }
     /// <summary>
        /// Hangs up a call asynchronously
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="isForEveryOne">Whether to hang up for everyone</param>
        /// <returns>Call connection status</returns>
        [HttpPost("/hangupAsync")]
        [Tags("Disconnect call APIs")]
        public async Task<IActionResult> HangupAsync(string callConnectionId, bool isForEveryOne)
        {
            try
            {
                CallConnection callConnection = _service.GetCallConnection(callConnectionId);
                var correlationId = _service.GetCallConnectionProperties(callConnectionId).CorrelationId;
                var disconnectStatus = await callConnection.HangUpAsync(isForEveryOne);
                string successMessage = $"Call hung up successfully. CallConnectionId: {callConnectionId}, correlation id: {correlationId}, status: {disconnectStatus.Status.ToString()}";
                _logger.LogInformation(successMessage);
                return Ok(new CallConnectionResponse 
                { 
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = disconnectStatus.Status.ToString()
                });
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error hanging up call. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
                _logger.LogInformation(errorMessage);
                return Problem($"Failed to hang up call: {ex.Message}");
            }
        }

        /// <summary>
        /// Hangs up a call synchronously
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="isForEveryOne">Whether to hang up for everyone</param>
        /// <returns>Call connection status</returns>
        [HttpPost("/hangup")]
        [Tags("Disconnect call APIs")]
        public IActionResult Hangup(string callConnectionId, bool isForEveryOne)
        {
            try
            {
                CallConnection callConnection = _service.GetCallConnection(callConnectionId);
                var correlationId = _service.GetCallConnectionProperties(callConnectionId).CorrelationId;
                var disconnectStatus = callConnection.HangUp(isForEveryOne);
                string successMessage = $"Call hung up successfully. CallConnectionId: {callConnectionId}, correlation id: {correlationId}, status: {disconnectStatus.Status.ToString()}";
                _logger.LogInformation(successMessage);
                return Ok(new CallConnectionResponse 
                { 
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = disconnectStatus.Status.ToString()
                });
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error hanging up call. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
                _logger.LogInformation(errorMessage);
                return Problem($"Failed to hang up call: {ex.Message}");
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
