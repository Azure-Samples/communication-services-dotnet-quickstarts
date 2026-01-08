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
    [Route("api/ai")]
    [Produces("application/json")]
    public class AIController : ControllerBase
    {
        private readonly CallAutomationService _service;
        private readonly ILogger<AIController> _logger;
        private readonly ConfigurationRequest _config;

        public AIController(
            CallAutomationService service,
            ILogger<AIController> logger,
            IOptions<ConfigurationRequest> configOptions)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = configOptions.Value ?? throw new ArgumentNullException(nameof(configOptions));
        }

        // ──────────── COGNITIVE SERVICES INTEGRATION ───────────────────────────────

        /// <summary>
        /// Creates a call with Call Intelligence (Cognitive Services) enabled for advanced AI features (Async)
        /// </summary>
        /// <param name="target">Target phone number (with country code) or communication user ID</param>
        /// <param name="enableTranscription">Whether to enable transcription (default: true)</param>
        /// <param name="locale">Transcription locale (default: en-US)</param>
        [HttpPost("createCallWithCallIntelligenceAsync")]
        [Tags("AI - Call with Cognitive Services")]
        public async Task<IActionResult> CreateCallWithCallIntelligenceAsync(
            string target,
            bool enableTranscription = true,
            string locale = "en-US")
        {
            if (string.IsNullOrWhiteSpace(target))
                return BadRequest("Target is required");

            if (!target.StartsWith("8:") && !target.StartsWith("+"))
                return BadRequest("PSTN number must include country code (e.g., +1 for US)");

            if (string.IsNullOrWhiteSpace(_config.CognitiveServiceEndpoint))
                return BadRequest("CognitiveServiceEndpoint must be configured in appsettings.json to use Call Intelligence features");

            _logger.LogInformation($"Creating call with Call Intelligence. Target={target}, Locale={locale}");

            try
            {
                var callbackUri = new Uri(new Uri(_config.CallbackUriHost), "/api/callbacks");
                var websocketUri = _config.CallbackUriHost.Replace("https", "wss") + "/ws";

                CallInvite callInvite;
                if (target.StartsWith("8:"))
                {
                    callInvite = new CallInvite(new CommunicationUserIdentifier(target));
                }
                else
                {
                    callInvite = new CallInvite(
                        new PhoneNumberIdentifier(target),
                        new PhoneNumberIdentifier(_config.AcsPhoneNumber));
                }

                var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
                {
                    CallIntelligenceOptions = new CallIntelligenceOptions
                    {
                        CognitiveServicesEndpoint = new Uri(_config.CognitiveServiceEndpoint)
                    }
                };

                if (enableTranscription)
                {
                    var transcriptionOptions = new TranscriptionOptions(
                        new Uri(websocketUri),
                        locale,
                        enableTranscription,
                        TranscriptionTransport.Websocket);
                    createCallOptions.TranscriptionOptions = transcriptionOptions;
                }

                CreateCallResult result = await _service.GetCallAutomationClient().CreateCallAsync(createCallOptions);

                var props = result.CallConnectionProperties;
                _logger.LogInformation($"Call created with Call Intelligence. CallConnectionId={props.CallConnectionId}");

                return Ok(new
                {
                    CallConnectionId = props.CallConnectionId,
                    CorrelationId = props.CorrelationId,
                    Status = props.CallConnectionState.ToString(),
                    CallIntelligenceEnabled = true,
                    TranscriptionEnabled = enableTranscription,
                    CognitiveServicesEndpoint = _config.CognitiveServiceEndpoint
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating call with Call Intelligence");
                return Problem($"Failed to create call with Call Intelligence: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a call with Call Intelligence (Cognitive Services) enabled for advanced AI features (Sync)
        /// </summary>
        /// <param name="target">Target phone number (with country code) or communication user ID</param>
        /// <param name="enableTranscription">Whether to enable transcription (default: true)</param>
        /// <param name="locale">Transcription locale (default: en-US)</param>
        [HttpPost("createCallWithCallIntelligence")]
        [Tags("AI - Call with Cognitive Services")]
        public IActionResult CreateCallWithCallIntelligence(
            string target,
            bool enableTranscription = true,
            string locale = "en-US")
        {
            return CreateCallWithCallIntelligenceAsync(target, enableTranscription, locale).Result;
        }

        // ──────────── TRANSCRIPTION ENDPOINTS ─────────────────────────────────────

        /// <summary>
        /// Creates a call with transcription enabled (Async)
        /// </summary>
        /// <param name="target">Target phone number (with country code) or communication user ID</param>
        /// <param name="locale">Transcription locale (default: en-US)</param>
        /// <param name="enableTranscription">Whether to enable transcription on call creation</param>
        [HttpPost("createCallWithTranscriptionAsync")]
        [Tags("AI - Transcription")]
        public async Task<IActionResult> CreateCallWithTranscriptionAsync(
            string target,
            string locale = "en-US",
            bool enableTranscription = true)
        {
            if (string.IsNullOrWhiteSpace(target))
                return BadRequest("Target is required");

            if (!target.StartsWith("8:") && !target.StartsWith("+"))
                return BadRequest("PSTN number must include country code (e.g., +1 for US)");

            _logger.LogInformation($"Creating call with transcription. Target={target}, Locale={locale}, Enabled={enableTranscription}");

            try
            {
                var callbackUri = new Uri(new Uri(_config.CallbackUriHost), "/api/callbacks");
                var websocketUri = _config.CallbackUriHost.Replace("https", "wss") + "/ws";

                var transcriptionOptions = new TranscriptionOptions(
                    new Uri(websocketUri),
                    locale,
                    enableTranscription,
                    TranscriptionTransport.Websocket);

                CallInvite callInvite;
                if (target.StartsWith("8:"))
                {
                    callInvite = new CallInvite(new CommunicationUserIdentifier(target));
                }
                else
                {
                    callInvite = new CallInvite(
                        new PhoneNumberIdentifier(target),
                        new PhoneNumberIdentifier(_config.AcsPhoneNumber));
                }

                var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
                {
                    TranscriptionOptions = transcriptionOptions
                };

                CreateCallResult result = await _service.GetCallAutomationClient().CreateCallAsync(createCallOptions);

                var props = result.CallConnectionProperties;
                _logger.LogInformation($"Call created with transcription. CallConnectionId={props.CallConnectionId}");

                return Ok(new
                {
                    CallConnectionId = props.CallConnectionId,
                    CorrelationId = props.CorrelationId,
                    Status = props.CallConnectionState.ToString(),
                    TranscriptionEnabled = enableTranscription
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating call with transcription");
                return Problem($"Failed to create call with transcription: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a call with transcription enabled (Sync)
        /// </summary>
        /// <param name="target">Target phone number (with country code) or communication user ID</param>
        /// <param name="locale">Transcription locale (default: en-US)</param>
        /// <param name="enableTranscription">Whether to enable transcription on call creation</param>
        [HttpPost("createCallWithTranscription")]
        [Tags("AI - Transcription")]
        public IActionResult CreateCallWithTranscription(
            string target,
            string locale = "en-US",
            bool enableTranscription = true)
        {
            return CreateCallWithTranscriptionAsync(target, locale, enableTranscription).Result;
        }

        /// <summary>
        /// Starts transcription on an active call (Async)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="locale">Transcription locale (optional, default from initial setup)</param>
        [HttpPost("startTranscriptionAsync")]
        [Tags("AI - Transcription")]
        public async Task<IActionResult> StartTranscriptionAsync(
            string callConnectionId,
            string locale = null)
        {
            if (string.IsNullOrWhiteSpace(callConnectionId))
                return BadRequest("CallConnectionId is required");

            _logger.LogInformation($"Starting transcription. CallId={callConnectionId}, Locale={locale}");

            try
            {
                var callMedia = _service.GetCallMedia(callConnectionId);
                var props = _service.GetCallConnectionProperties(callConnectionId);

                if (string.IsNullOrWhiteSpace(locale))
                {
                    await callMedia.StartTranscriptionAsync();
                }
                else
                {
                    var options = new StartTranscriptionOptions
                    {
                        Locale = locale,
                        OperationContext = "StartTranscriptionContext"
                    };
                    await callMedia.StartTranscriptionAsync(options);
                }

                _logger.LogInformation("Transcription started successfully");
                return Ok(new
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = props.CorrelationId,
                    Status = "TranscriptionStarted",
                    Locale = locale ?? "default"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting transcription");
                return Problem($"Failed to start transcription: {ex.Message}");
            }
        }

        /// <summary>
        /// Starts transcription on an active call (Sync)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="locale">Transcription locale (optional, default from initial setup)</param>
        [HttpPost("startTranscription")]
        [Tags("AI - Transcription")]
        public IActionResult StartTranscription(
            string callConnectionId,
            string locale = null)
        {
            return StartTranscriptionAsync(callConnectionId, locale).Result;
        }

        /// <summary>
        /// Stops transcription on an active call (Async)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        [HttpPost("stopTranscriptionAsync")]
        [Tags("AI - Transcription")]
        public async Task<IActionResult> StopTranscriptionAsync(string callConnectionId)
        {
            if (string.IsNullOrWhiteSpace(callConnectionId))
                return BadRequest("CallConnectionId is required");

            _logger.LogInformation($"Stopping transcription. CallId={callConnectionId}");

            try
            {
                var callMedia = _service.GetCallMedia(callConnectionId);
                var props = _service.GetCallConnectionProperties(callConnectionId);

                await callMedia.StopTranscriptionAsync();

                _logger.LogInformation("Transcription stopped successfully");
                return Ok(new
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = props.CorrelationId,
                    Status = "TranscriptionStopped"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping transcription");
                return Problem($"Failed to stop transcription: {ex.Message}");
            }
        }

        /// <summary>
        /// Stops transcription on an active call (Sync)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        [HttpPost("stopTranscription")]
        [Tags("AI - Transcription")]
        public IActionResult StopTranscription(string callConnectionId)
        {
            return StopTranscriptionAsync(callConnectionId).Result;
        }

        /// <summary>
        /// Updates transcription locale on an active transcription session (Async)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="locale">New transcription locale</param>
        [HttpPost("updateTranscriptionAsync")]
        [Tags("AI - Transcription")]
        public async Task<IActionResult> UpdateTranscriptionAsync(
            string callConnectionId,
            string locale)
        {
            if (string.IsNullOrWhiteSpace(callConnectionId))
                return BadRequest("CallConnectionId is required");

            if (string.IsNullOrWhiteSpace(locale))
                return BadRequest("Locale is required");

            _logger.LogInformation($"Updating transcription locale. CallId={callConnectionId}, Locale={locale}");

            try
            {
                var callMedia = _service.GetCallMedia(callConnectionId);
                var props = _service.GetCallConnectionProperties(callConnectionId);

                await callMedia.UpdateTranscriptionAsync(locale);

                _logger.LogInformation("Transcription locale updated successfully");
                return Ok(new
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = props.CorrelationId,
                    Status = "TranscriptionUpdated",
                    NewLocale = locale
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating transcription");
                return Problem($"Failed to update transcription: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates transcription locale on an active transcription session (Sync)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="locale">New transcription locale</param>
        [HttpPost("updateTranscription")]
        [Tags("AI - Transcription")]
        public IActionResult UpdateTranscription(
            string callConnectionId,
            string locale)
        {
            return UpdateTranscriptionAsync(callConnectionId, locale).Result;
        }

        // ──────────── SPEECH RECOGNITION ENDPOINTS ─────────────────────────────────

        /// <summary>
        /// Starts speech recognition on a call (Async)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="target">Target participant to recognize from</param>
        /// <param name="initialSilenceTimeoutSeconds">Initial silence timeout in seconds (default: 15)</param>
        /// <param name="endSilenceTimeoutSeconds">End silence timeout in seconds (default: 5)</param>
        /// <param name="speechLanguage">Speech recognition language (default: en-US)</param>
        [HttpPost("recognizeSpeechAsync")]
        [Tags("AI - Speech Recognition")]
        public async Task<IActionResult> RecognizeSpeechAsync(
            string callConnectionId,
            string target,
            int initialSilenceTimeoutSeconds = 15,
            int endSilenceTimeoutSeconds = 5,
            string speechLanguage = "en-US")
        {
            if (string.IsNullOrWhiteSpace(callConnectionId))
                return BadRequest("CallConnectionId is required");

            if (string.IsNullOrWhiteSpace(target))
                return BadRequest("Target is required");

            if (!target.StartsWith("8:") && !target.StartsWith("+"))
                return BadRequest("PSTN number must include country code (e.g., +1 for US)");

            _logger.LogInformation($"Starting speech recognition. CallId={callConnectionId}, Target={target}, Language={speechLanguage}");

            try
            {
                var callMedia = _service.GetCallMedia(callConnectionId);
                var props = _service.GetCallConnectionProperties(callConnectionId);
                var callbackUri = new Uri(new Uri(_config.CallbackUriHost), "/api/callbacks");

                CommunicationIdentifier identifier = target.StartsWith("8:")
                    ? new CommunicationUserIdentifier(target)
                    : new PhoneNumberIdentifier(target);

                var textSource = new TextSource("Please respond with your message.")
                {
                    VoiceName = "en-US-NancyNeural"
                };

                var speechOptions = new CallMediaRecognizeSpeechOptions(identifier)
                {
                    Prompt = textSource,
                    InterruptPrompt = false,
                    InitialSilenceTimeout = TimeSpan.FromSeconds(initialSilenceTimeoutSeconds),
                    EndSilenceTimeout = TimeSpan.FromSeconds(endSilenceTimeoutSeconds),
                    OperationContext = "SpeechRecognitionContext",
                    OperationCallbackUri = callbackUri,
                    SpeechLanguage = speechLanguage
                };

                await callMedia.StartRecognizingAsync(speechOptions);

                _logger.LogInformation("Speech recognition started successfully");
                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = props.CorrelationId,
                    Status = "SpeechRecognitionStarted"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting speech recognition");
                return Problem($"Failed to start speech recognition: {ex.Message}");
            }
        }

        /// <summary>
        /// Starts speech recognition on a call (Sync)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="target">Target participant to recognize from</param>
        /// <param name="initialSilenceTimeoutSeconds">Initial silence timeout in seconds (default: 15)</param>
        /// <param name="endSilenceTimeoutSeconds">End silence timeout in seconds (default: 5)</param>
        /// <param name="speechLanguage">Speech recognition language (default: en-US)</param>
        [HttpPost("recognizeSpeech")]
        [Tags("AI - Speech Recognition")]
        public IActionResult RecognizeSpeech(
            string callConnectionId,
            string target,
            int initialSilenceTimeoutSeconds = 15,
            int endSilenceTimeoutSeconds = 5,
            string speechLanguage = "en-US")
        {
            return RecognizeSpeechAsync(callConnectionId, target, initialSilenceTimeoutSeconds, endSilenceTimeoutSeconds, speechLanguage).Result;
        }

        /// <summary>
        /// Starts speech or DTMF recognition on a call (Async)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="target">Target participant to recognize from</param>
        /// <param name="maxTonesToCollect">Maximum number of DTMF tones to collect</param>
        [HttpPost("recognizeSpeechOrDtmfAsync")]
        [Tags("AI - Speech Recognition")]
        public async Task<IActionResult> RecognizeSpeechOrDtmfAsync(
            string callConnectionId,
            string target,
            int maxTonesToCollect = 4)
        {
            if (string.IsNullOrWhiteSpace(callConnectionId))
                return BadRequest("CallConnectionId is required");

            if (string.IsNullOrWhiteSpace(target))
                return BadRequest("Target is required");

            if (!target.StartsWith("8:") && !target.StartsWith("+"))
                return BadRequest("PSTN number must include country code (e.g., +1 for US)");

            _logger.LogInformation($"Starting speech or DTMF recognition. CallId={callConnectionId}, Target={target}");

            try
            {
                var callMedia = _service.GetCallMedia(callConnectionId);
                var props = _service.GetCallConnectionProperties(callConnectionId);
                var callbackUri = new Uri(new Uri(_config.CallbackUriHost), "/api/callbacks");

                CommunicationIdentifier identifier = target.StartsWith("8:")
                    ? new CommunicationUserIdentifier(target)
                    : new PhoneNumberIdentifier(target);

                var textSource = new TextSource("Please respond by speaking or pressing keys on your keypad.")
                {
                    VoiceName = "en-US-NancyNeural"
                };

                var recognizeOptions = new CallMediaRecognizeSpeechOrDtmfOptions(identifier, maxTonesToCollect)
                {
                    Prompt = textSource,
                    InterruptPrompt = false,
                    InitialSilenceTimeout = TimeSpan.FromSeconds(15),
                    EndSilenceTimeout = TimeSpan.FromSeconds(5),
                    OperationContext = "SpeechOrDTMFContext",
                    OperationCallbackUri = callbackUri
                };

                await callMedia.StartRecognizingAsync(recognizeOptions);

                _logger.LogInformation("Speech or DTMF recognition started successfully");
                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = props.CorrelationId,
                    Status = "SpeechOrDtmfRecognitionStarted"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting speech or DTMF recognition");
                return Problem($"Failed to start speech or DTMF recognition: {ex.Message}");
            }
        }

        /// <summary>
        /// Starts speech or DTMF recognition on a call (Sync)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="target">Target participant to recognize from</param>
        /// <param name="maxTonesToCollect">Maximum number of DTMF tones to collect</param>
        [HttpPost("recognizeSpeechOrDtmf")]
        [Tags("AI - Speech Recognition")]
        public IActionResult RecognizeSpeechOrDtmf(
            string callConnectionId,
            string target,
            int maxTonesToCollect = 4)
        {
            return RecognizeSpeechOrDtmfAsync(callConnectionId, target, maxTonesToCollect).Result;
        }

        /// <summary>
        /// Starts choice-based recognition (voice commands) on a call (Async)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="target">Target participant to recognize from</param>
        [HttpPost("recognizeChoiceAsync")]
        [Tags("AI - Speech Recognition")]
        public async Task<IActionResult> RecognizeChoiceAsync(
            string callConnectionId,
            string target)
        {
            if (string.IsNullOrWhiteSpace(callConnectionId))
                return BadRequest("CallConnectionId is required");

            if (string.IsNullOrWhiteSpace(target))
                return BadRequest("Target is required");

            if (!target.StartsWith("8:") && !target.StartsWith("+"))
                return BadRequest("PSTN number must include country code (e.g., +1 for US)");

            _logger.LogInformation($"Starting choice recognition. CallId={callConnectionId}, Target={target}");

            try
            {
                var callMedia = _service.GetCallMedia(callConnectionId);
                var props = _service.GetCallConnectionProperties(callConnectionId);
                var callbackUri = new Uri(new Uri(_config.CallbackUriHost), "/api/callbacks");

                CommunicationIdentifier identifier = target.StartsWith("8:")
                    ? new CommunicationUserIdentifier(target)
                    : new PhoneNumberIdentifier(target);

                var textSource = new TextSource("Please say yes or no.")
                {
                    VoiceName = "en-US-NancyNeural"
                };

                var choices = new List<RecognitionChoice>
                {
                    new RecognitionChoice("yes", new[] { "yes", "yeah", "sure", "confirm" }),
                    new RecognitionChoice("no", new[] { "no", "nope", "cancel", "negative" })
                };

                var recognizeOptions = new CallMediaRecognizeChoiceOptions(identifier, choices)
                {
                    InterruptPrompt = false,
                    InitialSilenceTimeout = TimeSpan.FromSeconds(15),
                    Prompt = textSource,
                    OperationContext = "ChoiceRecognitionContext",
                    OperationCallbackUri = callbackUri
                };

                await callMedia.StartRecognizingAsync(recognizeOptions);

                _logger.LogInformation("Choice recognition started successfully");
                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = props.CorrelationId,
                    Status = "ChoiceRecognitionStarted"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting choice recognition");
                return Problem($"Failed to start choice recognition: {ex.Message}");
            }
        }

        /// <summary>
        /// Starts choice-based recognition (voice commands) on a call (Sync)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="target">Target participant to recognize from</param>
        [HttpPost("recognizeChoice")]
        [Tags("AI - Speech Recognition")]
        public IActionResult RecognizeChoice(
            string callConnectionId,
            string target)
        {
            return RecognizeChoiceAsync(callConnectionId, target).Result;
        }

        // ──────────── CONTINUOUS DTMF RECOGNITION ─────────────────────────────────

        /// <summary>
        /// Starts advanced choice recognition with speech phrases enabled (requires Cognitive Services) (Async)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="target">Target participant to recognize from</param>
        /// <param name="promptText">Text prompt to play (optional)</param>
        [HttpPost("recognizeChoiceWithSpeechAsync")]
        [Tags("AI - Recognition")]
        public async Task<IActionResult> RecognizeChoiceWithSpeechAsync(
            string callConnectionId,
            string target,
            string promptText = "Hi, this is recognize test. Please say yes or no, or press 1 for yes and 2 for no.")
        {
            if (string.IsNullOrWhiteSpace(callConnectionId))
                return BadRequest("CallConnectionId is required");

            if (string.IsNullOrWhiteSpace(target))
                return BadRequest("Target is required");

            if (!target.StartsWith("8:") && !target.StartsWith("+"))
                return BadRequest("PSTN number must include country code (e.g., +1 for US)");

            if (string.IsNullOrWhiteSpace(_config.CognitiveServiceEndpoint))
                return BadRequest("CognitiveServiceEndpoint must be configured to use speech-enabled choice recognition");

            _logger.LogInformation($"Starting choice recognition with speech. CallId={callConnectionId}, Target={target}");

            try
            {
                var callMedia = _service.GetCallMedia(callConnectionId);
                var props = _service.GetCallConnectionProperties(callConnectionId);

                CommunicationIdentifier identifier = target.StartsWith("8:")
                    ? new CommunicationUserIdentifier(target)
                    : new PhoneNumberIdentifier(target);

                var textSource = new TextSource(promptText)
                {
                    VoiceName = "en-US-NancyNeural"
                };

                // With Cognitive Services, we can include both speech phrases and DTMF tones
                var choices = new List<RecognitionChoice>
                {
                    new RecognitionChoice("Confirm", new[] { "Confirm", "Yes", "One" })
                    {
                        Tone = DtmfTone.One
                    },
                    new RecognitionChoice("Cancel", new[] { "Cancel", "No", "Two" })
                    {
                        Tone = DtmfTone.Two
                    }
                };

                var recognizeOptions = new CallMediaRecognizeChoiceOptions(identifier, choices)
                {
                    InterruptPrompt = false,
                    InitialSilenceTimeout = TimeSpan.FromSeconds(10),
                    Prompt = textSource,
                    OperationContext = "ChoiceWithSpeechContext"
                };

                await callMedia.StartRecognizingAsync(recognizeOptions);

                _logger.LogInformation("Choice recognition with speech started successfully");
                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = props.CorrelationId,
                    Status = "ChoiceWithSpeechRecognitionStarted"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting choice recognition with speech");
                return Problem($"Failed to start choice recognition with speech: {ex.Message}");
            }
        }

        /// <summary>
        /// Starts advanced choice recognition with speech phrases enabled (requires Cognitive Services) (Sync)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="target">Target participant to recognize from</param>
        /// <param name="promptText">Text prompt to play (optional)</param>
        [HttpPost("recognizeChoiceWithSpeech")]
        [Tags("AI - Recognition")]
        public IActionResult RecognizeChoiceWithSpeech(
            string callConnectionId,
            string target,
            string promptText = "Hi, this is recognize test. Please say yes or no, or press 1 for yes and 2 for no.")
        {
            return RecognizeChoiceWithSpeechAsync(callConnectionId, target, promptText).Result;
        }

        /// <summary>
        /// Starts advanced speech recognition with custom vocabulary (requires Cognitive Services) (Async)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="target">Target participant to recognize from</param>
        /// <param name="promptText">Text prompt to play</param>
        /// <param name="speechLanguage">Speech recognition language (default: en-US)</param>
        [HttpPost("recognizeSpeechAdvancedAsync")]
        [Tags("AI - Recognition")]
        public async Task<IActionResult> RecognizeSpeechAdvancedAsync(
            string callConnectionId,
            string target,
            string promptText = "Please tell me your request.",
            string speechLanguage = "en-US")
        {
            if (string.IsNullOrWhiteSpace(callConnectionId))
                return BadRequest("CallConnectionId is required");

            if (string.IsNullOrWhiteSpace(target))
                return BadRequest("Target is required");

            if (!target.StartsWith("8:") && !target.StartsWith("+"))
                return BadRequest("PSTN number must include country code (e.g., +1 for US)");

            if (string.IsNullOrWhiteSpace(_config.CognitiveServiceEndpoint))
                return BadRequest("CognitiveServiceEndpoint must be configured for advanced speech recognition");

            _logger.LogInformation($"Starting advanced speech recognition. CallId={callConnectionId}, Target={target}, Language={speechLanguage}");

            try
            {
                var callMedia = _service.GetCallMedia(callConnectionId);
                var props = _service.GetCallConnectionProperties(callConnectionId);

                CommunicationIdentifier identifier = target.StartsWith("8:")
                    ? new CommunicationUserIdentifier(target)
                    : new PhoneNumberIdentifier(target);

                var textSource = new TextSource(promptText)
                {
                    VoiceName = "en-US-NancyNeural"
                };

                var speechOptions = new CallMediaRecognizeSpeechOptions(identifier)
                {
                    Prompt = textSource,
                    InterruptPrompt = false,
                    InitialSilenceTimeout = TimeSpan.FromSeconds(15),
                    EndSilenceTimeout = TimeSpan.FromSeconds(10),
                    OperationContext = "AdvancedSpeechRecognitionContext",
                    SpeechLanguage = speechLanguage
                };

                await callMedia.StartRecognizingAsync(speechOptions);

                _logger.LogInformation("Advanced speech recognition started successfully");
                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = props.CorrelationId,
                    Status = "AdvancedSpeechRecognitionStarted"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting advanced speech recognition");
                return Problem($"Failed to start advanced speech recognition: {ex.Message}");
            }
        }

        /// <summary>
        /// Starts advanced speech recognition with custom vocabulary (requires Cognitive Services) (Sync)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="target">Target participant to recognize from</param>
        /// <param name="promptText">Text prompt to play</param>
        /// <param name="speechLanguage">Speech recognition language (default: en-US)</param>
        [HttpPost("recognizeSpeechAdvanced")]
        [Tags("AI - Recognition")]
        public IActionResult RecognizeSpeechAdvanced(
            string callConnectionId,
            string target,
            string promptText = "Please tell me your request.",
            string speechLanguage = "en-US")
        {
            return RecognizeSpeechAdvancedAsync(callConnectionId, target, promptText, speechLanguage).Result;
        }

        /// <summary>
        /// Starts continuous DTMF recognition on a call (Async)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="target">Target participant to recognize from</param>
        [HttpPost("startContinuousDtmfRecognitionAsync")]
        [Tags("AI - Recognition")]
        public async Task<IActionResult> StartContinuousDtmfRecognitionAsync(
            string callConnectionId,
            string target)
        {
            if (string.IsNullOrWhiteSpace(callConnectionId))
                return BadRequest("CallConnectionId is required");

            if (string.IsNullOrWhiteSpace(target))
                return BadRequest("Target is required");

            if (!target.StartsWith("8:") && !target.StartsWith("+"))
                return BadRequest("PSTN number must include country code (e.g., +1 for US)");

            _logger.LogInformation($"Starting continuous DTMF recognition. CallId={callConnectionId}, Target={target}");

            try
            {
                var callMedia = _service.GetCallMedia(callConnectionId);
                var props = _service.GetCallConnectionProperties(callConnectionId);

                CommunicationIdentifier identifier = target.StartsWith("8:")
                    ? new CommunicationUserIdentifier(target)
                    : new PhoneNumberIdentifier(target);

                await callMedia.StartContinuousDtmfRecognitionAsync(identifier);

                _logger.LogInformation("Continuous DTMF recognition started successfully");
                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = props.CorrelationId,
                    Status = "ContinuousDtmfRecognitionStarted"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting continuous DTMF recognition");
                return Problem($"Failed to start continuous DTMF recognition: {ex.Message}");
            }
        }

        /// <summary>
        /// Starts continuous DTMF recognition on a call (Sync)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="target">Target participant to recognize from</param>
        [HttpPost("startContinuousDtmfRecognition")]
        [Tags("AI - Recognition")]
        public IActionResult StartContinuousDtmfRecognition(
            string callConnectionId,
            string target)
        {
            return StartContinuousDtmfRecognitionAsync(callConnectionId, target).Result;
        }

        /// <summary>
        /// Stops continuous DTMF recognition on a call (Async)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="target">Target participant to stop recognition for</param>
        [HttpPost("stopContinuousDtmfRecognitionAsync")]
        [Tags("AI - Recognition")]
        public async Task<IActionResult> StopContinuousDtmfRecognitionAsync(
            string callConnectionId,
            string target)
        {
            if (string.IsNullOrWhiteSpace(callConnectionId))
                return BadRequest("CallConnectionId is required");

            if (string.IsNullOrWhiteSpace(target))
                return BadRequest("Target is required");

            if (!target.StartsWith("8:") && !target.StartsWith("+"))
                return BadRequest("PSTN number must include country code (e.g., +1 for US)");

            _logger.LogInformation($"Stopping continuous DTMF recognition. CallId={callConnectionId}, Target={target}");

            try
            {
                var callMedia = _service.GetCallMedia(callConnectionId);
                var props = _service.GetCallConnectionProperties(callConnectionId);

                CommunicationIdentifier identifier = target.StartsWith("8:")
                    ? new CommunicationUserIdentifier(target)
                    : new PhoneNumberIdentifier(target);

                await callMedia.StopContinuousDtmfRecognitionAsync(identifier);

                _logger.LogInformation("Continuous DTMF recognition stopped successfully");
                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = props.CorrelationId,
                    Status = "ContinuousDtmfRecognitionStopped"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping continuous DTMF recognition");
                return Problem($"Failed to stop continuous DTMF recognition: {ex.Message}");
            }
        }

        /// <summary>
        /// Stops continuous DTMF recognition on a call (Sync)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="target">Target participant to stop recognition for</param>
        [HttpPost("stopContinuousDtmfRecognition")]
        [Tags("AI - Recognition")]
        public IActionResult StopContinuousDtmfRecognition(
            string callConnectionId,
            string target)
        {
            return StopContinuousDtmfRecognitionAsync(callConnectionId, target).Result;
        }

        // ──────────── TEXT-TO-SPEECH ENDPOINTS ─────────────────────────────────────

        /// <summary>
        /// Plays text-to-speech to a specific target using TextSource (Async)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="target">Target participant</param>
        /// <param name="text">Text to convert to speech</param>
        /// <param name="voiceName">Voice name (default: en-US-NancyNeural)</param>
        [HttpPost("playTextToSpeechAsync")]
        [Tags("AI - Text-to-Speech")]
        public async Task<IActionResult> PlayTextToSpeechAsync(
            string callConnectionId,
            string target,
            string text,
            string voiceName = "en-US-NancyNeural")
        {
            if (string.IsNullOrWhiteSpace(callConnectionId))
                return BadRequest("CallConnectionId is required");

            if (string.IsNullOrWhiteSpace(target))
                return BadRequest("Target is required");

            if (string.IsNullOrWhiteSpace(text))
                return BadRequest("Text is required");

            if (!target.StartsWith("8:") && !target.StartsWith("+"))
                return BadRequest("PSTN number must include country code (e.g., +1 for US)");

            _logger.LogInformation($"Playing text-to-speech. CallId={callConnectionId}, Target={target}, Voice={voiceName}");

            try
            {
                var callMedia = _service.GetCallMedia(callConnectionId);
                var props = _service.GetCallConnectionProperties(callConnectionId);

                CommunicationIdentifier identifier = target.StartsWith("8:")
                    ? new CommunicationUserIdentifier(target)
                    : new PhoneNumberIdentifier(target);

                var textSource = new TextSource(text)
                {
                    VoiceName = voiceName
                };

                var playOptions = new PlayOptions(textSource, new List<CommunicationIdentifier> { identifier })
                {
                    OperationContext = "TextToSpeechContext"
                };

                await callMedia.PlayAsync(playOptions);

                _logger.LogInformation("Text-to-speech playback started successfully");
                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = props.CorrelationId,
                    Status = "TextToSpeechStarted"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error playing text-to-speech");
                return Problem($"Failed to play text-to-speech: {ex.Message}");
            }
        }

        /// <summary>
        /// Plays text-to-speech to a specific target using TextSource (Sync)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="target">Target participant</param>
        /// <param name="text">Text to convert to speech</param>
        /// <param name="voiceName">Voice name (default: en-US-NancyNeural)</param>
        [HttpPost("playTextToSpeech")]
        [Tags("AI - Text-to-Speech")]
        public IActionResult PlayTextToSpeech(
            string callConnectionId,
            string target,
            string text,
            string voiceName = "en-US-NancyNeural")
        {
            return PlayTextToSpeechAsync(callConnectionId, target, text, voiceName).Result;
        }

        /*
        /// <summary>
        /// Plays text-to-speech to all participants using TextSource
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="text">Text to convert to speech</param>
        /// <param name="voiceName">Voice name (default: en-US-NancyNeural)</param>
        [HttpPost("playTextToSpeechToAll")]
        [Tags("AI - Text-to-Speech")]
        public async Task<IActionResult> PlayTextToSpeechToAll(
            string callConnectionId,
            string text,
            string voiceName = "en-US-NancyNeural")
        {
            if (string.IsNullOrWhiteSpace(callConnectionId))
                return BadRequest("CallConnectionId is required");

            if (string.IsNullOrWhiteSpace(text))
                return BadRequest("Text is required");

            _logger.LogInformation($"Playing text-to-speech to all. CallId={callConnectionId}, Voice={voiceName}");

            try
            {
                var callMedia = _service.GetCallMedia(callConnectionId);
                var props = _service.GetCallConnectionProperties(callConnectionId);

                var textSource = new TextSource(text)
                {
                    VoiceName = voiceName
                };

                var playOptions = new PlayToAllOptions(textSource)
                {
                    OperationContext = "TextToSpeechToAllContext"
                };

                await callMedia.PlayToAllAsync(playOptions);

                _logger.LogInformation("Text-to-speech to all started successfully");
                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = props.CorrelationId,
                    Status = "TextToSpeechToAllStarted"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error playing text-to-speech to all");
                return Problem($"Failed to play text-to-speech to all: {ex.Message}");
            }
        }
        */

        /// <summary>
        /// Plays SSML (Speech Synthesis Markup Language) to a specific target (Async)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="target">Target participant</param>
        /// <param name="ssml">SSML content</param>
        [HttpPost("playSsmlAsync")]
        [Tags("AI - Text-to-Speech")]
        public async Task<IActionResult> PlaySsmlAsync(
            string callConnectionId,
            string target,
            string ssml= "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"><voice name=\"en-US-JennyNeural\">Hi, this is ssml test played through ssml source thanks. Goodbye!</voice></speak>")
        {
            if (string.IsNullOrWhiteSpace(callConnectionId))
                return BadRequest("CallConnectionId is required");

            if (string.IsNullOrWhiteSpace(target))
                return BadRequest("Target is required");

            if (string.IsNullOrWhiteSpace(ssml))
                return BadRequest("SSML is required");

            if (!target.StartsWith("8:") && !target.StartsWith("+"))
                return BadRequest("PSTN number must include country code (e.g., +1 for US)");

            _logger.LogInformation($"Playing SSML. CallId={callConnectionId}, Target={target}");

            try
            {
                var callMedia = _service.GetCallMedia(callConnectionId);
                var props = _service.GetCallConnectionProperties(callConnectionId);

                CommunicationIdentifier identifier = target.StartsWith("8:")
                    ? new CommunicationUserIdentifier(target)
                    : new PhoneNumberIdentifier(target);

                var ssmlSource = new SsmlSource(ssml);

                var playOptions = new PlayOptions(ssmlSource, new List<CommunicationIdentifier> { identifier })
                {
                    OperationContext = "SsmlPlayContext"
                };

                await callMedia.PlayAsync(playOptions);

                _logger.LogInformation("SSML playback started successfully");
                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = props.CorrelationId,
                    Status = "SsmlPlaybackStarted"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error playing SSML");
                return Problem($"Failed to play SSML: {ex.Message}");
            }
        }

        /// <summary>
        /// Plays SSML (Speech Synthesis Markup Language) to a specific target (Sync)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="target">Target participant</param>
        /// <param name="ssml">SSML content</param>
        [HttpPost("playSsml")]
        [Tags("AI - Text-to-Speech")]
        public IActionResult PlaySsml(
            string callConnectionId,
            string target,
            string ssml= "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"><voice name=\"en-US-JennyNeural\">Hi, this is ssml test played through ssml source thanks. Goodbye!</voice></speak>")
        {
            return PlaySsmlAsync(callConnectionId, target, ssml).Result;
        }
    }
}
