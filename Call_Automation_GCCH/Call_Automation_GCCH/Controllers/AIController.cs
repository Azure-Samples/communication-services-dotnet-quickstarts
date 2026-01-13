using Azure.Communication;
using Azure.Communication.CallAutomation;
using Call_Automation_GCCH.Models;
using Call_Automation_GCCH.Services;
using Microsoft.AspNetCore.Mvc;
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

        #region AI(COGNITIVE) SERVICES - CALLS
        /// <summary>
        /// Creates a call with Call Intelligence (Cognitive Services) enabled for advanced AI features (Async)
        /// </summary>
        /// <param name="targetPstnOrAcsIdentity">Target phone number (with country code) or communication user ID</param>
        /// <param name="enableTranscription">Whether to enable transcription (default: true)</param>
        /// <param name="locale">Transcription locale (default: en-US)</param>
        [HttpPost("createCallWithIntelligenceAsync")]
        [Tags("AI - Call with AI(Cognitive) Services")]
        public Task<IActionResult> CreateCallWithCallIntelligenceAsync(
            string targetPstnOrAcsIdentity,
            bool enableTranscription = true,
            string locale = "en-US")
        {
            return CreateCallWithAIFeaturesInternal(targetPstnOrAcsIdentity, locale, enableTranscription, enableCallIntelligence: true, isAsync: true);
        }

        /// <summary>
        /// Creates a call with Call Intelligence (Cognitive Services) enabled for advanced AI features (Sync)
        /// </summary>
        /// <param name="targetPstnOrAcsIdentity">Target phone number (with country code) or communication user ID</param>
        /// <param name="enableTranscription">Whether to enable transcription (default: true)</param>
        /// <param name="locale">Transcription locale (default: en-US)</param>
        [HttpPost("createCallWithIntelligence")]
        [Tags("AI - Call with AI(Cognitive) Services")]
        public Task<IActionResult> CreateCallWithCallIntelligence(
            string targetPstnOrAcsIdentity,
            bool enableTranscription = true,
            string locale = "en-US")
        {
            return CreateCallWithAIFeaturesInternal(targetPstnOrAcsIdentity, locale, enableTranscription, enableCallIntelligence: true, isAsync: false);
        }
        #endregion

        #region TRANSCRIPTION
        /// <summary>
        /// Creates a call with transcription enabled (Async)
        /// </summary>
        /// <param name="target">Target phone number (with country code) or communication user ID</param>
        /// <param name="locale">Transcription locale (default: en-US)</param>
        /// <param name="enableTranscription">Whether to enable transcription on call creation</param>
        [HttpPost("createCallWithTranscriptionAsync")]
        [Tags("AI - Transcription")]
        public Task<IActionResult> CreateCallWithTranscriptionAsync(
            string target,
            string locale = "en-US",
            bool enableTranscription = true)
        {
            return CreateCallWithAIFeaturesInternal(target, locale, enableTranscription, enableCallIntelligence: false, isAsync: true);
        }

        /// <summary>
        /// Creates a call with transcription enabled (Sync)
        /// </summary>
        /// <param name="target">Target phone number (with country code) or communication user ID</param>
        /// <param name="locale">Transcription locale (default: en-US)</param>
        /// <param name="enableTranscription">Whether to enable transcription on call creation</param>
        [HttpPost("createCallWithTranscription")]
        [Tags("AI - Transcription")]
        public Task<IActionResult> CreateCallWithTranscription(
            string target,
            string locale = "en-US",
            bool enableTranscription = true)
        {
            return CreateCallWithAIFeaturesInternal(target, locale, enableTranscription, enableCallIntelligence: false, isAsync: false);
        }

        /// <summary>
        /// Starts transcription on an active call (Async)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="locale">Transcription locale (optional, default from initial setup)</param>
        [HttpPost("startTranscriptionAsync")]
        [Tags("AI - Transcription")]
        public Task<IActionResult> StartTranscriptionAsync(
            string callConnectionId,
            string locale = null)
        {
            return StartTranscriptionInternal(callConnectionId, locale, isAsync: true);
        }

        /// <summary>
        /// Starts transcription on an active call (Sync)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="locale">Transcription locale (optional, default from initial setup)</param>
        [HttpPost("startTranscription")]
        [Tags("AI - Transcription")]
        public Task<IActionResult> StartTranscription(
            string callConnectionId,
            string locale = null)
        {
            return StartTranscriptionInternal(callConnectionId, locale, isAsync: false);
        }

        /// <summary>
        /// Stops transcription on an active call (Async)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        [HttpPost("stopTranscriptionAsync")]
        [Tags("AI - Transcription")]
        public Task<IActionResult> StopTranscriptionAsync(string callConnectionId)
        {
            return StopTranscriptionInternal(callConnectionId, isAsync: true);
        }

        /// <summary>
        /// Stops transcription on an active call (Sync)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        [HttpPost("stopTranscription")]
        [Tags("AI - Transcription")]
        public Task<IActionResult> StopTranscription(string callConnectionId)
        {
            return StopTranscriptionInternal(callConnectionId, isAsync: false);
        }

        /// <summary>
        /// Updates transcription locale on an active transcription session (Async)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="locale">New transcription locale</param>
        [HttpPost("updateTranscriptionAsync")]
        [Tags("AI - Transcription")]
        public Task<IActionResult> UpdateTranscriptionAsync(
            string callConnectionId,
            string locale)
        {
            return UpdateTranscriptionInternal(callConnectionId, locale, isAsync: true);
        }

        /// <summary>
        /// Updates transcription locale on an active transcription session (Sync)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="locale">New transcription locale</param>
        [HttpPost("updateTranscription")]
        [Tags("AI - Transcription")]
        public Task<IActionResult> UpdateTranscription(
            string callConnectionId,
            string locale)
        {
            return UpdateTranscriptionInternal(callConnectionId, locale, isAsync: false);
        }
        #endregion

        #region  RECOGNITION
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
        public Task<IActionResult> RecognizeSpeechAsync(
            string callConnectionId,
            string target,
            int initialSilenceTimeoutSeconds = 15,
            int endSilenceTimeoutSeconds = 5,
            string speechLanguage = "en-US")
        {
            return RecognizeSpeechInternal(callConnectionId, target, initialSilenceTimeoutSeconds, endSilenceTimeoutSeconds, speechLanguage, isAsync: true);
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
        public Task<IActionResult> RecognizeSpeech(
            string callConnectionId,
            string target,
            int initialSilenceTimeoutSeconds = 15,
            int endSilenceTimeoutSeconds = 5,
            string speechLanguage = "en-US")
        {
            return RecognizeSpeechInternal(callConnectionId, target, initialSilenceTimeoutSeconds, endSilenceTimeoutSeconds, speechLanguage, isAsync: false);
        }

        /// <summary>
        /// Starts speech or DTMF recognition on a call (Async)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="target">Target participant to recognize from</param>
        /// <param name="maxTonesToCollect">Maximum number of DTMF tones to collect</param>
        [HttpPost("recognizeSpeechOrDtmfAsync")]
        [Tags("AI - Speech Recognition")]
        public Task<IActionResult> RecognizeSpeechOrDtmfAsync(
            string callConnectionId,
            string target,
            int maxTonesToCollect = 4)
        {
            return RecognizeSpeechOrDtmfInternal(callConnectionId, target, maxTonesToCollect, isAsync: true);
        }

        /// <summary>
        /// Starts speech or DTMF recognition on a call (Sync)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="target">Target participant to recognize from</param>
        /// <param name="maxTonesToCollect">Maximum number of DTMF tones to collect</param>
        [HttpPost("recognizeSpeechOrDtmf")]
        [Tags("AI - Speech Recognition")]
        public Task<IActionResult> RecognizeSpeechOrDtmf(
            string callConnectionId,
            string target,
            int maxTonesToCollect = 4)
        {
            return RecognizeSpeechOrDtmfInternal(callConnectionId, target, maxTonesToCollect, isAsync: false);
        }

        /// <summary>
        /// Starts choice-based recognition (voice commands) on a call (Async)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="target">Target participant to recognize from</param>
        [HttpPost("recognizeChoiceAsync")]
        [Tags("AI - Speech Recognition")]
        public Task<IActionResult> RecognizeChoiceAsync(
            string callConnectionId,
            string target)
        {
            return RecognizeChoiceInternal(callConnectionId, target, isAsync: true);
        }

        /// <summary>
        /// Starts choice-based recognition (voice commands) on a call (Sync)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="target">Target participant to recognize from</param>
        [HttpPost("recognizeChoice")]
        [Tags("AI - Speech Recognition")]
        public Task<IActionResult> RecognizeChoice(
            string callConnectionId,
            string target)
        {
            return RecognizeChoiceInternal(callConnectionId, target, isAsync: false);
        }

        /// <summary>
        /// Starts advanced speech recognition with custom vocabulary (requires Cognitive Services) (Async)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="target">Target participant to recognize from</param>
        /// <param name="promptText">Text prompt to play</param>
        /// <param name="speechLanguage">Speech recognition language (default: en-US)</param>
        [HttpPost("recognizeSpeechAdvancedAsync")]
        [Tags("AI - Speech Recognition")]
        public Task<IActionResult> RecognizeSpeechAdvancedAsync(
            string callConnectionId,
            string target,
            string promptText = "Please tell me your request.",
            string speechLanguage = "en-US")
        {
            return RecognizeSpeechAdvancedInternal(callConnectionId, target, promptText, speechLanguage, isAsync: true);
        }

        /// <summary>
        /// Starts advanced speech recognition with custom vocabulary (requires Cognitive Services) (Sync)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="target">Target participant to recognize from</param>
        /// <param name="promptText">Text prompt to play</param>
        /// <param name="speechLanguage">Speech recognition language (default: en-US)</param>
        [HttpPost("recognizeSpeechAdvanced")]
        [Tags("AI - Speech Recognition")]
        public Task<IActionResult> RecognizeSpeechAdvanced(
            string callConnectionId,
            string target,
            string promptText = "Please tell me your request.",
            string speechLanguage = "en-US")
        {
            return RecognizeSpeechAdvancedInternal(callConnectionId, target, promptText, speechLanguage, isAsync: false);
        }

        #endregion

        #region CONTINUOUS DTMF RECOGNITION
        /// <summary>
        /// Starts advanced choice recognition with speech phrases enabled (requires Cognitive Services) (Async)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="target">Target participant to recognize from</param>
        /// <param name="promptText">Text prompt to play (optional)</param>
        [HttpPost("recognizeChoiceWithSpeechAsync")]
        [Tags("AI - Recognition")]
        public Task<IActionResult> RecognizeChoiceWithSpeechAsync(
            string callConnectionId,
            string target,
            string promptText = "Hi, this is recognize test. Please say yes or no, or press 1 for yes and 2 for no.")
        {
            return RecognizeChoiceWithSpeechInternal(callConnectionId, target, promptText, isAsync: true);
        }

        /// <summary>
        /// Starts advanced choice recognition with speech phrases enabled (requires Cognitive Services) (Sync)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="target">Target participant to recognize from</param>
        /// <param name="promptText">Text prompt to play (optional)</param>
        [HttpPost("recognizeChoiceWithSpeech")]
        [Tags("AI - Recognition")]
        public Task<IActionResult> RecognizeChoiceWithSpeech(
            string callConnectionId,
            string target,
            string promptText = "Hi, this is recognize test. Please say yes or no, or press 1 for yes and 2 for no.")
        {
            return RecognizeChoiceWithSpeechInternal(callConnectionId, target, promptText, isAsync: false);
        }

        /// <summary>
        /// Starts continuous DTMF recognition on a call (Async)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="target">Target participant to recognize from</param>
        [HttpPost("startContinuousDtmfRecognitionAsync")]
        [Tags("AI - Recognition")]
        public Task<IActionResult> StartContinuousDtmfRecognitionAsync(
            string callConnectionId,
            string target)
        {
            return HandleContinuousDtmfInternal(callConnectionId, target, start: true, isAsync: true);
        }

        /// <summary>
        /// Starts continuous DTMF recognition on a call (Sync)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        [HttpPost("startContinuousDtmfRecognition")]
        [Tags("AI - Recognition")]
        public Task<IActionResult> StartContinuousDtmfRecognition(
            string callConnectionId,
            string target)
        {
            return HandleContinuousDtmfInternal(callConnectionId, target, start: true, isAsync: false);
        }

        /// <summary>
        /// Stops continuous DTMF recognition on a call (Async)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="target">Target participant to stop recognition for</param>
        [HttpPost("stopContinuousDtmfRecognitionAsync")]
        [Tags("AI - Recognition")]
        public Task<IActionResult> StopContinuousDtmfRecognitionAsync(
            string callConnectionId,
            string target)
        {
            return HandleContinuousDtmfInternal(callConnectionId, target, start: false, isAsync: true);
        }

        /// <summary>
        /// Stops continuous DTMF recognition on a call (Sync)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="target">Target participant to stop recognition for</param>
        [HttpPost("stopContinuousDtmfRecognition")]
        [Tags("AI - Recognition")]
        public Task<IActionResult> StopContinuousDtmfRecognition(
            string callConnectionId,
            string target)
        {
            return HandleContinuousDtmfInternal(callConnectionId, target, start: false, isAsync: false);
        }
        #endregion

        #region Play Text Source Media SOURCE ENDPOINTS
        /// <summary>
        /// Plays text-to-speech to a specific target using TextSource (Async)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="target">Target participant</param>
        /// <param name="text">Text to convert to speech</param>
        /// <param name="voiceName">Voice name (default: en-US-NancyNeural)</param>
        [HttpPost("playTextToTargetAsync")]
        [Tags("AI - Play Text Source Media")]
        public Task<IActionResult> playTextSourceToTargetAsync(
            string callConnectionId,
            string target,
            string text,
            string voiceName = "en-US-NancyNeural")
        {
            return PlayTextToTargetInternal(callConnectionId, target, text, voiceName, isAsync: true);
        }

        /// <summary>
        /// Plays text-to-speech to a specific target using TextSource (Sync)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="target">Target participant</param>
        /// <param name="text">Text to convert to speech</param>
        /// <param name="voiceName">Voice name (default: en-US-NancyNeural)</param>
        [HttpPost("playTextToTarget")]
        [Tags("AI - Play Text Source Media")]
        public Task<IActionResult> PlayTextToSpeech(
            string callConnectionId,
            string target,
            string text,
            string voiceName = "en-US-NancyNeural")
        {
            return PlayTextToTargetInternal(callConnectionId, target, text, voiceName, isAsync: false);
        }

        /// <summary>
        /// Plays SSML (Speech Synthesis Markup Language) to a specific target (Async)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="target">Target participant</param>
        /// <param name="ssml">SSML content</param>
        [HttpPost("playSsmlAsync")]
        [Tags("AI - Play SSML Source Media")]
        public Task<IActionResult> PlaySsmlAsync(
            string callConnectionId,
            string target,
            string ssml= "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"><voice name=\"en-US-JennyNeural\">Hi, this is ssml test played through ssml source thanks. Goodbye!</voice></speak>")
        {
            return PlaySsmlInternal(callConnectionId, target, ssml, isAsync: true);
        }

        /// <summary>
        /// Plays SSML (Speech Synthesis Markup Language) to a specific target (Sync)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="target">Target participant</param>
        /// <param name="ssml">SSML content</param>
        [HttpPost("playSsml")]
        [Tags("AI - Play SSML Source Media")]
        public Task<IActionResult> PlaySsml(
            string callConnectionId,
            string target,
            string ssml= "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"><voice name=\"en-US-JennyNeural\">Hi, this is ssml test played through ssml source thanks. Goodbye!</voice></speak>")
        {
            return PlaySsmlInternal(callConnectionId, target, ssml, isAsync: false);
        }

        /// <summary>
        /// Plays text-to-speech to all participants (Async)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="text">Text to convert to speech</param>
        /// <param name="voiceName">Voice name (default: en-US-NancyNeural)</param>
        [HttpPost("playTextToAllAsync")]
        [Tags("AI - Play Text Source Media")]
        public Task<IActionResult> PlayTextToSpeechToAllAsync(
            string callConnectionId,
            string text,
            string voiceName = "en-US-NancyNeural")
        {
            return PlayTextToAllInternal(callConnectionId, text, voiceName, isAsync: true);
        }

        /// <summary>
        /// Plays text-to-speech to all participants (Sync)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="text">Text to convert to speech</param>
        /// <param name="voiceName">Voice name (default: en-US-NancyNeural)</param>
        [HttpPost("playTextToAll")]
        [Tags("AI - Play Text Source Media")]
        public Task<IActionResult> PlayTextToSpeechToAll(
            string callConnectionId,
            string text,
            string voiceName = "en-US-NancyNeural")
        {
            return PlayTextToAllInternal(callConnectionId, text, voiceName, isAsync: false);
        }

        /// <summary>
        /// Plays text-to-speech with barge-in capability (interrupts current audio) (Async)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="text">Text to convert to speech</param>
        /// <param name="voiceName">Voice name (default: en-US-NancyNeural)</param>
        [HttpPost("playTextBargeInAsync")]
        [Tags("AI - Play Text Source Media")]
        public Task<IActionResult> PlayTextToSpeechBargeInAsync(
            string callConnectionId,
            string text = "Hi, this is barge in test played through text source. Goodbye!",
            string voiceName = "en-US-NancyNeural")
        {
            return PlayTextBargeInInternal(callConnectionId, text, voiceName, isAsync: true);
        }

        /// <summary>
        /// Plays SSML to all participants (Async)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="ssml">SSML content</param>
        [HttpPost("playSsmlToAllAsync")]
        [Tags("AI - Play SSML Source Media")]
        public Task<IActionResult> PlaySsmlToAllAsync(
            string callConnectionId,
            string ssml = "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"><voice name=\"en-US-JennyNeural\">Hi, this is ssml test played through ssml source thanks. Goodbye!</voice></speak>")
        {
            return PlaySsmlToAllInternal(callConnectionId, ssml, isAsync: true);
        }

        /// <summary>
        /// Plays SSML to all participants (Sync)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="ssml">SSML content</param>
        [HttpPost("playSsmlToAll")]
        [Tags("AI - Play SSML Source Media")]
        public Task<IActionResult> PlaySsmlToAll(
            string callConnectionId,
            string ssml = "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"><voice name=\"en-US-JennyNeural\">Hi, this is ssml test played through ssml source thanks. Goodbye!</voice></speak>")
        {
            return PlaySsmlToAllInternal(callConnectionId, ssml, isAsync: false);
        }

        /// <summary>
        /// Plays SSML with barge-in capability (interrupts current audio) (Async)
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="ssml">SSML content</param>
        [HttpPost("playSsmlBargeInAsync")]
        [Tags("AI - Play SSML Source Media")]
        public Task<IActionResult> PlaySsmlBargeInAsync(
            string callConnectionId,
            string ssml = "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"><voice name=\"en-US-JennyNeural\">Hi, this is ssml barge in test played through ssml source. Goodbye!</voice></speak>")
        {
            return PlaySsmlBargeInInternal(callConnectionId, ssml, isAsync: true);
        }

        #endregion

        #region INTERNAL / PRIVATE METHODS
        private async Task<IActionResult> CreateCallWithAIFeaturesInternal(
            string target,
            string locale,
            bool enableTranscription,
            bool enableCallIntelligence,
            bool isAsync)
        {
            if (string.IsNullOrWhiteSpace(target))
                return BadRequest("Target is required");

            if (!target.StartsWith("8:") && !target.StartsWith("+"))
                return BadRequest("PSTN number must include country code (e.g., +1 for US)");

            if (enableCallIntelligence)
            {
                if (string.IsNullOrWhiteSpace(_config.CognitiveServiceEndpoint))
                    return BadRequest("CognitiveServiceEndpoint must be configured in appsettings.json to use Call Intelligence features");

                // Validate the Cognitive Services endpoint format
                if (!Uri.TryCreate(_config.CognitiveServiceEndpoint, UriKind.Absolute, out Uri cognitiveUri))
                    return BadRequest($"Invalid CognitiveServiceEndpoint format: {_config.CognitiveServiceEndpoint}");

                _logger.LogInformation($"Using Cognitive Services Endpoint: {_config.CognitiveServiceEndpoint}");
            }

            var featureDescription = enableCallIntelligence ? "Call Intelligence" : "transcription";
            _logger.LogInformation($"Creating call with {featureDescription}. Target={target}, Locale={locale}, TranscriptionEnabled={enableTranscription}");

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

                var createCallOptions = new CreateCallOptions(callInvite, callbackUri);

                if (enableCallIntelligence)
                {
                    var cognitiveEndpointUri = new Uri(_config.CognitiveServiceEndpoint);
                    _logger.LogInformation($"Setting up CallIntelligenceOptions with endpoint: {cognitiveEndpointUri}");
                    
                    createCallOptions.CallIntelligenceOptions = new CallIntelligenceOptions
                    {
                        CognitiveServicesEndpoint = cognitiveEndpointUri
                    };
                    
                    _logger.LogInformation("CallIntelligenceOptions configured successfully");
                }

                if (enableTranscription)
                {
                    var transcriptionOptions = new TranscriptionOptions(
                        new Uri(websocketUri),
                        locale,
                        enableTranscription,
                        TranscriptionTransport.Websocket);
                    createCallOptions.TranscriptionOptions = transcriptionOptions;
                }

                CreateCallResult result;
                if (isAsync)
                {
                    result = await _service.GetCallAutomationClient().CreateCallAsync(createCallOptions);
                }
                else
                {
                    result = _service.GetCallAutomationClient().CreateCall(createCallOptions);
                }

                var props = result.CallConnectionProperties;
                _logger.LogInformation($"Call created with {featureDescription}. CallConnectionId={props.CallConnectionId}");

                var response = new
                {
                    CallConnectionId = props.CallConnectionId,
                    CorrelationId = props.CorrelationId,
                    Status = props.CallConnectionState.ToString(),
                    TranscriptionEnabled = enableTranscription
                };

                if (enableCallIntelligence)
                {
                    return Ok(new
                    {
                        response.CallConnectionId,
                        response.CorrelationId,
                        response.Status,
                        CallIntelligenceEnabled = true,
                        response.TranscriptionEnabled,
                        CognitiveServicesEndpoint = _config.CognitiveServiceEndpoint
                    });
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating call with {featureDescription}. Target={target}, CognitiveEndpoint={_config.CognitiveServiceEndpoint}");
                
                var errorDetails = new
                {
                    ErrorMessage = ex.Message,
                    Target = target,
                    FeatureDescription = featureDescription,
                    CognitiveEndpoint = enableCallIntelligence ? _config.CognitiveServiceEndpoint : "N/A",
                    InnerException = ex.InnerException?.Message,
                    StackTrace = ex.StackTrace
                };
                
                _logger.LogError($"Detailed error info: {System.Text.Json.JsonSerializer.Serialize(errorDetails)}");
                return Problem($"Failed to create call with {featureDescription}: {ex.Message}");
            }
        }

        private async Task<IActionResult> StartTranscriptionInternal(string callConnectionId, string locale, bool isAsync)
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
                    if (isAsync)
                    {
                        await callMedia.StartTranscriptionAsync();
                    }
                    else
                    {
                        callMedia.StartTranscription();
                    }
                }
                else
                {
                    var options = new StartTranscriptionOptions
                    {
                        Locale = locale,
                        OperationContext = "StartTranscriptionContext"
                    };

                    if (isAsync)
                    {
                        await callMedia.StartTranscriptionAsync(options);
                    }
                    else
                    {
                        callMedia.StartTranscription(options);
                    }
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
        private async Task<IActionResult> StopTranscriptionInternal(string callConnectionId, bool isAsync)
        {
            if (string.IsNullOrWhiteSpace(callConnectionId))
                return BadRequest("CallConnectionId is required");

            _logger.LogInformation($"Stopping transcription. CallId={callConnectionId}");

            try
            {
                var callMedia = _service.GetCallMedia(callConnectionId);
                var props = _service.GetCallConnectionProperties(callConnectionId);

                if (isAsync)
                {
                    await callMedia.StopTranscriptionAsync();
                }
                else
                {
                    callMedia.StopTranscription();
                }

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
        private async Task<IActionResult> UpdateTranscriptionInternal(string callConnectionId, string locale, bool isAsync)
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

                if (isAsync)
                {
                    await callMedia.UpdateTranscriptionAsync(locale);
                }
                else
                {
                    callMedia.UpdateTranscription(locale);
                }

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
        private async Task<IActionResult> HandleContinuousDtmfInternal(
            string callConnectionId,
            string target,
            bool start,
            bool isAsync)
        {
            if (string.IsNullOrWhiteSpace(callConnectionId))
                return BadRequest("CallConnectionId is required");

            if (string.IsNullOrWhiteSpace(target))
                return BadRequest("Target is required");

            if (!target.StartsWith("8:") && !target.StartsWith("+"))
                return BadRequest("PSTN number must include country code (e.g., +1 for US)");

            var action = start ? "Starting" : "Stopping";
            _logger.LogInformation($"{action} continuous DTMF recognition. CallId={callConnectionId}, Target={target}");

            try
            {
                var callMedia = _service.GetCallMedia(callConnectionId);
                var props = _service.GetCallConnectionProperties(callConnectionId);

                CommunicationIdentifier identifier = target.StartsWith("8:")
                    ? new CommunicationUserIdentifier(target)
                    : new PhoneNumberIdentifier(target);

                if (start)
                {
                    if (isAsync)
                        await callMedia.StartContinuousDtmfRecognitionAsync(identifier);
                    else
                        callMedia.StartContinuousDtmfRecognition(identifier);
                }
                else
                {
                    if (isAsync)
                        await callMedia.StopContinuousDtmfRecognitionAsync(identifier);
                    else
                        callMedia.StopContinuousDtmfRecognition(identifier);
                }

                var status = start ? "ContinuousDtmfRecognitionStarted" : "ContinuousDtmfRecognitionStopped";
                _logger.LogInformation($"Continuous DTMF recognition {(start ? "started" : "stopped")} successfully");

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = props.CorrelationId,
                    Status = status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error {action.ToLower()} continuous DTMF recognition");
                return Problem($"Failed to {action.ToLower()} continuous DTMF recognition: {ex.Message}");
            }
        }

        private async Task<IActionResult> RecognizeSpeechInternal(
            string callConnectionId,
            string target,
            int initialSilenceTimeoutSeconds,
            int endSilenceTimeoutSeconds,
            string speechLanguage,
            bool isAsync)
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

                if (isAsync)
                {
                    await callMedia.StartRecognizingAsync(speechOptions);
                }
                else
                {
                    callMedia.StartRecognizing(speechOptions);
                }

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

        private async Task<IActionResult> RecognizeSpeechOrDtmfInternal(
            string callConnectionId,
            string target,
            int maxTonesToCollect,
            bool isAsync)
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

                if (isAsync)
                {
                    await callMedia.StartRecognizingAsync(recognizeOptions);
                }
                else
                {
                    callMedia.StartRecognizing(recognizeOptions);
                }

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

        private async Task<IActionResult> RecognizeChoiceInternal(
            string callConnectionId,
            string target,
            bool isAsync)
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

                if (isAsync)
                {
                    await callMedia.StartRecognizingAsync(recognizeOptions);
                }
                else
                {
                    callMedia.StartRecognizing(recognizeOptions);
                }

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

        private async Task<IActionResult> RecognizeSpeechAdvancedInternal(
            string callConnectionId,
            string target,
            string promptText,
            string speechLanguage,
            bool isAsync)
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

                if (isAsync)
                {
                    await callMedia.StartRecognizingAsync(speechOptions);
                }
                else
                {
                    callMedia.StartRecognizing(speechOptions);
                }

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

        private async Task<IActionResult> RecognizeChoiceWithSpeechInternal(
            string callConnectionId,
            string target,
            string promptText,
            bool isAsync)
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

                if (isAsync)
                {
                    await callMedia.StartRecognizingAsync(recognizeOptions);
                }
                else
                {
                    callMedia.StartRecognizing(recognizeOptions);
                }

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

        private async Task<IActionResult> PlayTextToTargetInternal(
            string callConnectionId,
            string target,
            string text,
            string voiceName,
            bool isAsync)
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

                if (isAsync)
                {
                    await callMedia.PlayAsync(playOptions);
                }
                else
                {
                    callMedia.Play(playOptions);
                }

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

        private async Task<IActionResult> PlaySsmlInternal(
            string callConnectionId,
            string target,
            string ssml,
            bool isAsync)
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

                if (isAsync)
                {
                    await callMedia.PlayAsync(playOptions);
                }
                else
                {
                    callMedia.Play(playOptions);
                }

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

        private async Task<IActionResult> PlayTextToAllInternal(
            string callConnectionId,
            string text,
            string voiceName,
            bool isAsync)
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

                var playToAllOptions = new PlayToAllOptions(textSource)
                {
                    OperationContext = "TextToSpeechToAllContext"
                };

                if (isAsync)
                {
                    await callMedia.PlayToAllAsync(playToAllOptions);
                }
                else
                {
                    callMedia.PlayToAll(playToAllOptions);
                }

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

        private async Task<IActionResult> PlayTextBargeInInternal(
            string callConnectionId,
            string text,
            string voiceName,
            bool isAsync)
        {
            if (string.IsNullOrWhiteSpace(callConnectionId))
                return BadRequest("CallConnectionId is required");

            if (string.IsNullOrWhiteSpace(text))
                return BadRequest("Text is required");

            _logger.LogInformation($"Playing text-to-speech with barge-in. CallId={callConnectionId}, Voice={voiceName}");

            try
            {
                var callMedia = _service.GetCallMedia(callConnectionId);
                var props = _service.GetCallConnectionProperties(callConnectionId);

                var textSource = new TextSource(text)
                {
                    VoiceName = voiceName
                };

                var playToAllOptions = new PlayToAllOptions(textSource)
                {
                    OperationContext = "TextToSpeechBargeInContext",
                    InterruptCallMediaOperation = true
                };

                if (isAsync)
                {
                    await callMedia.PlayToAllAsync(playToAllOptions);
                }
                else
                {
                    callMedia.PlayToAll(playToAllOptions);
                }

                _logger.LogInformation("Text-to-speech barge-in started successfully");
                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = props.CorrelationId,
                    Status = "TextToSpeechBargeInStarted"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error playing text-to-speech with barge-in");
                return Problem($"Failed to play text-to-speech with barge-in: {ex.Message}");
            }
        }

        private async Task<IActionResult> PlaySsmlToAllInternal(
            string callConnectionId,
            string ssml,
            bool isAsync)
        {
            if (string.IsNullOrWhiteSpace(callConnectionId))
                return BadRequest("CallConnectionId is required");

            if (string.IsNullOrWhiteSpace(ssml))
                return BadRequest("SSML is required");

            _logger.LogInformation($"Playing SSML to all. CallId={callConnectionId}");

            try
            {
                var callMedia = _service.GetCallMedia(callConnectionId);
                var props = _service.GetCallConnectionProperties(callConnectionId);

                var ssmlSource = new SsmlSource(ssml);

                var playToAllOptions = new PlayToAllOptions(ssmlSource)
                {
                    OperationContext = "SsmlToAllContext"
                };

                if (isAsync)
                {
                    await callMedia.PlayToAllAsync(playToAllOptions);
                }
                else
                {
                    callMedia.PlayToAll(playToAllOptions);
                }

                _logger.LogInformation("SSML to all started successfully");
                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = props.CorrelationId,
                    Status = "SsmlToAllStarted"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error playing SSML to all");
                return Problem($"Failed to play SSML to all: {ex.Message}");
            }
        }

        private async Task<IActionResult> PlaySsmlBargeInInternal(
            string callConnectionId,
            string ssml,
            bool isAsync)
        {
            if (string.IsNullOrWhiteSpace(callConnectionId))
                return BadRequest("CallConnectionId is required");

            if (string.IsNullOrWhiteSpace(ssml))
                return BadRequest("SSML is required");

            _logger.LogInformation($"Playing SSML with barge-in. CallId={callConnectionId}");

            try
            {
                var callMedia = _service.GetCallMedia(callConnectionId);
                var props = _service.GetCallConnectionProperties(callConnectionId);

                var ssmlSource = new SsmlSource(ssml);

                var playToAllOptions = new PlayToAllOptions(ssmlSource)
                {
                    OperationContext = "SsmlBargeInContext",
                    InterruptCallMediaOperation = true
                };

                if (isAsync)
                {
                    await callMedia.PlayToAllAsync(playToAllOptions);
                }
                else
                {
                    callMedia.PlayToAll(playToAllOptions);
                }

                _logger.LogInformation("SSML barge-in started successfully");
                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = props.CorrelationId,
                    Status = "SsmlBargeInStarted"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error playing SSML with barge-in");
                return Problem($"Failed to play SSML with barge-in: {ex.Message}");
            }
        }
        #endregion
    }
}