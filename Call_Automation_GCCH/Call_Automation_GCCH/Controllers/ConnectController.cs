using System;
using System.Threading.Tasks;
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
    [Route("api/connect")]
    [Produces("application/json")]
    public class ConnectController : ControllerBase
    {
        private readonly ICallAutomationService _service;
        private readonly ILogger<ConnectController> _logger;
        private readonly AcsCommunicationSettings _config;

        public ConnectController(
            ICallAutomationService service,
            ILogger<ConnectController> logger, IOptions<AcsCommunicationSettings> configOptions)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = configOptions.Value ?? throw new ArgumentNullException(nameof(configOptions));
        }

        /// <summary>
        /// Connects to an existing server call with optional media streaming and transcription.
        /// Include "transcriptionOptions" to enable transcription.
        /// Include "mediaStreamingOptions" to enable media streaming.
        /// Omit either object to disable that feature.
        /// </summary>
        [HttpPost("connectCallAsync")]
        [Tags("Connect Call APIs")]
        public Task<IActionResult> ConnectCallAsync([FromBody] ConnectCallRequest request)
            => HandleConnectCall(request, async: true);

        [HttpPost("connectCall")]
        [Tags("Connect Call APIs")]
        public IActionResult ConnectCall([FromBody] ConnectCallRequest request)
            => HandleConnectCall(request, async: false).Result;

        private async Task<IActionResult> HandleConnectCall(ConnectCallRequest request, bool async)
        {
            if (string.IsNullOrEmpty(request.ServerCallId))
                return BadRequest("serverCallId is required");

            _logger.LogInformation("Connecting to call. ServerCallId={ServerCallId}, MediaStreaming={HasMedia}, Transcription={HasTranscription}",
                request.ServerCallId, request.MediaStreamingOptions != null, request.TranscriptionOptions != null);

            try
            {
                var callbackUri = new Uri(new Uri(_config.CallbackUriHost), "/api/callbacks");
                var websocketUri = _config.CallbackUriHost.Replace("https://", "wss://").Replace("http://", "ws://").TrimEnd('/') + "/ws";

                var callLocator = new ServerCallLocator(request.ServerCallId);
                var connectOpts = new ConnectCallOptions(callLocator, callbackUri)
                {
                    OperationContext = request.OperationContext
                };

                if (request.MediaStreamingOptions != null)
                {
                    var ms = request.MediaStreamingOptions;
                    var audioChannel = (ms.MediaStreamingAudioChannel ?? "Mixed").Equals("Unmixed", StringComparison.OrdinalIgnoreCase)
                        ? MediaStreamingAudioChannel.Unmixed : MediaStreamingAudioChannel.Mixed;
                    var audioFormat = (ms.AudioFormat ?? "Pcm16KMono").Equals("Pcm24KMono", StringComparison.OrdinalIgnoreCase)
                        ? AudioFormat.Pcm24KMono : AudioFormat.Pcm16KMono;

                    connectOpts.MediaStreamingOptions = new MediaStreamingOptions(
                        new Uri(websocketUri),
                        MediaStreamingContent.Audio,
                        audioChannel,
                        MediaStreamingTransport.Websocket,
                        ms.StartMediaStreaming)
                    {
                        EnableBidirectional = ms.EnableBidirectional,
                        AudioFormat = audioFormat
                    };
                }

                if (request.TranscriptionOptions != null)
                {
                    var tc = request.TranscriptionOptions;
                    connectOpts.TranscriptionOptions = new TranscriptionOptions(
                        new Uri(websocketUri),
                        tc.Locale ?? "en-US",
                        tc.StartTranscription,
                        TranscriptionTransport.Websocket)
                    {
                        EnableIntermediateResults = tc.EnableIntermediateResults
                    };
                }

                if (request.CallIntelligenceOptions != null)
                {
                    if (string.IsNullOrWhiteSpace(request.CallIntelligenceOptions.CognitiveServicesEndpoint))
                        throw new ArgumentException("callIntelligenceOptions.cognitiveServicesEndpoint is required and cannot be empty.");

                    if (!Uri.TryCreate(request.CallIntelligenceOptions.CognitiveServicesEndpoint, UriKind.Absolute, out var cogEndpoint))
                        throw new ArgumentException($"callIntelligenceOptions.cognitiveServicesEndpoint is not a valid URI: '{request.CallIntelligenceOptions.CognitiveServicesEndpoint}'");

                    connectOpts.CallIntelligenceOptions = new CallIntelligenceOptions
                    {
                        CognitiveServicesEndpoint = cogEndpoint
                    };
                }

                ConnectCallResult result = async
                    ? await _service.GetCallAutomationClient().ConnectCallAsync(connectOpts)
                    : _service.GetCallAutomationClient().ConnectCall(connectOpts);

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
                _logger.LogError(ex, "Error connecting to call");
                return Problem($"Failed to connect to call: {ex.Message}");
            }
        }
    }
}
