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
        private readonly ICallAutomationService _service;
        private readonly ILogger<MediaController> _logger;
        private readonly AcsCommunicationSettings _config;

        public MediaController(ICallAutomationService service, ILogger<MediaController> logger, IOptions<AcsCommunicationSettings> configOptions)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = configOptions.Value ?? throw new ArgumentNullException(nameof(configOptions));
        }

        /// <summary>Plays audio to a specific target. Include "playOptions" for loop/interrupt. Omit for simple play.</summary>
        [HttpPost("/playAsync")]
        [Tags("Play Media")]
        public Task<IActionResult> PlayAsync([FromBody] PlayRequest request) => HandlePlay(request, async: true);
        [HttpPost("/play")]
        [Tags("Play Media")]
        public IActionResult Play([FromBody] PlayRequest request) => HandlePlay(request, async: false).Result;

        /// <summary>Plays audio to all participants. Include "playToAllOptions" for loop/interrupt. Omit for simple play.</summary>
        [HttpPost("/playToAllAsync")]
        [Tags("Play Media")]
        public Task<IActionResult> PlayToAllAsync([FromBody] PlayToAllRequest request) => HandlePlayToAll(request, async: true);
        [HttpPost("/playToAll")]
        [Tags("Play Media")]
        public IActionResult PlayToAll([FromBody] PlayToAllRequest request) => HandlePlayToAll(request, async: false).Result;

        /// <summary>Holds a participant. Include "holdOptions" for hold music and context. Omit for simple hold.</summary>
        [HttpPost("/holdAsync")]
        [Tags("Hold Management")]
        public Task<IActionResult> HoldAsync([FromBody] HoldRequest request) => HandleHold(request, async: true);
        [HttpPost("/hold")]
        [Tags("Hold Management")]
        public IActionResult Hold([FromBody] HoldRequest request) => HandleHold(request, async: false).Result;

        /// <summary>Unholds a participant. Include "unholdOptions" for custom context. Omit for simple unhold.</summary>
        [HttpPost("/unholdAsync")]
        [Tags("Hold Management")]
        public Task<IActionResult> UnholdAsync([FromBody] UnholdRequest request) => HandleUnhold(request, async: true);
        [HttpPost("/unhold")]
        [Tags("Hold Management")]
        public IActionResult Unhold([FromBody] UnholdRequest request) => HandleUnhold(request, async: false).Result;

        /// <summary>Starts media streaming on a call.</summary>
        [HttpPost("/startMediaStreamingAsync")]
        [Tags("Media Streaming")]
        public Task<IActionResult> StartMediaStreamingAsync([FromBody] MediaStreamingActionRequest request) => HandleMediaStreaming(request, start: true, async: true);
        [HttpPost("/startMediaStreaming")]
        [Tags("Media Streaming")]
        public IActionResult StartMediaStreaming([FromBody] MediaStreamingActionRequest request) => HandleMediaStreaming(request, start: true, async: false).Result;

        /// <summary>Stops media streaming on a call.</summary>
        [HttpPost("/stopMediaStreamingAsync")]
        [Tags("Media Streaming")]
        public Task<IActionResult> StopMediaStreamingAsync([FromBody] MediaStreamingActionRequest request) => HandleMediaStreaming(request, start: false, async: true);
        [HttpPost("/stopMediaStreaming")]
        [Tags("Media Streaming")]
        public IActionResult StopMediaStreaming([FromBody] MediaStreamingActionRequest request) => HandleMediaStreaming(request, start: false, async: false).Result;

        /// <summary>Cancels all media operations on a call.</summary>
        [HttpPost("/cancelAllMediaOperationsAsync")]
        [Tags("Media Operations")]
        public Task<IActionResult> CancelAllMediaOperationsAsync([FromBody] MediaStreamingActionRequest request) => HandleCancelAllMediaOperations(request.CallConnectionId, async: true);
        [HttpPost("/cancelAllMediaOperations")]
        [Tags("Media Operations")]
        public IActionResult CancelAllMediaOperations([FromBody] MediaStreamingActionRequest request) => HandleCancelAllMediaOperations(request.CallConnectionId, async: false).Result;

        /// <summary>Starts recognition. Set "recognizeType" then include the matching options object.</summary>
        [HttpPost("/startRecognizeAsync")]
        [Tags("Recognition")]
        public Task<IActionResult> StartRecognizeAsync([FromBody] StartRecognizeRequest request) => HandleRecognize(request, async: true);
        [HttpPost("/startRecognize")]
        [Tags("Recognition")]
        public IActionResult StartRecognize([FromBody] StartRecognizeRequest request) => HandleRecognize(request, async: false).Result;

        /// <summary>Interrupts current audio and plays an announcement.</summary>
        [HttpPost("/interruptAudioAndAnnounceAsync")]
        [Tags("Audio Announcements")]
        public Task<IActionResult> InterruptAudioAndAnnounceAsync([FromBody] InterruptAudioAndAnnounceRequest request) => HandleInterruptAudioAndAnnounce(request, async: true);
        [HttpPost("/interruptAudioAndAnnounce")]
        [Tags("Audio Announcements")]
        public IActionResult InterruptAudioAndAnnounce([FromBody] InterruptAudioAndAnnounceRequest request) => HandleInterruptAudioAndAnnounce(request, async: false).Result;

        /// <summary>Starts transcription on a call.</summary>
        [HttpPost("/startTranscriptionAsync")]
        [Tags("Transcription")]
        public Task<IActionResult> StartTranscriptionAsync([FromBody] StartTranscriptionRequest request) => HandleTranscription(request.CallConnectionId, TranscriptionAction.Start, request.Locale, request.OperationContext, async: true);
        [HttpPost("/startTranscription")]
        [Tags("Transcription")]
        public IActionResult StartTranscription([FromBody] StartTranscriptionRequest request) => HandleTranscription(request.CallConnectionId, TranscriptionAction.Start, request.Locale, request.OperationContext, async: false).Result;

        /// <summary>Stops transcription on a call.</summary>
        [HttpPost("/stopTranscriptionAsync")]
        [Tags("Transcription")]
        public Task<IActionResult> StopTranscriptionAsync([FromBody] StopTranscriptionRequest request) => HandleTranscription(request.CallConnectionId, TranscriptionAction.Stop, null, request.OperationContext, async: true);
        [HttpPost("/stopTranscription")]
        [Tags("Transcription")]
        public IActionResult StopTranscription([FromBody] StopTranscriptionRequest request) => HandleTranscription(request.CallConnectionId, TranscriptionAction.Stop, null, request.OperationContext, async: false).Result;

        /// <summary>Updates transcription locale on a call.</summary>
        [HttpPost("/updateTranscriptionAsync")]
        [Tags("Transcription")]
        public Task<IActionResult> UpdateTranscriptionAsync([FromBody] UpdateTranscriptionRequest request) => HandleUpdateTranscription(request, async: true);
        [HttpPost("/updateTranscription")]
        [Tags("Transcription")]
        public IActionResult UpdateTranscription([FromBody] UpdateTranscriptionRequest request) => HandleUpdateTranscription(request, async: false).Result;

        /// <summary>Sends DTMF tones to a participant.</summary>
        [HttpPost("/sendDtmfTonesAsync")]
        [Tags("Send DTMF")]
        public Task<IActionResult> SendDtmfTonesAsync([FromBody] SendDtmfTonesRequest request) => HandleSendDtmfTones(request, async: true);
        [HttpPost("/sendDtmfTones")]
        [Tags("Send DTMF")]
        public IActionResult SendDtmfTones([FromBody] SendDtmfTonesRequest request) => HandleSendDtmfTones(request, async: false).Result;

        /// <summary>Starts continuous DTMF recognition.</summary>
        [HttpPost("/startContinuousDtmfAsync")]
        [Tags("Continuous DTMF")]
        public Task<IActionResult> StartContinuousDtmfAsync([FromBody] ContinuousDtmfRequest request) => HandleContinuousDtmf(request, start: true, async: true);
        [HttpPost("/startContinuousDtmf")]
        [Tags("Continuous DTMF")]
        public IActionResult StartContinuousDtmf([FromBody] ContinuousDtmfRequest request) => HandleContinuousDtmf(request, start: true, async: false).Result;

        /// <summary>Stops continuous DTMF recognition.</summary>
        [HttpPost("/stopContinuousDtmfAsync")]
        [Tags("Continuous DTMF")]
        public Task<IActionResult> StopContinuousDtmfAsync([FromBody] ContinuousDtmfRequest request) => HandleContinuousDtmf(request, start: false, async: true);
        [HttpPost("/stopContinuousDtmf")]
        [Tags("Continuous DTMF")]
        public IActionResult StopContinuousDtmf([FromBody] ContinuousDtmfRequest request) => HandleContinuousDtmf(request, start: false, async: false).Result;

        // ??? PRIVATE HANDLERS ???????????????????????????????????????????????????

        private FileSource BuildFileSource() => new FileSource(new Uri(_config.CallbackUriHost + "/audio/prompt.wav"));

        private async Task<IActionResult> HandlePlay(PlayRequest request, bool async)
        {
            var identifier = ResolveIdentifier(request.Target);
            if (identifier == null) return BadRequest("target must be an ACS user ID (8:...) or phone number (+...)");

            _logger.LogInformation("Play. CallId={CallId}, Target={Target}", request.CallConnectionId, request.Target);
            try
            {
                var callMedia = _service.GetCallMedia(request.CallConnectionId);
                var props = _service.GetCallConnectionProperties(request.CallConnectionId);
                var fileSource = BuildFileSource();
                var targets = new List<CommunicationIdentifier> { identifier };

                Response<PlayResult> result;
                if (request.PlayOptions != null)
                {
                    var po = request.PlayOptions;
                    var opts = new PlayOptions(fileSource, targets)
                    { OperationContext = po.OperationContext, InterruptHoldAudio = po.InterruptHoldAudio, Loop = po.Loop };
                    result = async ? await callMedia.PlayAsync(opts) : callMedia.Play(opts);
                }
                else
                {
                    result = async ? await callMedia.PlayAsync(fileSource, targets) : callMedia.Play(fileSource, targets);
                }

                return Ok(new CallConnectionResponse { CallConnectionId = request.CallConnectionId, CorrelationId = props.CorrelationId, Status = result.GetRawResponse().Status.ToString() });
            }
            catch (Exception ex) { _logger.LogError(ex, "Error in Play"); return Problem($"Failed to play: {ex.Message}"); }
        }

        private async Task<IActionResult> HandlePlayToAll(PlayToAllRequest request, bool async)
        {
            _logger.LogInformation("PlayToAll. CallId={CallId}", request.CallConnectionId);
            try
            {
                var callMedia = _service.GetCallMedia(request.CallConnectionId);
                var props = _service.GetCallConnectionProperties(request.CallConnectionId);
                var fileSource = BuildFileSource();

                Response<PlayResult> result;
                if (request.PlayToAllOptions != null)
                {
                    var po = request.PlayToAllOptions;
                    var opts = new PlayToAllOptions(fileSource)
                    { OperationContext = po.OperationContext, InterruptCallMediaOperation = po.InterruptCallMediaOperation, Loop = po.Loop };
                    result = async ? await callMedia.PlayToAllAsync(opts) : callMedia.PlayToAll(opts);
                }
                else
                {
                    result = async ? await callMedia.PlayToAllAsync(fileSource) : callMedia.PlayToAll(fileSource);
                }

                return Ok(new CallConnectionResponse { CallConnectionId = request.CallConnectionId, CorrelationId = props.CorrelationId, Status = result.GetRawResponse().Status.ToString() });
            }
            catch (Exception ex) { _logger.LogError(ex, "Error in PlayToAll"); return Problem($"Failed to play to all: {ex.Message}"); }
        }

        private async Task<IActionResult> HandleHold(HoldRequest request, bool async)
        {
            var identifier = ResolveIdentifier(request.Target);
            if (identifier == null) return BadRequest("target must be an ACS user ID (8:...) or phone number (+...)");

            _logger.LogInformation("Hold. CallId={CallId}, Target={Target}", request.CallConnectionId, request.Target);
            try
            {
                var callMedia = _service.GetCallMedia(request.CallConnectionId);
                var props = _service.GetCallConnectionProperties(request.CallConnectionId);

                if (request.HoldOptions != null)
                {
                    var ho = request.HoldOptions;
                    var opts = new HoldOptions(identifier) { OperationContext = ho.OperationContext };
                    if (ho.PlayHoldMusic) opts.PlaySource = BuildFileSource();
                    if (async) await callMedia.HoldAsync(opts); else callMedia.Hold(opts);
                }
                else
                {
                    if (async) await callMedia.HoldAsync(identifier); else callMedia.Hold(identifier);
                }

                return Ok(new CallConnectionResponse { CallConnectionId = request.CallConnectionId, CorrelationId = props.CorrelationId, Status = props.CallConnectionState.ToString() });
            }
            catch (Exception ex) { _logger.LogError(ex, "Error in Hold"); return Problem($"Failed to hold: {ex.Message}"); }
        }

        private async Task<IActionResult> HandleUnhold(UnholdRequest request, bool async)
        {
            var identifier = ResolveIdentifier(request.Target);
            if (identifier == null) return BadRequest("target must be an ACS user ID (8:...) or phone number (+...)");

            _logger.LogInformation("Unhold. CallId={CallId}, Target={Target}", request.CallConnectionId, request.Target);
            try
            {
                var callMedia = _service.GetCallMedia(request.CallConnectionId);
                var props = _service.GetCallConnectionProperties(request.CallConnectionId);

                if (request.UnholdOptions != null)
                {
                    var opts = new UnholdOptions(identifier) { OperationContext = request.UnholdOptions.OperationContext };
                    if (async) await callMedia.UnholdAsync(opts); else callMedia.Unhold(opts);
                }
                else
                {
                    if (async) await callMedia.UnholdAsync(identifier); else callMedia.Unhold(identifier);
                }

                return Ok(new CallConnectionResponse { CallConnectionId = request.CallConnectionId, CorrelationId = props.CorrelationId, Status = props.CallConnectionState.ToString() });
            }
            catch (Exception ex) { _logger.LogError(ex, "Error in Unhold"); return Problem($"Failed to unhold: {ex.Message}"); }
        }

        private async Task<IActionResult> HandleMediaStreaming(MediaStreamingActionRequest request, bool start, bool async)
        {
            _logger.LogInformation("{Action} media streaming. CallId={CallId}", start ? "Starting" : "Stopping", request.CallConnectionId);
            try
            {
                var callMedia = _service.GetCallMedia(request.CallConnectionId);
                var props = _service.GetCallConnectionProperties(request.CallConnectionId);
                var callbackUri = new Uri(new Uri(_config.CallbackUriHost), "/api/callbacks");
                Response response;

                if (start)
                {
                    var opts = new StartMediaStreamingOptions { OperationContext = request.OperationContext, OperationCallbackUri = callbackUri };
                    response = async ? await callMedia.StartMediaStreamingAsync(opts) : callMedia.StartMediaStreaming(opts);
                }
                else
                {
                    var opts = new StopMediaStreamingOptions { OperationContext = request.OperationContext, OperationCallbackUri = callbackUri };
                    response = async ? await callMedia.StopMediaStreamingAsync(opts) : callMedia.StopMediaStreaming(opts);
                }

                return Ok(new CallConnectionResponse { CallConnectionId = request.CallConnectionId, CorrelationId = props.CorrelationId, Status = response.Status.ToString() });
            }
            catch (Exception ex) { _logger.LogError(ex, "Error during media streaming operation"); return Problem($"Failed media streaming operation: {ex.Message}"); }
        }

        private async Task<IActionResult> HandleCancelAllMediaOperations(string callConnectionId, bool async)
        {
            _logger.LogInformation("CancelAllMediaOperations. CallId={CallId}", callConnectionId);
            try
            {
                var props = _service.GetCallConnectionProperties(callConnectionId);
                var callMedia = _service.GetCallMedia(callConnectionId);
                var result = async ? await callMedia.CancelAllMediaOperationsAsync() : callMedia.CancelAllMediaOperations();
                return Ok(new CallConnectionResponse { CallConnectionId = callConnectionId, CorrelationId = props.CorrelationId, Status = result.GetRawResponse().Status.ToString() });
            }
            catch (Exception ex) { _logger.LogError(ex, "Error cancelling all media operations"); return Problem($"Failed to cancel all media operations: {ex.Message}"); }
        }

        private async Task<IActionResult> HandleRecognize(StartRecognizeRequest request, bool async)
        {
            if (string.IsNullOrEmpty(request.CallConnectionId))
                return BadRequest("callConnectionId is required");

            var identifier = ResolveIdentifier(request.Target);
            if (identifier == null)
                return BadRequest("target must be an ACS user ID (8:...) or phone number (+...)");

            if (string.IsNullOrEmpty(request.RecognizeType))
                return BadRequest("recognizeType is required. Valid: Dtmf, Choice, Speech, SpeechOrDtmf");

            if (!Enum.TryParse<RecognizeType>(request.RecognizeType, ignoreCase: true, out var type))
                return BadRequest($"Invalid recognizeType '{request.RecognizeType}'. Valid: Dtmf, Choice, Speech, SpeechOrDtmf");

            _logger.LogInformation("Recognize. CallId={CallId}, Type={Type}", request.CallConnectionId, type);
            try
            {
                var callMedia = _service.GetCallMedia(request.CallConnectionId);
                var props = _service.GetCallConnectionProperties(request.CallConnectionId);
                var fileSource = BuildFileSource();

                switch (type)
                {
                    case RecognizeType.Dtmf:
                    {
                        var d = request.DtmfOptions ?? new DtmfOptionsRequest();
                        var dtmfOpts = new CallMediaRecognizeDtmfOptions(identifier, maxTonesToCollect: d.MaxTonesToCollect)
                        {
                            Prompt = fileSource, InterruptPrompt = request.InterruptPrompt,
                            InitialSilenceTimeout = TimeSpan.FromSeconds(request.InitialSilenceTimeoutSec),
                            InterToneTimeout = TimeSpan.FromSeconds(d.InterToneTimeoutSec),
                            OperationContext = request.OperationContext
                        };
                        if (!string.IsNullOrEmpty(d.StopTones))
                        {
                            var parsed = ParseTones(d.StopTones);
                            if (parsed != null) foreach (var t in parsed) dtmfOpts.StopTones.Add(t);
                        }
                        if (async) await callMedia.StartRecognizingAsync(dtmfOpts); else callMedia.StartRecognizing(dtmfOpts);
                        break;
                    }
                    case RecognizeType.Choice:
                    {
                        if (request.ChoiceOptions == null || request.ChoiceOptions.Choices.Count == 0)
                            return BadRequest("choiceOptions with at least one choice is required for recognizeType 'Choice'.");
                        var choices = request.ChoiceOptions.Choices.Select(c => new RecognitionChoice(c.Label, c.Phrases)).ToList();
                        var choiceOpts = new CallMediaRecognizeChoiceOptions(identifier, choices)
                        {
                            Prompt = fileSource, InterruptPrompt = request.InterruptPrompt,
                            InitialSilenceTimeout = TimeSpan.FromSeconds(request.InitialSilenceTimeoutSec),
                            OperationContext = request.OperationContext
                        };
                        if (async) await callMedia.StartRecognizingAsync(choiceOpts); else callMedia.StartRecognizing(choiceOpts);
                        break;
                    }
                    case RecognizeType.Speech:
                    {
                        var s = request.SpeechOptions ?? new SpeechOptionsRequest();
                        var speechOpts = new CallMediaRecognizeSpeechOptions(identifier)
                        {
                            Prompt = fileSource, InterruptPrompt = request.InterruptPrompt,
                            InitialSilenceTimeout = TimeSpan.FromSeconds(request.InitialSilenceTimeoutSec),
                            EndSilenceTimeout = TimeSpan.FromSeconds(s.EndSilenceTimeoutSec),
                            OperationContext = request.OperationContext
                        };
                        if (!string.IsNullOrEmpty(s.SpeechLanguage)) speechOpts.SpeechLanguage = s.SpeechLanguage;
                        if (async) await callMedia.StartRecognizingAsync(speechOpts); else callMedia.StartRecognizing(speechOpts);
                        break;
                    }
                    case RecognizeType.SpeechOrDtmf:
                    {
                        var sd = request.SpeechOrDtmfOptions ?? new SpeechOrDtmfOptionsRequest();
                        var bothOpts = new CallMediaRecognizeSpeechOrDtmfOptions(identifier, sd.MaxTonesToCollect)
                        {
                            Prompt = fileSource, InterruptPrompt = request.InterruptPrompt,
                            InitialSilenceTimeout = TimeSpan.FromSeconds(request.InitialSilenceTimeoutSec),
                            EndSilenceTimeout = TimeSpan.FromSeconds(sd.EndSilenceTimeoutSec),
                            InterToneTimeout = TimeSpan.FromSeconds(sd.InterToneTimeoutSec),
                            OperationContext = request.OperationContext
                        };
                        if (!string.IsNullOrEmpty(sd.SpeechLanguage)) bothOpts.SpeechLanguage = sd.SpeechLanguage;
                        if (async) await callMedia.StartRecognizingAsync(bothOpts); else callMedia.StartRecognizing(bothOpts);
                        break;
                    }
                }

                return Ok(new CallConnectionResponse { CallConnectionId = request.CallConnectionId, CorrelationId = props.CorrelationId, Status = props.CallConnectionState.ToString() });
            }
            catch (Exception ex) { _logger.LogError(ex, "Error during recognition"); return Problem($"Failed to start recognition: {ex.Message}"); }
        }

        private async Task<IActionResult> HandleInterruptAudioAndAnnounce(InterruptAudioAndAnnounceRequest request, bool async)
        {
            var identifier = ResolveIdentifier(request.Target);
            if (identifier == null) return BadRequest("target must be an ACS user ID (8:...) or phone number (+...)");

            _logger.LogInformation("InterruptAudioAndAnnounce. CallId={CallId}, Target={Target}", request.CallConnectionId, request.Target);
            try
            {
                var callMedia = _service.GetCallMedia(request.CallConnectionId);
                var props = _service.GetCallConnectionProperties(request.CallConnectionId);
                var opts = new InterruptAudioAndAnnounceOptions(BuildFileSource(), identifier) { OperationContext = request.OperationContext };
                if (async) await callMedia.InterruptAudioAndAnnounceAsync(opts); else callMedia.InterruptAudioAndAnnounce(opts);
                return Ok(new CallConnectionResponse { CallConnectionId = request.CallConnectionId, CorrelationId = props.CorrelationId, Status = props.CallConnectionState.ToString() });
            }
            catch (Exception ex) { _logger.LogError(ex, "Error in InterruptAudioAndAnnounce"); return Problem($"Failed to interrupt audio and announce: {ex.Message}"); }
        }

        private enum TranscriptionAction { Start, Stop }

        private async Task<IActionResult> HandleTranscription(string callConnectionId, TranscriptionAction action, string? locale, string operationContext, bool async)
        {
            _logger.LogInformation("{Action} transcription. CallId={CallId}, Locale={Locale}", action, callConnectionId, locale);
            try
            {
                var callMedia = _service.GetCallMedia(callConnectionId);
                var props = _service.GetCallConnectionProperties(callConnectionId);

                if (action == TranscriptionAction.Start)
                {
                    var opts = new StartTranscriptionOptions { Locale = locale ?? "en-US", OperationContext = operationContext };
                    if (async) await callMedia.StartTranscriptionAsync(opts); else callMedia.StartTranscription(opts);
                }
                else
                {
                    var opts = new StopTranscriptionOptions { OperationContext = operationContext };
                    if (async) await callMedia.StopTranscriptionAsync(opts); else callMedia.StopTranscription(opts);
                }

                return Ok(new CallConnectionResponse { CallConnectionId = callConnectionId, CorrelationId = props.CorrelationId, Status = props.CallConnectionState.ToString() });
            }
            catch (Exception ex) { _logger.LogError(ex, "Error during transcription {Action}", action); return Problem($"Failed to {action.ToString().ToLower()} transcription: {ex.Message}"); }
        }

        private async Task<IActionResult> HandleUpdateTranscription(UpdateTranscriptionRequest request, bool async)
        {
            _logger.LogInformation("UpdateTranscription. CallId={CallId}, Locale={Locale}", request.CallConnectionId, request.Locale);
            try
            {
                var callMedia = _service.GetCallMedia(request.CallConnectionId);
                var props = _service.GetCallConnectionProperties(request.CallConnectionId);
                var opts = new UpdateTranscriptionOptions(request.Locale ?? "en-US") { OperationContext = request.OperationContext };
                if (async) await callMedia.UpdateTranscriptionAsync(opts); else callMedia.UpdateTranscription(opts);
                return Ok(new CallConnectionResponse { CallConnectionId = request.CallConnectionId, CorrelationId = props.CorrelationId, Status = props.CallConnectionState.ToString() });
            }
            catch (Exception ex) { _logger.LogError(ex, "Error updating transcription"); return Problem($"Failed to update transcription: {ex.Message}"); }
        }

        private async Task<IActionResult> HandleSendDtmfTones(SendDtmfTonesRequest request, bool async)
        {
            if (string.IsNullOrEmpty(request.CallConnectionId)) return BadRequest("callConnectionId is required");
            var parsedTones = ParseTones(request.Tones);
            if (parsedTones == null || parsedTones.Count == 0)
                return BadRequest("At least one valid tone is required. Valid: zero-nine, pound, asterisk (or 0-9, #, *)");
            var identifier = ResolveIdentifier(request.Target);
            if (identifier == null) return BadRequest("target must be an ACS user ID (8:...) or phone number (+...)");

            _logger.LogInformation("SendDtmfTones [{Tones}]. CallId={CallId}", string.Join(",", parsedTones), request.CallConnectionId);
            try
            {
                var callMedia = _service.GetCallMedia(request.CallConnectionId);
                var opts = new SendDtmfTonesOptions(parsedTones, identifier)
                { OperationContext = request.OperationContext, OperationCallbackUri = new Uri(new Uri(_config.CallbackUriHost), "/api/callbacks") };
                if (async) await callMedia.SendDtmfTonesAsync(opts); else callMedia.SendDtmfTones(opts);

                var props = _service.GetCallConnectionProperties(request.CallConnectionId);
                return Ok(new CallConnectionResponse { CallConnectionId = request.CallConnectionId, CorrelationId = props.CorrelationId, Status = props.CallConnectionState.ToString() });
            }
            catch (Exception ex) { _logger.LogError(ex, "Error sending DTMF tones"); return Problem($"Failed to send DTMF tones: {ex.Message}"); }
        }

        private async Task<IActionResult> HandleContinuousDtmf(ContinuousDtmfRequest request, bool start, bool async)
        {
            if (string.IsNullOrEmpty(request.CallConnectionId)) return BadRequest("callConnectionId is required");
            var identifier = ResolveIdentifier(request.Target);
            if (identifier == null) return BadRequest("target must be an ACS user ID (8:...) or phone number (+...)");

            _logger.LogInformation("{Action} continuous DTMF. CallId={CallId}", start ? "Starting" : "Stopping", request.CallConnectionId);
            try
            {
                var callMedia = _service.GetCallMedia(request.CallConnectionId);
                var opts = new ContinuousDtmfRecognitionOptions(identifier) { OperationContext = request.OperationContext };

                if (start)
                    if (async) await callMedia.StartContinuousDtmfRecognitionAsync(opts); else callMedia.StartContinuousDtmfRecognition(opts);
                else
                    if (async) await callMedia.StopContinuousDtmfRecognitionAsync(opts); else callMedia.StopContinuousDtmfRecognition(opts);

                var props = _service.GetCallConnectionProperties(request.CallConnectionId);
                return Ok(new CallConnectionResponse { CallConnectionId = request.CallConnectionId, CorrelationId = props.CorrelationId, Status = props.CallConnectionState.ToString() });
            }
            catch (Exception ex) { _logger.LogError(ex, "Error in continuous DTMF"); return Problem($"Failed continuous DTMF operation: {ex.Message}"); }
        }

        // ??? UTILITIES ??????????????????????????????????????????????????????????

        private enum RecognizeType { Dtmf, Choice, Speech, SpeechOrDtmf }

        private static CommunicationIdentifier? ResolveIdentifier(string? target)
        {
            if (string.IsNullOrEmpty(target)) return null;
            if (target.StartsWith("8:")) return new CommunicationUserIdentifier(target);
            if (target.StartsWith("+")) return new PhoneNumberIdentifier(target);
            return null;
        }

        private static List<DtmfTone>? ParseTones(string? tones)
        {
            if (string.IsNullOrWhiteSpace(tones)) return null;
            var map = new Dictionary<string, DtmfTone>(StringComparer.OrdinalIgnoreCase)
            {
                ["zero"] = DtmfTone.Zero, ["one"] = DtmfTone.One, ["two"] = DtmfTone.Two,
                ["three"] = DtmfTone.Three, ["four"] = DtmfTone.Four, ["five"] = DtmfTone.Five,
                ["six"] = DtmfTone.Six, ["seven"] = DtmfTone.Seven, ["eight"] = DtmfTone.Eight,
                ["nine"] = DtmfTone.Nine, ["pound"] = DtmfTone.Pound, ["asterisk"] = DtmfTone.Asterisk,
                ["0"] = DtmfTone.Zero, ["1"] = DtmfTone.One, ["2"] = DtmfTone.Two,
                ["3"] = DtmfTone.Three, ["4"] = DtmfTone.Four, ["5"] = DtmfTone.Five,
                ["6"] = DtmfTone.Six, ["7"] = DtmfTone.Seven, ["8"] = DtmfTone.Eight,
                ["9"] = DtmfTone.Nine, ["#"] = DtmfTone.Pound, ["*"] = DtmfTone.Asterisk
            };
            return tones.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Where(t => map.ContainsKey(t)).Select(t => map[t]).ToList();
        }
    }
}
