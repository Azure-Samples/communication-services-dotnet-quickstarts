using System;
using System.Threading.Tasks;
using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Call_Automation_GCCH.Models;
using Call_Automation_GCCH.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Call_Automation_GCCH.Controllers
{
    [ApiController]
    [Route("api")]
    [Produces("application/json")]
    public class CallAutomationEventsController : ControllerBase
    {
        private readonly CallAutomationService _service;
        private readonly ILogger<CallAutomationEventsController> _logger;
        private readonly ConfigurationRequest _config; // final, bound object

        public CallAutomationEventsController(
            CallAutomationService service,
            ILogger<CallAutomationEventsController> logger, IOptions<ConfigurationRequest> configOptions)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = configOptions.Value ?? throw new ArgumentNullException(nameof(configOptions));
        }

        /// <summary>
        /// Returns all logs collected by the LogCollector
        /// </summary>
        /// <returns>All collected logs</returns>
        [HttpGet("/logs")]
        public IActionResult GetLogs()
        {
            return Ok(LogCollector.GetAll());
        }

        /// <summary>
        /// Handles EventGrid events for incoming calls and recording status updates
        /// </summary>
        /// <param name="eventGridEvents">The array of EventGrid events</param>
        /// <returns>Action result indicating success or error</returns>
        [HttpPost("events")]
        public async Task<IActionResult> HandleEvents([FromBody] EventGridEvent[] eventGridEvents)
        {
            try
            {
                _logger.LogInformation($"Received {eventGridEvents.Length} event(s) in /api/events");
                foreach (var eventGridEvent in eventGridEvents)
                {
                    try
                    {
                        _logger.LogInformation($"Processing event: {eventGridEvent.EventType}, Id: {eventGridEvent.Id}");

                        if (eventGridEvent.TryGetSystemEventData(out object eventData))
                        {
                            if (eventData is SubscriptionValidationEventData subscriptionValidationEventData)
                            {
                                _logger.LogInformation($"Subscription validation event received with code: {subscriptionValidationEventData.ValidationCode}");

                                var responseData = new SubscriptionValidationResponse
                                {
                                    ValidationResponse = subscriptionValidationEventData.ValidationCode
                                };
                                return Ok(responseData);
                            }
                            if (eventData is AcsIncomingCallEventData incomingCallEventData)
                            {
                                try
                                {
                                    var callerId = incomingCallEventData.FromCommunicationIdentifier.RawId;
                                    _logger.LogInformation($"Incoming call from caller ID: {callerId}, CorrelationId: {incomingCallEventData.CorrelationId}");

                                    var callbackUri = new Uri(new Uri(_config.CallbackUriHost), $"/api/callbacks");
                                    _logger.LogInformation($"Incoming call - correlationId: {incomingCallEventData.CorrelationId}, Callback url: {callbackUri}");

                                    var options = new AnswerCallOptions(incomingCallEventData.IncomingCallContext, callbackUri)
                                    {
                                        // ACS GCCH Phase 2
                                        // CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) }
                                    };

                                    _logger.LogInformation($"Answering call with correlationId: {incomingCallEventData.CorrelationId}");

                                    AnswerCallResult answerCallResult = await _service.GetCallAutomationClient().AnswerCallAsync(options);
                                    var callConnectionMedia = answerCallResult.CallConnection.GetCallMedia();

                                    _logger.LogInformation($"Call answered successfully. CallConnectionId: {answerCallResult.CallConnection.CallConnectionId}");
                                }
                                catch (Exception callEx)
                                {
                                    _logger.LogError($"Error handling incoming call: {callEx.Message}");
                                }
                            }
                            if (eventData is AcsRecordingFileStatusUpdatedEventData statusUpdated)
                            {
                                try
                                {
                                    CallAutomationService.SetRecordingLocation(statusUpdated.RecordingStorageInfo.RecordingChunks[0].ContentLocation);
                                    _logger.LogInformation($"The recording location is: {statusUpdated.RecordingStorageInfo.RecordingChunks[0].ContentLocation}");
                                }
                                catch (Exception recordingEx)
                                {
                                    _logger.LogError($"Error handling recording status: {recordingEx.Message}");
                                }
                            }
                        }
                    }
                    catch (Exception eventEx)
                    {
                        _logger.LogError($"Error processing event: {eventEx.Message}");
                    }
                }
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in /api/events: {ex.Message}");
                return Problem($"Error processing events: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles Call Automation callbacks for various call events
        /// </summary>
        /// <param name="cloudEvents">The array of CloudEvents</param>
        /// <returns>Action result indicating success or error</returns>
        [HttpPost("callbacks")]
        public IActionResult HandleCallbacks([FromBody] CloudEvent[] cloudEvents)
        {
            try
            {
                _logger.LogInformation($"Received {cloudEvents.Length} cloud event(s) in /api/callbacks");

                foreach (var cloudEvent in cloudEvents)
                {
                    try
                    {
                        CallAutomationEventBase parsedEvent = CallAutomationEventParser.Parse(cloudEvent);
                        _logger.LogInformation(
                            "Received call event: {type}, callConnectionID: {connId}, serverCallId: {serverId}",
                            parsedEvent.GetType(),
                            parsedEvent.CallConnectionId,
                            parsedEvent.ServerCallId);

                        ProcessCallEvent(parsedEvent);
                    }
                    catch (Exception eventEx)
                    {
                        _logger.LogError($"Error processing call event: {eventEx.Message}");
                    }
                }
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in /api/callbacks: {ex.Message}");
                return Problem($"Error processing callbacks: {ex.Message}");
            }
        }
        ///// <summary>
        ///// Updates the IsArizona configuration and switches PMA endpoint accordingly
        ///// </summary>
        ///// <param name="isArizona">Boolean flag to determine which PMA endpoint to use</param>
        ///// <returns>Action result indicating success or error</returns>
        //[HttpPost("/setRegion")]
        //[Tags("Region Configuration")]
        //public IActionResult SetRegion(bool isArizona)
        //{
        //    try
        //    {
        //        _logger.LogInformation($"Changing region configuration. IsArizona: {isArizona}");

        //        // Get the current configuration section
        //        var configSection = HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetSection("CommunicationSettings");

        //        // Get the current endpoint being used to determine if an update is needed
        //        string currentEndpoint = _service.GetCurrentPmaEndpoint() ?? string.Empty;
        //        string newEndpoint = isArizona
        //            ? configSection["PmaEndpointArizona"] ?? string.Empty
        //            : configSection["PmaEndpointTexas"] ?? string.Empty;

        //        // Check if new endpoint is empty
        //        if (string.IsNullOrEmpty(newEndpoint))
        //        {
        //            _logger.LogWarning($"The {(isArizona ? "PmaEndpointArizona" : "PmaEndpointTexas")} setting is empty");
        //        }

        //        // Only update if the endpoint would actually change
        //        if (currentEndpoint == newEndpoint)
        //        {
        //            if (string.IsNullOrEmpty(currentEndpoint))
        //            {
        //                return Ok($"Configuration unchanged as the endpoints are empty");
        //            }
        //            else
        //            {
        //                return Ok($"Configuration unchanged. Already using {(isArizona ? "Arizona" : "Texas")} region.");
        //            }
        //        }

        //        // Update the IsArizona setting in memory
        //        ((IConfigurationSection)configSection.GetSection("IsArizona")).Value = isArizona.ToString();

        //        // Update the client with the new endpoint
        //        var connectionString = configSection["AcsConnectionString"] ?? string.Empty;
        //        if (string.IsNullOrEmpty(connectionString))
        //        {
        //            _logger.LogError("AcsConnectionString is empty");
        //            return Problem("AcsConnectionString is empty");
        //        }

        //        _service.UpdateClient(connectionString, newEndpoint);

        //        return Ok($"Region updated successfully to {(isArizona ? "Arizona" : "Texas")}.");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"Error updating region configuration: {ex.Message}");
        //        return Problem($"Failed to update region configuration: {ex.Message}");
        //    }
        //}


        /// <summary>
        /// Processes individual call automation events
        /// </summary>
        /// <param name="parsedEvent">The parsed call automation event</param>
        private void ProcessCallEvent(CallAutomationEventBase parsedEvent)
        {
            if (parsedEvent is CallConnected callConnected)
            {
                try
                {
                    _logger.LogInformation($"Processing CallConnected event for CallConnectionId: {callConnected.CallConnectionId}");

                    CallConnectionProperties callConnectionProperties = _service.GetCallConnectionProperties(callConnected.CallConnectionId);

                    _logger.LogInformation($"Received call event: {callConnected.GetType()}, CallConnectionId: {callConnected.CallConnectionId}");

                    _logger.LogInformation("************************************************************");

                    _logger.LogInformation($"CORRELATION ID: {callConnectionProperties.CorrelationId}");

                    _logger.LogInformation("************************************************************");
                }
                catch (Exception eventEx)
                {
                    _logger.LogError($"Error processing CallConnected event: {eventEx.Message}");
                }
            }
            else if (parsedEvent is ConnectFailed connectFailed)
            {
                _logger.LogInformation($"Received call event: {connectFailed.GetType()}, CallConnectionId: {connectFailed.CallConnectionId}, CorrelationId: {connectFailed.CorrelationId}, " +
                          $"subCode: {connectFailed.ResultInformation?.SubCode}, message: {connectFailed.ResultInformation?.Message}, context: {connectFailed.OperationContext}");
            }
            else if (parsedEvent is RecognizeCompleted recognizeCompleted)
            {
                _logger.LogInformation($"Received call event: {recognizeCompleted.GetType()}, CallConnectionId: {recognizeCompleted.CallConnectionId}");
                _logger.LogInformation($"Operation Context: {recognizeCompleted.OperationContext}");

                switch (recognizeCompleted.RecognizeResult)
                {
                    case DtmfResult dtmfResult:
                        var tones = dtmfResult.Tones;
                        // Join all the DTMF tones into a single string
                        var toneList = string.Join(", ", tones.Select(t => t.ToString()));
                        _logger.LogInformation(
                            $"Recognize completed successfully, CallConnectionId: {recognizeCompleted.CallConnectionId}, tones=[{toneList}]"
                        );
                        break;
                    case ChoiceResult choiceResult:
                        var labelDetected = choiceResult.Label;
                        var phraseDetected = choiceResult.RecognizedPhrase;
                        _logger.LogInformation(
                            $"Recognize completed successfully, CallConnectionId: {recognizeCompleted.CallConnectionId}, labelDetected={labelDetected}, phraseDetected={phraseDetected}"
                        );
                        break;
                    case SpeechResult speechResult:
                        var text = speechResult.Speech;
                        _logger.LogInformation(
                            $"Recognize completed successfully, CallConnectionId: {recognizeCompleted.CallConnectionId}, text={text}"
                        );
                        break;
                    default:
                        _logger.LogInformation(
                            $"Recognize completed successfully, CallConnectionId: {recognizeCompleted.CallConnectionId}, recognizeResult={recognizeCompleted.RecognizeResult}"
                        );
                        break;
                }
            }
            else if (parsedEvent is RecognizeFailed recognizeFailed)
            {
                _logger.LogInformation($"Received call event: {recognizeFailed.GetType()}, CallConnectionId: {recognizeFailed.CallConnectionId}, CorrelationId: {recognizeFailed.CorrelationId}, " +
                           $"subCode: {recognizeFailed.ResultInformation?.SubCode}, message: {recognizeFailed.ResultInformation?.Message}, context: {recognizeFailed.OperationContext}");
                _logger.LogInformation($"Failed play source index: {recognizeFailed.FailedPlaySourceIndex}");
            }
            else if (parsedEvent is PlayCompleted playCompleted)
            {
                _logger.LogInformation($"Received call event: {playCompleted.GetType()}, CallConnectionId: {playCompleted.CallConnectionId}");
                _logger.LogInformation($"Operation Context: {playCompleted.OperationContext}");
            }
            else if (parsedEvent is PlayFailed playFailed)
            {
                _logger.LogInformation($"Received call event: {playFailed.GetType()}, CallConnectionId: {playFailed.CallConnectionId}, CorrelationId: {playFailed.CorrelationId}, " +
                          $"subCode: {playFailed.ResultInformation?.SubCode}, message: {playFailed.ResultInformation?.Message}, context: {playFailed.OperationContext}");
                _logger.LogInformation($"Failed play source index: {playFailed.FailedPlaySourceIndex}");
            }
            else if (parsedEvent is PlayCanceled playCanceled)
            {
                _logger.LogInformation($"Received call event: {playCanceled.GetType()}, CallConnectionId: {playCanceled.CallConnectionId}");
            }
            else if (parsedEvent is RecognizeCanceled recognizeCanceled)
            {
                _logger.LogInformation($"Received call event: {recognizeCanceled.GetType()}, CallConnectionId: {recognizeCanceled.CallConnectionId}");
            }
            else if (parsedEvent is AddParticipantSucceeded addParticipantSucceeded)
            {
                _logger.LogInformation($"Received call event: {addParticipantSucceeded.GetType()}, CallConnectionId: {addParticipantSucceeded.CallConnectionId}");
            }
            else if (parsedEvent is AddParticipantFailed addParticipantFailed)
            {
                _logger.LogInformation($"Received call event: {addParticipantFailed.GetType()}, CallConnectionId: {addParticipantFailed.CallConnectionId}, CorrelationId: {addParticipantFailed.CorrelationId}, " +
                          $"subCode: {addParticipantFailed.ResultInformation?.SubCode}, message: {addParticipantFailed.ResultInformation?.Message}, context: {addParticipantFailed.OperationContext}");
            }
            else if (parsedEvent is RemoveParticipantSucceeded removeParticipantSucceeded)
            {
                _logger.LogInformation($"Received call event: {removeParticipantSucceeded.GetType()}, CallConnectionId: {removeParticipantSucceeded.CallConnectionId}");
            }
            else if (parsedEvent is RemoveParticipantFailed removeParticipantFailed)
            {
                _logger.LogInformation($"Received call event: {removeParticipantFailed.GetType()}, CallConnectionId: {removeParticipantFailed.CallConnectionId}, CorrelationId: {removeParticipantFailed.CorrelationId}, " +
                          $"subCode: {removeParticipantFailed.ResultInformation?.SubCode}, message: {removeParticipantFailed.ResultInformation?.Message}, context: {removeParticipantFailed.OperationContext}");
            }
            else if (parsedEvent is CancelAddParticipantSucceeded cancelAddParticipantSucceeded)
            {
                _logger.LogInformation($"Received call event: {cancelAddParticipantSucceeded.GetType()}, CallConnectionId: {cancelAddParticipantSucceeded.CallConnectionId}");
            }
            else if (parsedEvent is CancelAddParticipantFailed cancelAddParticipantFailed)
            {
                _logger.LogInformation($"Received call event: {cancelAddParticipantFailed.GetType()}, CallConnectionId: {cancelAddParticipantFailed.CallConnectionId}, CorrelationId: {cancelAddParticipantFailed.CorrelationId}, " +
                          $"subCode: {cancelAddParticipantFailed.ResultInformation?.SubCode}, message: {cancelAddParticipantFailed.ResultInformation?.Message}, context: {cancelAddParticipantFailed.OperationContext}");
            }
            else if (parsedEvent is SendDtmfTonesCompleted sendDtmfTonesCompleted)
            {
                _logger.LogInformation($"Received call event: {sendDtmfTonesCompleted.GetType()}, CallConnectionId: {sendDtmfTonesCompleted.CallConnectionId}");
            }
            else if (parsedEvent is SendDtmfTonesFailed sendDtmfTonesFailed)
            {
                _logger.LogInformation($"Received call event: {sendDtmfTonesFailed.GetType()}, CallConnectionId: {sendDtmfTonesFailed.CallConnectionId}, CorrelationId: {sendDtmfTonesFailed.CorrelationId}, " +
                          $"subCode: {sendDtmfTonesFailed.ResultInformation?.SubCode}, message: {sendDtmfTonesFailed.ResultInformation?.Message}, context: {sendDtmfTonesFailed.OperationContext}");
            }
            else if (parsedEvent is ContinuousDtmfRecognitionToneReceived continuousDtmfRecognitionToneReceived)
            {
                _logger.LogInformation($"Received call event: {continuousDtmfRecognitionToneReceived.GetType()}, CallConnectionId: {continuousDtmfRecognitionToneReceived.CallConnectionId}");

                _logger.LogInformation($"Tone detected: sequenceId={continuousDtmfRecognitionToneReceived.SequenceId}, tone={continuousDtmfRecognitionToneReceived.Tone}");
            }
            else if (parsedEvent is ContinuousDtmfRecognitionStopped continuousDtmfRecognitionStopped)
            {
                _logger.LogInformation($"Received call event: {continuousDtmfRecognitionStopped.GetType()}, CallConnectionId: {continuousDtmfRecognitionStopped.CallConnectionId}");
            }
            else if (parsedEvent is ContinuousDtmfRecognitionToneFailed continuousDtmfRecognitionToneFailed)
            {
                _logger.LogInformation($"Received call event: {continuousDtmfRecognitionToneFailed.GetType()}, CallConnectionId: {continuousDtmfRecognitionToneFailed.CallConnectionId}, CorrelationId: {continuousDtmfRecognitionToneFailed.CorrelationId}, " +
                          $"subCode: {continuousDtmfRecognitionToneFailed.ResultInformation?.SubCode}, message: {continuousDtmfRecognitionToneFailed.ResultInformation?.Message}, context: {continuousDtmfRecognitionToneFailed.OperationContext}");
            }
            //else if (parsedEvent is HoldAudioStarted holdAudioStarted)
            //{
            //    _logger.LogInformation($"Received call event: {holdAudioStarted.GetType()}, CallConnectionId: {holdAudioStarted.CallConnectionId}");
            //}
            //else if (parsedEvent is HoldAudioPaused holdAudioPaused)
            //{
            //    _logger.LogInformation($"Received call event: {holdAudioPaused.GetType()}, CallConnectionId: {holdAudioPaused.CallConnectionId}");
            //}
            //else if (parsedEvent is HoldAudioResumed holdAudioResumed)
            //{
            //    _logger.LogInformation($"Received call event: {holdAudioResumed.GetType()}, CallConnectionId: {holdAudioResumed.CallConnectionId}");
            //}
            //else if (parsedEvent is HoldAudioCompleted holdAudioCompleted)
            //{
            //    _logger.LogInformation($"Received call event: {holdAudioCompleted.GetType()}, CallConnectionId: {holdAudioCompleted.CallConnectionId}");
            //}
            //else if (parsedEvent is HoldFailed holdFailed)
            //{
            //    _logger.LogInformation($"Received call event: {holdFailed.GetType()}, CallConnectionId: {holdFailed.CallConnectionId}, CorrelationId: {holdFailed.CorrelationId}, " +
            //              $"subCode: {holdFailed.ResultInformation?.SubCode}, message: {holdFailed.ResultInformation?.Message}, context: {holdFailed.OperationContext}");
            //}
            //else if (parsedEvent is TranscriptionStarted transcriptionStarted)
            //{
            //    _logger.LogInformation($"Received call event: {transcriptionStarted.GetType()}, CallConnectionId: {transcriptionStarted.CallConnectionId}");
            //    _logger.LogInformation($"Operation context: {transcriptionStarted.OperationContext}");
            //}
            //else if (parsedEvent is TranscriptionStopped transcriptionStopped)
            //{
            //    _logger.LogInformation($"Received call event: {transcriptionStopped.GetType()}, CallConnectionId: {transcriptionStopped.CallConnectionId}");
            //    _logger.LogInformation($"Operation context: {transcriptionStopped.OperationContext}");
            //}
            //else if (parsedEvent is TranscriptionUpdated transcriptionUpdated)
            //{
            //    _logger.LogInformation($"Received call event: {transcriptionUpdated.GetType()}, CallConnectionId: {transcriptionUpdated.CallConnectionId}, CorrelationId: {transcriptionUpdated.CorrelationId}");
            //    _logger.LogInformation($"Operation context: {transcriptionUpdated.OperationContext}");
            //}
            //else if (parsedEvent is MediaStreamingStarted mediaStreamingStarted)
            //{
            //    _logger.LogInformation($"Received call event: {mediaStreamingStarted.GetType()}, CallConnectionId: {mediaStreamingStarted.CallConnectionId}, CorrelationId: {mediaStreamingStarted.CorrelationId}");
            //    _logger.LogInformation($"Operation context: {mediaStreamingStarted.OperationContext}");
            //}
            //else if (parsedEvent is MediaStreamingStopped mediaStreamingStopped)
            //{
            //    _logger.LogInformation($"Received call event: {mediaStreamingStopped.GetType()}, CallConnectionId: {mediaStreamingStopped.CallConnectionId}, CorrelationId: {mediaStreamingStopped.CorrelationId}");
            //    _logger.LogInformation($"Operation context: {mediaStreamingStopped.OperationContext}");
            //}
            //else if (parsedEvent is MediaStreamingFailed mediaStreamingFailed)
            //{
            //    _logger.LogInformation($"Received call event: {mediaStreamingFailed.GetType()}, CorrelationId: {mediaStreamingFailed.CorrelationId}, " +
            //              $"subCode: {mediaStreamingFailed.ResultInformation?.SubCode}, message: {mediaStreamingFailed.ResultInformation?.Message}, context: {mediaStreamingFailed.OperationContext}");
            //}
            else if (parsedEvent is CallDisconnected callDisconnected)
            {
                _logger.LogInformation($"Received call event: {callDisconnected.GetType()}, CallConnectionId: {callDisconnected.CallConnectionId}, CorrelationId: {callDisconnected.CorrelationId}");
            }
            else if (parsedEvent is CreateCallFailed createCallFailed)
            {
                _logger.LogInformation($"Received call event: {createCallFailed.GetType()}, CorrelationId: {createCallFailed.CorrelationId}, " +
                          $"subCode: {createCallFailed.ResultInformation?.SubCode}, message: {createCallFailed.ResultInformation?.Message}, context: {createCallFailed.OperationContext}");
            }
            else if (parsedEvent is CallTransferAccepted callTransferAccepted)
            {
                _logger.LogInformation($"Received call event: {callTransferAccepted.GetType()}, CallConnectionId: {callTransferAccepted.CallConnectionId}, CorrelationId: {callTransferAccepted.CorrelationId}");
            }
            else if (parsedEvent is CallTransferFailed callTransferFailed)
            {
                _logger.LogInformation($"Received call event: {callTransferFailed.GetType()}, CorrelationId: {callTransferFailed.CorrelationId}, " +
                          $"subCode: {callTransferFailed.ResultInformation?.SubCode}, message: {callTransferFailed.ResultInformation?.Message}, context: {callTransferFailed.OperationContext}");
            }
            else if (parsedEvent is RecordingStateChanged recordingStateChanged)
            {
                _logger.LogInformation($"Received call event: {recordingStateChanged.GetType()}, CallConnectionId: {recordingStateChanged.CallConnectionId}, CorrelationId: {recordingStateChanged.CorrelationId}");
                _logger.LogInformation($"Recording State: {recordingStateChanged.State}");
            }
            //else if (parsedEvent is DialogStarted dialogStarted)
            //{
            //    _logger.LogInformation($"Received call event: {dialogStarted.GetType()}, CallConnectionId: {dialogStarted.CallConnectionId}, CorrelationId: {dialogStarted.CorrelationId}");
            //    _logger.LogInformation($"Dialog Id: {dialogStarted.DialogId}");

            //}
            //else if (parsedEvent is DialogTransfer dialogTransfer)
            //{
            //    _logger.LogInformation($"Received call event: {dialogTransfer.GetType()}, CallConnectionId: {dialogTransfer.CallConnectionId}, CorrelationId: {dialogTransfer.CorrelationId}");
            //    _logger.LogInformation($"Dialog Id: {dialogTransfer.DialogId}");
            //}
            //else if (parsedEvent is DialogHangup dialogHangup)
            //{
            //    _logger.LogInformation($"Received call event: {dialogHangup.GetType()}, CallConnectionId: {dialogHangup.CallConnectionId}, CorrelationId: {dialogHangup.CorrelationId}");
            //    _logger.LogInformation($"Dialog Id: {dialogHangup.DialogId}");
            //}
            //else if (parsedEvent is DialogConsent dialogConsent)
            //{
            //    _logger.LogInformation($"Received call event: {dialogConsent.GetType()}, CallConnectionId: {dialogConsent.CallConnectionId}, CorrelationId: {dialogConsent.CorrelationId}");
            //    _logger.LogInformation($"Dialog Id: {dialogConsent.DialogId}");
            //}
            //else if (parsedEvent is DialogCompleted dialogCompleted)
            //{
            //    _logger.LogInformation($"Received call event: {dialogCompleted.GetType()}, CallConnectionId: {dialogCompleted.CallConnectionId}, CorrelationId: {dialogCompleted.CorrelationId}");
            //    _logger.LogInformation($"Dialog Id: {dialogCompleted.DialogId}");
            //}
            //else if (parsedEvent is DialogFailed dialogFailed)
            //{
            //    _logger.LogInformation($"Received call event: {dialogFailed.GetType()}, CorrelationId: {dialogFailed.CorrelationId}, " +
            //              $"subCode: {dialogFailed.ResultInformation?.SubCode}, message: {dialogFailed.ResultInformation?.Message}, context: {dialogFailed.OperationContext}");
            //}
        }

        //#region Incoming Call with Media Streaming

        ///// <summary>
        ///// Handles incoming calls with media streaming using query parameters to configure options
        ///// </summary>
        ///// <remarks>
        ///// ## URL Template for Azure Communication Services Configuration
        ///// 
        ///// ```
        ///// https://your-domain.com/api/events/incomingcall?audioChannelMixed=true&amp;audioFormat16k=true&amp;mediaStreaming=true&amp;bidirectionalStreaming=true
        ///// ```
        ///// 
        ///// Simply copy this URL and change the true/false values as needed for your specific configuration.
        ///// </remarks>
        ///// <param name="eventGridEvents">The array of EventGrid events</param>
        ///// <param name="audioChannelMixed">If true, use Mixed audio channel; if false, use Unmixed</param>
        ///// <param name="audioFormat16k">If true, use 16kHz format; if false, use 24kHz</param>
        ///// <param name="mediaStreaming">If true, enable media streaming; if false, disable</param>
        ///// <param name="bidirectionalStreaming">If true, enable bidirectional streaming; if false, disable</param>
        ///// <returns>Action result indicating success or error</returns>
        //[HttpPost("events/incomingcall")]
        //[Tags("Incoming Call with Media Streaming Options")]
        //public async Task<IActionResult> HandleIncomingCallWithOptions(
        //    [FromBody] EventGridEvent[] eventGridEvents,
        //    [FromQuery] bool audioChannelMixed = true,
        //    [FromQuery] bool audioFormat16k = true,
        //    [FromQuery] bool mediaStreaming = true,
        //    [FromQuery] bool bidirectionalStreaming = true)
        //{
        //    MediaStreamingAudioChannel audioChannel = audioChannelMixed 
        //        ? MediaStreamingAudioChannel.Mixed 
        //        : MediaStreamingAudioChannel.Unmixed;
            
        //    bool isPcm24kHz = !audioFormat16k;

        //    return await HandleIncomingCallWithMediaStreaming(
        //        eventGridEvents,
        //        audioChannel,
        //        mediaStreaming,
        //        isPcm24kHz,
        //        bidirectionalStreaming);
        //}

        ///// <summary>
        ///// Generic handler for incoming calls with specific media streaming configurations
        ///// </summary>
        //private async Task<IActionResult> HandleIncomingCallWithMediaStreaming(
        //    EventGridEvent[] eventGridEvents,
        //    MediaStreamingAudioChannel audioChannel,
        //    bool enableMediaStreaming,
        //    bool isPcm24kHz,
        //    bool enableBidirectional)
        //{
        //    try
        //    {
        //        _logger.LogInformation($"Received {eventGridEvents.Length} event(s) for incoming call with media streaming");
        //        foreach (var eventGridEvent in eventGridEvents)
        //        {
        //            try
        //            {
        //                _logger.LogInformation($"Processing event: {eventGridEvent.EventType}, Id: {eventGridEvent.Id}");

        //                if (eventGridEvent.TryGetSystemEventData(out object eventData))
        //                {
        //                    if (eventData is SubscriptionValidationEventData subscriptionValidationEventData)
        //                    {
        //                        _logger.LogInformation($"Subscription validation event received with code: {subscriptionValidationEventData.ValidationCode}");

        //                        var responseData = new SubscriptionValidationResponse
        //                        {
        //                            ValidationResponse = subscriptionValidationEventData.ValidationCode
        //                        };
        //                        return Ok(responseData);
        //                    }
        //                    if (eventData is AcsIncomingCallEventData incomingCallEventData)
        //                    {
        //                        try
        //                        {
        //                            var callerId = incomingCallEventData.FromCommunicationIdentifier.RawId;
        //                            _logger.LogInformation($"Incoming call from caller ID: {callerId}, CorrelationId: {incomingCallEventData.CorrelationId}");

        //                            var callbackUri = new Uri(new Uri(_config.CallbackUriHost), $"/api/callbacks");
        //                            var websocketUri = new Uri(_config.CallbackUriHost.Replace("https", "wss") + "/ws");
                                    
        //                            _logger.LogInformation($"Incoming call with media streaming - correlationId: {incomingCallEventData.CorrelationId}, " +
        //                                $"AudioChannel: {audioChannel}, EnableMediaStreaming: {enableMediaStreaming}, " +
        //                                $"IsPcm24kHz: {isPcm24kHz}, EnableBidirectional: {enableBidirectional}");

        //                            MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(
        //                                websocketUri,
        //                                MediaStreamingContent.Audio,
        //                                audioChannel,
        //                                MediaStreamingTransport.Websocket,
        //                                enableMediaStreaming)
        //                            {
        //                                EnableBidirectional = enableBidirectional,
        //                                AudioFormat = isPcm24kHz ? AudioFormat.Pcm24KMono : AudioFormat.Pcm16KMono
        //                            };

        //                            var options = new AnswerCallOptions(incomingCallEventData.IncomingCallContext, callbackUri)
        //                            {
        //                                MediaStreamingOptions = mediaStreamingOptions
        //                                // ACS GCCH Phase 2
        //                                // CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) }
        //                            };

        //                            _logger.LogInformation($"Answering call with correlationId: {incomingCallEventData.CorrelationId} and media streaming options " +
        //                                $"(AudioChannel: {(audioChannel == MediaStreamingAudioChannel.Mixed ? "Mixed" : "Unmixed")}, " +
        //                                $"Format: {(isPcm24kHz ? "24kHz" : "16kHz")}, " +
        //                                $"Streaming: {(enableMediaStreaming ? "Enabled" : "Disabled")}, " +
        //                                $"Bidirectional: {(enableBidirectional ? "Enabled" : "Disabled")})");

        //                            AnswerCallResult answerCallResult = await _service.GetCallAutomationClient().AnswerCallAsync(options);
        //                            var callConnectionMedia = answerCallResult.CallConnection.GetCallMedia();

        //                            _logger.LogInformation($"Call answered successfully with media streaming. CallConnectionId: {answerCallResult.CallConnection.CallConnectionId}");

        //                            // Start media streaming if enabled
        //                            if (enableMediaStreaming)
        //                            {
        //                                try
        //                                {
        //                                    await callConnectionMedia.StartMediaStreamingAsync();
        //                                    _logger.LogInformation($"Media streaming started for CallConnectionId: {answerCallResult.CallConnection.CallConnectionId}");
        //                                }
        //                                catch (Exception streamingEx)
        //                                {
        //                                    _logger.LogError($"Error starting media streaming: {streamingEx.Message}");
        //                                }
        //                            }
        //                        }
        //                        catch (Exception callEx)
        //                        {
        //                            _logger.LogError($"Error handling incoming call with media streaming: {callEx.Message}");
        //                        }
        //                    }
        //                    if (eventData is AcsRecordingFileStatusUpdatedEventData statusUpdated)
        //                    {
        //                        try
        //                        {
        //                            CallAutomationService.SetRecordingLocation(statusUpdated.RecordingStorageInfo.RecordingChunks[0].ContentLocation);
        //                            _logger.LogInformation($"The recording location is: {statusUpdated.RecordingStorageInfo.RecordingChunks[0].ContentLocation}");
        //                        }
        //                        catch (Exception recordingEx)
        //                        {
        //                            _logger.LogError($"Error handling recording status: {recordingEx.Message}");
        //                        }
        //                    }
        //                }
        //            }
        //            catch (Exception eventEx)
        //            {
        //                _logger.LogError($"Error processing event: {eventEx.Message}");
        //            }
        //        }
        //        return Ok();
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"Error in handling incoming call with media streaming: {ex.Message}");
        //        return Problem($"Error processing events: {ex.Message}");
        //    }
        //}

        //#endregion
    }
}



//app.MapPost("/setConfigurations", (ConfigurationRequest configurationRequest, CallAutomationService service, ILogger<Program> logger) =>
//{
//    try
//    {
//        logger.LogInformation("Setting configurations...");

//        acsConnectionString = string.Empty;
//        acsPhoneNumber = string.Empty;
//        callbackUriHost = string.Empty;
//     //   fileSourceUri = string.Empty;

//        if (configurationRequest != null)
//        {
//            configuration.AcsConnectionString = !string.IsNullOrEmpty(configurationRequest.AcsConnectionString)
//                ? configurationRequest.AcsConnectionString
//                : throw new ArgumentNullException(nameof(configurationRequest.AcsConnectionString));

//            configuration.pmaEndpoint = !string.IsNullOrEmpty(configurationRequest.pmaEndpoint) ? configurationRequest.pmaEndpoint : throw new ArgumentNullException(nameof(configurationRequest.pmaEndpoint));
//            //configuration.CongnitiveServiceEndpoint = !string.IsNullOrEmpty(configurationRequest.CongnitiveServiceEndpoint) ? configurationRequest.CongnitiveServiceEndpoint : throw new ArgumentNullException(nameof(configurationRequest.CongnitiveServiceEndpoint));
//            configuration.AcsPhoneNumber = !string.IsNullOrEmpty(configurationRequest.AcsPhoneNumber) ? configurationRequest.AcsPhoneNumber : throw new ArgumentNullException(nameof(configurationRequest.AcsPhoneNumber));
//            configuration.CallbackUriHost = !string.IsNullOrEmpty(configurationRequest.CallbackUriHost) ? configurationRequest.CallbackUriHost : throw new ArgumentNullException(nameof(configurationRequest.CallbackUriHost));
//            service.SetConfiguration(configurationRequest);
//        }

//        acsConnectionString = configuration.AcsConnectionString;
//        acsPhoneNumber = configuration.AcsPhoneNumber;
//        callbackUriHost = configuration.CallbackUriHost;
//        //  fileSourceUri = "https://sample-videos.com/audio/mp3/crowd-cheering.mp3";

//        logger.LogInformation($"Configuration set: AcsPhoneNumber={acsPhoneNumber}, CallbackUriHost={callbackUriHost}");

//        client = new CallAutomationClient(connectionString: acsConnectionString);
//        logger.LogInformation("Initialized call automation client.");
//        return Results.Ok("Configuration set successfully. Initialized call automation client.");
//    }
//    catch (Exception ex)
//    {
//        logger.LogError($"Error in setConfigurations: {ex.Message}");
//        return Results.Problem($"Failed to set configuration: {ex.Message}");
//    }
//}).WithTags("1. Add Connection string and configuration settings.");