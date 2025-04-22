using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
using Azure;
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
    [Route("api/media")]
    [Produces("application/json")]
    public class MediaController : ControllerBase
    {
        private readonly CallAutomationService _service;
        private readonly ILogger<MediaController> _logger;
        private readonly ConfigurationRequest _config; // final, bound object

        public MediaController(
            CallAutomationService service,
            ILogger<MediaController> logger, IOptions<ConfigurationRequest> configOptions)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = configOptions.Value ?? throw new ArgumentNullException(nameof(configOptions));
        }

        #region Play Media with File Source

        /// <summary>
        /// Plays a file to a specific ACS target asynchronously.
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="acsTarget">The ACS user identifier to play to</param>
        /// <returns>Play operation result</returns>
        [HttpPost("/playFileSourceAcsTargetAsync")]
        [ProducesResponseType(typeof(CallConnectionResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        [Tags("Play FileSource Media APIs")]
        public async Task<IActionResult> PlayFileSourceAcsTargetAsync(string callConnectionId, string acsTarget)
        {
            try
            {
                if (string.IsNullOrEmpty(callConnectionId))
                {
                    return BadRequest("Call Connection ID is required");
                }

                if (string.IsNullOrEmpty(acsTarget))
                {
                    return BadRequest("ACS Target ID is required");
                }

                _logger.LogInformation($"Playing file source to ACS target. CallConnectionId: {callConnectionId}, Target: {acsTarget}");

                CallMedia callMedia = _service.GetCallMedia(callConnectionId);
                var _fileSourceUri = _config.CallbackUriHost + "/audio/prompt.wav";
                FileSource fileSource = new FileSource(new Uri(_fileSourceUri));
                var correlationId = (_service.GetCallConnectionProperties(callConnectionId)).CorrelationId;
                var callStatus = (_service.GetCallConnectionProperties(callConnectionId)).CallConnectionState.ToString();

                var playTo = new List<CommunicationIdentifier>
                {
                    new CommunicationUserIdentifier(acsTarget)
                };

                var playToOptions = new PlayOptions(fileSource, playTo)
                {
                    OperationContext = "playToContext"
                };

                var playResponse = await callMedia.PlayAsync(playToOptions);
                var operationStatus = playResponse.GetRawResponse().ToString();

                string successMessage = $"File source played successfully to ACS target. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, CallStatus: {callStatus}, OperationStatus: {operationStatus}";
                _logger.LogInformation(successMessage);

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = operationStatus
                });
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error playing file source to ACS target. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
                _logger.LogError(errorMessage);

                return Problem($"Failed to play file source: {ex.Message}");
            }
        }

        /// <summary>
        /// Plays a file to a specific ACS target synchronously.
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="acsTarget">The ACS user identifier to play to</param>
        /// <returns>Play operation result</returns>
        [HttpPost("/playFileSourceToAcsTarget")]
        [ProducesResponseType(typeof(CallConnectionResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        [Tags("Play FileSource Media APIs")]
        public IActionResult PlayFileSourceToAcsTarget(string callConnectionId, string acsTarget)
        {
            try
            {
                if (string.IsNullOrEmpty(callConnectionId))
                {
                    return BadRequest("Call Connection ID is required");
                }

                if (string.IsNullOrEmpty(acsTarget))
                {
                    return BadRequest("ACS Target ID is required");
                }

                _logger.LogInformation($"Playing file source to ACS target. CallConnectionId: {callConnectionId}, Target: {acsTarget}");

                CallMedia callMedia = _service.GetCallMedia(callConnectionId);
                var _fileSourceUri = _config.CallbackUriHost + "/audio/prompt.wav";
                FileSource fileSource = new FileSource(new Uri(_fileSourceUri));
                var correlationId = (_service.GetCallConnectionProperties(callConnectionId)).CorrelationId;
                var callStatus = (_service.GetCallConnectionProperties(callConnectionId)).CallConnectionState.ToString();

                var playTo = new List<CommunicationIdentifier>
                {
                    new CommunicationUserIdentifier(acsTarget)
                };

                var playToOptions = new PlayOptions(fileSource, playTo)
                {
                    OperationContext = "playToContext"
                };

                var playResponse = callMedia.Play(playToOptions);
                var operationStatus = playResponse.GetRawResponse().ToString();

                string successMessage = $"File source played successfully to ACS target. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, CallStatus: {callStatus}, OperationStatus: {operationStatus}";
                _logger.LogInformation(successMessage);

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = operationStatus
                });
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error playing file source to ACS target. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
                _logger.LogError(errorMessage);

                return Problem($"Failed to play file source: {ex.Message}");
            }
        }

        /// <summary>
        /// Plays a file to all participants asynchronously.
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <returns>Play operation result</returns>
        [HttpPost("/playFileSourceToAllAsync")]
        [Tags("Play FileSource Media APIs")]
        public async Task<IActionResult> PlayFileSourceToAllAsync(string callConnectionId)
        {
            try
            {
                if (string.IsNullOrEmpty(callConnectionId))
                {
                    return BadRequest("Call Connection ID is required");
                }

                _logger.LogInformation($"Playing file source to all participants. CallConnectionId: {callConnectionId}");

                CallMedia callMedia = _service.GetCallMedia(callConnectionId);
                var _fileSourceUri = _config.CallbackUriHost + "/audio/prompt.wav";
                FileSource fileSource = new FileSource(new Uri(_fileSourceUri));
                var correlationId = (_service.GetCallConnectionProperties(callConnectionId)).CorrelationId;
                var callStatus = (_service.GetCallConnectionProperties(callConnectionId)).CallConnectionState.ToString();

                var playToAllOptions = new PlayToAllOptions(fileSource)
                {
                    OperationContext = "playToAllContext"
                };

                var playResponse = await callMedia.PlayToAllAsync(playToAllOptions);
                var operationStatus = playResponse.GetRawResponse().ToString();

                string successMessage = $"File source played successfully to all participants. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, CallStatus: {callStatus}, OperationStatus: {operationStatus}";
                _logger.LogInformation(successMessage);

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = operationStatus
                });
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error playing file source to all participants. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
                _logger.LogError(errorMessage);

                return Problem($"Failed to play file source: {ex.Message}");
            }
        }

        /// <summary>
        /// Plays a file to all participants synchronously.
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <returns>Play operation result</returns>
        [HttpPost("/playFileSourceToAll")]
        [Tags("Play FileSource Media APIs")]
        public IActionResult PlayFileSourceToAll(string callConnectionId)
        {
            try
            {
                if (string.IsNullOrEmpty(callConnectionId))
                {
                    return BadRequest("Call Connection ID is required");
                }

                _logger.LogInformation($"Playing file source to all participants. CallConnectionId: {callConnectionId}");

                CallMedia callMedia = _service.GetCallMedia(callConnectionId);
                var _fileSourceUri = _config.CallbackUriHost + "/audio/prompt.wav";
                FileSource fileSource = new FileSource(new Uri(_fileSourceUri));
                var correlationId = (_service.GetCallConnectionProperties(callConnectionId)).CorrelationId;
                var callStatus = (_service.GetCallConnectionProperties(callConnectionId)).CallConnectionState.ToString();

                var playToAllOptions = new PlayToAllOptions(fileSource)
                {
                    OperationContext = "playToAllContext"
                };

                var playResponse = callMedia.PlayToAll(playToAllOptions);
                var operationStatus = playResponse.GetRawResponse().ToString();

                string successMessage = $"File source played successfully to all participants. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, CallStatus: {callStatus}, OperationStatus: {operationStatus}";
                _logger.LogInformation(successMessage);

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = operationStatus
                });
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error playing file source to all participants. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
                _logger.LogError(errorMessage);

                return Problem($"Failed to play file source: {ex.Message}");
            }
        }

        /// <summary>
        /// Plays a file source to all participants with barge-in enabled (interrupting current media).
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <returns>Play operation result</returns>
        [HttpPost("/playFileSourceBargeInAsync")]
        [Tags("Play FileSource Media APIs")]
        public async Task<IActionResult> PlayFileSourceBargeInAsync(string callConnectionId)
        {
            try
            {
                if (string.IsNullOrEmpty(callConnectionId))
                {
                    return BadRequest("Call Connection ID is required");
                }

                _logger.LogInformation($"Playing file source barge-in. CallConnectionId: {callConnectionId}");

                CallMedia callMedia = _service.GetCallMedia(callConnectionId);
                var _fileSourceUri = _config.CallbackUriHost + "/audio/prompt.wav";
                FileSource fileSource = new FileSource(new Uri(_fileSourceUri));
                var correlationId = (_service.GetCallConnectionProperties(callConnectionId)).CorrelationId;
                var callStatus = (_service.GetCallConnectionProperties(callConnectionId)).CallConnectionState.ToString();

                var playToAllOptions = new PlayToAllOptions(fileSource)
                {
                    OperationContext = "playBargeInContext",
                    InterruptCallMediaOperation = true
                };

                var playResponse = await callMedia.PlayToAllAsync(playToAllOptions);
                var operationStatus = playResponse.GetRawResponse().ToString();

                string successMessage = $"File source barge-in played successfully. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, CallStatus: {callStatus}, OperationStatus: {operationStatus}";
                _logger.LogInformation(successMessage);

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = operationStatus
                });
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error playing file source barge-in. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
                _logger.LogError(errorMessage);

                return Problem($"Failed to play file source: {ex.Message}");
            }
        }
        #endregion
        #region Media Streaming

        [HttpPost("/createCallToAcsWithMediaStreamingAsync")]
        [Tags("Media streaming APIs")]
        public async Task<IActionResult> CreateCallToAcsWithMediaStreamingAsync(
            string acsTarget,
            bool isEnableBidirectional,
            bool isPcm24kMono)
        {
            try
            {
                var callbackUri = new Uri(new Uri(_config.CallbackUriHost), "/api/callbacks");
                var websocketUri = _config.CallbackUriHost.Replace("https", "wss") + "/ws";

                MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(
                    new Uri(websocketUri),
                    MediaStreamingContent.Audio,
                    MediaStreamingAudioChannel.Unmixed,
                    MediaStreamingTransport.Websocket,
                    true)
                {
                    EnableBidirectional = isEnableBidirectional,
                    AudioFormat = isPcm24kMono ? AudioFormat.Pcm24KMono : AudioFormat.Pcm16KMono
                };

                var callInvite = new CallInvite(new CommunicationUserIdentifier(acsTarget));
                var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
                {
                    MediaStreamingOptions = mediaStreamingOptions
                };

                CreateCallResult createCallResult = await _service.GetCallAutomationClient().CreateCallAsync(createCallOptions);
                string callConnectionId = createCallResult.CallConnectionProperties.CallConnectionId;
                string correlationId = createCallResult.CallConnectionProperties.CorrelationId;
                // Use call state or some other property as "operation status"
                string operationStatus = createCallResult.CallConnectionProperties.CallConnectionState.ToString();

                string successMessage = $"[createCallToAcsWithMediaStreamingAsync] Created ACS call. " +
                                        $"CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, Status: {operationStatus}";
                _logger.LogInformation(successMessage);

                return Ok(new
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = operationStatus
                });
            }
            catch (Exception ex)
            {
                string errorMessage = $"[createCallToAcsWithMediaStreamingAsync] Error creating ACS call: {ex.Message}";
                _logger.LogError(errorMessage);
                return Problem($"Failed to create ACS call with media streaming: {ex.Message}");
            }
        }

        [HttpPost("/createCallToAcsWithMediaStreaming")]
        [Tags("Media streaming APIs")]
        public IActionResult CreateCallToAcsWithMediaStreaming(
            string acsTarget,
            bool isEnableBidirectional,
            bool isPcm24kMono)
        {
            try
            {
                var callbackUri = new Uri(new Uri(_config.CallbackUriHost), "/api/callbacks");
                var websocketUri = _config.CallbackUriHost.Replace("https", "wss") + "/ws";

                MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(
                    new Uri(websocketUri),
                    MediaStreamingContent.Audio,
                    MediaStreamingAudioChannel.Unmixed,
                    MediaStreamingTransport.Websocket,
                    true)
                {
                    EnableBidirectional = isEnableBidirectional,
                    AudioFormat = isPcm24kMono ? AudioFormat.Pcm24KMono : AudioFormat.Pcm16KMono
                };

                var callInvite = new CallInvite(new CommunicationUserIdentifier(acsTarget));
                var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
                {
                    MediaStreamingOptions = mediaStreamingOptions
                };

                CreateCallResult createCallResult = _service.GetCallAutomationClient().CreateCall(createCallOptions);
                string callConnectionId = createCallResult.CallConnectionProperties.CallConnectionId;
                string correlationId = createCallResult.CallConnectionProperties.CorrelationId;
                string operationStatus = createCallResult.CallConnectionProperties.CallConnectionState.ToString();

                string successMessage = $"[createCallToAcsWithMediaStreaming] Created ACS call. " +
                                        $"CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, Status: {operationStatus}";
                _logger.LogInformation(successMessage);

                return Ok(new
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = operationStatus
                });
            }
            catch (Exception ex)
            {
                string errorMessage = $"[createCallToAcsWithMediaStreaming] Error creating ACS call: {ex.Message}";
                _logger.LogError(errorMessage);
                return Problem($"Failed to create ACS call with media streaming: {ex.Message}");
            }
        }

        [HttpPost("/startMediaStreamingAsync")]
        [Tags("Media streaming APIs")]
        public async Task<IActionResult> StartMediaStreamingAsync(string callConnectionId)
        {
            try
            {
                var callMedia = _service.GetCallMedia(callConnectionId);
                var correlationId = _service.GetCallConnectionProperties(callConnectionId).CorrelationId;

                Response response = await callMedia.StartMediaStreamingAsync();
                string operationStatus = response.Status.ToString();

                string successMessage = $"[startMediaStreamingAsync] Media streaming started. " +
                                        $"CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, Status: {operationStatus}";
                _logger.LogInformation(successMessage);

                return Ok(new
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = operationStatus
                });
            }
            catch (Exception ex)
            {
                string errorMessage = $"[startMediaStreamingAsync] Error: {ex.Message}, CallConnectionId: {callConnectionId}";
                _logger.LogError(errorMessage);
                return Problem($"Failed to start media streaming: {ex.Message}");
            }
        }

        [HttpPost("/startMediaStreaming")]
        [Tags("Media streaming APIs")]
        public IActionResult StartMediaStreaming(string callConnectionId)
        {
            try
            {
                var callMedia = _service.GetCallMedia(callConnectionId);
                var correlationId = _service.GetCallConnectionProperties(callConnectionId).CorrelationId;

                Response response = callMedia.StartMediaStreaming();
                string operationStatus = response.Status.ToString();

                string successMessage = $"[startMediaStreaming] Media streaming started. " +
                                        $"CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, Status: {operationStatus}";
                _logger.LogInformation(successMessage);

                return Ok(new
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = operationStatus
                });
            }
            catch (Exception ex)
            {
                string errorMessage = $"[startMediaStreaming] Error: {ex.Message}, CallConnectionId: {callConnectionId}";
                _logger.LogError(errorMessage);
                return Problem($"Failed to start media streaming: {ex.Message}");
            }
        }

        [HttpPost("/stopMediaStreamingAsync")]
        [Tags("Media streaming APIs")]
        public async Task<IActionResult> StopMediaStreamingAsync(string callConnectionId)
        {
            try
            {
                var callMedia = _service.GetCallMedia(callConnectionId);
                var correlationId = _service.GetCallConnectionProperties(callConnectionId).CorrelationId;

                Response response = await callMedia.StopMediaStreamingAsync();
                string operationStatus = response.Status.ToString();

                string successMessage = $"[stopMediaStreamingAsync] Media streaming stopped. " +
                                        $"CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, Status: {operationStatus}";
                _logger.LogInformation(successMessage);

                return Ok(new
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = operationStatus
                });
            }
            catch (Exception ex)
            {
                string errorMessage = $"[stopMediaStreamingAsync] Error: {ex.Message}, CallConnectionId: {callConnectionId}";
                _logger.LogError(errorMessage);
                return Problem($"Failed to stop media streaming: {ex.Message}");
            }
        }

        [HttpPost("/stopMediaStreaming")]
        [Tags("Media streaming APIs")]
        public IActionResult StopMediaStreaming(string callConnectionId)
        {
            try
            {
                var callMedia = _service.GetCallMedia(callConnectionId);
                var correlationId = _service.GetCallConnectionProperties(callConnectionId).CorrelationId;

                Response response = callMedia.StopMediaStreaming();
                string operationStatus = response.Status.ToString();

                string successMessage = $"[stopMediaStreaming] Media streaming stopped. " +
                                        $"CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, Status: {operationStatus}";
                _logger.LogInformation(successMessage);

                return Ok(new
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = operationStatus
                });
            }
            catch (Exception ex)
            {
                string errorMessage = $"[stopMediaStreaming] Error: {ex.Message}, CallConnectionId: {callConnectionId}";
                _logger.LogError(errorMessage);
                return Problem($"Failed to stop media streaming: {ex.Message}");
            }
        }

        [HttpPost("/startMediaStreamingWithOptionsAsync")]
        [Tags("Media streaming APIs")]
        public async Task<IActionResult> StartMediaStreamingWithOptionsAsync(string callConnectionId)
        {
            try
            {
                var callMedia = _service.GetCallMedia(callConnectionId);
                var correlationId = _service.GetCallConnectionProperties(callConnectionId).CorrelationId;

                StartMediaStreamingOptions options = new()
                {
                    OperationContext = "StartMediaStreamingContext",
                    OperationCallbackUri = new Uri(new Uri(_config.CallbackUriHost), "/api/callbacks")
                };

                Response response = await callMedia.StartMediaStreamingAsync(options);
                string operationStatus = response.Status.ToString();

                string successMessage = $"[startMediaStreamingWithOptionsAsync] Media streaming with options started. " +
                                        $"CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, Status: {operationStatus}";
                _logger.LogInformation(successMessage);

                return Ok(new
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = operationStatus
                });
            }
            catch (Exception ex)
            {
                string errorMessage = $"[startMediaStreamingWithOptionsAsync] Error: {ex.Message}, CallConnectionId: {callConnectionId}";
                _logger.LogError(errorMessage);
                return Problem($"Failed to start media streaming with options: {ex.Message}");
            }
        }

        [HttpPost("/startMediaStreamingWithOptions")]
        [Tags("Media streaming APIs")]
        public IActionResult StartMediaStreamingWithOptions(string callConnectionId)
        {
            try
            {
                var callMedia = _service.GetCallMedia(callConnectionId);
                var correlationId = _service.GetCallConnectionProperties(callConnectionId).CorrelationId;

                StartMediaStreamingOptions options = new()
                {
                    OperationContext = "StartMediaStreamingContext",
                    OperationCallbackUri = new Uri(new Uri(_config.CallbackUriHost), "/api/callbacks")
                };

                Response response = callMedia.StartMediaStreaming(options);
                string operationStatus = response.Status.ToString();

                string successMessage = $"[startMediaStreamingWithOptions] Media streaming with options started. " +
                                        $"CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, Status: {operationStatus}";
                _logger.LogInformation(successMessage);

                return Ok(new
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = operationStatus
                });
            }
            catch (Exception ex)
            {
                string errorMessage = $"[startMediaStreamingWithOptions] Error: {ex.Message}, CallConnectionId: {callConnectionId}";
                _logger.LogError(errorMessage);
                return Problem($"Failed to start media streaming with options: {ex.Message}");
            }
        }

        [HttpPost("/stopMediaStreamingWithOptionsAsync")]
        [Tags("Media streaming APIs")]
        public async Task<IActionResult> StopMediaStreamingWithOptionsAsync(string callConnectionId)
        {
            try
            {
                var callMedia = _service.GetCallMedia(callConnectionId);
                var correlationId = _service.GetCallConnectionProperties(callConnectionId).CorrelationId;

                StopMediaStreamingOptions options = new()
                {
                    OperationContext = "StopMediaStreamingContext",
                    OperationCallbackUri = new Uri(new Uri(_config.CallbackUriHost), "/api/callbacks")
                };

                Response response = await callMedia.StopMediaStreamingAsync(options);
                string operationStatus = response.Status.ToString();

                string successMessage = $"[stopMediaStreamingWithOptionsAsync] Media streaming with options stopped. " +
                                        $"CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, Status: {operationStatus}";
                _logger.LogInformation(successMessage);

                return Ok(new
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = operationStatus
                });
            }
            catch (Exception ex)
            {
                string errorMessage = $"[stopMediaStreamingWithOptionsAsync] Error: {ex.Message}, CallConnectionId: {callConnectionId}";
                _logger.LogError(errorMessage);
                return Problem($"Failed to stop media streaming with options: {ex.Message}");
            }
        }

        [HttpPost("/stopMediaStreamingWithOptions")]
        [Tags("Media streaming APIs")]
        public IActionResult StopMediaStreamingWithOptions(string callConnectionId)
        {
            try
            {
                var callMedia = _service.GetCallMedia(callConnectionId);
                var correlationId = _service.GetCallConnectionProperties(callConnectionId).CorrelationId;

                StopMediaStreamingOptions options = new()
                {
                    OperationContext = "StopMediaStreamingContext",
                    OperationCallbackUri = new Uri(new Uri(_config.CallbackUriHost), "/api/callbacks")
                };

                Response response = callMedia.StopMediaStreaming(options);
                string operationStatus = response.Status.ToString();

                string successMessage = $"[stopMediaStreamingWithOptions] Media streaming with options stopped. " +
                                        $"CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, Status: {operationStatus}";
                _logger.LogInformation(successMessage);

                return Ok(new
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = operationStatus
                });
            }
            catch (Exception ex)
            {
                string errorMessage = $"[stopMediaStreamingWithOptions] Error: {ex.Message}, CallConnectionId: {callConnectionId}";
                _logger.LogError(errorMessage);
                return Problem($"Failed to stop media streaming with options: {ex.Message}");
            }
        }

        #endregion

        #region Cancel All Media Operations

        [HttpPost("/cancelAllMediaOperationAsync")]
        [Tags("Cancel All Media Opertation APIs")]
        public async Task<IActionResult> CancelAllMediaOperationAsync(string callConnectionId)
        {
            try
            {
                // Get the correlationId from the call properties
                var callProperties = _service.GetCallConnectionProperties(callConnectionId);
                var correlationId = callProperties.CorrelationId;

                // Cancel all media operations
                // If CancelAllMediaOperationsAsync returns a Response, capture that:
                var response = await _service.GetCallMedia(callConnectionId).CancelAllMediaOperationsAsync();
                string operationStatus = response.GetRawResponse().Status.ToString();

                string successMessage = $"[cancelAllMediaOperationAsync] All media operations cancelled. " +
                                        $"CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, Status: {operationStatus}";
                _logger.LogInformation(successMessage);

                return Ok(new
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = operationStatus
                });
            }
            catch (Exception ex)
            {
                string errorMessage = $"[cancelAllMediaOperationAsync] Error: {ex.Message}, CallConnectionId: {callConnectionId}";
                _logger.LogError(errorMessage);
                return Problem($"Failed to cancel all media operations: {ex.Message}. CallConnectionId: {callConnectionId}");
            }
        }

        [HttpPost("/cancelAllMediaOperation")]
        [Tags("Cancel All Media Opertation APIs")]
        public IActionResult CancelAllMediaOperation(string callConnectionId)
        {
            try
            {
                // Get the correlationId from the call properties
                var callProperties = _service.GetCallConnectionProperties(callConnectionId);
                var correlationId = callProperties.CorrelationId;

                // Cancel all media operations
                var response = _service.GetCallMedia(callConnectionId).CancelAllMediaOperations();
                string operationStatus = response.GetRawResponse().Status.ToString();

                string successMessage = $"[cancelAllMediaOperation] All media operations cancelled. " +
                                        $"CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, Status: {operationStatus}";
                _logger.LogInformation(successMessage);

                return Ok(new
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = operationStatus
                });
            }
            catch (Exception ex)
            {
                string errorMessage = $"[cancelAllMediaOperation] Error: {ex.Message}, CallConnectionId: {callConnectionId}";
                _logger.LogError(errorMessage);
                return Problem($"Failed to cancel all media operations: {ex.Message}. CallConnectionId: {callConnectionId}");
            }
        }

        #endregion
        #region Recognization

        /// <summary>
        /// Recognize DTMF asynchronously.
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="acsTarget">The ACS user identifier</param>
        /// <returns>status result</returns>
        [HttpPost("/recognizeDTMFAcsTargetAsync")]
        [ProducesResponseType(typeof(CallConnectionResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        [Tags("Start Recognization APIs")]
        public async Task<IActionResult> RecognizeDTMFAcsTargetAsync(string callConnectionId, string acsTarget)
        {
            try
            {
                if (string.IsNullOrEmpty(callConnectionId))
                {
                    return BadRequest("Call Connection ID is required");
                }

                if (string.IsNullOrEmpty(acsTarget))
                {
                    return BadRequest("ACS Target ID is required");
                }

                _logger.LogInformation($"Starting DTMF recognition with ACS target. CallConnectionId: {callConnectionId}, Target: {acsTarget}");

                CommunicationIdentifier target = new CommunicationUserIdentifier(acsTarget);

                CallMedia callMedia = _service.GetCallMedia(callConnectionId);

                TextSource textSource = new TextSource("Hi, this is recognize test. please provide input thanks!.")
                {
                    VoiceName = "en-US-NancyNeural"
                };

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

                await callMedia.StartRecognizingAsync(recognizeOptions);

                var correlationId = (_service.GetCallConnectionProperties(callConnectionId)).CorrelationId;
                var callStatus = (_service.GetCallConnectionProperties(callConnectionId)).CallConnectionState.ToString();

                string successMessage = $"DTMF recognition started successfully with ACS target. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, CallStatus: {callStatus}";
                _logger.LogInformation(successMessage);

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = callStatus
                });
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error starting DTMF recognition with ACS target. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
                _logger.LogError(errorMessage);

                return Problem($"Failed to start DTMF recognition: {ex.Message}");
            }
        }

        /// <summary>
        /// Recognize DTMF.
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="acsTarget">The ACS user identifier</param>
        /// <returns>status result</returns>
        [HttpPost("/recognizeDTMFAcsTarget")]
        [ProducesResponseType(typeof(CallConnectionResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        [Tags("Start Recognization APIs")]
        public IActionResult RecognizeDTMFAcsTarget(string callConnectionId, string acsTarget)
        {
            try
            {
                if (string.IsNullOrEmpty(callConnectionId))
                {
                    return BadRequest("Call Connection ID is required");
                }

                if (string.IsNullOrEmpty(acsTarget))
                {
                    return BadRequest("ACS Target ID is required");
                }

                _logger.LogInformation($"Starting DTMF recognition with ACS target. CallConnectionId: {callConnectionId}, Target: {acsTarget}");

                CommunicationIdentifier target = new CommunicationUserIdentifier(acsTarget);

                CallMedia callMedia = _service.GetCallMedia(callConnectionId);

                TextSource textSource = new TextSource("Hi, this is recognize test. please provide input thanks!.")
                {
                    VoiceName = "en-US-NancyNeural"
                };

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

                callMedia.StartRecognizing(recognizeOptions);

                var correlationId = (_service.GetCallConnectionProperties(callConnectionId)).CorrelationId;
                var callStatus = (_service.GetCallConnectionProperties(callConnectionId)).CallConnectionState.ToString();

                string successMessage = $"DTMF recognition started successfully with ACS target. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, CallStatus: {callStatus}";
                _logger.LogInformation(successMessage);

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = callStatus
                });
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error starting DTMF recognition with ACS target. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
                _logger.LogError(errorMessage);

                return Problem($"Failed to start DTMF recognition: {ex.Message}");
            }
        }

        #endregion
        #region Hold/Unhold

        /// <summary>
        /// Hold ACS target asynchronously.
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="acsTarget">The ACS user identifier</param>
        /// <param name="isPlaySource">true or false</param>
        /// <returns>status result</returns>
        [HttpPost("/holdAcsTargetAsync")]
        [ProducesResponseType(typeof(CallConnectionResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        [Tags("Hold Participant APIs")]
        public async Task<IActionResult> HoldAcsTargetAsync(string callConnectionId, string acsTarget, bool isPlaySource)
        {
            try
            {
                if (string.IsNullOrEmpty(callConnectionId))
                {
                    return BadRequest("Call Connection ID is required");
                }

                if (string.IsNullOrEmpty(acsTarget))
                {
                    return BadRequest("ACS Target ID is required");
                }

                _logger.LogInformation($"Hold ACS target. CallConnectionId: {callConnectionId}, Target: {acsTarget}");

                CommunicationIdentifier target = new CommunicationUserIdentifier(acsTarget);

                CallMedia callMedia = _service.GetCallMedia(callConnectionId);

                if (isPlaySource)
                {
                    TextSource textSource = new TextSource("You are on hold please wait..")
                    {
                        VoiceName = "en-US-NancyNeural"
                    };

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

                var correlationId = (_service.GetCallConnectionProperties(callConnectionId)).CorrelationId;
                var callStatus = (_service.GetCallConnectionProperties(callConnectionId)).CallConnectionState.ToString();

                string successMessage = $"Hold successfully on ACS target. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, CallStatus: {callStatus}";
                _logger.LogInformation(successMessage);

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = callStatus
                });
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error holding ACS target. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
                _logger.LogError(errorMessage);

                return Problem($"Failed to hold: {ex.Message}");
            }
        }

        /// <summary>
        /// Hold ACS target.
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="acsTarget">The ACS user identifier</param>
        /// <param name="isPlaySource">true or false</param>
        /// <returns>status result</returns>
        [HttpPost("/holdAcsTarget")]
        [ProducesResponseType(typeof(CallConnectionResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        [Tags("Hold Participant APIs")]
        public IActionResult HoldAcsTarget(string callConnectionId, string acsTarget, bool isPlaySource)
        {
            try
            {
                if (string.IsNullOrEmpty(callConnectionId))
                {
                    return BadRequest("Call Connection ID is required");
                }

                if (string.IsNullOrEmpty(acsTarget))
                {
                    return BadRequest("ACS Target ID is required");
                }

                _logger.LogInformation($"Hold ACS target. CallConnectionId: {callConnectionId}, Target: {acsTarget}");

                CommunicationIdentifier target = new CommunicationUserIdentifier(acsTarget);

                CallMedia callMedia = _service.GetCallMedia(callConnectionId);

                if (isPlaySource)
                {
                    TextSource textSource = new TextSource("You are on hold please wait..")
                    {
                        VoiceName = "en-US-NancyNeural"
                    };

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

                var correlationId = (_service.GetCallConnectionProperties(callConnectionId)).CorrelationId;
                var callStatus = (_service.GetCallConnectionProperties(callConnectionId)).CallConnectionState.ToString();

                string successMessage = $"Hold successfully on ACS target. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, CallStatus: {callStatus}";
                _logger.LogInformation(successMessage);

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = callStatus
                });
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error holding ACS target. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
                _logger.LogError(errorMessage);

                return Problem($"Failed to hold: {ex.Message}");
            }
        }

        /// <summary>
        /// Unhold ACS target asynchronously.
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="acsTarget">The ACS user identifier</param>
        /// <param name="isPlaySource">true or false</param>
        /// <returns>status result</returns>
        [HttpPost("/unholdAcsTargetAsync")]
        [ProducesResponseType(typeof(CallConnectionResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        [Tags("Hold Participant APIs")]
        public async Task<IActionResult> UnholdAcsTargetAsync(string callConnectionId, string acsTarget)
        {
            try
            {
                if (string.IsNullOrEmpty(callConnectionId))
                {
                    return BadRequest("Call Connection ID is required");
                }

                if (string.IsNullOrEmpty(acsTarget))
                {
                    return BadRequest("ACS Target ID is required");
                }

                _logger.LogInformation($"Unhold ACS target. CallConnectionId: {callConnectionId}, Target: {acsTarget}");

                CommunicationIdentifier target = new CommunicationUserIdentifier(acsTarget);

                CallMedia callMedia = _service.GetCallMedia(callConnectionId);

                UnholdOptions unholdOptions = new UnholdOptions(target)
                {
                    OperationContext = "unholdUserContext"
                };
                await callMedia.UnholdAsync(unholdOptions);

                var correlationId = (_service.GetCallConnectionProperties(callConnectionId)).CorrelationId;
                var callStatus = (_service.GetCallConnectionProperties(callConnectionId)).CallConnectionState.ToString();

                string successMessage = $"Unhold successfully on ACS target. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, CallStatus: {callStatus}";
                _logger.LogInformation(successMessage);

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = callStatus
                });
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error unholding ACS target. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
                _logger.LogError(errorMessage);

                return Problem($"Failed to unhold: {ex.Message}");
            }
        }

        /// <summary>
        /// Unhold ACS target.
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="acsTarget">The ACS user identifier</param>
        /// <param name="isPlaySource">true or false</param>
        /// <returns>status result</returns>
        [HttpPost("/unholdAcsTarget")]
        [ProducesResponseType(typeof(CallConnectionResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        [Tags("Hold Participant APIs")]
        public IActionResult UnholdAcsTarget(string callConnectionId, string acsTarget)
        {
            try
            {
                if (string.IsNullOrEmpty(callConnectionId))
                {
                    return BadRequest("Call Connection ID is required");
                }

                if (string.IsNullOrEmpty(acsTarget))
                {
                    return BadRequest("ACS Target ID is required");
                }

                _logger.LogInformation($"Unhold ACS target. CallConnectionId: {callConnectionId}, Target: {acsTarget}");

                CommunicationIdentifier target = new CommunicationUserIdentifier(acsTarget);

                CallMedia callMedia = _service.GetCallMedia(callConnectionId);

                UnholdOptions unholdOptions = new UnholdOptions(target)
                {
                    OperationContext = "unholdUserContext"
                };
                callMedia.Unhold(unholdOptions);

                var correlationId = (_service.GetCallConnectionProperties(callConnectionId)).CorrelationId;
                var callStatus = (_service.GetCallConnectionProperties(callConnectionId)).CallConnectionState.ToString();

                string successMessage = $"Unhold successfully on ACS target. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, CallStatus: {callStatus}";
                _logger.LogInformation(successMessage);

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = callStatus
                });
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error unholding ACS target. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
                _logger.LogError(errorMessage);

                return Problem($"Failed to unhold: {ex.Message}");
            }
        }

        // Requires Cognitive services
        ///// <summary>
        ///// Interrupt audio and announce asynchronously.
        ///// </summary>
        ///// <param name="callConnectionId">The call connection ID</param>
        ///// <param name="acsTarget">The ACS user identifier</param>
        ///// <returns>status result</returns>
        //[HttpPost("/interruptAudioAndAnnounceAsync")]
        //[ProducesResponseType(typeof(CallConnectionResponse), 200)]
        //[ProducesResponseType(typeof(ProblemDetails), 400)]
        //[ProducesResponseType(typeof(ProblemDetails), 500)]
        //[Tags("Hold Participant APIs")]
        //public async Task<IActionResult> InterruptAudioAndAnnounceAsync(string callConnectionId, string acsTarget)
        //{
        //    try
        //    {
        //        if (string.IsNullOrEmpty(callConnectionId))
        //        {
        //            return BadRequest("Call Connection ID is required");
        //        }

        //        if (string.IsNullOrEmpty(acsTarget))
        //        {
        //            return BadRequest("ACS Target ID is required");
        //        }

        //        _logger.LogInformation($"Interrupt audio and announce to ACS target. CallConnectionId: {callConnectionId}, Target: {acsTarget}");

        //        CommunicationIdentifier target = new CommunicationUserIdentifier(acsTarget);

        //        CallMedia callMedia = _service.GetCallMedia(callConnectionId);

        //        TextSource textSource = new TextSource("Hi, This is interrup audio and announcement test")
        //        {
        //            VoiceName = "en-US-NancyNeural"
        //        };

        //        InterruptAudioAndAnnounceOptions interruptAudio = new InterruptAudioAndAnnounceOptions(textSource, target)
        //        {
        //            OperationContext = "innterruptContext"
        //        };

        //        await callMedia.InterruptAudioAndAnnounceAsync(interruptAudio);

        //        var correlationId = (_service.GetCallConnectionProperties(callConnectionId)).CorrelationId;
        //        var callStatus = (_service.GetCallConnectionProperties(callConnectionId)).CallConnectionState.ToString();

        //        string successMessage = $"Interrupt audio and announce successfully on ACS target. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, CallStatus: {callStatus}";
        //        _logger.LogInformation(successMessage);

        //        return Ok(new CallConnectionResponse
        //        {
        //            CallConnectionId = callConnectionId,
        //            CorrelationId = correlationId,
        //            Status = callStatus
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        string errorMessage = $"Error interrupting audio and announce on ACS target. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
        //        _logger.LogError(errorMessage);

        //        return Problem($"Failed to interrupt audio and announce: {ex.Message}");
        //    }
        //}

        ///// <summary>
        ///// Interrupt audio and announce.
        ///// </summary>
        ///// <param name="callConnectionId">The call connection ID</param>
        ///// <param name="acsTarget">The ACS user identifier</param>
        ///// <returns>status result</returns>
        //[HttpPost("/interruptAudioAndAnnounce")]
        //[ProducesResponseType(typeof(CallConnectionResponse), 200)]
        //[ProducesResponseType(typeof(ProblemDetails), 400)]
        //[ProducesResponseType(typeof(ProblemDetails), 500)]
        //[Tags("Hold Participant APIs")]
        //public IActionResult InterruptAudioAndAnnounce(string callConnectionId, string acsTarget)
        //{
        //    try
        //    {
        //        if (string.IsNullOrEmpty(callConnectionId))
        //        {
        //            return BadRequest("Call Connection ID is required");
        //        }

        //        if (string.IsNullOrEmpty(acsTarget))
        //        {
        //            return BadRequest("ACS Target ID is required");
        //        }

        //        _logger.LogInformation($"Interrupt audio and announce to ACS target. CallConnectionId: {callConnectionId}, Target: {acsTarget}");

        //        CommunicationIdentifier target = new CommunicationUserIdentifier(acsTarget);

        //        CallMedia callMedia = _service.GetCallMedia(callConnectionId);

        //        TextSource textSource = new TextSource("Hi, This is interrup audio and announcement test")
        //        {
        //            VoiceName = "en-US-NancyNeural"
        //        };

        //        InterruptAudioAndAnnounceOptions interruptAudio = new InterruptAudioAndAnnounceOptions(textSource, target)
        //        {
        //            OperationContext = "innterruptContext"
        //        };

        //        callMedia.InterruptAudioAndAnnounce(interruptAudio);

        //        var correlationId = (_service.GetCallConnectionProperties(callConnectionId)).CorrelationId;
        //        var callStatus = (_service.GetCallConnectionProperties(callConnectionId)).CallConnectionState.ToString();

        //        string successMessage = $"Interrupt audio and announce successfully on ACS target. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, CallStatus: {callStatus}";
        //        _logger.LogInformation(successMessage);

        //        return Ok(new CallConnectionResponse
        //        {
        //            CallConnectionId = callConnectionId,
        //            CorrelationId = correlationId,
        //            Status = callStatus
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        string errorMessage = $"Error interrupting audio and announce on ACS target. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
        //        _logger.LogError(errorMessage);

        //        return Problem($"Failed to interrupt audio and announce: {ex.Message}");
        //    }
        //}

        ///// <summary>
        ///// Interrupt hold with play.
        ///// </summary>
        ///// <param name="callConnectionId">The call connection ID</param>
        ///// <param name="acsTarget">The ACS user identifier</param>
        ///// <returns>status result</returns>
        //[HttpPost("/interruptHoldWithPlay")]
        //[ProducesResponseType(typeof(CallConnectionResponse), 200)]
        //[ProducesResponseType(typeof(ProblemDetails), 400)]
        //[ProducesResponseType(typeof(ProblemDetails), 500)]
        //[Tags("Hold Participant APIs")]
        //public IActionResult InterruptHoldWithPlay(string callConnectionId, string acsTarget)
        //{
        //    try
        //    {
        //        if (string.IsNullOrEmpty(callConnectionId))
        //        {
        //            return BadRequest("Call Connection ID is required");
        //        }

        //        if (string.IsNullOrEmpty(acsTarget))
        //        {
        //            return BadRequest("ACS Target ID is required");
        //        }

        //        _logger.LogInformation($"Interrupt hold with play to ACS target. CallConnectionId: {callConnectionId}, Target: {acsTarget}");

        //        CallMedia callMedia = _service.GetCallMedia(callConnectionId);

        //        TextSource textSource = new TextSource("Hi, This is interrup audio and announcement test")
        //        {
        //            VoiceName = "en-US-NancyNeural"
        //        };

        //        List<CommunicationIdentifier> playTo = new List<CommunicationIdentifier> { new CommunicationUserIdentifier(acsTarget) };
        //        PlayOptions playToOptions = new PlayOptions(textSource, playTo)
        //        {
        //            OperationContext = "playToContext",
        //            InterruptHoldAudio = true
        //        };

        //        callMedia.Play(playToOptions);

        //        var correlationId = (_service.GetCallConnectionProperties(callConnectionId)).CorrelationId;
        //        var callStatus = (_service.GetCallConnectionProperties(callConnectionId)).CallConnectionState.ToString();

        //        string successMessage = $"Interrupt hold with play successfully on ACS target. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, CallStatus: {callStatus}";
        //        _logger.LogInformation(successMessage);

        //        return Ok(new CallConnectionResponse
        //        {
        //            CallConnectionId = callConnectionId,
        //            CorrelationId = correlationId,
        //            Status = callStatus
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        string errorMessage = $"Error interrupting hold and play on ACS target. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
        //        _logger.LogError(errorMessage);

        //        return Problem($"Failed to interrupt hold and play: {ex.Message}");
        //    }
        //}

        #endregion
    }
}


#region Play Media with File Source

//app.MapPost("/playFileSourceToPstnTargetAsync", async (string callConnectionId, string pstnTarget, ILogger<Program> logger) =>
//{
//    try
//    {
//        CallMedia callMedia = GetCallMedia(callConnectionId);
//      //  FileSource fileSource = new FileSource(new Uri(fileSourceUri));
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
//      //  FileSource fileSource = new FileSource(new Uri(fileSourceUri));
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
//       // FileSource fileSource = new FileSource(new Uri(fileSourceUri));
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
//      //  FileSource fileSource = new FileSource(new Uri(fileSourceUri));
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
# region Media streaming
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

