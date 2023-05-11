// © Microsoft Corporation. All rights reserved.

using Azure;
using Azure.Communication.CallAutomation;
using System.Collections.Immutable;
using System.Net;
using CallAutomation.Scenarios.Interfaces;
using CallAutomation.Scenarios.Utils;


namespace CallAutomation.Scenarios.Handlers
{
    public class CallEventHandler :
        IEventGridEventHandler<IncomingCallEvent>,
        IEventGridEventHandler<RecordingFileStatusUpdatedEvent>,
        IEventCloudEventHandler<AddParticipantFailed>,
        IEventCloudEventHandler<AddParticipantSucceeded>,
        IEventCloudEventHandler<CallConnected>,
        IEventCloudEventHandler<CallDisconnected>,
        IEventCloudEventHandler<CallTransferAccepted>,
        IEventCloudEventHandler<CallTransferFailed>,
        IEventCloudEventHandler<ParticipantsUpdated>,
        IEventCloudEventHandler<PlayCompleted>,
        IEventCloudEventHandler<PlayFailed>,
        IEventCloudEventHandler<PlayCanceled>,
        IEventCloudEventHandler<RecognizeCompleted>,
        IEventCloudEventHandler<RecognizeFailed>,
        IEventCloudEventHandler<RecognizeCanceled>,
        IEventCloudEventHandler<RecordingStateChanged>
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<CallEventHandler> _logger;
        private readonly ICallAutomationService _callAutomationService;
        private readonly ICallContextService _callContextService;       

        public CallEventHandler(
            IConfiguration configuration,
            ILogger<CallEventHandler> logger,
            ICallAutomationService callAutomationService,
            ICallContextService callContextService)
        {
            _configuration = configuration;
            _logger = logger;
            _callAutomationService = callAutomationService;
            _callContextService = callContextService;
        }

        public async Task Handle(IncomingCallEvent incomingCallEvent)
        {
            try
            {
                _logger.LogInformation("IncomingCallEvent received");

                var to = incomingCallEvent.To.RawId;
                var allowList = _callAutomationService.GetAllowedIncomingIdentitiesList();

                if (allowList == null || allowList.Length == 0)
                {
                    _logger.LogCritical("IncomingCall invoked with an empty allow list. Set your allowed incoming communication identifiers or the server will accept calls from itself!");
                }

                if (allowList == null || allowList.Any(x => to.EndsWith(x, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogInformation($"Accepting call for {to}");

                    var answerCallResult = await _callAutomationService.AnswerCallAsync(incomingCallEvent);
                    var callConnectionId = answerCallResult.CallConnectionProperties.CallConnectionId;

                    // store the customer's MRI
                    var callerAcsId = incomingCallEvent.From.RawId;
                    _callContextService.SetCustomerAcsId(callConnectionId, callerAcsId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "IncomingCallEvent failed unexpectedly");
                throw;
            }
        }

        public async Task Handle(AddParticipantFailed addParticipantFailed, string callerId)
        {
            using (_logger.BeginScope(GetLogContext(addParticipantFailed.CorrelationId, addParticipantFailed.CallConnectionId, addParticipantFailed.OperationContext)))
            {
                try
                {
                    _logger.LogInformation($"AddParticipantFailed received");

                    _logger.LogError($"Failed to add agent to the call");

                    if (addParticipantFailed.OperationContext == Constants.OperationContext.AgentJoining)
                    {
                        var callConnectionId = addParticipantFailed.CallConnectionId;
                        var callConnection = _callAutomationService.GetCallConnection(callConnectionId);
                        var callMedia = callConnection.GetCallMedia();
                        await _callAutomationService.CancelAllMediaOperationsAsync(callMedia, callConnectionId, Constants.OperationContext.WaitingForAgent);
                        await _callAutomationService.PlayHoldMusicAsync(Constants.OperationContext.WaitingForAgent, callMedia);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogCritical($"AddParticipantFailed unexpectedly failed to unassign agents: {ex}");
                    throw;
                }
            }
        }

        public Task Handle(AddParticipantSucceeded addParticipantSucceeded, string callerId)
        {
            using (_logger.BeginScope(GetLogContext(addParticipantSucceeded.CorrelationId, addParticipantSucceeded.CallConnectionId, addParticipantSucceeded.OperationContext)))
            {
                try
                {
                    var operationContext = addParticipantSucceeded.OperationContext;

                    _logger.LogInformation($"AddParticipantSucceeded received for OperationContext '{operationContext}'");

                    if (operationContext == Constants.OperationContext.AgentJoining)
                    {
                        _logger.LogInformation($"Attempting to stop hold music");

                        var callConnectionId = addParticipantSucceeded.CallConnectionId;

                        _callContextService.AddAgentAcsId(callConnectionId, addParticipantSucceeded.Participant.RawId);
                    }
                    else if (operationContext == Constants.OperationContext.SupervisorJoining)
                    {
                        _logger.LogInformation($"Adding supervisor to call.");
                        var callConnectionId = addParticipantSucceeded.CallConnectionId;
                        var callConnection = _callAutomationService.GetCallConnection(callConnectionId);
                        callConnection.MuteParticipants(addParticipantSucceeded.Participant, operationContext);

                        _callContextService.AddAgentAcsId(callConnectionId, addParticipantSucceeded.Participant.RawId);
                    }
                    else
                    {
                        _logger.LogWarning($"AddParticipantSucceeded for a non-agent. Call ID was '{addParticipantSucceeded.CorrelationId}'");
                    }

                    return Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    // couldn't stop hold music!?
                    _logger.LogCritical($"AddParticipantSucceeded unexpectedly failed: {ex}");
                    throw;
                }
            }
        }

        public async Task Handle(CallConnected callConnected, string callerId)
        {
            using (_logger.BeginScope(GetLogContext(callConnected.CorrelationId, callConnected.CallConnectionId, callConnected.OperationContext)))
            {
                _logger.LogDebug($"CallConnected received, with callerId : {callerId}");

                var operationContext = callConnected.OperationContext;

                if (operationContext == Constants.OperationContext.AgentJoining || operationContext == Constants.OperationContext.SupervisorJoining)
                {
                    _logger.LogCritical($"CallConnected was for an agent or supervisor, callerId was '{callerId}'. Set the allowlist to prevent accepting calls from the IVR.");
                    return;
                }

                var callId = callConnected.CorrelationId;
                var callConnectionId = callConnected.CallConnectionId;


                var callConnection = _callAutomationService.GetCallConnection(callConnected.CallConnectionId);
                var callMedia = callConnection.GetCallMedia();

                _logger.LogInformation($"Call connected with ID '{callId}'");

                var ivrText = _callAutomationService.GetIvrText();

                if (callConnected.OperationContext?.StartsWith(Constants.OperationContext.ScheduledCallbackDialout) ?? false)
                {
                    _logger.LogInformation($"CallConnected was for a customer callback request");

                    var ivrConfig = _callAutomationService.GetIvrConfig();
                    var textToSpeechLocale = ivrConfig["TextToSpeechLocale"];
                    await _callAutomationService.PlayCallbackDialoutOptionsAsync(callerId, callMedia, textToSpeechLocale);
                }
                else
                {
                    var prerollText = ivrText[Constants.IvrTextKeys.Greeting];

                    var ivrConfig = _callAutomationService.GetIvrConfig();
                    var textToSpeechLocale = ivrConfig["TextToSpeechLocale"];

                    // Recognized phone number
                    if (ivrConfig.GetValue<bool>("UseNlu") && ivrConfig.GetValue<bool>("UseAiPairing"))
                    {
                        _logger.LogInformation("Caller ID was recognized, sending to AI pairing immediately");
                        //await PromptCustomerForAiPairing(callConnection, textToSpeechLocale, callerId);
                    }
                    else
                    {
                        _logger.LogInformation("Caller ID was recognized, sending to IVR immediately");
                        await PlayMainMenu(callConnection, textToSpeechLocale, callerId, prerollText);
                    }

                }
            }
        }

        // Handles bot removed or unexpectedly disconnected case
        public async Task Handle(CallDisconnected callDisconnected, string callerId)
        {
            using (_logger.BeginScope(GetLogContext(callDisconnected.CorrelationId, callDisconnected.CallConnectionId, callDisconnected.OperationContext)))
            {
                try
                {
                    _logger.LogInformation($"CallDisconnected received");



                    var callConnectionId = callDisconnected.CallConnectionId;
                    var callId = callDisconnected.CorrelationId;
                    var customerAcsId = _callContextService.GetCustomerAcsId(callConnectionId);

                    if (!string.IsNullOrWhiteSpace(customerAcsId))
                    {
                        _logger.LogInformation($"CallDisconnected received while customer may still be on the call, call ID '{callId}'");
                    }

                    _callContextService.RemoveCustomerId(callConnectionId);
                    _callContextService.RemoveCustomerAcsId(callConnectionId);
                    _callContextService.RemoveAgentAcsAcsIds(callConnectionId);
                    _callContextService.RemoveEstimatedWaitTime(callConnectionId);
                    _callContextService.RemoveClassification(callConnectionId);

                    if (_callAutomationService.GetIvrConfig().GetValue<bool>("UseNlu"))
                    {
                        _callContextService.RemoveAccountIdSpeechRecognizerCancellationTokenSource(callConnectionId);
                        _callContextService.RemovePairingSpeechRecognizerCancellationTokenSource(callConnectionId);
                        _callContextService.RemoveMainMenuSpeechRecognizerCancellationTokenSource(callConnectionId);

                        // remove audiostream
                        var mediaSubscriptionId = _callContextService.GetMediaSubscriptionId(callConnectionId);
                        if (!string.IsNullOrWhiteSpace(mediaSubscriptionId))
                        {
                            _callContextService.RemoveAudioStream(mediaSubscriptionId);
                            _callContextService.RemoveMediaSubscriptionId(callConnectionId);
                            _callContextService.RemoveCallSummary(callConnectionId);
                        }
                    }
                }
                catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
                {
                    _logger.LogInformation($"CallDisconnected received a NotFound in one of its operations: {ex}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"CallDisconnected failed unexpectedly: {ex}");
                    throw;
                }
            }
        }

        public Task Handle(CallTransferAccepted callTransferAccepted, string callerId)
        {
            throw new NotImplementedException();
        }

        public Task Handle(CallTransferFailed callTransferFailed, string callerId)
        {
            throw new NotImplementedException();
        }

        public async Task Handle(ParticipantsUpdated participantsUpdated, string callerId)
        {
            using (_logger.BeginScope(GetLogContext(participantsUpdated.CorrelationId, participantsUpdated.CallConnectionId, participantsUpdated.OperationContext)))
            {
                try
                {
                    _logger.LogInformation("ParticipantsUpdated received");

                    var callConnectionId = participantsUpdated.CallConnectionId;
                    var callId = participantsUpdated.CorrelationId;
                    var customerAcsId = _callContextService.GetCustomerAcsId(callConnectionId);

                    if (!string.IsNullOrWhiteSpace(customerAcsId))
                    {
                        // customer still in the call, nothing to do
                        if (participantsUpdated.Participants.Any(x => x.Identifier.RawId.Equals(customerAcsId, StringComparison.OrdinalIgnoreCase)))
                        {
                            _logger.LogInformation("ParticipantsUpdated received and customer still in the call");

                            // just wait for customer to hang up or an escalation case
                        }
                        // customer hung up
                        else
                        {
                            _callContextService.RemoveCustomerId(callConnectionId);
                            _callContextService.RemoveCustomerAcsId(callConnectionId);
                            _callContextService.RemoveAgentAcsAcsIds(callConnectionId);
                            _callContextService.RemoveEstimatedWaitTime(callConnectionId);
                            _callContextService.RemoveClassification(callConnectionId);

                            if (_callAutomationService.GetIvrConfig().GetValue<bool>("UseNlu"))
                            {
                                _callContextService.RemoveAccountIdSpeechRecognizerCancellationTokenSource(callConnectionId);
                                _callContextService.RemovePairingSpeechRecognizerCancellationTokenSource(callConnectionId);
                                _callContextService.RemoveMainMenuSpeechRecognizerCancellationTokenSource(callConnectionId);

                                // remove audiostream
                                var mediaSubscriptionId = _callContextService.GetMediaSubscriptionId(callConnectionId);
                                if (!string.IsNullOrWhiteSpace(mediaSubscriptionId))
                                {
                                    _callContextService.RemoveAudioStream(mediaSubscriptionId);
                                    _callContextService.RemoveMediaSubscriptionId(callConnectionId);
                                    _callContextService.RemoveCallSummary(callConnectionId);
                                }
                            }

                           // await CleanupCallJob(callId);

                            // try and hang up the call
                            await _callAutomationService.GetCallConnection(callConnectionId).HangUpAsync(true);
                        }
                    }
                    else
                    {
                        _logger.LogInformation($"ParticipantsUpdated called without a customer ACS ID for call '{callId}'");
                    }
                }
                catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
                {
                    _logger.LogInformation($"ParticipantsUpdated received a NotFound in one of its operations: {ex}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"ParticipantsUpdated failed unexpectedly: {ex}");
                    throw;
                }
            }
        }

        public async Task Handle(PlayCompleted playCompleted, string callerId)
        {
            using (_logger.BeginScope(GetLogContext(playCompleted.CorrelationId, playCompleted.CallConnectionId, playCompleted.OperationContext)))
            {
                try
                {
                    _logger.LogInformation("PlayCompleted received");

                    var callConnectionId = playCompleted.CallConnectionId;
                    var operationContext = playCompleted.OperationContext;
                    var callId = playCompleted.CorrelationId;

                    switch (operationContext)
                    {
                        case Constants.OperationContext.EndCall:
                            _logger.LogInformation("Customer requested to end call, hanging up");
                            await _callAutomationService.EndCallAsync(callConnectionId, operationContext);
                            break;
                        case QueueConstants.MagentaDefault:                             
                        case QueueConstants.MagentaHome:
                            _logger.LogInformation("Add Participant initiate the call:");
                            await _callAutomationService.AddParticipantAsync(callConnectionId, "agent", _configuration["AddParticipant"]);

                            break;
                        case QueueConstants.MagentaTV:
                        case QueueConstants.MagentaMobile:
                            //await EnqueueCall(callConnectionId, operationContext, callId, callerId);
                            break;
                        case Constants.OperationContext.ScheduledCallbackAccepted:
                            _logger.LogInformation("Customer requested a scheduled callback, hanging up");
                            await _callAutomationService.EndCallAsync(callConnectionId, operationContext);
                            break;
                        case Constants.OperationContext.ScheduledCallbackRejected:
                            //await EnqueueCall(callConnectionId, operationContext, callId, callerId, false);
                            break;
                        case Constants.OperationContext.ScheduledCallbackDialoutAccepted:
                            //await EnqueueScheduledCall(callConnectionId, callId);
                            break;
                        case Constants.OperationContext.ScheduledCallbackDialoutRejected:
                            _logger.LogInformation("Customer rejected the callback, hanging up");
                            await _callAutomationService.EndCallAsync(callConnectionId, operationContext);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"PlayCompleted failed unexpectedly: {ex}");
                    throw;
                }
            }
        }

        public async Task Handle(PlayFailed playFailed, string callerId)
        {
            using (_logger.BeginScope(GetLogContext(playFailed.CorrelationId, playFailed.CallConnectionId, playFailed.OperationContext)))
            {
                var callId = playFailed.CorrelationId;
                var callConnectionId = playFailed.CallConnectionId;
                var operationContext = playFailed.OperationContext;
                _logger.LogCritical($"PlayFailed received for OperationContext: '{operationContext}', call ID was '{callId}'");

                var callConnection = _callAutomationService.GetCallConnection(callConnectionId);
                var callMedia = callConnection.GetCallMedia();
                var ivrConfig = _callAutomationService.GetIvrConfig();

                await _callAutomationService.CancelAllMediaOperationsAsync(callMedia, callConnectionId, operationContext);

                _logger.LogInformation($"PlayFailed routing call ID '{callId}' to all agents queue");

                await _callAutomationService.PlayMenuChoiceAsync(DtmfTone.Zero, callMedia, ivrConfig["TextToSpeechLocale"]);
            }
        }

        public Task Handle(PlayCanceled playCanceled, string callerId)
        {
            using (_logger.BeginScope(GetLogContext(playCanceled.CorrelationId, playCanceled.CallConnectionId, playCanceled.OperationContext)))
            {
                _logger.LogInformation($"PlayCanceled received for OperationContext: '{playCanceled.OperationContext}'");

                return Task.CompletedTask;
            }
        }

        public async Task Handle(RecognizeCompleted recognizeCompleted, string callerId)
        {
            using (_logger.BeginScope(GetLogContext(recognizeCompleted.CorrelationId, recognizeCompleted.CallConnectionId, recognizeCompleted.OperationContext)))
            {
                var operationContext = recognizeCompleted.OperationContext;

                _logger.LogInformation($"RecognizeCompleted for OperationContext '{operationContext}'");

                var callConnectionId = recognizeCompleted.CallConnectionId;
                var callConnection = _callAutomationService.GetCallConnection(callConnectionId);
                var callMedia = callConnection.GetCallMedia();

                var ivrConfig = _callAutomationService.GetIvrConfig();
                var textToSpeechLocale = ivrConfig["TextToSpeechLocale"];
                var ivrText = _callAutomationService.GetIvrText();

                var useCustomPhraseRecognition = ivrConfig.GetValue<bool>("UseNlu") && ivrConfig.GetValue<bool>("UseCustomPhraseRecognition");

                switch (operationContext)
                {
                    case Constants.OperationContext.AccountIdValidation:
                        _logger.LogDebug($"RecognizeCompleted event - (Authentication OperationContext) received ===  {recognizeCompleted.RecognizeResult}");

                        if (ivrConfig.GetValue<bool>("UseNlu"))
                        {
                            _logger.LogInformation("RecognizeCompleted called account ID speech recognizer cancel");
                            CancelAccountIdSpeechRecognizer(callConnectionId, operationContext);
                        }

                        var authenticationCollectTonesResult = (CollectTonesResult)recognizeCompleted.RecognizeResult;
                        var accountId = authenticationCollectTonesResult.CombineAll();
                        //await ValidateAccount(callConnection, callConnectionId, accountId, textToSpeechLocale, callerId);
                        break;

                    case Constants.OperationContext.MainMenu:
                        _logger.LogDebug($"RecognizeCompleted event - (MainMenu OperationContext) received ===  {recognizeCompleted.RecognizeResult}");

                        switch (recognizeCompleted.RecognizeResult)
                        {
                            // Take action for Recognition through Choices
                            case ChoiceResult choiceResult:
                                if (!useCustomPhraseRecognition)
                                {
                                    await _callAutomationService.PlayMenuChoiceAsync(choiceResult.Label, callMedia, textToSpeechLocale);                             


                                }                               
                                break;

                            //Take action for Recognition through DTMF
                            case CollectTonesResult mainMenuCollectedTonesResult:
                                if (useCustomPhraseRecognition)
                                {
                                    CancelMainMenuSpeechRecognizer(callConnectionId, operationContext);
                                }
                                await _callAutomationService.PlayMenuChoiceAsync(mainMenuCollectedTonesResult.Tones[0], callMedia, textToSpeechLocale);
                                break;

                            default:
                                _logger.LogError($"Unexpected recognize event result identified for connection id: {recognizeCompleted.CallConnectionId}");
                                break;
                        }
                        break;

                    case Constants.OperationContext.AiPairing when (recognizeCompleted.RecognizeResult is CollectTonesResult dtmfTones):
                        _logger.LogInformation("RecognizeCompleted called AI pairing speech recognizer cancel");
                        CancelAiPairingSpeechRecognizer(callConnectionId, operationContext);

                        // play IVR menu
                        await PlayMainMenu(callConnection, textToSpeechLocale, callerId);
                        break;

                    case Constants.OperationContext.ScheduledCallbackOffer:
                        _logger.LogDebug($"RecognizeCompleted event - (ScheduledCallbackOffer OperationContext) received ===  {recognizeCompleted.RecognizeResult}");

                        switch (recognizeCompleted.RecognizeResult)
                        {
                            // Take action for Recognition through Choices
                            case ChoiceResult choiceResult:
                                await _callAutomationService.PlayCallbackOfferChoiceAsync(callerId, choiceResult.Label, callMedia, textToSpeechLocale);
                                break;

                            //Take action for Recognition through DTMF
                            case CollectTonesResult callbackOfferResult:
                                await _callAutomationService.PlayCallbackOfferChoiceAsync(callerId, callbackOfferResult.Tones[0], callMedia, textToSpeechLocale);
                                break;

                            default:
                                _logger.LogError($"Unexpected recognize event result identified for connection id: {recognizeCompleted.CallConnectionId}");
                                break;
                        }
                        break;

                    case Constants.OperationContext.ScheduledCallbackTimeSelectionMenu:
                        _logger.LogDebug($"RecognizeCompleted event - (ScheduledCallbackTimeSelectionMenu OperationContext) received ===  {recognizeCompleted.RecognizeResult}");

                        var estimatedWaitTime = _callContextService.GetEstimatedWaitTime(callConnectionId) ?? 0;
                        TimeSpan? scheduleAfter = null;

                        switch (recognizeCompleted.RecognizeResult)
                        {
                            // Take action for Recognition through Choices
                            case ChoiceResult choiceResult:
                                scheduleAfter = await _callAutomationService.PlayCallbackTimeSelectionChoiceAsync(
                                    callerId, choiceResult.Label, callMedia, textToSpeechLocale, estimatedWaitTime);

                                if (scheduleAfter is not null)
                                {
                                    //await EnqueueCall(callConnectionId, operationContext,
                                    //    recognizeCompleted.CorrelationId, callerId, checkEstimatedWaitTime: false, scheduleAfter: scheduleAfter);
                                }

                                break;

                            //Take action for Recognition through DTMF
                            case CollectTonesResult callbackTimeSelectionResult:
                                scheduleAfter = await _callAutomationService.PlayCallbackTimeSelectionChoiceAsync(
                                    callerId, callbackTimeSelectionResult.Tones[0], callMedia, textToSpeechLocale, estimatedWaitTime);

                                if (scheduleAfter is not null)
                                {
                                    //await EnqueueCall(callConnectionId, operationContext,
                                    //    recognizeCompleted.CorrelationId, callerId, checkEstimatedWaitTime: false, scheduleAfter: scheduleAfter);
                                }

                                break;

                            default:
                                _logger.LogError($"Unexpected recognize event result identified for connection id: {recognizeCompleted.CallConnectionId}");
                                break;
                        }
                        break;

                    case Constants.OperationContext.ScheduledCallbackDialout:
                        _logger.LogDebug($"RecognizeCompleted event - (ScheduledCallbackDialout OperationContext) received ===  {recognizeCompleted.RecognizeResult}");

                        switch (recognizeCompleted.RecognizeResult)
                        {
                            // Take action for Recognition through Choices
                            case ChoiceResult choiceResult:
                                if (choiceResult.Label.Equals(Constants.IvrCallbackChoices.One, StringComparison.OrdinalIgnoreCase))
                                {
                                    await _callAutomationService.PlayTextToSpeechToAllAsync(callMedia,
                                        ivrText[$"{Constants.IvrTextKeys.ScheduledCallbackDialoutAccepted}"],
                                        Constants.OperationContext.ScheduledCallbackDialoutAccepted, textToSpeechLocale);
                                }
                                else
                                {
                                    await _callAutomationService.PlayTextToSpeechToAllAsync(callMedia,
                                        ivrText[$"{Constants.IvrTextKeys.ScheduledCallbackDialoutRejected}"],
                                        Constants.OperationContext.ScheduledCallbackDialoutRejected, textToSpeechLocale);
                                }

                                break;

                            //Take action for Recognition through DTMF
                            case CollectTonesResult callbackDialoutResult:
                                if (callbackDialoutResult.Tones[0] == DtmfTone.One)
                                {
                                    await _callAutomationService.PlayTextToSpeechToAllAsync(callMedia,
                                        ivrText[$"{Constants.IvrTextKeys.ScheduledCallbackDialoutAccepted}"],
                                        Constants.OperationContext.ScheduledCallbackDialoutAccepted, textToSpeechLocale);
                                }
                                else
                                {
                                    await _callAutomationService.PlayTextToSpeechToAllAsync(callMedia,
                                        ivrText[$"{Constants.IvrTextKeys.ScheduledCallbackDialoutRejected}"],
                                        Constants.OperationContext.ScheduledCallbackDialoutRejected, textToSpeechLocale);
                                }

                                break;

                            default:
                                _logger.LogError($"Unexpected recognize event result identified for connection id: {recognizeCompleted.CallConnectionId}");
                                break;
                        }
                        break;

                    default:
                        break;
                }
            }
        }

        public async Task Handle(RecognizeFailed recognizeFailed, string callerId)
        {
            using (_logger.BeginScope(GetLogContext(recognizeFailed.CorrelationId, recognizeFailed.CallConnectionId, recognizeFailed.OperationContext)))
            {
                var operationContext = recognizeFailed.OperationContext;

                _logger.LogInformation($"RecognizeFailed for OperationContext '{operationContext}'");

                var callConnectionId = recognizeFailed.CallConnectionId;
                var callConnection = _callAutomationService.GetCallConnection(callConnectionId);
                var callMedia = callConnection.GetCallMedia();

                var ivrText = _callAutomationService.GetIvrText();
                var ivrConfig = _callAutomationService.GetIvrConfig();
                var textToSpeechLocale = ivrConfig["TextToSpeechLocale"];

                var useNlu = ivrConfig.GetValue<bool>("UseNlu");
                var useCustomPhraseRecognition = useNlu && ivrConfig.GetValue<bool>("UseCustomPhraseRecognition");

                switch (operationContext)
                {
                    // customer made no input
                    case Constants.OperationContext.MainMenu when (recognizeFailed.ReasonCode == ReasonCode.RecognizeSpeechOptionNotMatched):
                        if (!useCustomPhraseRecognition)
                        {
                            await PlayMainMenu(callConnection, textToSpeechLocale, callerId);
                        }
                        break;
                    case Constants.OperationContext.MainMenu when (recognizeFailed.ReasonCode == ReasonCode.RecognizeInitialSilenceTimedOut):
                        if (useCustomPhraseRecognition)
                        {
                            CancelMainMenuSpeechRecognizer(callConnectionId, operationContext);
                        }
                        await PlayMainMenu(callConnection, textToSpeechLocale, callerId);
                        break;
                    case Constants.OperationContext.MainMenu when (recognizeFailed.ReasonCode == ReasonCode.RecognizeIncorrectToneDetected):
                        if (useCustomPhraseRecognition)
                        {
                            CancelMainMenuSpeechRecognizer(callConnectionId, operationContext);
                        }
                        await PlayMainMenu(callConnection, textToSpeechLocale, callerId, prerollText: ivrText[Constants.IvrTextKeys.InvalidOption]);
                        break;
                    case Constants.OperationContext.ScheduledCallbackOffer when (recognizeFailed.ReasonCode == ReasonCode.RecognizeInitialSilenceTimedOut):
                    case Constants.OperationContext.ScheduledCallbackOffer when (recognizeFailed.ReasonCode == ReasonCode.RecognizeSpeechOptionNotMatched):
                    case Constants.OperationContext.ScheduledCallbackTimeSelectionMenu when (recognizeFailed.ReasonCode == ReasonCode.RecognizeInitialSilenceTimedOut):
                    case Constants.OperationContext.ScheduledCallbackTimeSelectionMenu when (recognizeFailed.ReasonCode == ReasonCode.RecognizeSpeechOptionNotMatched):
                        await _callAutomationService.PlayTextToSpeechToAllAsync(callMedia, ivrText[Constants.IvrTextKeys.ScheduledCallbackRejected],
                            Constants.OperationContext.ScheduledCallbackRejected, textToSpeechLocale);
                        break;
                    case Constants.OperationContext.ScheduledCallbackDialout when (recognizeFailed.ReasonCode == ReasonCode.RecognizeInitialSilenceTimedOut):
                    case Constants.OperationContext.ScheduledCallbackDialout when (recognizeFailed.ReasonCode == ReasonCode.RecognizeSpeechOptionNotMatched):
                        await _callAutomationService.PlayTextToSpeechToAllAsync(callMedia, ivrText[Constants.IvrTextKeys.NoResponse],
                            Constants.OperationContext.ScheduledCallbackDialoutRejected, textToSpeechLocale);
                        break;
                    // couldn't recognize menu choice
                    case Constants.OperationContext.ScheduledCallbackOffer when (recognizeFailed.ReasonCode == ReasonCode.RecognizeIncorrectToneDetected):
                    case Constants.OperationContext.ScheduledCallbackTimeSelectionMenu when (recognizeFailed.ReasonCode == ReasonCode.RecognizeIncorrectToneDetected):
                        await _callAutomationService.PlayTextToSpeechToAllAsync(callMedia, $"{ivrText[Constants.IvrTextKeys.InvalidOption]} {ivrText[Constants.IvrTextKeys.ScheduledCallbackRejected]}",
                            Constants.OperationContext.ScheduledCallbackRejected, textToSpeechLocale);
                        break;
                    case Constants.OperationContext.ScheduledCallbackDialout when (recognizeFailed.ReasonCode == ReasonCode.RecognizeIncorrectToneDetected):
                        await _callAutomationService.PlayCallbackDialoutOptionsAsync(callerId, callMedia, textToSpeechLocale, ivrText[Constants.IvrTextKeys.InvalidOption]);
                        break;
                    // route caller to all agents queue
                    case Constants.OperationContext.AccountIdValidation:
                        if (useNlu)
                        {
                            _logger.LogInformation("RecognizeFailed called account ID speech recognizer cancel");
                            CancelAccountIdSpeechRecognizer(callConnectionId, operationContext);
                        }

                        var failedPrerollText = recognizeFailed.ReasonCode == ReasonCode.RecognizeInitialSilenceTimedOut
                            ? ivrText["CustomerQueryTimeout"]
                            : ivrText["AccountIdValidationFailed"];

                        await _callAutomationService.PlayMenuChoiceAsync(DtmfTone.Zero, callMedia, textToSpeechLocale, prerollText: failedPrerollText);
                        break;
                    case Constants.OperationContext.AiPairing when (recognizeFailed.ReasonCode == ReasonCode.RecognizeInitialSilenceTimedOut):
                        _logger.LogInformation($"AiPairing timed out during DTMF detection and customer said nothing");
                        CancelAiPairingSpeechRecognizer(callConnectionId, operationContext);

                        var prerollText = ivrText["CustomerQueryTimeout"];
                        await PlayMainMenu(callConnection, textToSpeechLocale, callerId, prerollText);
                        break;
                }
            }
        }

        public Task Handle(RecognizeCanceled recognizeCanceled, string callerId)
        {
            using (_logger.BeginScope(GetLogContext(recognizeCanceled.CorrelationId, recognizeCanceled.CallConnectionId, recognizeCanceled.OperationContext)))
            {
                _logger.LogInformation($"RecognizeCanceled received for OperationContext: '{recognizeCanceled.OperationContext}'");

                return Task.CompletedTask;
            }
        }

        public Task Handle(RecordingStateChanged recordingStateChanged, string callerId)
        {
            throw new NotImplementedException();
        }


        #region private helpers


        private async Task PlayMainMenu(CallConnection callConnection, string textToSpeechLocale, string callerId, string? prerollText = null)
        {
            var callConnectionId = callConnection.CallConnectionId;
            _logger.LogInformation($"PlayMainMenu called with connection ID '{callConnectionId}'");

            var callMedia = callConnection.GetCallMedia();
            var ivrConfig = _callAutomationService.GetIvrConfig();
            if (ivrConfig.GetValue<bool>("UseNlu") && ivrConfig.GetValue<bool>("UseCustomPhraseRecognition"))
            {
                var dtmfCancellationTokenSource = new CancellationTokenSource();
                var speechRecognitionCancellationTokenSource = new CancellationTokenSource();

                if (!_callContextService.SetMainMenuSpeechRecognizerCancellationTokenSource(callConnectionId, speechRecognitionCancellationTokenSource))
                {
                    _logger.LogWarning($"PlayMainMenu already had a speechRecognitionCancellationTokenSource, connection ID '{callConnectionId}'");
                }

                await _callAutomationService.PlayMenuOptionsAsync(callerId, callMedia, textToSpeechLocale, prerollText: prerollText, dtmfCancellationTokenSource.Token);

                _logger.LogInformation("PlayMainMenu listening for customer audio");

                CallConnectionProperties callConnectionProerties = await callConnection.GetCallConnectionPropertiesAsync();
                var mediaSubscriptionId = callConnectionProerties.MediaSubscriptionId;
                //var audioStreeam = _callContextService.GetAudioStream(mediaSubscriptionId);
               // if (audioStreeam != null)
                {
                    _logger.LogInformation($"PlayMainMenu found an audiostream for MediaSubscriptionId '{mediaSubscriptionId}', connection ID '{callConnectionId}'");

                    try
                    {
                        _callContextService.SetMainMenuSpeechRecognizerCancellationTokenSource(callConnectionId, speechRecognitionCancellationTokenSource);

                        var phrases = _callAutomationService.GetAllRecognizedPhrasesAsDtmfTones();
                        var segmentationSilenceTimeoutMs = ivrConfig["SegmentationSilenceTimeoutMsForCustomerQuery"];
                       // await _speechRecognizerService.RecognizePhraseFromAudioStreamAsync(audioStreeam, ivrConfig["SpeechRecognitionLanguage"], "1000", phrases.Keys, speechRecognitionCancellationTokenSource.Token, async (phrase) =>
                        {
                            if (!speechRecognitionCancellationTokenSource.IsCancellationRequested)
                            {
                                _logger.LogInformation("RecognizePhraseFromAudioStreamAsync called cancel");
                                speechRecognitionCancellationTokenSource.Cancel();
                            }

                            //if (!string.IsNullOrWhiteSpace(phrase))
                            {
                                // stop any audio currently playing + stop DTMF recognizer
                                dtmfCancellationTokenSource.Cancel();
                                await _callAutomationService.CancelAllMediaOperationsAsync(callMedia, callConnectionId, Constants.OperationContext.MainMenu);

                                var dtmfTone = DtmfTone.Zero;
                                //if (phrases.TryGetValue(phrase, out var tone))
                                //{
                                //    dtmfTone = tone;
                                //}

                                //_logger.LogInformation($"Customer utterance was: {phrase} and was mapped to DtmfTone '{dtmfTone}'");

                                await _callAutomationService.PlayMenuChoiceAsync(dtmfTone, callMedia, textToSpeechLocale);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation($"PlayMainMenu unexpectedly failed or timed out, queuing customer to all agents. Exception: {ex}");
                        dtmfCancellationTokenSource.Cancel();
                        speechRecognitionCancellationTokenSource.Cancel();
                        await _callAutomationService.CancelAllMediaOperationsAsync(callMedia, callConnectionId, Constants.OperationContext.AiPairing);

                        // queue the customer into all agents
                        await _callAutomationService.PlayMenuChoiceAsync(DtmfTone.Zero, callMedia, textToSpeechLocale);
                    }
                }
            }
            else
            {
                await _callAutomationService.PlayMenuOptionsAsync(callerId, callMedia, textToSpeechLocale, prerollText: prerollText);
            }
        }

        private async Task PromptCustomerForAccountId(CallConnection callConnection, string textToSpeechLocale, string callerId)
        {
            var dtmfCancellationTokenSource = new CancellationTokenSource();

            var ivrConfig = _callAutomationService.GetIvrConfig();
            var callConnectionId = callConnection.CallConnectionId;
            var callMedia = callConnection.GetCallMedia();
            var ivrText = _callAutomationService.GetIvrText();

            await _callAutomationService.StartRecognizingDtmfAsync(
                callerId,
                Constants.OperationContext.AccountIdValidation,
                callMedia,
                textToSpeechLocale,
                ivrConfig.GetValue<int>("AccountIdTimeout"),
                prerollText: ivrText[Constants.IvrTextKeys.Greeting],
                cancellationToken: dtmfCancellationTokenSource.Token);

            var speechRecognitionCancellationTokenSource = new CancellationTokenSource();

            _logger.LogInformation("Listening for account ID number via voice recognition");

            CallConnectionProperties callConnectionProerties = await callConnection.GetCallConnectionPropertiesAsync();
            var mediaSubscriptionId = callConnectionProerties.MediaSubscriptionId;
            _callContextService.SetMediaSubscriptionId(callConnectionId, mediaSubscriptionId);

            //var audioStreeam = _callContextService.GetAudioStream(mediaSubscriptionId);
            //if (audioStreeam != null)
            {
                _logger.LogInformation($"CallConnected found an audiostream for MediaSubscriptionId '{mediaSubscriptionId}', connection ID '{callConnectionId}'");

                var segmentationSilenceTimeoutMs = ivrConfig["SegmentationSilenceTimeoutMsForAccountId"];
                var digitsToCollect = 6;

                _callContextService.SetAccountIdSpeechRecognizerCancellationTokenSource(callConnectionId, speechRecognitionCancellationTokenSource);

                // TODO: make this configurable
                //await _speechRecognizerService.RecognizeDigitsFromAudioStreamAsync(audioStreeam, digitsToCollect, ivrConfig["SpeechRecognitionLanguage"], segmentationSilenceTimeoutMs, speechRecognitionCancellationTokenSource.Token, async (accountId) =>
                //{
                //    if (!speechRecognitionCancellationTokenSource.IsCancellationRequested)
                //    {
                //        _logger.LogInformation("RecognizeDigitsFromAudioStreamAsync called cancel");
                //        speechRecognitionCancellationTokenSource.Cancel();
                //    }

                //    if (!string.IsNullOrWhiteSpace(accountId))
                //    {
                //        // stop any audio currently playing + stop DTMF recognizer
                //        dtmfCancellationTokenSource.Cancel();
                //        await _callAutomationService.CancelAllMediaOperationsAsync(callMedia, callConnectionId, Constants.OperationContext.AccountIdValidation);

                //        // validate account ID we received from speech recognizer
                //        await ValidateAccount(callConnection, callConnectionId, accountId, textToSpeechLocale, callerId);
                //    }
                //    else
                //    {
                //        _logger.LogInformation($"RecognizeDigitsFromAudioStream captured no usable audio for connection ID '{callConnectionId}'");
                //    }
                //});
            }
        }

        private void CancelMainMenuSpeechRecognizer(string callConnectionId, string operationContext)
        {
            try
            {
                _logger.LogInformation($"CancelMainMenuSpeechRecognition called with connection ID '{callConnectionId}' and OperationContext '{operationContext}'");

                var cancellationToken = _callContextService.GetMainMenuSpeechRecognizerCancellationTokenSource(callConnectionId);
                cancellationToken?.Cancel();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Could not call cancel on MainMenu Speech Recognizer CancellationTokenSource, connection ID '{callConnectionId}', OperationContext '{operationContext}'");
            }
            finally
            {
                _callContextService.RemoveMainMenuSpeechRecognizerCancellationTokenSource(callConnectionId);
            }
        }

        private void CancelAccountIdSpeechRecognizer(string callConnectionId, string operationContext)
        {
            try
            {
                _logger.LogInformation($"CancelAccountIdSpeechRecognizer called with connection ID '{callConnectionId}' and OperationContext '{operationContext}'");

                var speechRecognitionCancellationTokenSource = _callContextService.GetAccountIdSpeechRecognizerCancellationTokenSource(callConnectionId);
                speechRecognitionCancellationTokenSource?.Cancel();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to cancel account ID speech recognizer token, connection ID '{callConnectionId}', OperationContext '{operationContext}'");
            }
        }

        private void CancelAiPairingSpeechRecognizer(string callConnectionId, string operationContext)
        {
            try
            {
                _logger.LogInformation($"CancelAiPairingSpeechRecognizer called with connection ID '{callConnectionId}' and OperationContext '{operationContext}'");

                var token = _callContextService.GetPairingSpeechRecognizerCancellationTokenSource(callConnectionId);
                token?.Cancel();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to cancel AI pairing token, connection ID '{callConnectionId}', OperationContext '{operationContext}'");
            }
        }

        private static IDictionary<string, object> GetLogContext(string callId, string callConnectionId, string? operationContext = null)
        {
            try
            {
                var context = new Dictionary<string, object>
                {
                    { "IVR_CallId", callId },
                    { "IVR_CallConnectionId", callConnectionId }
                };

                if (!string.IsNullOrWhiteSpace(operationContext))
                {
                    context.Add("IVR_OperationContext", operationContext);
                }

                return context;
            }
            catch (Exception)
            {
                return ImmutableDictionary<string, object>.Empty;
            }
        }

        public Task Handle(RecordingFileStatusUpdatedEvent eventName)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}