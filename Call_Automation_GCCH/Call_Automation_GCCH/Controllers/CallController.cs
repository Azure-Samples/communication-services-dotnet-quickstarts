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
        private readonly ICallAutomationService _service;
        private readonly ILogger<CallController> _logger;
        private readonly AcsCommunicationSettings _config;

        public CallController(
            ICallAutomationService service,
            ILogger<CallController> logger,
            IOptions<AcsCommunicationSettings> configOptions)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = configOptions.Value ?? throw new ArgumentNullException(nameof(configOptions));
        }

        /// <summary>
        /// Creates an outbound call with optional transcription and media streaming.
        /// Omit "transcriptionOptions" / "mediaStreamingOptions" for a basic call.
        /// </summary>
        [HttpPost("createCallAsync")]
        [Tags("Outbound Call APIs")]
        public Task<IActionResult> CreateCallAsync([FromBody] CreateCallWithOptionsRequest request)
            => HandleCreateCallWithOptions(request, async: true);

        [HttpPost("createCall")]
        [Tags("Outbound Call APIs")]
        public IActionResult CreateCall([FromBody] CreateCallWithOptionsRequest request)
            => HandleCreateCallWithOptions(request, async: false).Result;

        /// <summary>
        /// Creates a group call with optional transcription and media streaming.
        /// Omit "transcriptionOptions" / "mediaStreamingOptions" for a basic call.
        /// </summary>
        [HttpPost("createGroupCallAsync")]
        [Tags("Group Call APIs")]
        public Task<IActionResult> CreateGroupCallAsync([FromBody] CreateGroupCallWithOptionsRequest request)
            => HandleGroupCallWithOptions(request, async: true);

        [HttpPost("createGroupCall")]
        [Tags("Group Call APIs")]
        public IActionResult CreateGroupCall([FromBody] CreateGroupCallWithOptionsRequest request)
            => HandleGroupCallWithOptions(request, async: false).Result;

        /// <summary>Transfers a call to another participant.</summary>
        [HttpPost("transferCallAsync")]
        [Tags("Transfer Call APIs")]
        public Task<IActionResult> TransferCallAsync([FromBody] TransferCallRequest request)
            => HandleTransferCall(request, async: true);

        [HttpPost("transferCall")]
        [Tags("Transfer Call APIs")]
        public IActionResult TransferCall([FromBody] TransferCallRequest request)
            => HandleTransferCall(request, async: false).Result;

        /// <summary>Hangs up a call.</summary>
        [HttpPost("hangupAsync")]
        [Tags("Disconnect call APIs")]
        public Task<IActionResult> HangupAsync(string callConnectionId, bool isForEveryone)
            => HandleHangup(callConnectionId, isForEveryone, async: true);

        [HttpPost("hangup")]
        [Tags("Disconnect call APIs")]
        public IActionResult Hangup(string callConnectionId, bool isForEveryone)
            => HandleHangup(callConnectionId, isForEveryone, async: false).Result;

        //
        // ========  HELPERS  ========
        //

        private async Task<IActionResult> HandleTransferCall(TransferCallRequest request, bool async)
        {
            if (string.IsNullOrEmpty(request.CallConnectionId))
                return BadRequest("callConnectionId is required");

            var idType = request.IsPstn ? "PSTN" : "ACS";
            _logger.LogInformation("Transfer {Type} call. Target={Target}, Transferee={Transferee}", idType, request.TransferTarget, request.Transferee);

            try
            {
                var connection = _service.GetCallConnection(request.CallConnectionId);
                var correlationId = _service.GetCallConnectionProperties(request.CallConnectionId).CorrelationId;

                TransferToParticipantOptions options = request.IsPstn
                    ? new TransferToParticipantOptions(new PhoneNumberIdentifier(request.TransferTarget))
                      { OperationContext = "TransferCallContext", Transferee = new PhoneNumberIdentifier(request.Transferee) }
                    : new TransferToParticipantOptions(new CommunicationUserIdentifier(request.TransferTarget))
                      { OperationContext = "TransferCallContext", Transferee = new CommunicationUserIdentifier(request.Transferee) };

                Response<TransferCallToParticipantResult> resp = async
                    ? await connection.TransferCallToParticipantAsync(options)
                    : connection.TransferCallToParticipant(options);

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = request.CallConnectionId,
                    CorrelationId = correlationId,
                    Status = resp.GetRawResponse().Status.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transferring {Type} call", idType);
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

            _logger.LogInformation("Hangup. CallId={CallId}, ForEveryone={ForEveryone}", callConnectionId, isForEveryone);

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

        // -- CreateCallWithOptions handler ---------------------------------------

        private async Task<IActionResult> HandleCreateCallWithOptions(CreateCallWithOptionsRequest request, bool async)
        {
            if (string.IsNullOrEmpty(request.Target))
                return BadRequest("Target is required");

            var idType = request.IsPstn ? "PSTN" : "ACS";
            _logger.LogInformation("CreateCallWithOptions. Target={Target}, Type={Type}, Transcription={HasTranscription}, MediaStreaming={HasMediaStreaming}",
                request.Target, idType, request.TranscriptionOptions != null, request.MediaStreamingOptions != null);

            try
            {
                var callbackUri = new Uri(new Uri(_config.CallbackUriHost), "/api/callbacks");
                CallInvite invite = request.IsPstn
                    ? new CallInvite(new PhoneNumberIdentifier(request.Target), new PhoneNumberIdentifier(_config.AcsPhoneNumber))
                    : new CallInvite(new CommunicationUserIdentifier(request.Target));

                var options = new CreateCallOptions(invite, callbackUri)
                {
                    OperationContext = request.OperationContext
                };

                if (request.TranscriptionOptions != null)
                    options.TranscriptionOptions = ToSdkTranscriptionOptions(request.TranscriptionOptions);

                if (request.MediaStreamingOptions != null)
                    options.MediaStreamingOptions = ToSdkMediaStreamingOptions(request.MediaStreamingOptions);

                if (request.CallIntelligenceOptions != null)
                    options.CallIntelligenceOptions = ToSdkCallIntelligenceOptions(request.CallIntelligenceOptions);

                CreateCallResult result = async
                    ? await _service.GetCallAutomationClient().CreateCallAsync(options)
                    : _service.GetCallAutomationClient().CreateCall(options);

                var props = result.CallConnectionProperties;
                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = props.CallConnectionId,
                    CorrelationId = props.CorrelationId,
                    Status = props.CallConnectionState.ToString()
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateCall");
                return Problem($"Failed to create {idType} call: {ex.Message}");
            }
        }

        private async Task<IActionResult> HandleGroupCallWithOptions(CreateGroupCallWithOptionsRequest request, bool async)
        {
            if (request.Targets == null || request.Targets.Count == 0)
                return BadRequest("At least one target is required");

            _logger.LogInformation("CreateGroupCallWithOptions. Targets={Targets}, Transcription={HasTranscription}, MediaStreaming={HasMediaStreaming}",
                string.Join(",", request.Targets), request.TranscriptionOptions != null, request.MediaStreamingOptions != null);

            try
            {
                var idList = new List<CommunicationIdentifier>();
                foreach (var t in request.Targets)
                {
                    var trimmed = t?.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;

                    if (trimmed.StartsWith("8:"))
                        idList.Add(new CommunicationUserIdentifier(trimmed));
                    else if (trimmed.StartsWith("+"))
                        idList.Add(new PhoneNumberIdentifier(trimmed));
                    else
                        return BadRequest($"Invalid target '{trimmed}'. Must be ACS user ID (8:...) or phone number (+...)");
                }

                if (idList.Count == 0)
                    return BadRequest("No valid targets provided");

                var callbackUri = new Uri(new Uri(_config.CallbackUriHost), "/api/callbacks");
                var groupOpts = new CreateGroupCallOptions(idList, callbackUri)
                {
                    SourceCallerIdNumber = new PhoneNumberIdentifier(_config.AcsPhoneNumber),
                    OperationContext = request.OperationContext,
                    SourceDisplayName = request.SourceDisplayName
                };

                if (request.TranscriptionOptions != null)
                    groupOpts.TranscriptionOptions = ToSdkTranscriptionOptions(request.TranscriptionOptions);

                if (request.MediaStreamingOptions != null)
                    groupOpts.MediaStreamingOptions = ToSdkMediaStreamingOptions(request.MediaStreamingOptions);

                if (request.CallIntelligenceOptions != null)
                    groupOpts.CallIntelligenceOptions = ToSdkCallIntelligenceOptions(request.CallIntelligenceOptions);

                CreateCallResult result = async
                    ? await _service.GetCallAutomationClient().CreateGroupCallAsync(groupOpts)
                    : _service.GetCallAutomationClient().CreateGroupCall(groupOpts);

                var props = result.CallConnectionProperties;
                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = props.CallConnectionId,
                    CorrelationId = props.CorrelationId,
                    Status = props.CallConnectionState.ToString()
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateGroupCallWithOptions");
                return Problem($"Failed to create group call with options: {ex.Message}");
            }
        }

        // -- Shared helpers ------------------------------------------------------

        private TranscriptionOptions ToSdkTranscriptionOptions(TranscriptionOptionsRequest config)
        {
            var wsUri = BuildWebSocketUri();
            return new TranscriptionOptions(
                new Uri(wsUri),
                config.Locale ?? "en-US",
                config.StartTranscription,
                TranscriptionTransport.Websocket)
            {
                EnableIntermediateResults = config.EnableIntermediateResults
            };
        }

        private MediaStreamingOptions ToSdkMediaStreamingOptions(MediaStreamingOptionsRequest config)
        {
            var wsUri = BuildWebSocketUri();
            var audioChannel = (config.MediaStreamingAudioChannel ?? "Mixed").Equals("Unmixed", StringComparison.OrdinalIgnoreCase)
                ? MediaStreamingAudioChannel.Unmixed : MediaStreamingAudioChannel.Mixed;
            var format = (config.AudioFormat ?? "Pcm16KMono").Equals("Pcm24KMono", StringComparison.OrdinalIgnoreCase)
                ? AudioFormat.Pcm24KMono : AudioFormat.Pcm16KMono;

            return new MediaStreamingOptions(
                new Uri(wsUri),
                MediaStreamingContent.Audio,
                audioChannel,
                MediaStreamingTransport.Websocket,
                config.StartMediaStreaming)
            {
                EnableBidirectional = config.EnableBidirectional,
                AudioFormat = format
            };
        }

        private string BuildWebSocketUri()
        {
            return _config.CallbackUriHost.Replace("https://", "wss://").Replace("http://", "ws://").TrimEnd('/') + "/ws";
        }

        private CallIntelligenceOptions ToSdkCallIntelligenceOptions(CallIntelligenceOptionsRequest config)
        {
            if (string.IsNullOrWhiteSpace(config.CognitiveServicesEndpoint))
                throw new ArgumentException("callIntelligenceOptions.cognitiveServicesEndpoint is required and cannot be empty.");

            if (!Uri.TryCreate(config.CognitiveServicesEndpoint, UriKind.Absolute, out var endpoint))
                throw new ArgumentException($"callIntelligenceOptions.cognitiveServicesEndpoint is not a valid URI: '{config.CognitiveServicesEndpoint}'");

            return new CallIntelligenceOptions
            {
                CognitiveServicesEndpoint = endpoint
            };
        }
    }

}
