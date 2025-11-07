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
    [Route("api/media")]
    [Produces("application/json")]
    public class MediaController : ControllerBase
    {
        private readonly CallAutomationService _service;
        private readonly ILogger<MediaController> _logger;
        private readonly ConfigurationRequest _config;

        public MediaController(
            CallAutomationService service,
            ILogger<MediaController> logger,
            IOptions<ConfigurationRequest> configOptions)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = configOptions.Value ?? throw new ArgumentNullException(nameof(configOptions));
        }

        // ───────────── PLAY FILE SOURCE ────────────────────────────────────────────
        [HttpPost("/playFileSourceToTargetAsync")]
        [Tags("Play FileSource Media")]
        public Task<IActionResult> PlayFileSourceToTargetAsync(
            string callConnectionId,
            string target)
        {
            if (string.IsNullOrEmpty(target))
                return Task.FromResult<IActionResult>(BadRequest("Target is required"));
                
            if (!target.StartsWith("8:") && !target.StartsWith("+"))
                return Task.FromResult<IActionResult>(BadRequest("PSTN number must include country code (e.g., +1 for US)"));
                
            CommunicationIdentifier identifier = target.StartsWith("8:") 
                ? new CommunicationUserIdentifier(target)
                : new PhoneNumberIdentifier(target);
                
            return HandlePlayFileSource(
                callConnectionId,
                new List<CommunicationIdentifier> { identifier },
                playToAll: false,
                bargeIn: false,
                async: true);
        }

        [HttpPost("/playFileSourceToTarget")]
        [Tags("Play FileSource Media")]
        public IActionResult PlayFileSourceToTarget(
            string callConnectionId,
            string target)
        {
            if (string.IsNullOrEmpty(target))
                return BadRequest("Target is required");
                
            if (!target.StartsWith("8:") && !target.StartsWith("+"))
                return BadRequest("PSTN number must include country code (e.g., +1 for US)");
                
            CommunicationIdentifier identifier = target.StartsWith("8:") 
                ? new CommunicationUserIdentifier(target)
                : new PhoneNumberIdentifier(target);
                
            return HandlePlayFileSource(
                callConnectionId,
                new List<CommunicationIdentifier> { identifier },
                playToAll: false,
                bargeIn: false,
                async: false).Result;
        }

        [HttpPost("/playFileSourceToAllAsync")]
        [Tags("Play FileSource Media")]
        public Task<IActionResult> PlayFileSourceToAllAsync(string callConnectionId)
            => HandlePlayFileSource(callConnectionId, targets: null, playToAll: true, bargeIn: false, async: true);

        [HttpPost("/playFileSourceToAll")]
        [Tags("Play FileSource Media")]
        public IActionResult PlayFileSourceToAll(string callConnectionId)
            => HandlePlayFileSource(callConnectionId, targets: null, playToAll: true, bargeIn: false, async: false).Result;

        [HttpPost("/playFileSourceBargeInAsync")]
        [Tags("Play FileSource Media")]
        public Task<IActionResult> PlayFileSourceBargeInAsync(string callConnectionId)
            => HandlePlayFileSource(callConnectionId, targets: null, playToAll: true, bargeIn: true, async: true);

        [HttpPost("/interruptHoldWithPlay")]
        [Tags("Play FileSource Media")]
        public IActionResult InterruptHoldWithPlay(
            string callConnectionId,
            string target)
        {
            if (string.IsNullOrEmpty(target))
                return BadRequest("Target is required");
                
            if (!target.StartsWith("8:") && !target.StartsWith("+"))
                return BadRequest("PSTN number must include country code (e.g., +1 for US)");
                
            CommunicationIdentifier identifier = target.StartsWith("8:") 
                ? new CommunicationUserIdentifier(target)
                : new PhoneNumberIdentifier(target);
                
            return HandlePlayFileSource(
                callConnectionId,
                new List<CommunicationIdentifier> { identifier },
                playToAll: false,
                bargeIn: true,
                async: false).Result;
        }

        // ──────────── MEDIA STREAMING: CREATE CALL ─────────────────────────────────
        /// <summary>
        /// Creates a call with media streaming capabilities asynchronously
        /// </summary>
        /// <param name="target">Target phone number (with country code) or communication user ID</param>
        /// <param name="isMixed">True for mixed audio channel (all participants combined), false for unmixed (separate streams)</param>
        /// <param name="enableMediaStreaming">Whether to enable media streaming</param>
        /// <param name="isEnableBidirectional">Whether to enable bidirectional streaming</param>
        /// <param name="isPcm24kMono">Whether to use PCM 24kHz mono format</param>
        [HttpPost("/createCallWithMediaStreamingAsync")]
        [Tags("Media Streaming")]
        public Task<IActionResult> CreateCallWithMediaStreamingAsync(
            string target,
            bool isMixed = true,
            bool enableMediaStreaming = false,
            bool isEnableBidirectional = false,
            bool isPcm24kMono = false)
        {
            if (string.IsNullOrEmpty(target))
                return Task.FromResult<IActionResult>(BadRequest("Target is required"));
                
            if (!target.StartsWith("8:") && !target.StartsWith("+"))
                return Task.FromResult<IActionResult>(BadRequest("PSTN number must include country code (e.g., +1 for US)"));
                
            var audioChannel = isMixed ? MediaStreamingAudioChannel.Mixed : MediaStreamingAudioChannel.Unmixed;
                
            return HandleCreateCallWithMediaStreaming(
                target,
                audioChannel,
                enableMediaStreaming,
                isEnableBidirectional,
                isPcm24kMono,
                async: true);
        }

        /// <summary>
        /// Creates a call with media streaming capabilities synchronously
        /// </summary>
        /// <param name="target">Target phone number (with country code) or communication user ID</param>
        /// <param name="isMixed">True for mixed audio channel (all participants combined), false for unmixed (separate streams)</param>
        /// <param name="enableMediaStreaming">Whether to enable media streaming</param>
        /// <param name="isEnableBidirectional">Whether to enable bidirectional streaming</param>
        /// <param name="isPcm24kMono">Whether to use PCM 24kHz mono format</param>
        [HttpPost("/createCallWithMediaStreaming")]
        [Tags("Media Streaming")]
        public IActionResult CreateCallWithMediaStreaming(
            string target,
            bool isMixed = true,
            bool enableMediaStreaming = false,
            bool isEnableBidirectional = false,
            bool isPcm24kMono = false)
        {
            if (string.IsNullOrEmpty(target))
                return BadRequest("Target is required");
                
            if (!target.StartsWith("8:") && !target.StartsWith("+"))
                return BadRequest("PSTN number must include country code (e.g., +1 for US)");
                
            var audioChannel = isMixed ? MediaStreamingAudioChannel.Mixed : MediaStreamingAudioChannel.Unmixed;
                
            return HandleCreateCallWithMediaStreaming(
                target,
                audioChannel,
                enableMediaStreaming,
                isEnableBidirectional,
                isPcm24kMono,
                async: false).Result;
        }

        // ──────────── MEDIA STREAMING: START/STOP ─────────────────────────────────
        [HttpPost("/startMediaStreamingAsync")]
        [Tags("Media Streaming")]
        public Task<IActionResult> StartMediaStreamingAsync(string callConnectionId)
            => HandleMediaStreaming(callConnectionId, start: true, withOptions: false, async: true);

        [HttpPost("/startMediaStreaming")]
        [Tags("Media Streaming")]
        public IActionResult StartMediaStreaming(string callConnectionId)
            => HandleMediaStreaming(callConnectionId, start: true, withOptions: false, async: false).Result;

        [HttpPost("/stopMediaStreamingAsync")]
        [Tags("Media Streaming")]
        public Task<IActionResult> StopMediaStreamingAsync(string callConnectionId)
            => HandleMediaStreaming(callConnectionId, start: false, withOptions: false, async: true);

        [HttpPost("/stopMediaStreaming")]
        [Tags("Media Streaming")]
        public IActionResult StopMediaStreaming(string callConnectionId)
            => HandleMediaStreaming(callConnectionId, start: false, withOptions: false, async: false).Result;

        [HttpPost("/startMediaStreamingWithOptionsAsync")]
        [Tags("Media Streaming")]
        public Task<IActionResult> StartMediaStreamingWithOptionsAsync(string callConnectionId)
            => HandleMediaStreaming(callConnectionId, start: true, withOptions: true, async: true);

        [HttpPost("/startMediaStreamingWithOptions")]
        [Tags("Media Streaming")]
        public IActionResult StartMediaStreamingWithOptions(string callConnectionId)
            => HandleMediaStreaming(callConnectionId, start: true, withOptions: true, async: false).Result;

        [HttpPost("/stopMediaStreamingWithOptionsAsync")]
        [Tags("Media Streaming")]
        public Task<IActionResult> StopMediaStreamingWithOptionsAsync(string callConnectionId)
            => HandleMediaStreaming(callConnectionId, start: false, withOptions: true, async: true);

        [HttpPost("/stopMediaStreamingWithOptions")]
        [Tags("Media Streaming")]
        public IActionResult StopMediaStreamingWithOptions(string callConnectionId)
            => HandleMediaStreaming(callConnectionId, start: false, withOptions: true, async: false).Result;

        // ────────────── CANCEL ALL MEDIA ───────────────────────────────────────────
        [HttpPost("/cancelAllMediaOperationAsync")]
        [Tags("Media Operations")]
        public Task<IActionResult> CancelAllMediaOperationAsync(string callConnectionId)
            => HandleCancelAllMediaOperations(callConnectionId, async: true);

        [HttpPost("/cancelAllMediaOperation")]
        [Tags("Media Operations")]
        public IActionResult CancelAllMediaOperation(string callConnectionId)
            => HandleCancelAllMediaOperations(callConnectionId, async: false).Result;

        // ──────────────── RECOGNIZE ────────────────────────────────────────────────
        [HttpPost("/recognizeDTMFAsync")]
        [Tags("Recognition")]
        public Task<IActionResult> RecognizeDTMFAsync(string callConnectionId, string target)
        {
            if (string.IsNullOrEmpty(target))
                return Task.FromResult<IActionResult>(BadRequest("Target is required"));
                
            if (!target.StartsWith("8:") && !target.StartsWith("+"))
                return Task.FromResult<IActionResult>(BadRequest("PSTN number must include country code (e.g., +1 for US)"));
                
            CommunicationIdentifier identifier = target.StartsWith("8:") 
                ? new CommunicationUserIdentifier(target)
                : new PhoneNumberIdentifier(target);
                
            return HandleRecognize(callConnectionId, identifier, RecognizeType.Dtmf, async: true);
        }

        [HttpPost("/recognizeDTMF")]
        [Tags("Recognition")]
        public IActionResult RecognizeDTMF(string callConnectionId, string target)
        {
            if (string.IsNullOrEmpty(target))
                return BadRequest("Target is required");
                
            if (!target.StartsWith("8:") && !target.StartsWith("+"))
                return BadRequest("PSTN number must include country code (e.g., +1 for US)");
                
            CommunicationIdentifier identifier = target.StartsWith("8:") 
                ? new CommunicationUserIdentifier(target)
                : new PhoneNumberIdentifier(target);
                
            return HandleRecognize(callConnectionId, identifier, RecognizeType.Dtmf, async: false).Result;
        }

        // ──────────── HOLD / UNHOLD ─────────────────────────────────────────────────
        [HttpPost("/holdTargetAsync")]
        [Tags("Hold Management")]
        public Task<IActionResult> HoldTargetAsync(string callConnectionId, string target, bool isPlaySource)
        {
            if (string.IsNullOrEmpty(target))
                return Task.FromResult<IActionResult>(BadRequest("Target is required"));
                
            if (!target.StartsWith("8:") && !target.StartsWith("+"))
                return Task.FromResult<IActionResult>(BadRequest("PSTN number must include country code (e.g., +1 for US)"));
                
            CommunicationIdentifier identifier = target.StartsWith("8:") 
                ? new CommunicationUserIdentifier(target)
                : new PhoneNumberIdentifier(target);
                
            return HandleHold(callConnectionId, identifier, isPlaySource, unhold: false, async: true);
        }

        [HttpPost("/holdTarget")]
        [Tags("Hold Management")]
        public IActionResult HoldTarget(string callConnectionId, string target, bool isPlaySource)
        {
            if (string.IsNullOrEmpty(target))
                return BadRequest("Target is required");
                
            if (!target.StartsWith("8:") && !target.StartsWith("+"))
                return BadRequest("PSTN number must include country code (e.g., +1 for US)");
                
            CommunicationIdentifier identifier = target.StartsWith("8:") 
                ? new CommunicationUserIdentifier(target)
                : new PhoneNumberIdentifier(target);
                
            return HandleHold(callConnectionId, identifier, isPlaySource, unhold: false, async: false).Result;
        }

        [HttpPost("/unholdTargetAsync")]
        [Tags("Hold Management")]
        public Task<IActionResult> UnholdTargetAsync(string callConnectionId, string target)
        {
            if (string.IsNullOrEmpty(target))
                return Task.FromResult<IActionResult>(BadRequest("Target is required"));
                
            if (!target.StartsWith("8:") && !target.StartsWith("+"))
                return Task.FromResult<IActionResult>(BadRequest("PSTN number must include country code (e.g., +1 for US)"));
                
            CommunicationIdentifier identifier = target.StartsWith("8:") 
                ? new CommunicationUserIdentifier(target)
                : new PhoneNumberIdentifier(target);
                
            return HandleHold(callConnectionId, identifier, playSource: false, unhold: true, async: true);
        }

        [HttpPost("/unholdTarget")]
        [Tags("Hold Management")]
        public IActionResult UnholdTarget(string callConnectionId, string target)
        {
            if (string.IsNullOrEmpty(target))
                return BadRequest("Target is required");
                
            if (!target.StartsWith("8:") && !target.StartsWith("+"))
                return BadRequest("PSTN number must include country code (e.g., +1 for US)");
                
            CommunicationIdentifier identifier = target.StartsWith("8:") 
                ? new CommunicationUserIdentifier(target)
                : new PhoneNumberIdentifier(target);
                
            return HandleHold(callConnectionId, identifier, playSource: false, unhold: true, async: false).Result;
        }

        // ────────── INTERRUPT AUDIO AND ANNOUNCE ───────────────────────────────────
        [HttpPost("/interruptAudioAndAnnounceAsync")]
        [Tags("Audio Announcements")]
        public Task<IActionResult> InterruptAudioAndAnnounceAsync(string callConnectionId, string target)
        {
            if (string.IsNullOrEmpty(target))
                return Task.FromResult<IActionResult>(BadRequest("Target is required"));
                
            if (!target.StartsWith("8:") && !target.StartsWith("+"))
                return Task.FromResult<IActionResult>(BadRequest("PSTN number must include country code (e.g., +1 for US)"));
                
            CommunicationIdentifier identifier = target.StartsWith("8:") 
                ? new CommunicationUserIdentifier(target)
                : new PhoneNumberIdentifier(target);
                
            return HandleInterruptAudioAndAnnounce(callConnectionId, identifier, async: true);
        }

        [HttpPost("/interruptAudioAndAnnounce")]
        [Tags("Audio Announcements")]
        public IActionResult InterruptAudioAndAnnounce(string callConnectionId, string target)
        {
            if (string.IsNullOrEmpty(target))
                return BadRequest("Target is required");
                
            if (!target.StartsWith("8:") && !target.StartsWith("+"))
                return BadRequest("PSTN number must include country code (e.g., +1 for US)");
                
            CommunicationIdentifier identifier = target.StartsWith("8:") 
                ? new CommunicationUserIdentifier(target)
                : new PhoneNumberIdentifier(target);
                
            return HandleInterruptAudioAndAnnounce(callConnectionId, identifier, async: false).Result;
        }

        // ───────────── PRIVATE HANDLERS ──────────────────────────────────────────

        private async Task<IActionResult> HandlePlayFileSource(
            string callConnectionId,
            List<CommunicationIdentifier> targets,
            bool playToAll,
            bool bargeIn,
            bool async)
        {
            _logger.LogInformation($"Playing file source. CallId={callConnectionId}, PlayToAll={playToAll}, Targets={(targets != null ? string.Join(',', targets.Select(t => t.RawId)) : "All")}, BargeIn={bargeIn}, Async={async}");
            try
            {
                var callMedia = _service.GetCallMedia(callConnectionId);
                var props = _service.GetCallConnectionProperties(callConnectionId);
                var fileSource = new FileSource(new Uri(_config.CallbackUriHost + "/audio/prompt.wav"));

                if (playToAll)
                {
                    var options = new PlayToAllOptions(fileSource)
                    {
                        OperationContext = "playToAllContext",
                        InterruptCallMediaOperation = bargeIn
                    };
                    var response = async ? await callMedia.PlayToAllAsync(options) : callMedia.PlayToAll(options);
                    var status = response.GetRawResponse().ToString();
                    _logger.LogInformation($"Played to all. Status={status}");
                    return Ok(new CallConnectionResponse { CallConnectionId = callConnectionId, CorrelationId = props.CorrelationId, Status = status });
                }
                else
                {
                    if (targets == null || targets.Count == 0)
                        return BadRequest("Target(s) required for play operation.");

                    var options = new PlayOptions(fileSource, targets)
                    {
                        OperationContext = "playToContext",
                        InterruptHoldAudio = bargeIn
                    };
                    var response = async ? await callMedia.PlayAsync(options) : callMedia.Play(options);
                    var status = response.GetRawResponse().ToString();
                    _logger.LogInformation($"Played to targets. Status={status}");
                    return Ok(new CallConnectionResponse { CallConnectionId = callConnectionId, CorrelationId = props.CorrelationId, Status = status });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error playing file source.");
                return Problem($"Failed to play file source: {ex.Message}");
            }
        }

        private async Task<IActionResult> HandleCreateCallWithMediaStreaming(
            string target,
            MediaStreamingAudioChannel audioChannel,
            bool enableMediaStreaming,
            bool enableBidirectional,
            bool pcm24kMono,
            bool async)
        {
            bool isPstn = !target.StartsWith("8:");
            string targetType = isPstn ? "PSTN" : "ACS";
            
            _logger.LogInformation($"Creating call with media streaming. Target={target}, Type={targetType}, Channel={audioChannel}, EnableMedia={enableMediaStreaming}, Bidirectional={enableBidirectional}, PCM24kMono={pcm24kMono}, Async={async}");
            try
            {
                var callbackUri = new Uri(new Uri(_config.CallbackUriHost), "/api/callbacks");
                var websocketUri = _config.CallbackUriHost.Replace("https", "wss") + "/ws";

                MediaStreamingOptions mediaOpts = new MediaStreamingOptions(
                    new Uri(websocketUri),
                    MediaStreamingContent.Audio,
                    audioChannel,
                    MediaStreamingTransport.Websocket,
                    enableMediaStreaming)
                {
                    EnableBidirectional = enableBidirectional,
                    AudioFormat = pcm24kMono ? AudioFormat.Pcm24KMono : AudioFormat.Pcm16KMono
                };

                var invite = isPstn
                    ? new CallInvite(new PhoneNumberIdentifier(target), new PhoneNumberIdentifier(_config.AcsPhoneNumber))
                    : new CallInvite(new CommunicationUserIdentifier(target));
                    
                var createOpts = new CreateCallOptions(invite, callbackUri)
                {
                    MediaStreamingOptions = mediaOpts
                };

                CreateCallResult result = async
                    ? await _service.GetCallAutomationClient().CreateCallAsync(createOpts)
                    : _service.GetCallAutomationClient().CreateCall(createOpts);

                var props = result.CallConnectionProperties;
                var status = props.CallConnectionState.ToString();
                _logger.LogInformation($"Call created. CallConnectionId={props.CallConnectionId}, Status={status}");
                return Ok(new { CallConnectionId = props.CallConnectionId, CorrelationId = props.CorrelationId, Status = status });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating call with media streaming");
                return Problem($"Failed to create call with media streaming: {ex.Message}");
            }
        }

        private async Task<IActionResult> HandleMediaStreaming(
            string callConnectionId,
            bool start,
            bool withOptions,
            bool async)
        {
            _logger.LogInformation($"{(start ? "Starting" : "Stopping")} media streaming. CallId={callConnectionId}, WithOptions={withOptions}, Async={async}");
            try
            {
                var callMedia = _service.GetCallMedia(callConnectionId);
                var props = _service.GetCallConnectionProperties(callConnectionId);
                Response response;

                if (start)
                {
                    if (withOptions)
                    {
                        var opts = new StartMediaStreamingOptions
                        {
                            OperationContext = "StartMediaStreamingContext",
                            OperationCallbackUri = new Uri(new Uri(_config.CallbackUriHost), "/api/callbacks")
                        };
                        response = async ? await callMedia.StartMediaStreamingAsync(opts) : callMedia.StartMediaStreaming(opts);
                    }
                    else
                    {
                        response = async ? await callMedia.StartMediaStreamingAsync() : callMedia.StartMediaStreaming();
                    }
                }
                else // stop
                {
                    if (withOptions)
                    {
                        var opts = new StopMediaStreamingOptions
                        {
                            OperationContext = "StopMediaStreamingContext",
                            OperationCallbackUri = new Uri(new Uri(_config.CallbackUriHost), "/api/callbacks")
                        };
                        response = async ? await callMedia.StopMediaStreamingAsync(opts) : callMedia.StopMediaStreaming(opts);
                    }
                    else
                    {
                        response = async ? await callMedia.StopMediaStreamingAsync() : callMedia.StopMediaStreaming();
                    }
                }

                var status = response.Status.ToString();
                _logger.LogInformation($"Media streaming {(start ? "started" : "stopped")}. Status={status}");
                return Ok(new { CallConnectionId = callConnectionId, CorrelationId = props.CorrelationId, Status = status });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during media streaming operation");
                return Problem($"Failed media streaming operation: {ex.Message}");
            }
        }

        private async Task<IActionResult> HandleCancelAllMediaOperations(string callConnectionId, bool async)
        {
            _logger.LogInformation($"Cancelling all media operations. CallId={callConnectionId}, Async={async}");
            try
            {
                var props = _service.GetCallConnectionProperties(callConnectionId);
                var callMedia = _service.GetCallMedia(callConnectionId);
                Response<CancelAllMediaOperationsResult> result = async
                  ? await callMedia.CancelAllMediaOperationsAsync()
                  : callMedia.CancelAllMediaOperations();

                // ← Pull status from the raw response
                var status = result.GetRawResponse().Status.ToString();

                _logger.LogInformation($"Cancelled all media operations. Status={status}");
                return Ok(new { CallConnectionId = callConnectionId, CorrelationId = props.CorrelationId, Status = status });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling all media operations");
                return Problem($"Failed to cancel all media operations: {ex.Message}");
            }
        }

        private enum RecognizeType { Dtmf, Choice, Speech, SpeechOrDtmf }

        private async Task<IActionResult> HandleRecognize(
            string callConnectionId,
            CommunicationIdentifier target,
            RecognizeType type,
            bool async)
        {
            _logger.LogInformation($"Starting recognition. CallId={callConnectionId}, Type={type}, Async={async}");
            try
            {
                var callMedia = _service.GetCallMedia(callConnectionId);
                var props = _service.GetCallConnectionProperties(callConnectionId);
                var textSource = new TextSource("Please respond.") { VoiceName = "en-US-NancyNeural" };
                var fileSource = new FileSource(new Uri(_config.CallbackUriHost + "/audio/prompt.wav"));

                switch (type)
                {
                    case RecognizeType.Dtmf:
                        var dtmfOpts = new CallMediaRecognizeDtmfOptions(target, maxTonesToCollect: 4)
                        {
                            Prompt = fileSource,
                            InterruptPrompt = false,
                            InitialSilenceTimeout = TimeSpan.FromSeconds(15),
                            InterToneTimeout = TimeSpan.FromSeconds(5),
                            OperationContext = "DtmfContext"
                        };
                        if (async) await callMedia.StartRecognizingAsync(dtmfOpts); else callMedia.StartRecognizing(dtmfOpts);
                        break;
                    case RecognizeType.Choice:
                        var choiceOpts = new CallMediaRecognizeChoiceOptions(target, GetChoices())
                        {
                            Prompt = textSource,
                            InterruptPrompt = false,
                            InitialSilenceTimeout = TimeSpan.FromSeconds(10),
                            OperationContext = "ChoiceContext"
                        };
                        if (async) await callMedia.StartRecognizingAsync(choiceOpts); else callMedia.StartRecognizing(choiceOpts);
                        break;
                    case RecognizeType.Speech:
                        var speechOpts = new CallMediaRecognizeSpeechOptions(target)
                        {
                            Prompt = textSource,
                            InterruptPrompt = false,
                            InitialSilenceTimeout = TimeSpan.FromSeconds(15),
                            EndSilenceTimeout = TimeSpan.FromSeconds(15),
                            OperationContext = "SpeechContext"
                        };
                        if (async) await callMedia.StartRecognizingAsync(speechOpts); else callMedia.StartRecognizing(speechOpts);
                        break;
                    case RecognizeType.SpeechOrDtmf:
                        var bothOpts = new CallMediaRecognizeSpeechOrDtmfOptions(target, 4)
                        {
                            Prompt = textSource,
                            InterruptPrompt = false,
                            InitialSilenceTimeout = TimeSpan.FromSeconds(15),
                            EndSilenceTimeout = TimeSpan.FromSeconds(5),
                            OperationContext = "SpeechOrDTMFContext"
                        };
                        if (async) await callMedia.StartRecognizingAsync(bothOpts); else callMedia.StartRecognizing(bothOpts);
                        break;
                }

                _logger.LogInformation("Recognition started successfully");
                return Ok(new CallConnectionResponse { CallConnectionId = callConnectionId, CorrelationId = props.CorrelationId, Status = props.CallConnectionState.ToString() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during recognition");
                return Problem($"Failed to start recognition: {ex.Message}");
            }
        }

        private IEnumerable<RecognitionChoice> GetChoices() =>
            new List<RecognitionChoice>
            {
                new RecognitionChoice("yes", new[] { "yes", "yeah" }),
                new RecognitionChoice("no", new[] { "no", "nope" })
            };

        private async Task<IActionResult> HandleHold(
            string callConnectionId,
            CommunicationIdentifier target,
            bool playSource,
            bool unhold,
            bool async)
        {
            _logger.LogInformation($"{(unhold ? "Unhold" : "Hold")} participant. CallId={callConnectionId}, Target={target.RawId}, PlaySource={playSource}, Async={async}");
            try
            {
                var callMedia = _service.GetCallMedia(callConnectionId);
                var props = _service.GetCallConnectionProperties(callConnectionId);

                if (unhold)
                {
                    var opts = new UnholdOptions(target)
                    {
                        OperationContext = "unholdUserContext"
                    };
                    if (async) await callMedia.UnholdAsync(opts); else callMedia.Unhold(opts);
                }
                else
                {
                    var opts = new HoldOptions(target)
                    {
                        OperationContext = "holdUserContext"
                    };
                    if (playSource)
                        opts.PlaySource = new FileSource(new Uri(_config.CallbackUriHost + "/audio/prompt.wav"));
                    if (async) await callMedia.HoldAsync(opts); else callMedia.Hold(opts);
                }

                _logger.LogInformation("Hold/Unhold operation completed");
                return Ok(new CallConnectionResponse { CallConnectionId = callConnectionId, CorrelationId = props.CorrelationId, Status = props.CallConnectionState.ToString() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during hold/unhold");
                return Problem($"Failed to {(unhold ? "unhold" : "hold")}: {ex.Message}");
            }
        }

        private async Task<IActionResult> HandleInterruptAudioAndAnnounce(
            string callConnectionId,
            CommunicationIdentifier target,
            bool async)
        {
            _logger.LogInformation($"Interrupt audio and announce. CallId={callConnectionId}, Target={target.RawId}, Async={async}");
            try
            {
                var callMedia = _service.GetCallMedia(callConnectionId);
                var props = _service.GetCallConnectionProperties(callConnectionId);
                var fileSource = new FileSource(new Uri(_config.CallbackUriHost + "/audio/prompt.wav"));
                var opts = new InterruptAudioAndAnnounceOptions(fileSource, target)
                {
                    OperationContext = "interruptContext"
                };

                if (async) await callMedia.InterruptAudioAndAnnounceAsync(opts); else callMedia.InterruptAudioAndAnnounce(opts);

                _logger.LogInformation("Interrupt audio and announce completed");
                return Ok(new CallConnectionResponse { CallConnectionId = callConnectionId, CorrelationId = props.CorrelationId, Status = props.CallConnectionState.ToString() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during interrupt audio and announce");
                return Problem($"Failed to interrupt audio and announce: {ex.Message}");
            }
        }
    }
}

#region Play Media with File Source



//app.MapPost("/playFileSourceToPstnTargetAsync", async (string callConnectionId, string pstnTarget, ILogger<Program> logger) =>
//{
//    try
//    {
//        CallMedia callMedia = GetCallMedia(callConnectionId);
//        //  FileSource fileSource = new FileSource(new Uri(fileSourceUri));
//        List<CommunicationIdentifier> playTo = new List<CommunicationIdentifier> { new PhoneNumberIdentifier(pstnTarget) };
//        PlayOptions playToOptions = new PlayOptions(fileSource, playTo)
//        {
//            OperationContext = "playToContext"
//        };

//        logger.LogInformation($"Playing file source to PSTN target. CallConnectionId: {callConnectionId}, Target: {pstnTarget}");
//        await callMedia.PlayAsync(playToOptions);

//        string successMessage = $"File source played successfully to PSTN target. CallConnectionId: {callConnectionId}";
//        LogCollector.Log(successMessage);
//        logger.LogInformation(successMessage);

//        return Results.Ok(new { CallConnectionId = callConnectionId, Status = "Succeeded" });
//    }
//    catch (Exception ex)
//    {
//        string errorMessage = $"Error playing file source to PSTN target. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
//        LogCollector.Log(errorMessage);
//        logger.LogInformation(errorMessage);

//        return Results.Problem($"Failed to play file source: {ex.Message}");
//    }
//}).WithTags("Play FileSource Media APIs");

//app.MapPost("/playFileSourceToPstnTarget", (string callConnectionId, string pstnTarget, ILogger<Program> logger) =>
//{
//    try
//    {
//        CallMedia callMedia = GetCallMedia(callConnectionId);
//        //  FileSource fileSource = new FileSource(new Uri(fileSourceUri));
//        List<CommunicationIdentifier> playTo = new List<CommunicationIdentifier> { new PhoneNumberIdentifier(pstnTarget) };
//        PlayOptions playToOptions = new PlayOptions(fileSource, playTo)
//        {
//            OperationContext = "playToContext"
//        };

//        logger.LogInformation($"Playing file source to PSTN target. CallConnectionId: {callConnectionId}, Target: {pstnTarget}");
//        callMedia.Play(playToOptions);

//        string successMessage = $"File source played successfully to PSTN target. CallConnectionId: {callConnectionId}";
//        LogCollector.Log(successMessage);
//        logger.LogInformation(successMessage);

//        return Results.Ok(new { CallConnectionId = callConnectionId, Status = "Succeeded" });
//    }
//    catch (Exception ex)
//    {
//        string errorMessage = $"Error playing file source to PSTN target. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
//        LogCollector.Log(errorMessage);
//        logger.LogInformation(errorMessage);

//        return Results.Problem($"Failed to play file source: {ex.Message}");
//    }
//}).WithTags("Play FileSource Media APIs");


//app.MapPost("/playFileSourceToTeamsTargetAsync", async (string callConnectionId, string teamsObjectId, ILogger<Program> logger) =>
//{
//    try
//    {
//        CallMedia callMedia = GetCallMedia(callConnectionId);
//        // FileSource fileSource = new FileSource(new Uri(fileSourceUri));
//        List<CommunicationIdentifier> playTo = new List<CommunicationIdentifier> { new MicrosoftTeamsUserIdentifier(teamsObjectId) };
//        PlayOptions playToOptions = new PlayOptions(fileSource, playTo)
//        {
//            OperationContext = "playToContext"
//        };

//        logger.LogInformation($"Playing file source to Teams target. CallConnectionId: {callConnectionId}, Target: {teamsObjectId}");
//        await callMedia.PlayAsync(playToOptions);

//        string successMessage = $"File source played successfully to Teams target. CallConnectionId: {callConnectionId}";
//        LogCollector.Log(successMessage);
//        logger.LogInformation(successMessage);

//        return Results.Ok(new { CallConnectionId = callConnectionId, Status = "Succeeded" });
//    }
//    catch (Exception ex)
//    {
//        string errorMessage = $"Error playing file source to Teams target. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
//        LogCollector.Log(errorMessage);
//        logger.LogInformation(errorMessage);

//        return Results.Problem($"Failed to play file source: {ex.Message}");
//    }
//}).WithTags("Play FileSource Media APIs");

//app.MapPost("/playFileSourceToTeamsTarget", (string callConnectionId, string teamsObjectId, ILogger<Program> logger) =>
//{
//    try
//    {
//        CallMedia callMedia = GetCallMedia(callConnectionId);
//        //  FileSource fileSource = new FileSource(new Uri(fileSourceUri));
//        List<CommunicationIdentifier> playTo = new List<CommunicationIdentifier> { new MicrosoftTeamsUserIdentifier(teamsObjectId) };
//        PlayOptions playToOptions = new PlayOptions(fileSource, playTo)
//        {
//            OperationContext = "playToContext"
//        };

//        logger.LogInformation($"Playing file source to Teams target. CallConnectionId: {callConnectionId}, Target: {teamsObjectId}");
//        callMedia.Play(playToOptions);

//        string successMessage = $"File source played successfully to Teams target. CallConnectionId: {callConnectionId}";
//        LogCollector.Log(successMessage);
//        logger.LogInformation(successMessage);

//        return Results.Ok(new { CallConnectionId = callConnectionId, Status = "Succeeded" });
//    }
//    catch (Exception ex)
//    {
//        string errorMessage = $"Error playing file source to Teams target. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
//        LogCollector.Log(errorMessage);
//        logger.LogInformation(errorMessage);

//        return Results.Problem($"Failed to play file source: {ex.Message}");
//    }
//}).WithTags("Play FileSource Media APIs");

#endregion
#region Recognization
/*
app.MapPost("/recognizeDTMFAsync", async (string callConnectionId, string pstnTarget, ILogger<Program> logger) =>
{
    try
    {
        CallMedia callMedia = GetCallMedia(callConnectionId);
        TextSource textSource = new TextSource("Hi, this is recognize test. please provide input thanks!.")
        {
            VoiceName = "en-US-NancyNeural"
        };

        var target = new PhoneNumberIdentifier(pstnTarget);

        var recognizeOptions =
                new CallMediaRecognizeDtmfOptions(
                    targetParticipant: target, maxTonesToCollect: 4)
                {
                    InterruptPrompt = false,
                    InterToneTimeout = TimeSpan.FromSeconds(5),
                    OperationContext = "DtmfContext",
                    InitialSilenceTimeout = TimeSpan.FromSeconds(15),
                    Prompt = textSource
                };

        logger.LogInformation($"Starting DTMF recognition. CallConnectionId: {callConnectionId}, Target: {pstnTarget}");
        await callMedia.StartRecognizingAsync(recognizeOptions);
        
        string successMessage = $"DTMF recognition started successfully. CallConnectionId: {callConnectionId}";
        LogCollector.Log(successMessage);
        logger.LogInformation(successMessage);
        
        return Results.Ok(new { CallConnectionId = callConnectionId, Status = "Succeeded" });
    }
    catch (Exception ex)
    {
        string errorMessage = $"Error starting DTMF recognition. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
        LogCollector.Log(errorMessage);
        logger.LogInformation(errorMessage);
        
        return Results.Problem($"Failed to start DTMF recognition: {ex.Message}");
    }
}).WithTags("Start Recognization APIs");

app.MapPost("/recognizeDTMF", (string callConnectionId, string pstnTarget, ILogger<Program> logger) =>
{
    try
    {
        CallMedia callMedia = GetCallMedia(callConnectionId);
        TextSource textSource = new TextSource("Hi, this is recognize test. please provide input thanks!.")
        {
            VoiceName = "en-US-NancyNeural"
        };

        var target = new PhoneNumberIdentifier(pstnTarget);

        var recognizeOptions =
                new CallMediaRecognizeDtmfOptions(
                    targetParticipant: target, maxTonesToCollect: 4)
                {
                    InterruptPrompt = false,
                    InterToneTimeout = TimeSpan.FromSeconds(5),
                    OperationContext = "DtmfContext",
                    InitialSilenceTimeout = TimeSpan.FromSeconds(15),
                    Prompt = textSource
                };

        logger.LogInformation($"Starting DTMF recognition. CallConnectionId: {callConnectionId}, Target: {pstnTarget}");
        callMedia.StartRecognizing(recognizeOptions);
        
        string successMessage = $"DTMF recognition started successfully. CallConnectionId: {callConnectionId}";
        LogCollector.Log(successMessage);
        logger.LogInformation(successMessage);
        
        return Results.Ok(new { CallConnectionId = callConnectionId, Status = "Succeeded" });
    }
    catch (Exception ex)
    {
        string errorMessage = $"Error starting DTMF recognition. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
        LogCollector.Log(errorMessage);
        logger.LogInformation(errorMessage);
        
        return Results.Problem($"Failed to start DTMF recognition: {ex.Message}");
    }
}).WithTags("Start Recognization APIs");


app.MapPost("/recognizeChoiceAsync", async (string callConnectionId, string pstnTarget, ILogger<Program> logger) =>
{
    try
    {
        CallMedia callMedia = GetCallMedia(callConnectionId);
        TextSource textSource = new TextSource("Hi, this is recognize test. please provide input thanks!.")
        {
            VoiceName = "en-US-NancyNeural"
        };

        var target = new PhoneNumberIdentifier(pstnTarget);


        var recognizeOptions =
            new CallMediaRecognizeChoiceOptions(targetParticipant: target, GetChoices())
            {
                InterruptCallMediaOperation = false,
                InterruptPrompt = false,
                InitialSilenceTimeout = TimeSpan.FromSeconds(10),
                Prompt = textSource,
                OperationContext = "ChoiceContext"
            };

        logger.LogInformation($"Starting choice recognition. CallConnectionId: {callConnectionId}, Target: {pstnTarget}");
        await callMedia.StartRecognizingAsync(recognizeOptions);
        
        string successMessage = $"Choice recognition started successfully. CallConnectionId: {callConnectionId}";
        LogCollector.Log(successMessage);
        logger.LogInformation(successMessage);
        
        return Results.Ok(new { CallConnectionId = callConnectionId, Status = "Succeeded" });
    }
    catch (Exception ex)
    {
        string errorMessage = $"Error starting choice recognition. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
        LogCollector.Log(errorMessage);
        logger.LogInformation(errorMessage);
        
        return Results.Problem($"Failed to start choice recognition: {ex.Message}");
    }
}).WithTags("Start Recognization APIs");

app.MapPost("/recognizeChoice", (string callConnectionId, string pstnTarget, ILogger<Program> logger) =>
{
    try
    {
        CallMedia callMedia = GetCallMedia(callConnectionId);
        TextSource textSource = new TextSource("Hi, this is recognize test. please provide input thanks!.")
        {
            VoiceName = "en-US-NancyNeural"
        };

        var target = new PhoneNumberIdentifier(pstnTarget);


        var recognizeOptions =
            new CallMediaRecognizeChoiceOptions(targetParticipant: target, GetChoices())
            {
                InterruptCallMediaOperation = false,
                InterruptPrompt = false,
                InitialSilenceTimeout = TimeSpan.FromSeconds(10),
                Prompt = textSource,
                OperationContext = "ChoiceContext"
            };

        logger.LogInformation($"Starting choice recognition. CallConnectionId: {callConnectionId}, Target: {pstnTarget}");
        callMedia.StartRecognizingAsync(recognizeOptions);
        
        string successMessage = $"Choice recognition started successfully. CallConnectionId: {callConnectionId}";
        LogCollector.Log(successMessage);
        logger.LogInformation(successMessage);
        
        return Results.Ok(new { CallConnectionId = callConnectionId, Status = "Succeeded" });
    }
    catch (Exception ex)
    {
        string errorMessage = $"Error starting choice recognition. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
        LogCollector.Log(errorMessage);
        logger.LogInformation(errorMessage);
        
        return Results.Problem($"Failed to start choice recognition: {ex.Message}");
    }
}).WithTags("Start Recognization APIs");

app.MapPost("/recognizeSpeechAsync", async (string callConnectionId, string pstnTarget, ILogger<Program> logger) =>
{
    try
    {
        CallMedia callMedia = GetCallMedia(callConnectionId);
        TextSource textSource = new TextSource("Hi, this is recognize test. please provide input thanks!.")
        {
            VoiceName = "en-US-NancyNeural"
        };

        var target = new PhoneNumberIdentifier(pstnTarget);

        var recognizeOptions = new CallMediaRecognizeSpeechOptions(targetParticipant: target)
        {
            InterruptPrompt = false,
            OperationContext = "SpeechContext",
            InitialSilenceTimeout = TimeSpan.FromSeconds(15),
            Prompt = textSource,
            EndSilenceTimeout = TimeSpan.FromSeconds(15)
        };

        logger.LogInformation($"Starting speech recognition. CallConnectionId: {callConnectionId}, Target: {pstnTarget}");
        await callMedia.StartRecognizingAsync(recognizeOptions);
        
        string successMessage = $"Speech recognition started successfully. CallConnectionId: {callConnectionId}";
        LogCollector.Log(successMessage);
        logger.LogInformation(successMessage);
        
        return Results.Ok(new { CallConnectionId = callConnectionId, Status = "Succeeded" });
    }
    catch (Exception ex)
    {
        string errorMessage = $"Error starting speech recognition. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
        LogCollector.Log(errorMessage);
        logger.LogInformation(errorMessage);
        
        return Results.Problem($"Failed to start speech recognition: {ex.Message}");
    }
}).WithTags("Start Recognization APIs");

app.MapPost("/recognizeSpeech", (string callConnectionId, string pstnTarget, ILogger<Program> logger) =>
{
    try
    {
        CallMedia callMedia = GetCallMedia(callConnectionId);
        TextSource textSource = new TextSource("Hi, this is recognize test. please provide input thanks!.")
        {
            VoiceName = "en-US-NancyNeural"
        };

        var target = new PhoneNumberIdentifier(pstnTarget);

        var recognizeOptions = new CallMediaRecognizeSpeechOptions(targetParticipant: target)
        {
            InterruptPrompt = false,
            OperationContext = "SpeechContext",
            InitialSilenceTimeout = TimeSpan.FromSeconds(15),
            Prompt = textSource,
            EndSilenceTimeout = TimeSpan.FromSeconds(15)
        };

        logger.LogInformation($"Starting speech recognition. CallConnectionId: {callConnectionId}, Target: {pstnTarget}");
        callMedia.StartRecognizing(recognizeOptions);
        
        string successMessage = $"Speech recognition started successfully. CallConnectionId: {callConnectionId}";
        LogCollector.Log(successMessage);
        logger.LogInformation(successMessage);
        
        return Results.Ok(new { CallConnectionId = callConnectionId, Status = "Succeeded" });
    }
    catch (Exception ex)
    {
        string errorMessage = $"Error starting speech recognition. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
        LogCollector.Log(errorMessage);
        logger.LogInformation(errorMessage);
        
        return Results.Problem($"Failed to start speech recognition: {ex.Message}");
    }
}).WithTags("Start Recognization APIs");

app.MapPost("/recognizeSpeechOrDtmfAsync", async (string callConnectionId, string pstnTarget, ILogger<Program> logger) =>
{
    try
    {
        CallMedia callMedia = GetCallMedia(callConnectionId);
        TextSource textSource = new TextSource("Hi, this is recognize test. please provide input thanks!.")
        {
            VoiceName = "en-US-NancyNeural"
        };

        var target = new PhoneNumberIdentifier(pstnTarget);

        var recognizeOptions =
                   new CallMediaRecognizeSpeechOrDtmfOptions(
                       targetParticipant: target, maxTonesToCollect: 4)
                   {
                       InterruptPrompt = false,
                       OperationContext = "SpeechOrDTMFContext",
                       InitialSilenceTimeout = TimeSpan.FromSeconds(15),
                       Prompt = textSource,
                       EndSilenceTimeout = TimeSpan.FromSeconds(5)
                   };
                   
        logger.LogInformation($"Starting speech/DTMF recognition. CallConnectionId: {callConnectionId}, Target: {pstnTarget}");
        await callMedia.StartRecognizingAsync(recognizeOptions);
        
        string successMessage = $"Speech/DTMF recognition started successfully. CallConnectionId: {callConnectionId}";
        LogCollector.Log(successMessage);
        logger.LogInformation(successMessage);
        
        return Results.Ok(new { CallConnectionId = callConnectionId, Status = "Succeeded" });
    }
    catch (Exception ex)
    {
        string errorMessage = $"Error starting speech/DTMF recognition. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
        LogCollector.Log(errorMessage);
        logger.LogInformation(errorMessage);
        
        return Results.Problem($"Failed to start speech/DTMF recognition: {ex.Message}");
    }
}).WithTags("Start Recognization APIs");

app.MapPost("/recognizeSpeechOrDtmf", (string callConnectionId, string pstnTarget, ILogger<Program> logger) =>
{
    try
    {
        CallMedia callMedia = GetCallMedia(callConnectionId);
        TextSource textSource = new TextSource("Hi, this is recognize test. please provide input thanks!.")
        {
            VoiceName = "en-US-NancyNeural"
        };

        var target = new PhoneNumberIdentifier(pstnTarget);

        var recognizeOptions =
                   new CallMediaRecognizeSpeechOrDtmfOptions(
                       targetParticipant: target, maxTonesToCollect: 4)
                   {
                       InterruptPrompt = false,
                       OperationContext = "SpeechOrDTMFContext",
                       InitialSilenceTimeout = TimeSpan.FromSeconds(15),
                       Prompt = textSource,
                       EndSilenceTimeout = TimeSpan.FromSeconds(5)
                   };
                   
        logger.LogInformation($"Starting speech/DTMF recognition. CallConnectionId: {callConnectionId}, Target: {pstnTarget}");
        callMedia.StartRecognizingAsync(recognizeOptions);
        
        string successMessage = $"Speech/DTMF recognition started successfully. CallConnectionId: {callConnectionId}";
        LogCollector.Log(successMessage);
        logger.LogInformation(successMessage);
        
        return Results.Ok(new { CallConnectionId = callConnectionId, Status = "Succeeded" });
    }
    catch (Exception ex)
    {
        string errorMessage = $"Error starting speech/DTMF recognition. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
        LogCollector.Log(errorMessage);
        logger.LogInformation(errorMessage);
        
        return Results.Problem($"Failed to start speech/DTMF recognition: {ex.Message}");
    }
}).WithTags("Start Recognization APIs");

*/
#endregion
#region DTMF
/*
app.MapPost("/sendDTMFTonesAsync", async (string callConnectionId, string pstnTarget, ILogger<Program> logger) =>
{
    try
    {
        CommunicationIdentifier target = new PhoneNumberIdentifier(pstnTarget);

        List<DtmfTone> tones = new List<DtmfTone>
            {
                DtmfTone.Zero,
                DtmfTone.One
            };

        CallMedia callMedia = GetCallMedia(callConnectionId);

        logger.LogInformation($"Sending DTMF tones. CallConnectionId: {callConnectionId}, Target: {pstnTarget}, Tones: 0,1");
        await callMedia.SendDtmfTonesAsync(tones, target);
        
        string successMessage = $"DTMF tones sent successfully. CallConnectionId: {callConnectionId}";
        LogCollector.Log(successMessage);
        logger.LogInformation(successMessage);
        
        return Results.Ok(new { CallConnectionId = callConnectionId, Status = "Succeeded" });
    }
    catch (Exception ex)
    {
        string errorMessage = $"Error sending DTMF tones. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
        LogCollector.Log(errorMessage);
        logger.LogInformation(errorMessage);
        
        return Results.Problem($"Failed to send DTMF tones: {ex.Message}");
    }
}).WithTags("Send or Start DTMF APIs");

app.MapPost("/sendDTMFTones", (string callConnectionId, string pstnTarget, ILogger<Program> logger) =>
{
    try
    {
        CommunicationIdentifier target = new PhoneNumberIdentifier(pstnTarget);

        List<DtmfTone> tones = new List<DtmfTone>
            {
                DtmfTone.Zero,
                DtmfTone.One
            };

        CallMedia callMedia = GetCallMedia(callConnectionId);

        logger.LogInformation($"Sending DTMF tones. CallConnectionId: {callConnectionId}, Target: {pstnTarget}, Tones: 0,1");
        callMedia.SendDtmfTones(tones, target);
        
        string successMessage = $"DTMF tones sent successfully. CallConnectionId: {callConnectionId}";
        LogCollector.Log(successMessage);
        logger.LogInformation(successMessage);
        
        return Results.Ok(new { CallConnectionId = callConnectionId, Status = "Succeeded" });
    }
    catch (Exception ex)
    {
        string errorMessage = $"Error sending DTMF tones. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
        LogCollector.Log(errorMessage);
        logger.LogInformation(errorMessage);
        
        return Results.Problem($"Failed to send DTMF tones: {ex.Message}");
    }
}).WithTags("Send or Start DTMF APIs");

app.MapPost("/startContinuousDTMFTonesAsync", async (string callConnectionId, string pstnTarget, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation($"Starting continuous DTMF recognition. CallConnectionId: {callConnectionId}, Target: {pstnTarget}");
        LogCollector.Log($"Starting continuous DTMF recognition. CallConnectionId: {callConnectionId}, Target: {pstnTarget}");
        
        CommunicationIdentifier target = new PhoneNumberIdentifier(pstnTarget);

        CallMedia callMedia = GetCallMedia(callConnectionId);

        logger.LogInformation($"Executing StartContinuousDtmfRecognitionAsync. CallConnectionId: {callConnectionId}, Target: {pstnTarget}");
        LogCollector.Log($"Executing StartContinuousDtmfRecognitionAsync. CallConnectionId: {callConnectionId}, Target: {pstnTarget}");
        
        await callMedia.StartContinuousDtmfRecognitionAsync(target);
        
        string successMessage = $"Continuous DTMF recognition started successfully. CallConnectionId: {callConnectionId}";
        logger.LogInformation(successMessage);
        LogCollector.Log(successMessage);
        
        return Results.Ok(new { CallConnectionId = callConnectionId, Status = "Succeeded" });
    }
    catch (Exception ex)
    {
        string errorMessage = $"Error starting continuous DTMF recognition. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
        logger.LogInformation(errorMessage);
        LogCollector.Log(errorMessage);
        
        return Results.Problem($"Failed to start continuous DTMF recognition: {ex.Message}");
    }
}).WithTags("Send or Start DTMF APIs");

app.MapPost("/startContinuousDTMFTones", (string callConnectionId, string pstnTarget, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation($"Starting continuous DTMF recognition. CallConnectionId: {callConnectionId}, Target: {pstnTarget}");
        LogCollector.Log($"Starting continuous DTMF recognition. CallConnectionId: {callConnectionId}, Target: {pstnTarget}");
        
        CommunicationIdentifier target = new PhoneNumberIdentifier(pstnTarget);

        CallMedia callMedia = GetCallMedia(callConnectionId);

        logger.LogInformation($"Executing StartContinuousDtmfRecognition. CallConnectionId: {callConnectionId}, Target: {pstnTarget}");
        LogCollector.Log($"Executing StartContinuousDtmfRecognition. CallConnectionId: {callConnectionId}, Target: {pstnTarget}");
        
        callMedia.StartContinuousDtmfRecognition(target);
        
        string successMessage = $"Continuous DTMF recognition started successfully. CallConnectionId: {callConnectionId}";
        logger.LogInformation(successMessage);
        LogCollector.Log(successMessage);
        
        return Results.Ok(new { CallConnectionId = callConnectionId, Status = "Succeeded" });
    }
    catch (Exception ex)
    {
        string errorMessage = $"Error starting continuous DTMF recognition. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
        logger.LogInformation(errorMessage);
        LogCollector.Log(errorMessage);
        
        return Results.Problem($"Failed to start continuous DTMF recognition: {ex.Message}");
    }
}).WithTags("Send or Start DTMF APIs");

app.MapPost("/stopContinuousDTMFTonesAsync", async (string callConnectionId, string pstnTarget, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation($"Stopping continuous DTMF recognition. CallConnectionId: {callConnectionId}, Target: {pstnTarget}");
        LogCollector.Log($"Stopping continuous DTMF recognition. CallConnectionId: {callConnectionId}, Target: {pstnTarget}");
        
        CommunicationIdentifier target = new PhoneNumberIdentifier(pstnTarget);

        CallMedia callMedia = GetCallMedia(callConnectionId);

        logger.LogInformation($"Executing StopContinuousDtmfRecognitionAsync. CallConnectionId: {callConnectionId}, Target: {pstnTarget}");
        LogCollector.Log($"Executing StopContinuousDtmfRecognitionAsync. CallConnectionId: {callConnectionId}, Target: {pstnTarget}");
        
        await callMedia.StopContinuousDtmfRecognitionAsync(target);
        
        string successMessage = $"Continuous DTMF recognition stopped successfully. CallConnectionId: {callConnectionId}";
        logger.LogInformation(successMessage);
        LogCollector.Log(successMessage);
        
        return Results.Ok(new { CallConnectionId = callConnectionId, Status = "Succeeded" });
    }
    catch (Exception ex)
    {
        string errorMessage = $"Error stopping continuous DTMF recognition. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
        logger.LogInformation(errorMessage);
        LogCollector.Log(errorMessage);
        
        return Results.Problem($"Failed to stop continuous DTMF recognition: {ex.Message}");
    }
}).WithTags("Send or Start DTMF APIs");

app.MapPost("/stopContinuousDTMFTones", (string callConnectionId, string pstnTarget, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation($"Stopping continuous DTMF recognition. CallConnectionId: {callConnectionId}, Target: {pstnTarget}");
        LogCollector.Log($"Stopping continuous DTMF recognition. CallConnectionId: {callConnectionId}, Target: {pstnTarget}");
        
        CommunicationIdentifier target = new PhoneNumberIdentifier(pstnTarget);

        CallMedia callMedia = GetCallMedia(callConnectionId);

        logger.LogInformation($"Executing StopContinuousDtmfRecognition. CallConnectionId: {callConnectionId}, Target: {pstnTarget}");
        LogCollector.Log($"Executing StopContinuousDtmfRecognition. CallConnectionId: {callConnectionId}, Target: {pstnTarget}");
        
        callMedia.StopContinuousDtmfRecognition(target);
        
        string successMessage = $"Continuous DTMF recognition stopped successfully. CallConnectionId: {callConnectionId}";
        logger.LogInformation(successMessage);
        LogCollector.Log(successMessage);
        
        return Results.Ok(new { CallConnectionId = callConnectionId, Status = "Succeeded" });
    }
    catch (Exception ex)
    {
        string errorMessage = $"Error stopping continuous DTMF recognition. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
        logger.LogInformation(errorMessage);
        LogCollector.Log(errorMessage);
        
        return Results.Problem($"Failed to stop continuous DTMF recognition: {ex.Message}");
    }
}).WithTags("Send or Start DTMF APIs");
*/
#endregion
#region Hold/Unhold
/*
app.MapPost("/holdParticipantAsync", async (string callConnectionId, string pstnTarget, bool isPlaySource, ILogger<Program> logger) =>
{
    try
    {
        CommunicationIdentifier target = new PhoneNumberIdentifier(pstnTarget);

        CallMedia callMedia = GetCallMedia(callConnectionId);
        TextSource textSource = new TextSource("You are on hold please wait..")
        {
            VoiceName = "en-US-NancyNeural"
        };

        if (isPlaySource)
        {
            HoldOptions holdOptions = new HoldOptions(target)
            {
                PlaySource = textSource,
                OperationContext = "holdUserContext"
            };
            await callMedia.HoldAsync(holdOptions);
        }
        else
        {
            HoldOptions holdOptions = new HoldOptions(target)
            {
                OperationContext = "holdUserContext"
            };
            await callMedia.HoldAsync(holdOptions);
        }
        string successMessage = $"Participant put on hold successfully. CallConnectionId: {callConnectionId}";
        logger.LogInformation(successMessage);
        LogCollector.Log(successMessage);
        return Results.Ok(new { CallConnectionId = callConnectionId, Status = "Succeeded" });
    }
    catch (Exception ex)
    {
        string errorMessage = $"Error putting participant on hold. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
        logger.LogInformation(errorMessage);
        LogCollector.Log(errorMessage);
        return Results.Problem($"Failed to put participant on hold: {ex.Message}");
    }
}).WithTags("Hold Participant APIs");

app.MapPost("/holdParticipant", (string callConnectionId, string pstnTarget, bool isPlaySource, ILogger<Program> logger) =>
{
    try
    {
        CommunicationIdentifier target = new PhoneNumberIdentifier(pstnTarget);

        CallMedia callMedia = GetCallMedia(callConnectionId);
        TextSource textSource = new TextSource("You are on hold please wait..")
        {
            VoiceName = "en-US-NancyNeural"
        };

        if (isPlaySource)
        {
            HoldOptions holdOptions = new HoldOptions(target)
            {
                PlaySource = textSource,
                OperationContext = "holdUserContext"
            };
            callMedia.Hold(holdOptions);
        }
        else
        {
            HoldOptions holdOptions = new HoldOptions(target)
            {
                OperationContext = "holdUserContext"
            };
            callMedia.Hold(holdOptions);
        }
        string successMessage = $"Participant put on hold successfully. CallConnectionId: {callConnectionId}";
        logger.LogInformation(successMessage);
        LogCollector.Log(successMessage);
        return Results.Ok(new { CallConnectionId = callConnectionId, Status = "Succeeded" });
    }
    catch (Exception ex)
    {
        string errorMessage = $"Error putting participant on hold. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
        logger.LogInformation(errorMessage);
        LogCollector.Log(errorMessage);
        return Results.Problem($"Failed to put participant on hold: {ex.Message}");
    }
}).WithTags("Hold Participant APIs");

app.MapPost("/interrupAudioAndAnnounceAsync", async (string callConnectionId, string pstnTarget, ILogger<Program> logger) =>
{
    try
    {
        CommunicationIdentifier target = new PhoneNumberIdentifier(pstnTarget);

        CallMedia callMedia = GetCallMedia(callConnectionId);

        TextSource textSource = new TextSource("Hi, This is interrup audio and announcement test")
        {
            VoiceName = "en-US-NancyNeural"
        };

        InterruptAudioAndAnnounceOptions interruptAudio = new InterruptAudioAndAnnounceOptions(textSource, target)
        {
            OperationContext = "innterruptContext"
        };

        await callMedia.InterruptAudioAndAnnounceAsync(interruptAudio);
        string successMessage = $"Audio interrupted and announcement made successfully. CallConnectionId: {callConnectionId}";
        logger.LogInformation(successMessage);
        LogCollector.Log(successMessage);
        return Results.Ok(new { CallConnectionId = callConnectionId, Status = "Succeeded" });
    }
    catch (Exception ex)
    {
        string errorMessage = $"Error interrupting audio and announcing. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
        logger.LogInformation(errorMessage);
        LogCollector.Log(errorMessage);
        return Results.Problem($"Failed to interrupt audio and announce: {ex.Message}");
    }
}).WithTags("Hold Participant APIs");

app.MapPost("/interrupAudioAndAnnounce", (string callConnectionId, string pstnTarget, ILogger <Program> logger) =>
{
    try
    {
        CommunicationIdentifier target = new PhoneNumberIdentifier(pstnTarget);

        CallMedia callMedia = GetCallMedia(callConnectionId);

        TextSource textSource = new TextSource("Hi, This is interrupt audio and announcement test.")
        {
            VoiceName = "en-US-NancyNeural"
        };

        InterruptAudioAndAnnounceOptions interruptAudio = new InterruptAudioAndAnnounceOptions(textSource, target)
        {
            OperationContext = "innterruptContext"
        };

        callMedia.InterruptAudioAndAnnounce(interruptAudio);
        string successMessage = $"Audio interrupted and announcement made successfully. CallConnectionId: {callConnectionId}";
        logger.LogInformation(successMessage);
        LogCollector.Log(successMessage);
        return Results.Ok(new { CallConnectionId = callConnectionId, Status = "Succeeded" });
    }
    catch (Exception ex)
    {
        string errorMessage = $"Error interrupting audio and announcing. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
        logger.LogInformation(errorMessage);
        LogCollector.Log(errorMessage);
        return Results.Problem($"Failed to interrupt audio and announce: {ex.Message}");
    }
}).WithTags("Hold Participant APIs");


app.MapPost("/unholdParticipantAsync", async (string callConnectionId, string pstnTarget, ILogger<Program> logger) =>
{
    try
    {
        CommunicationIdentifier target = new PhoneNumberIdentifier(pstnTarget);

        CallMedia callMedia = GetCallMedia(callConnectionId);

        UnholdOptions unholdOptions = new UnholdOptions(target)
        {
            OperationContext = "unholdUserContext"
        };

        await callMedia.UnholdAsync(unholdOptions);
        string successMessage = $"Participant taken off hold successfully. CallConnectionId: {callConnectionId}";
        logger.LogInformation(successMessage);
        LogCollector.Log(successMessage);
        return Results.Ok(new { CallConnectionId = callConnectionId, Status = "Succeeded" });
    }
    catch (Exception ex)
    {
        string errorMessage = $"Error taking participant off hold. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
        logger.LogInformation(errorMessage);
        LogCollector.Log(errorMessage);
        return Results.Problem($"Failed to take participant off hold: {ex.Message}");
    }
}).WithTags("Hold Participant APIs");

app.MapPost("/unholdParticipant", (string pstnTarget, string callConnectionId, ILogger<Program> logger) =>
{
    try
    {
        CommunicationIdentifier target = new PhoneNumberIdentifier(pstnTarget);

        CallMedia callMedia = GetCallMedia(callConnectionId);

        UnholdOptions unholdOptions = new UnholdOptions(target)
        {
            OperationContext = "unholdUserContext"
        };

        callMedia.Unhold(unholdOptions);
        string successMessage = $"Participant taken off hold successfully. CallConnectionId: {callConnectionId}";
        logger.LogInformation(successMessage);
        LogCollector.Log(successMessage);
        return Results.Ok(new { CallConnectionId = callConnectionId, Status = "Succeeded" });
    }
    catch (Exception ex)
    {
        string errorMessage = $"Error taking participant off hold. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
        logger.LogInformation(errorMessage);
        LogCollector.Log(errorMessage);
        return Results.Problem($"Failed to take participant off hold: {ex.Message}");
    }
}).WithTags("Hold Participant APIs");

app.MapPost("/interruptHoldWithPlay", (string pstnTarget, string callConnectionId, ILogger<Program> logger) =>
{
    try
    {
        CallMedia callMedia = GetCallMedia(callConnectionId);

        TextSource textSource = new TextSource("Hi, This is interrupt audio and announcement test.")
        {
            VoiceName = "en-US-NancyNeural"
        };

        List<CommunicationIdentifier> playTo = new List<CommunicationIdentifier> { new PhoneNumberIdentifier(pstnTarget) };
        PlayOptions playToOptions = new PlayOptions(textSource, playTo)
        {
            OperationContext = "playToContext",
            InterruptHoldAudio = true
        };

        callMedia.Play(playToOptions);
        string successMessage = $"Hold audio interrupted successfully. CallConnectionId: {callConnectionId}";
        logger.LogInformation(successMessage);
        LogCollector.Log(successMessage);
        return Results.Ok(new { CallConnectionId = callConnectionId, Status = "Succeeded" });
    }
    catch (Exception ex)
    {
        string errorMessage = $"Error interrupting hold audio. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
        logger.LogInformation(errorMessage);
        LogCollector.Log(errorMessage);
        return Results.Problem($"Failed to interrupt hold audio: {ex.Message}");
    }
}).WithTags("Hold Participant APIs");
*/
#endregion
#region Transcription
/*
app.MapPost("/createCallToPstnWithTranscriptionAsync", async (string targetPhoneNumber, ILogger<Program> logger) =>
{
    PhoneNumberIdentifier target = new PhoneNumberIdentifier(targetPhoneNumber);
    PhoneNumberIdentifier caller = new PhoneNumberIdentifier(acsPhoneNumber);


    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    eventCallbackUri = callbackUri;
    CallInvite callInvite = new CallInvite(target, caller);
    var websocketUri = callbackUriHost.Replace("https", "wss") + "/ws";
    TranscriptionOptions transcriptionOptions = new TranscriptionOptions(new Uri(websocketUri), TranscriptionTransport.Websocket,
         "en-us", true);

    var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
    {
        CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
        TranscriptionOptions = transcriptionOptions
    };

    CreateCallResult createCallResult = await client.CreateCallAsync(createCallOptions);

    logger.LogInformation($"Created async pstn transcription call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
}).WithTags("Transcription APIs");

app.MapPost("/createCallToPstnWithTranscription", (string targetPhoneNumber, ILogger<Program> logger) =>
{
    PhoneNumberIdentifier target = new PhoneNumberIdentifier(targetPhoneNumber);
    PhoneNumberIdentifier caller = new PhoneNumberIdentifier(acsPhoneNumber);

    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    eventCallbackUri = callbackUri;
    CallInvite callInvite = new CallInvite(target, caller);

    var websocketUri = callbackUriHost.Replace("https", "wss") + "/ws";
    TranscriptionOptions transcriptionOptions = new TranscriptionOptions(new Uri(websocketUri), TranscriptionTransport.Websocket,
         "en-us", true);
    var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
    {
        CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
        TranscriptionOptions = transcriptionOptions
    };

    CreateCallResult createCallResult = client.CreateCall(createCallOptions);

    logger.LogInformation($"Created pstn transcription call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
}).WithTags("Transcription APIs");

app.MapPost("/createCallToAcsWithTranscriptionAsync", async (string acsTarget, ILogger<Program> logger) =>
{
    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    eventCallbackUri = callbackUri;
    CallInvite callInvite = new CallInvite(new CommunicationUserIdentifier(acsTarget));
    var websocketUri = callbackUriHost.Replace("https", "wss") + "/ws";
    TranscriptionOptions transcriptionOptions = new TranscriptionOptions(new Uri(websocketUri), TranscriptionTransport.Websocket,
         "en-us", true);

    var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
    {
        CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
        TranscriptionOptions = transcriptionOptions
    };

    CreateCallResult createCallResult = await client.CreateCallAsync(createCallOptions);

    logger.LogInformation($"Created async acs transcription call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
}).WithTags("Transcription APIs");

app.MapPost("/createCallToAcsWithTranscription", (string acsTarget, ILogger<Program> logger) =>
{
    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    eventCallbackUri = callbackUri;
    CallInvite callInvite = new CallInvite(new CommunicationUserIdentifier(acsTarget));
    var websocketUri = callbackUriHost.Replace("https", "wss") + "/ws";
    TranscriptionOptions transcriptionOptions = new TranscriptionOptions(new Uri(websocketUri), TranscriptionTransport.Websocket,
          "en-us", true);

    var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
    {
        CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
        TranscriptionOptions = transcriptionOptions
    };

    CreateCallResult createCallResult = client.CreateCall(createCallOptions);

    logger.LogInformation($"Created acs transcription call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
}).WithTags("Transcription APIs");

app.MapPost("/createCallToTeamsWithTranscriptionAsync", async (string teamsObjectId, ILogger<Program> logger) =>
{
    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    eventCallbackUri = callbackUri;
    CallInvite callInvite = new CallInvite(new MicrosoftTeamsUserIdentifier(teamsObjectId));
    var websocketUri = callbackUriHost.Replace("https", "wss") + "/ws";
    TranscriptionOptions transcriptionOptions = new TranscriptionOptions(new Uri(websocketUri), TranscriptionTransport.Websocket,
           "en-us", true);

    var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
    {
        CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
        TranscriptionOptions = transcriptionOptions
    };

    CreateCallResult createCallResult = await client.CreateCallAsync(createCallOptions);

    logger.LogInformation($"Created async teams transcription call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
}).WithTags("Transcription APIs");

app.MapPost("/createCallToTeamsWithTranscription", (string teamsObjectId, ILogger<Program> logger) =>
{
    var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
    eventCallbackUri = callbackUri;
    CallInvite callInvite = new CallInvite(new MicrosoftTeamsUserIdentifier(teamsObjectId));
    var websocketUri = callbackUriHost.Replace("https", "wss") + "/ws";
    TranscriptionOptions transcriptionOptions = new TranscriptionOptions(new Uri(websocketUri), TranscriptionTransport.Websocket,
           "en-us", true);

    var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
    {
        CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
        TranscriptionOptions = transcriptionOptions
    };

    CreateCallResult createCallResult = client.CreateCall(createCallOptions);

    logger.LogInformation($"Created teams transcription call with connection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
}).WithTags("Transcription APIs");

app.MapPost("/startTranscriptionAsync", async (ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    await callMedia.StartTranscriptionAsync();
    return Results.Ok();
}).WithTags("Transcription APIs");

app.MapPost("/startTranscription", (ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    callMedia.StartTranscription();
    return Results.Ok();
}).WithTags("Transcription APIs");

app.MapPost("/updateTranscriptionAsync", async (string locale, ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    await callMedia.UpdateTranscriptionAsync(locale);
    return Results.Ok();
}).WithTags("Transcription APIs");

app.MapPost("/updateTranscription", (string locale, ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    callMedia.UpdateTranscription(locale);
    return Results.Ok();
}).WithTags("Transcription APIs");

app.MapPost("/stopTranscriptionAsync", async (ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    await callMedia.StopTranscriptionAsync();
    return Results.Ok();
}).WithTags("Transcription APIs");

app.MapPost("/stopTranscription", (ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    callMedia.StopTranscription();
    return Results.Ok();
}).WithTags("Transcription APIs");

app.MapPost("/startTranscriptionWithOptionsAsync", async (ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    StartTranscriptionOptions startTranscriptionOptions = new StartTranscriptionOptions()
    {
        OperationContext = "StartTranscriptionContext",
        Locale = "en-us"
    };
    await callMedia.StartTranscriptionAsync(startTranscriptionOptions);
    return Results.Ok();
}).WithTags("Transcription APIs");

app.MapPost("/startTranscriptionWithOptions", (ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    StartTranscriptionOptions startTranscriptionOptions = new StartTranscriptionOptions()
    {
        OperationContext = "StartTranscriptionContext",
        Locale = "en-us"
    };
    callMedia.StartTranscription(startTranscriptionOptions);
    return Results.Ok();
}).WithTags("Transcription APIs");

app.MapPost("/stopTranscriptionWithOptionsAsync", async (ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    await callMedia.StopTranscriptionAsync();
    return Results.Ok();
}).WithTags("Transcription APIs");

app.MapPost("/stopTranscriptionWithOptions", (ILogger<Program> logger) =>
{
    CallMedia callMedia = GetCallMedia();
    callMedia.StopTranscription();
    return Results.Ok();
}).WithTags("Transcription APIs");

*/
#endregion
#region Play Media with text Source

//app.MapPost("/playTextSourceToPstnTargetAsync", async (string pstnTarget, ILogger<Program> logger) =>
//{
//    CallMedia callMedia = GetCallMedia();

//    TextSource textSource = new TextSource("Hi, this is test source played through play source thanks. Goodbye!.")
//    {
//        VoiceName = "en-US-NancyNeural"
//    };

//    List<CommunicationIdentifier> playTo = new List<CommunicationIdentifier> { new PhoneNumberIdentifier(pstnTarget) };
//    PlayOptions playToOptions = new PlayOptions(textSource, playTo)
//    {
//        OperationContext = "playToContext"
//    };

//    await callMedia.PlayAsync(playToOptions);

//    return Results.Ok();
//}).WithTags("Play TextSource Media APIs");

//app.MapPost("/playTextSourceToPstnTarget", (string pstnTarget, ILogger<Program> logger) =>
//{
//    CallMedia callMedia = GetCallMedia();

//    TextSource textSource = new TextSource("Hi, this is test source played through play source thanks. Goodbye!.")
//    {
//        VoiceName = "en-US-NancyNeural"
//    };

//    List<CommunicationIdentifier> playTo = new List<CommunicationIdentifier> { new PhoneNumberIdentifier(pstnTarget) };
//    PlayOptions playToOptions = new PlayOptions(textSource, playTo)
//    {
//        OperationContext = "playToContext"
//    };

//    callMedia.Play(playToOptions);

//    return Results.Ok();
//}).WithTags("Play TextSource Media APIs");

//app.MapPost("/playTextSourceAcsTargetAsync", async (string acsTarget, ILogger<Program> logger) =>
//{
//    CallMedia callMedia = GetCallMedia();

//    TextSource textSource = new TextSource("Hi, this is test source played through play source thanks. Goodbye!.")
//    {
//        VoiceName = "en-US-NancyNeural"
//    };

//    List<CommunicationIdentifier> playTo = new List<CommunicationIdentifier> { new CommunicationUserIdentifier(acsTarget) };
//    PlayOptions playToOptions = new PlayOptions(textSource, playTo)
//    {
//        OperationContext = "playToContext"
//    };

//    await callMedia.PlayAsync(playToOptions);

//    return Results.Ok();
//}).WithTags("Play TextSource Media APIs");

//app.MapPost("/playTextSourceToAcsTarget", (string acsTarget, ILogger<Program> logger) =>
//{
//    CallMedia callMedia = GetCallMedia();

//    TextSource textSource = new TextSource("Hi, this is test source played through play source thanks. Goodbye!.")
//    {
//        VoiceName = "en-US-NancyNeural"
//    };

//    List<CommunicationIdentifier> playTo = new List<CommunicationIdentifier> { new CommunicationUserIdentifier(acsTarget) };
//    PlayOptions playToOptions = new PlayOptions(textSource, playTo)
//    {
//        OperationContext = "playToContext"
//    };

//    callMedia.Play(playToOptions);

//    return Results.Ok();
//}).WithTags("Play TextSource Media APIs");

//app.MapPost("/playTextSourceToTeamsTargetAsync", async (string teamsObjectId, ILogger<Program> logger) =>
//{
//    CallMedia callMedia = GetCallMedia();

//    TextSource textSource = new TextSource("Hi, this is test source played through play source thanks. Goodbye!.")
//    {
//        VoiceName = "en-US-NancyNeural"
//    };

//    List<CommunicationIdentifier> playTo = new List<CommunicationIdentifier> { new MicrosoftTeamsUserIdentifier(teamsObjectId) };
//    PlayOptions playToOptions = new PlayOptions(textSource, playTo)
//    {
//        OperationContext = "playToContext"
//    };

//    await callMedia.PlayAsync(playToOptions);

//    return Results.Ok();
//}).WithTags("Play TextSource Media APIs");

//app.MapPost("/playTextSourceToTeamsTarget", (string teamsObjectId, ILogger<Program> logger) =>
//{
//    CallMedia callMedia = GetCallMedia();

//    TextSource textSource = new TextSource("Hi, this is test source played through play source thanks. Goodbye!.")
//    {
//        VoiceName = "en-US-NancyNeural"
//    };

//    List<CommunicationIdentifier> playTo = new List<CommunicationIdentifier> { new MicrosoftTeamsUserIdentifier(teamsObjectId) };
//    PlayOptions playToOptions = new PlayOptions(textSource, playTo)
//    {
//        OperationContext = "playToContext"
//    };

//    callMedia.Play(playToOptions);

//    return Results.Ok();
//}).WithTags("Play TextSource Media APIs");

//app.MapPost("/playTextSourceToAllAsync", async (ILogger<Program> logger) =>
//{
//    CallMedia callMedia = GetCallMedia();

//    TextSource textSource = new TextSource("Hi, this is test source played through play source thanks. Goodbye!.")
//    {
//        VoiceName = "en-US-NancyNeural"
//    };

//    PlayToAllOptions playToAllOptions = new PlayToAllOptions(textSource)
//    {
//        OperationContext = "playToAllContext"
//    };
//    await callMedia.PlayToAllAsync(playToAllOptions);

//    return Results.Ok();
//}).WithTags("Play TextSource Media APIs");

//app.MapPost("/playTextSourceToAll", (ILogger<Program> logger) =>
//{
//    CallMedia callMedia = GetCallMedia();

//    TextSource textSource = new TextSource("Hi, this is test source played through play source thanks. Goodbye!.")
//    {
//        VoiceName = "en-US-NancyNeural"
//    };

//    PlayToAllOptions playToAllOptions = new PlayToAllOptions(textSource)
//    {
//        OperationContext = "playToAllContext"
//    };
//    callMedia.PlayToAll(playToAllOptions);

//    return Results.Ok();
//}).WithTags("Play TextSource Media APIs");

//app.MapPost("/playTextSourceBargeInAsync", async (ILogger<Program> logger) =>
//{
//    CallMedia callMedia = GetCallMedia();

//    TextSource textSource = new TextSource("Hi, this is barge in test played through play source thanks. Goodbye!.")
//    {
//        VoiceName = "en-US-NancyNeural"
//    };

//    PlayToAllOptions playToAllOptions = new PlayToAllOptions(textSource)
//    {
//        OperationContext = "playToAllContext",
//        InterruptCallMediaOperation = true
//    };
//    await callMedia.PlayToAllAsync(playToAllOptions);

//    return Results.Ok();
//}).WithTags("Play TextSource Media APIs");

#endregion
#region Play Media with Ssml Source

//app.MapPost("/playSsmlSourceToPstnTargetAsync", async (string pstnTarget, ILogger<Program> logger) =>
//{
//    CallMedia callMedia = GetCallMedia();

//    SsmlSource ssmlSource = new SsmlSource("<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"><voice name=\"en-US-JennyNeural\">Hi, this is ssml test played through ssml source thanks. Goodbye!</voice></speak>");

//    List<CommunicationIdentifier> playTo = new List<CommunicationIdentifier> { new PhoneNumberIdentifier(pstnTarget) };
//    PlayOptions playToOptions = new PlayOptions(ssmlSource, playTo)
//    {
//        OperationContext = "playToContext"
//    };

//    await callMedia.PlayAsync(playToOptions);

//    return Results.Ok();
//}).WithTags("Play SsmlSource Media APIs");

//app.MapPost("/playSsmlSourceToPstnTarget", (string pstnTarget, ILogger<Program> logger) =>
//{
//    CallMedia callMedia = GetCallMedia();
//    SsmlSource ssmlSource = new SsmlSource("<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"><voice name=\"en-US-JennyNeural\">Hi, this is ssml test played through ssml source thanks. Goodbye!</voice></speak>");
//    List<CommunicationIdentifier> playTo = new List<CommunicationIdentifier> { new PhoneNumberIdentifier(pstnTarget) };
//    PlayOptions playToOptions = new PlayOptions(ssmlSource, playTo)
//    {
//        OperationContext = "playToContext"
//    };

//    callMedia.Play(playToOptions);

//    return Results.Ok();
//}).WithTags("Play SsmlSource Media APIs");

//app.MapPost("/playSsmlSourceAcsTargetAsync", async (string acsTarget, ILogger<Program> logger) =>
//{
//    CallMedia callMedia = GetCallMedia();
//    SsmlSource ssmlSource = new SsmlSource("<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"><voice name=\"en-US-JennyNeural\">Hi, this is ssml test played through ssml source thanks. Goodbye!</voice></speak>");
//    List<CommunicationIdentifier> playTo = new List<CommunicationIdentifier> { new CommunicationUserIdentifier(acsTarget) };
//    PlayOptions playToOptions = new PlayOptions(ssmlSource, playTo)
//    {
//        OperationContext = "playToContext"
//    };

//    await callMedia.PlayAsync(playToOptions);

//    return Results.Ok();
//}).WithTags("Play SsmlSource Media APIs");

//app.MapPost("/playSsmlSourceToAcsTarget", (string acsTarget, ILogger<Program> logger) =>
//{
//    CallMedia callMedia = GetCallMedia();
//    SsmlSource ssmlSource = new SsmlSource("<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"><voice name=\"en-US-JennyNeural\">Hi, this is ssml test played through ssml source thanks. Goodbye!</voice></speak>");

//    List<CommunicationIdentifier> playTo = new List<CommunicationIdentifier> { new CommunicationUserIdentifier(acsTarget) };
//    PlayOptions playToOptions = new PlayOptions(ssmlSource, playTo)
//    {
//        OperationContext = "playToContext"
//    };

//    callMedia.Play(playToOptions);

//    return Results.Ok();
//}).WithTags("Play SsmlSource Media APIs");

//app.MapPost("/playSsmlSourceToTeamsTargetAsync", async (string teamsObjectId, ILogger<Program> logger) =>
//{
//    CallMedia callMedia = GetCallMedia();
//    SsmlSource ssmlSource = new SsmlSource("<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"><voice name=\"en-US-JennyNeural\">Hi, this is ssml test played through ssml source thanks. Goodbye!</voice></speak>");
//    List<CommunicationIdentifier> playTo = new List<CommunicationIdentifier> { new MicrosoftTeamsUserIdentifier(teamsObjectId) };
//    PlayOptions playToOptions = new PlayOptions(ssmlSource, playTo)
//    {
//        OperationContext = "playToContext"
//    };

//    await callMedia.PlayAsync(playToOptions);

//    return Results.Ok();
//}).WithTags("Play SsmlSource Media APIs");

//app.MapPost("/playSsmlSourceToTeamsTarget", (string teamsObjectId, ILogger<Program> logger) =>
//{
//    CallMedia callMedia = GetCallMedia();
//    SsmlSource ssmlSource = new SsmlSource("<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"><voice name=\"en-US-JennyNeural\">Hi, this is ssml test played through ssml source thanks. Goodbye!</voice></speak>");
//    List<CommunicationIdentifier> playTo = new List<CommunicationIdentifier> { new MicrosoftTeamsUserIdentifier(teamsObjectId) };
//    PlayOptions playToOptions = new PlayOptions(ssmlSource, playTo)
//    {
//        OperationContext = "playToContext"
//    };

//    callMedia.Play(playToOptions);

//    return Results.Ok();
//}).WithTags("Play SsmlSource Media APIs");

//app.MapPost("/playSsmlSourceToAllAsync", async (ILogger<Program> logger) =>
//{
//    CallMedia callMedia = GetCallMedia();
//    SsmlSource ssmlSource = new SsmlSource("<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"><voice name=\"en-US-JennyNeural\">Hi, this is ssml test played through ssml source thanks. Goodbye!</voice></speak>");
//    PlayToAllOptions playToAllOptions = new PlayToAllOptions(ssmlSource)
//    {
//        OperationContext = "playToAllContext"
//    };
//    await callMedia.PlayToAllAsync(playToAllOptions);

//    return Results.Ok();
//}).WithTags("Play SsmlSource Media APIs");

//app.MapPost("/playSsmlSourceToAll", (ILogger<Program> logger) =>
//{
//    CallMedia callMedia = GetCallMedia();
//    SsmlSource ssmlSource = new SsmlSource("<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"><voice name=\"en-US-JennyNeural\">Hi, this is ssml test played through ssml source thanks. Goodbye!</voice></speak>");

//    PlayToAllOptions playToAllOptions = new PlayToAllOptions(ssmlSource)
//    {
//        OperationContext = "playToAllContext"
//    };
//    callMedia.PlayToAll(playToAllOptions);

//    return Results.Ok();
//}).WithTags("Play SsmlSource Media APIs");

//app.MapPost("/playSsmlSourceBargeInAsync", async (ILogger<Program> logger) =>
//{
//    CallMedia callMedia = GetCallMedia();
//    SsmlSource ssmlSource = new SsmlSource("<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"><voice name=\"en-US-JennyNeural\">Hi, this is ssml barge in test played through ssml source thanks. Goodbye!</voice></speak>");
//    PlayToAllOptions playToAllOptions = new PlayToAllOptions(ssmlSource)
//    {
//        OperationContext = "playBargeInContext",
//        InterruptCallMediaOperation = true
//    };
//    await callMedia.PlayToAllAsync(playToAllOptions);

//    return Results.Ok();
//}).WithTags("Play SsmlSource Media APIs");

#endregion
#region Media streaming
/*
app.MapPost("/createCallToPstnWithMediaStreamingAsync", async (string targetPhoneNumber, bool isEnableBidirectional, bool isPcm24kMono, ILogger<Program> logger) =>
{
    try
    {
        PhoneNumberIdentifier target = new PhoneNumberIdentifier(targetPhoneNumber);
        PhoneNumberIdentifier caller = new PhoneNumberIdentifier(acsPhoneNumber);


        var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
        eventCallbackUri = callbackUri;
        CallInvite callInvite = new CallInvite(target, caller);
        var websocketUri = callbackUriHost.Replace("https", "wss") + "/ws";
        MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(new Uri(websocketUri), MediaStreamingContent.Audio,
            MediaStreamingAudioChannel.Unmixed, MediaStreamingTransport.Websocket, true);
        mediaStreamingOptions.EnableBidirectional = isEnableBidirectional;
        mediaStreamingOptions.AudioFormat = isPcm24kMono ? AudioFormat.Pcm24KMono : AudioFormat.Pcm16KMono;

        var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
        {
            // ACS GCCH Phase 2
            // CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
            MediaStreamingOptions = mediaStreamingOptions
        };

        CreateCallResult createCallResult = await client.CreateCallAsync(createCallOptions);
        string callConnectionId = createCallResult.CallConnectionProperties.CallConnectionId;
        
        string successMessage = $"Created async pstn media streaming call with connection id: {callConnectionId}";
        logger.LogInformation(successMessage);
        LogCollector.Log(successMessage);
        return Results.Ok(new { CallConnectionId = callConnectionId, Status = "Succeeded" });
    }
    catch (Exception ex)
    {
        string errorMessage = $"Error creating PSTN call with media streaming: {ex.Message}";
        logger.LogInformation(errorMessage);
        LogCollector.Log(errorMessage);
        return Results.Problem($"Failed to create PSTN call with media streaming: {ex.Message}");
    }
}).WithTags("Media streaming APIs");

app.MapPost("/createCallToPstnWithMediaStreaming", (string targetPhoneNumber, bool isEnableBidirectional, bool isPcm24kMono, ILogger<Program> logger) =>
{
    try
    {
        PhoneNumberIdentifier target = new PhoneNumberIdentifier(targetPhoneNumber);
        PhoneNumberIdentifier caller = new PhoneNumberIdentifier(acsPhoneNumber);


        var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
        eventCallbackUri = callbackUri;
        CallInvite callInvite = new CallInvite(target, caller);
        var websocketUri = callbackUriHost.Replace("https", "wss") + "/ws";
        MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(new Uri(websocketUri), MediaStreamingContent.Audio,
            MediaStreamingAudioChannel.Unmixed, MediaStreamingTransport.Websocket, true);
        mediaStreamingOptions.EnableBidirectional = isEnableBidirectional;
        mediaStreamingOptions.AudioFormat = isPcm24kMono ? AudioFormat.Pcm24KMono : AudioFormat.Pcm16KMono;

        var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
        {
            // ACS GCCH Phase 2
            // CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
            MediaStreamingOptions = mediaStreamingOptions
        };

        CreateCallResult createCallResult = client.CreateCall(createCallOptions);
        string callConnectionId = createCallResult.CallConnectionProperties.CallConnectionId;
        
        string successMessage = $"Created pstn media streaming call with connection id: {callConnectionId}";
        logger.LogInformation(successMessage);
        LogCollector.Log(successMessage);
        return Results.Ok(new { CallConnectionId = callConnectionId, Status = "Succeeded" });
    }
    catch (Exception ex)
    {
        string errorMessage = $"Error creating PSTN call with media streaming: {ex.Message}";
        logger.LogInformation(errorMessage);
        LogCollector.Log(errorMessage);
        return Results.Problem($"Failed to create PSTN call with media streaming: {ex.Message}");
    }
}).WithTags("Media streaming APIs");
*/
/*
app.MapPost("/createCallToTeamsWithMediaStreamingAsync", async (string teamsObjectId, bool isEnableBidirectional, bool isPcm24kMono, ILogger<Program> logger) =>
{
    try
    {
        var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
        eventCallbackUri = callbackUri;
        CallInvite callInvite = new CallInvite(new MicrosoftTeamsUserIdentifier(teamsObjectId));
        var websocketUri = callbackUriHost.Replace("https", "wss") + "/ws";
        MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(new Uri(websocketUri), MediaStreamingContent.Audio,
            MediaStreamingAudioChannel.Unmixed, MediaStreamingTransport.Websocket, true);
        mediaStreamingOptions.EnableBidirectional = isEnableBidirectional;
        mediaStreamingOptions.AudioFormat = isPcm24kMono ? AudioFormat.Pcm24KMono : AudioFormat.Pcm16KMono;

        var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
        {
            // ACS GCCH Phase 2
            // CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
            MediaStreamingOptions = mediaStreamingOptions
        };

        CreateCallResult createCallResult = await client.CreateCallAsync(createCallOptions);
        string callConnectionId = createCallResult.CallConnectionProperties.CallConnectionId;
        
        string successMessage = $"Created async teams call with connection id: {callConnectionId}";
        logger.LogInformation(successMessage);
        LogCollector.Log(successMessage);
        return Results.Ok(new { CallConnectionId = callConnectionId, Status = "Succeeded" });
    }
    catch (Exception ex)
    {
        string errorMessage = $"Error creating Teams call with media streaming: {ex.Message}";
        logger.LogInformation(errorMessage);
        LogCollector.Log(errorMessage);
        return Results.Problem($"Failed to create Teams call with media streaming: {ex.Message}");
    }
}).WithTags("Media streaming APIs");

app.MapPost("/createCallToTeamsWithMediaStreaming", (string teamsObjectId, bool isEnableBidirectional, bool isPcm24kMono, ILogger<Program> logger) =>
{
    try
    {
        var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks");
        eventCallbackUri = callbackUri;
        CallInvite callInvite = new CallInvite(new MicrosoftTeamsUserIdentifier(teamsObjectId));
        var websocketUri = callbackUriHost.Replace("https", "wss") + "/ws";
        MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(new Uri(websocketUri), MediaStreamingContent.Audio,
            MediaStreamingAudioChannel.Unmixed, MediaStreamingTransport.Websocket, true);
        mediaStreamingOptions.EnableBidirectional = isEnableBidirectional;
        mediaStreamingOptions.AudioFormat = isPcm24kMono ? AudioFormat.Pcm24KMono : AudioFormat.Pcm16KMono;

        var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
        {
            // ACS GCCH Phase 2
            // CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
            MediaStreamingOptions = mediaStreamingOptions
        };

        CreateCallResult createCallResult = client.CreateCall(createCallOptions);
        string callConnectionId = createCallResult.CallConnectionProperties.CallConnectionId;
        
        string successMessage = $"Created teams call with connection id: {callConnectionId}";
        logger.LogInformation(successMessage);
        LogCollector.Log(successMessage);
        return Results.Ok(new { CallConnectionId = callConnectionId, Status = "Succeeded" });
    }
    catch (Exception ex)
    {
        string errorMessage = $"Error creating Teams call with media streaming: {ex.Message}";
        logger.LogInformation(errorMessage);
        LogCollector.Log(errorMessage);
        return Results.Problem($"Failed to create Teams call with media streaming: {ex.Message}");
    }
}).WithTags("Media streaming APIs");
*/
#endregion

