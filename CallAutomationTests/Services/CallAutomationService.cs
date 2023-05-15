// © Microsoft Corporation. All rights reserved.

using Azure;
using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using CallAutomation.Scenarios.Handlers;
using CallAutomation.Scenarios.Interfaces;
using CallAutomation.Scenarios.Utils;
using Newtonsoft.Json;

namespace CallAutomation.Scenarios.Services
{
    public class CallAutomationService : ICallAutomationService
    {
        public static string recFileFormat;
        private readonly ILogger<CallAutomationService> _logger;
        private readonly CallAutomationClient _client;
        private readonly IConfiguration _configuration;



        public CallAutomationService(ILogger<CallAutomationService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _client = new CallAutomationClient(configuration.GetConnectionString("ACS"));
        }

        public async Task<AnswerCallResult> AnswerCallAsync(IncomingCallEvent incomingCallEvent)
        {
            _logger.LogInformation("AnswerCallAsync called");

            try
            {
                var callerId = Uri.EscapeDataString(incomingCallEvent.From.RawId.ToString());
                //var authKey = $"{AuthenticationConstants.EventGridAuthenticationQueryParameterName}={_configuration.GetConnectionString(AuthenticationConstants.EventGridSecretName)}";
                var callbackUri = new Uri($"{_configuration["BaseUri"]}/callbacks/{Guid.NewGuid()}?callerId={callerId}");
                var answerCallOptions = new AnswerCallOptions(incomingCallEvent.IncomingCallContext, callbackUri)
                {
                    AzureCognitiveServicesEndpointUrl = new Uri(_configuration["CognitiveServicesEndpointUri"])
                };

                //var ivrConfig = GetIvrConfig();
                //if (ivrConfig.GetValue<bool>("UseNlu"))
                //{
                //    _logger.LogInformation("Setting MediaStreamingOptions for incoming call");

                //    var streamUri = new Uri($"{ivrConfig["StreamingUri"]}/streams/{Guid.NewGuid()}?callerId={callerId}&{authKey}");
                //    answerCallOptions.MediaStreamingOptions = new MediaStreamingOptions(streamUri, MediaStreamingTransport.Websocket, MediaStreamingContent.Audio, MediaStreamingAudioChannel.Mixed);
                //}

                return await _client.AnswerCallAsync(answerCallOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError($"AnswerCallAsync failed unexpectedly: {ex}");

                throw;
            }
        }

        public async Task<CreateCallResult> CreateCallAsync(String targetId)
        {
            _logger.LogInformation($"CreateCallAsync called at target {targetId}");

            try
            {
                var callbackUri = new Uri($"{_configuration["BaseUri"]}/callbacks/{Guid.NewGuid()}?callerId={targetId}");
                var target = targetId.StartsWith("8:acs") ? new CallInvite(new CommunicationUserIdentifier(targetId))
                    : new CallInvite(new PhoneNumberIdentifier($"+{targetId.SanitizePhoneNumber()}"),
                            new PhoneNumberIdentifier(_configuration[""]));

                var createCallOptions = new CreateCallOptions(target, callbackUri)
                {
                    AzureCognitiveServicesEndpointUrl = new Uri(_configuration["CognitiveServicesEndpointUri"]),
                    OperationContext = Constants.OperationContext.ScheduledCallbackDialout
                };

                return await _client.CreateCallAsync(createCallOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError($"CreateCallAsync failed unexpectedly: {ex}");

                throw;
            }
        }

        public CallConnection GetCallConnection(string callConnectionId)
        {
            _logger.LogInformation($"GetCallConnection called for connection ID '{callConnectionId}'");

            try
            {
                return _client.GetCallConnection(callConnectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetCallConnection failed unexpectedly: {ex}");

                throw;
            }
        }

        public async Task StartRecognizingDtmfAsync(string callerId, string operationContext, CallMedia callMedia, string textToSpeechLocale, int initialSilenceTimeout, string? prerollText = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"StartRecognizingDtmfAsync called for OperationContext '{operationContext}'");

            var ivrText = GetIvrText();
            var ivrConfig = GetIvrConfig();

            string? textToSpeech;
            var allowInterrupt = true;

            // Start recognize prompt
            switch (operationContext)
            {
                case Constants.OperationContext.AccountIdValidation:
                    // TTS for speech rec or DTMF only?
                    var ivrTextKey = ivrConfig.GetValue<bool>("UseNlu") ? Constants.OperationContext.AccountIdValidation : "AccountIdDtmfValidation";
                    textToSpeech = string.IsNullOrWhiteSpace(prerollText)
                        ? ivrText[ivrTextKey]
                        : $"{prerollText} {ivrText[ivrTextKey]}";
                    await StartRecognizingDtmfAsync(callMedia, textToSpeech, textToSpeechLocale, maxTonesToCollect: 6, initialSilenceTimeout, operationContext, callerId, allowInterrupt, cancellationToken);
                    break;
                case Constants.OperationContext.AiPairing:
                    textToSpeech = string.IsNullOrWhiteSpace(prerollText)
                        ? ivrText["QueryCustomerPrompt"]
                        : $"{prerollText} {ivrText["QueryCustomerPrompt"]}";
                    await StartRecognizingDtmfAsync(callMedia, textToSpeech, textToSpeechLocale, maxTonesToCollect: 1, initialSilenceTimeout, operationContext, callerId, allowInterrupt, cancellationToken);
                    break;
                default:
                    _logger.LogWarning($"StartRecognizingDtmfAsync called with an unsupported OperationContext '{operationContext}'");
                    break;
            }
        }

        public async Task PlayMenuOptionsAsync(string callerId, CallMedia callMedia, string textToSpeechLocale, string? prerollText = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("PlayMenuOptionsAsync called");

            var ivrText = GetIvrText();
            var textToSpeech = string.IsNullOrWhiteSpace(prerollText)
                ? ivrText[Constants.OperationContext.MainMenu]
                : $"{prerollText} {ivrText[Constants.OperationContext.MainMenu]}";

            var ivrConfig = GetIvrConfig();
            if (ivrConfig.GetValue<bool>("UseCustomPhraseRecognition"))
            {
                // recognize 1-digit DTMF input
                await StartRecognizingDtmfAsync(callMedia, textToSpeech, textToSpeechLocale, maxTonesToCollect: 1, ivrConfig.GetValue<int>("MainMenuDtmfTimeout"), Constants.OperationContext.MainMenu, callerId, allowInterrupt: true, cancellationToken);
            }
            else
            {
                var choices = new List<RecognizeChoice>
                {
                    new(QueueConstants.MagentaHome, GetRecognizedPhrases("Queues:HomeServiceQueue:RecognizePhrases"))
                    {
                        Tone = DtmfTone.One
                    },
                    new(QueueConstants.MagentaMobile, GetRecognizedPhrases("Queues:MobileServiceQueue:RecognizePhrases"))
                    {
                        Tone = DtmfTone.Two
                    },
                    new(QueueConstants.MagentaTV, GetRecognizedPhrases("Queues:TvServiceQueue:RecognizePhrases"))
                    {
                        Tone = DtmfTone.Three
                    },
                    new(Constants.OperationContext.EndCall, GetRecognizedPhrases("Ivr:EndCallRecognizePhrases"))
                    {
                        Tone = DtmfTone.Four
                    },
                    // queue directly to all agents
                    new(QueueConstants.MagentaDefault, GetRecognizedPhrases("Ivr:OperatorRecognizePhrases"))
                    {
                        Tone = DtmfTone.Zero
                    }
                };

                // recognize speech or 1-digit DTMF input
                await StartRecognizingChoiceAsync(callMedia, textToSpeech, textToSpeechLocale, choices, Constants.OperationContext.MainMenu, callerId, cancellationToken);
            }
        }

        public async Task PlayMenuChoiceAsync(DtmfTone choiceOrTone, CallMedia callMedia, string textToSpeechLocale, string? prerollText = null)
        {
            _logger.LogInformation($"PlayMenuChoiceAsync called with '{choiceOrTone}'");

            var ivrText = GetIvrText();

            string textToSpeech;
            string operationContext;

            if (choiceOrTone == DtmfTone.One || choiceOrTone.ToString().Equals(QueueConstants.MagentaHome, StringComparison.OrdinalIgnoreCase))
            {
                operationContext = QueueConstants.MagentaHome;
                textToSpeech = string.IsNullOrWhiteSpace(prerollText)
                    ? ivrText[QueueConstants.MagentaHome]
                    : $"{prerollText} {ivrText[QueueConstants.MagentaHome]}";
            }
            else if (choiceOrTone == DtmfTone.Two || choiceOrTone.ToString().Equals(QueueConstants.MagentaMobile, StringComparison.OrdinalIgnoreCase))
            {
                operationContext = QueueConstants.MagentaMobile;
                textToSpeech = string.IsNullOrWhiteSpace(prerollText)
                    ? ivrText[QueueConstants.MagentaMobile]
                    : $"{prerollText} {ivrText[QueueConstants.MagentaMobile]}";
            }
            else if (choiceOrTone == DtmfTone.Three || choiceOrTone.ToString().Equals(QueueConstants.MagentaTV, StringComparison.OrdinalIgnoreCase))
            {
                operationContext = QueueConstants.MagentaTV;
                textToSpeech = string.IsNullOrWhiteSpace(prerollText)
                    ? ivrText[QueueConstants.MagentaTV]
                    : $"{prerollText} {ivrText[QueueConstants.MagentaTV]}";
            }
            else if (choiceOrTone == DtmfTone.Four || choiceOrTone.ToString().Equals(Constants.OperationContext.EndCall, StringComparison.OrdinalIgnoreCase))
            {
                operationContext = Constants.OperationContext.EndCall;
                textToSpeech = string.IsNullOrWhiteSpace(prerollText)
                    ? ivrText[Constants.OperationContext.EndCall]
                    : $"{prerollText} {ivrText[Constants.OperationContext.EndCall]}";
            }
            else
            {
                // unknown choice, queue to all agents
                _logger.LogInformation("PlayMenuChoiceAsync defaulting to all agents queue");
                textToSpeech = string.IsNullOrWhiteSpace(prerollText)
                    ? ivrText[$"{QueueConstants.MagentaDefault}"]
                    : $"{prerollText} {ivrText[QueueConstants.MagentaDefault]}";
                operationContext = QueueConstants.MagentaDefault;
            }

            await PlayTextToSpeechToAllAsync(callMedia, textToSpeech, operationContext, textToSpeechLocale);
        }

        public async Task PlayCallbackOfferOptionsAsync(string callerId, CallMedia callMedia, string textToSpeechLocale, string? prerollText = null)
        {
            _logger.LogInformation($"PlayCallbackOfferOptionsAsync called with '{callerId}'");

            var choices = new List<RecognizeChoice>
            {
                new(Constants.IvrCallbackChoices.Yes, GetRecognizedPhrases("Ivr:RecognizePhrasesYes"))
                {
                    Tone = DtmfTone.One
                },
                new(Constants.IvrCallbackChoices.No, GetRecognizedPhrases("Ivr:RecognizePhrasesNo"))
                {
                    Tone = DtmfTone.Two
                }
            };

            var ivrText = GetIvrText();
            var textToSpeech = string.IsNullOrWhiteSpace(prerollText)
                ? ivrText[Constants.IvrTextKeys.ScheduledCallbackOffered]
                : $"{prerollText} {ivrText[Constants.IvrTextKeys.ScheduledCallbackOffered]}";

            // Start recognize prompt - play audio and recognize 1-digit DTMF input
            await StartRecognizingChoiceAsync(callMedia, textToSpeech, textToSpeechLocale, choices, Constants.OperationContext.ScheduledCallbackOffer, callerId);
        }

        public async Task<bool> PlayCallbackOfferChoiceAsync(string callerId, DtmfTone choiceOrTone, CallMedia callMedia, string textToSpeechLocale)
        {
            _logger.LogInformation($"PlayCallbackOfferChoiceAsync called with '{choiceOrTone}'");

            var ivrText = GetIvrText();

            if (choiceOrTone == DtmfTone.One || choiceOrTone.ToString().Equals(Constants.IvrCallbackChoices.Yes, StringComparison.OrdinalIgnoreCase))
            {
                await PlayCallbackTimeSelectionOptionsAsync(callerId, callMedia, textToSpeechLocale);
                return true;
            }
            else
            {
                await PlayTextToSpeechToAllAsync(callMedia, ivrText[Constants.IvrTextKeys.ScheduledCallbackRejected],
                    Constants.OperationContext.ScheduledCallbackRejected, textToSpeechLocale);
                return false;
            }
        }

        public async Task PlayCallbackTimeSelectionOptionsAsync(string callerId, CallMedia callMedia, string textToSpeechLocale, string? prerollText = null)
        {
            _logger.LogInformation($"PlayCallbackTimeSelectionOptionsAsync called with '{callerId}'");

            var choices = new List<RecognizeChoice>
            {
                new(Constants.IvrCallbackChoices.One, new [] { "One" })
                {
                    Tone = DtmfTone.One
                },
                new(Constants.IvrCallbackChoices.Two, new [] { "Two" } )
                {
                    Tone = DtmfTone.Two
                },
                new(Constants.IvrCallbackChoices.Three, new [] { "Three" } )
                {
                    Tone = DtmfTone.Three
                }
            };

            var timeWindow1Minutes = _configuration.GetValue<int>("ScheduledCallbacks:TimeWindow1Minutes");
            var timeWindow2Minutes = _configuration.GetValue<int>("ScheduledCallbacks:TimeWindow2Minutes");

            var ivrText = GetIvrText();
            var textToSpeech = string.IsNullOrWhiteSpace(prerollText)
                ? string.Format(ivrText[Constants.IvrTextKeys.ScheduledCallbackTimeSelection], timeWindow1Minutes, timeWindow2Minutes)
                : $"{prerollText} {string.Format(ivrText[Constants.IvrTextKeys.ScheduledCallbackTimeSelection], timeWindow1Minutes, timeWindow2Minutes)}";

            // Start recognize prompt - play audio and recognize speech or 1-digit DTMF input
            await StartRecognizingChoiceAsync(callMedia, textToSpeech, textToSpeechLocale, choices, Constants.OperationContext.ScheduledCallbackTimeSelectionMenu, callerId);
        }

        public async Task<TimeSpan?> PlayCallbackTimeSelectionChoiceAsync(string callerId, DtmfTone choiceOrTone, CallMedia callMedia, string textToSpeechLocale, double estimatedWaitTime)
        {
            _logger.LogInformation($"PlayCallbackTimeSelectionChoiceAsync called with '{choiceOrTone}'");

            var ivrText = GetIvrText();
            var estimatedWaitTimeStr = estimatedWaitTime > 1 ? $"{estimatedWaitTime} {ivrText["Minutes"]}" : $"{estimatedWaitTime} {ivrText["Minute"]}";
            // Calculate next 15 min interval after current estimated wait time
            var timeWindow1Minutes = estimatedWaitTime + _configuration.GetValue<int>("ScheduledCallbacks:TimeWindow1Minutes");
            timeWindow1Minutes = Math.Ceiling(timeWindow1Minutes / 15) * 15;
            var timeWindow1MinutesStr = timeWindow1Minutes > 1 ? $"{timeWindow1Minutes} {ivrText["Minutes"]}" : $"{timeWindow1Minutes} {ivrText["Minute"]}";
            var timeWindow2Minutes = estimatedWaitTime + _configuration.GetValue<int>("ScheduledCallbacks:TimeWindow2Minutes");
            timeWindow2Minutes = Math.Ceiling(timeWindow2Minutes / 15) * 15;
            var timeWindow2MinutesStr = timeWindow2Minutes > 1 ? $"{timeWindow2Minutes} {ivrText["Minutes"]}" : $"{timeWindow2Minutes} {ivrText["Minute"]}";
            var target = callerId.StartsWith("8:acs") ? string.Empty : $"at the phone number {callerId.SanitizePhoneNumber()}";

            if (choiceOrTone == DtmfTone.One || choiceOrTone.ToString().Equals(Constants.IvrCallbackChoices.One, StringComparison.OrdinalIgnoreCase))
            {
                PlayTextToSpeechToAllAsync(callMedia, string.Format(ivrText[Constants.IvrTextKeys.ScheduledCallbackAccepted], estimatedWaitTimeStr, target),
                    Constants.OperationContext.ScheduledCallbackAccepted, textToSpeechLocale);
                return TimeSpan.FromMinutes(estimatedWaitTime);
            }
            if (choiceOrTone == DtmfTone.Two || choiceOrTone.ToString().Equals(Constants.IvrCallbackChoices.Two, StringComparison.OrdinalIgnoreCase))
            {
                PlayTextToSpeechToAllAsync(callMedia, string.Format(ivrText[Constants.IvrTextKeys.ScheduledCallbackAccepted], timeWindow1MinutesStr, target),
                    Constants.OperationContext.ScheduledCallbackAccepted, textToSpeechLocale);
                return TimeSpan.FromMinutes(timeWindow1Minutes);
            }
            if (choiceOrTone == DtmfTone.Three || choiceOrTone.ToString().Equals(Constants.IvrCallbackChoices.Three, StringComparison.OrdinalIgnoreCase))
            {
                PlayTextToSpeechToAllAsync(callMedia, string.Format(ivrText[Constants.IvrTextKeys.ScheduledCallbackAccepted], timeWindow2MinutesStr, target),
                    Constants.OperationContext.ScheduledCallbackAccepted, textToSpeechLocale);
                return TimeSpan.FromMinutes(timeWindow2Minutes);
            }
            else
            {
                await PlayTextToSpeechToAllAsync(callMedia, ivrText[Constants.IvrTextKeys.ScheduledCallbackRejected],
                    Constants.OperationContext.ScheduledCallbackRejected, textToSpeechLocale);
                return null;
            }
        }

        public async Task PlayCallbackDialoutOptionsAsync(string callerId, CallMedia callMedia, string textToSpeechLocale, string? prerollText = null)
        {
            _logger.LogInformation($"PlayCallbackDialoutOptionsAsync called with '{callerId}'");

            var choices = new List<RecognizeChoice>
            {
                new (Constants.IvrCallbackChoices.One, GetRecognizedPhrases("Ivr:RecognizePhrasesYes"))
                {
                    Tone = DtmfTone.One
                },
                new (Constants.IvrCallbackChoices.Two, GetRecognizedPhrases("Ivr:RecognizePhrasesNo"))
                {
                    Tone = DtmfTone.Two
                }
            };

            var ivrText = GetIvrText();
            var textToSpeech = $"{prerollText ?? string.Empty} {ivrText[Constants.IvrTextKeys.ScheduledCallbackDialout]}";

            await StartRecognizingChoiceAsync(callMedia, textToSpeech, textToSpeechLocale, choices, Constants.OperationContext.ScheduledCallbackDialout, callerId);
        }

        public async Task PlaceCallOnHoldAsync(string callConnectionId, string operationContext, string[] agentAcsIds, bool playMusic = true)
        {
            _logger.LogInformation($"PlaceCallOnHoldAsync called for connection ID '{callConnectionId}' and OperationContext '{operationContext}'");

            var callConnection = GetCallConnection(callConnectionId);

            switch (operationContext)
            {
                case Constants.OperationContext.Escalation:
                case Constants.OperationContext.Transfer:
                    _logger.LogInformation($"Attempting to remove agents from call connection '{callConnectionId}'");

                    if (agentAcsIds == null || agentAcsIds.Length == 0)
                    {
                        throw new ArgumentException("Agent ACS IDs are required", nameof(agentAcsIds));
                    }

                    foreach (var agentAcsId in agentAcsIds)
                    {
                        try
                        {
                            _logger.LogInformation($"Attempting to remove agent with ACS ID '{agentAcsId}' for call connection '{callConnectionId}'");

                            // remove agent from call
                            var agent = new CommunicationUserIdentifier(agentAcsId);
                            await callConnection.RemoveParticipantAsync(agent);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"PlaceCallOnHoldAsync failed to remove an agent, ACS ID was '{agentAcsId}'. Exception: {ex}");
                        }
                    }

                    break;
                case Constants.OperationContext.HoldCall:
                default:
                    _logger.LogInformation($"Removing agents not required for call connection '{callConnectionId}', OperationContext '{operationContext}'");
                    break;
            }

            if (playMusic)
            {
                _logger.LogInformation($"Playing hold music for call connection '{callConnectionId}', OperationContext '{operationContext}'");

                // play hold music
                await PlayHoldMusicAsync(operationContext, callConnection.GetCallMedia(), loop: true);
            }
        }

        public async Task AddParticipantAsync(string callConnectionId, string operationContext, string? acsId, int? invitationTimeoutInSeconds = 120)
        {
            _logger.LogInformation($"AddParticipantAsync called for connection ID '{callConnectionId}' and OperationContext '{operationContext}'");

            if (string.IsNullOrWhiteSpace(acsId))
            {
                throw new ArgumentException("ACS ID is required", nameof(acsId));
            }

            var agent = new CommunicationUserIdentifier(acsId);
            var callInvite = new CallInvite(agent);
            var addParticipantOptions = new AddParticipantOptions(callInvite)
            {
                OperationContext = operationContext,
                InvitationTimeoutInSeconds = invitationTimeoutInSeconds
            };
            var callConnection = GetCallConnection(callConnectionId);
            await callConnection.AddParticipantAsync(addParticipantOptions);
        }

        public async Task RemoveParticipantAsync(string callConnectionId, string operationContext, string acsId)
        {
            _logger.LogInformation($"RemoveParticipantAsync called for connection ID '{callConnectionId}' and OperationContext '{operationContext}'");

            if (string.IsNullOrWhiteSpace(acsId))
            {
                throw new ArgumentException("ACS ID is required", nameof(acsId));
            }

            var agent = new CommunicationUserIdentifier(acsId);
            var callConnection = GetCallConnection(callConnectionId);
            await callConnection.RemoveParticipantAsync(agent);
        }

        public async Task PlayHoldMusicAsync(string operationContext, CallMedia callMedia, bool loop = true)
        {
            _logger.LogInformation($"PlayHoldMusicAsync called for OperationContext '{operationContext}'");

            var holdMusicUri = GetIvrConfig()["HoldMusicUrl"];
            await PlayAudioFileToAllAsync(callMedia, holdMusicUri, operationContext, loop);
        }

        public async Task PlayEstimatedWaitTimeAsync(double estimatedWaitTime, string operationContext, CallMedia callMedia, string textToSpeechLocale)
        {
            _logger.LogInformation($"PlayEstimatedWaitTime announcing estimated wait time of '{estimatedWaitTime}' minutes");

            var ivrText = GetIvrText();
            var minutes = estimatedWaitTime > 1
                ? $"{estimatedWaitTime} {ivrText["Minutes"]}"
                : $"{estimatedWaitTime} {ivrText["Minute"]}";
            var textToSpeech = string.Format(ivrText["EstimatedWaitTime"], minutes);

            await PlayTextToSpeechToAllAsync(callMedia, textToSpeech, operationContext, textToSpeechLocale, loop: false);
        }

        public async Task PlayPairingCompletedAsync(string department, string operationContext, CallMedia callMedia, string textToSpeechLocale)
        {
            _logger.LogInformation($"PlayPairingCompletedAsync called with OperationContext '{operationContext}'");

            var ivrText = GetIvrText();
            var textToSpeech = string.Equals(department, Constants.Departments.Other, StringComparison.OrdinalIgnoreCase)
                ? ivrText["PairingCompletedOther"]
                : string.Format(ivrText["PairingCompleted"], department);
            await PlayTextToSpeechToAllAsync(callMedia, textToSpeech, operationContext, textToSpeechLocale, loop: false);
        }

        public async Task PlayTextToSpeechToAllAsync(CallMedia callMedia, string audioText, string operationContext, string audioTextLocale = "en-US", bool loop = false)
        {
            _logger.LogInformation($"PlayTextToSpeechToAllAsync called with '{audioText}' and operationContext '{operationContext}', loop = {loop}");

            //you can provide SourceLocale and VoiceGender as one option for playing audio
            var playSource = new TextSource(audioText)
            {
                SourceLocale = audioTextLocale,
                VoiceGender = GenderType.Female
            };

            await PlayAudioToAllAsync(callMedia, playSource, operationContext, loop);
        }

        public async Task EndCallAsync(string callConnectionId, string operationContext)
        {
            _logger.LogInformation($"EndCallAsync called with OperationContext '{operationContext}'");

            await _client.GetCallConnection(callConnectionId).HangUpAsync(true);
        }

        public async Task CancelAllMediaOperationsAsync(CallMedia callMedia, string callConnectionId, string operationContext)
        {
            _logger.LogInformation($"CancelAllMediaOperationsAsync called with OperationContext '{operationContext}' on connection ID '{callConnectionId}'");

            await callMedia.CancelAllMediaOperationsAsync();
        }

        public string[] GetAllowedIncomingIdentitiesList()
        {
            var allowList = GetArrayFromConfig("Ivr:AcceptCallsFor");
            return allowList;
        }

        public IConfigurationSection GetIvrConfig()
        {
            return _configuration.GetSection("Ivr");
        }

        public IConfigurationSection GetIvrText()
        {
            return _configuration.GetSection("IVRText");
        }

        public IConfigurationSection GetQueuesConfig()
        {
            return _configuration.GetSection("Queues");
        }

        public IDictionary<string, DtmfTone> GetAllRecognizedPhrasesAsDtmfTones()
        {
            var phrases = new Dictionary<string, DtmfTone>(StringComparer.OrdinalIgnoreCase);

            foreach (var phrase in GetRecognizedPhrases("Queues:HomeServiceQueue:RecognizePhrases"))
            {
                phrases.Add(phrase, DtmfTone.One);
            }

            foreach (var phrase in GetRecognizedPhrases("Queues:MobileServiceQueue:RecognizePhrases"))
            {
                phrases.Add(phrase, DtmfTone.Two);
            }

            foreach (var phrase in GetRecognizedPhrases("Queues:TvServiceQueue:RecognizePhrases"))
            {
                phrases.Add(phrase, DtmfTone.Three);
            }

            foreach (var phrase in GetRecognizedPhrases("Ivr:EndCallRecognizePhrases"))
            {
                phrases.Add(phrase, DtmfTone.Four);
            }

            return phrases;
        }

        private string[] GetArrayFromConfig(string section)
        {
            return _configuration.GetSection(section).Get<string[]>();
        }

        private string[] GetRecognizedPhrases(string section)
        {
            return GetArrayFromConfig(section);
        }

        private async Task StartRecognizingChoiceAsync(CallMedia callMedia, string textToSpeech, string textToSpeechLocale, List<RecognizeChoice> choices, string operationContext, string callerId, CancellationToken cancellationToken = default)
        {
            var prompt = new TextSource(textToSpeech)
            {
                SourceLocale = textToSpeechLocale,
                VoiceGender = GenderType.Female,
            };

            var ivrConfig = GetIvrConfig();
            var initialSilenceTimeout = ivrConfig.GetValue<int>("RecognizerInitialSilenceTimeout");
            var allowMenuInterrupt = ivrConfig.GetValue<bool>("AllowMenuInterrupt");

            var recognizeOptions = new CallMediaRecognizeChoiceOptions(CommunicationIdentifier.FromRawId(callerId), choices)
            {
                InitialSilenceTimeout = TimeSpan.FromSeconds(initialSilenceTimeout),
                OperationContext = operationContext,
                InterruptPrompt = allowMenuInterrupt,
                Prompt = prompt,
            };

            await callMedia.StartRecognizingAsync(recognizeOptions, cancellationToken);
        }

        private async Task StartRecognizingDtmfAsync(CallMedia callMedia, string textToSpeech, string textToSpeechLocale, int maxTonesToCollect, int initialSilenceTimeout, string operationContext, string callerId, bool allowInterrupt = true, CancellationToken cancellationToken = default)
        {
            var prompt = new TextSource(textToSpeech)
            {
                SourceLocale = textToSpeechLocale,
                VoiceGender = GenderType.Female,
            };

            var ivrConfig = GetIvrConfig();
            var interToneTimeout = ivrConfig.GetValue<int>("DtmfInterToneTimeout");

            var recognizeOptions = new CallMediaRecognizeDtmfOptions(CommunicationIdentifier.FromRawId(callerId), maxTonesToCollect: maxTonesToCollect)
            {
                InitialSilenceTimeout = TimeSpan.FromSeconds(initialSilenceTimeout),
                OperationContext = operationContext,
                InterruptPrompt = allowInterrupt,
                InterToneTimeout = TimeSpan.FromSeconds(interToneTimeout),
                Prompt = prompt
            };

            await callMedia.StartRecognizingAsync(recognizeOptions, cancellationToken);
        }

        private static async Task PlayAudioFileToAllAsync(CallMedia callMedia, string fileUri, string operationContext, bool loop = false)
        {
            var playSource = new FileSource(new Uri(fileUri));
            await PlayAudioToAllAsync(callMedia, playSource, operationContext, loop);
        }

        private static async Task PlayAudioToAllAsync(CallMedia callMedia, PlaySource playSource, string operationContext, bool loop)
        {
            var playOptions = new PlayOptions
            {
                OperationContext = operationContext,
                Loop = loop
            };

            await callMedia.PlayToAllAsync(playSource, playOptions);
        }

        public void ProcessEvents(CloudEvent[] cloudEvents)
        {
            throw new NotImplementedException();
        }

        public async Task<RecordingStateResult> StartRecordingAsync(string serverCallId)
        {
            _logger.LogInformation($"Start recording with server:");

            try
            {
                var recordingOptions = new StartRecordingOptions(new ServerCallLocator(serverCallId));
                return await _client.GetCallRecording().StartRecordingAsync(recordingOptions).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogError($"Start Recording failed unexpectedly: {e}");

                throw;
            }
        }

        public async Task<Response> StopRecordingAsync(string recordingId)
        {
            _logger.LogInformation($"Start recording with server:");

            try
            {
                return await _client.GetCallRecording().StopRecordingAsync(recordingId).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogError($"Stop Recording failed unexpectedly: {e}");

                throw;
            }
        }

        public async Task<Response> PauseRecordingAsync(string recordingId)
        {
            _logger.LogInformation($"Pause recording with server:");

            try
            {
                return _client.GetCallRecording().PauseRecording(recordingId);
                // return await _client.GetCallRecording().PauseRecordingAsync(recordingId).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogError($"Pause Recording failed unexpectedly: {e}");

                throw;
            }
        }
        public async Task<Response> ResumeRecordingAsync(string recordingId)
        {
            _logger.LogInformation($"Resume recording with server:");

            try
            {
                return await _client.GetCallRecording().ResumeRecordingAsync(recordingId).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogError($"Pause Recording failed unexpectedly: {e}");

                throw;
            }
        }

        public async Task ProcessFile(string downloadLocation, string documentId, string fileFormat, string downloadType)
        {
            var recordingDownloadUri = new Uri(downloadLocation);
            var response = await _client.GetCallRecording().DownloadStreamingAsync(recordingDownloadUri);

            Logger.LogInformation($"Download {downloadType} response  -- >" + response.GetRawResponse());
            Logger.LogInformation($"Save downloaded {downloadType} -- >");

            string filePath = ".\\" + documentId + "." + fileFormat;
            using (Stream streamToReadFrom = response.Value)
            {
                using (Stream streamToWriteTo = System.IO.File.Open(filePath, FileMode.Create))
                {
                    await streamToReadFrom.CopyToAsync(streamToWriteTo);
                    await streamToWriteTo.FlushAsync();
                }
            }

            if (string.Equals(downloadType, FileDownloadType.Metadata, StringComparison.InvariantCultureIgnoreCase) && System.IO.File.Exists(filePath))
            {
                Root deserializedFilePath = JsonConvert.DeserializeObject<Root>(System.IO.File.ReadAllText(filePath));
                recFileFormat = deserializedFilePath.recordingInfo.format;

                Logger.LogInformation($"Recording File Format is -- > {recFileFormat}");
            }
            var containerName = _configuration["BlobContainerName"];
            var blobStorageConnectionString = _configuration["BlobStorageConnectionString"];


            Logger.LogInformation($"Starting to upload {downloadType} to BlobStorage into container -- > {containerName}");

            var blobStorageHelperInfo = await BlobStorageHelper.UploadFileAsync(blobStorageConnectionString, containerName, filePath, filePath);
            if (blobStorageHelperInfo.Status)
            {
                Logger.LogInformation(blobStorageHelperInfo.Message);
                Logger.LogInformation($"Deleting temporary {downloadType} file being created");

                System.IO.File.Delete(filePath);
            }
            else
            {
                Logger.LogError($"{downloadType} file was not uploaded,{blobStorageHelperInfo.Message}");
            }
        }

    }
}
