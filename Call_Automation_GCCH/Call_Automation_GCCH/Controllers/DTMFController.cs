using Azure.Communication;
using Azure.Communication.CallAutomation;
using Call_Automation_GCCH.Models;
using Call_Automation_GCCH.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Call_Automation_GCCH.Controllers
{
    [ApiController]
    [Route("api/dtmf")]
    [Produces("application/json")]
    public class DTMFController : ControllerBase
    {
        private readonly CallAutomationService _service;
        private readonly ILogger<DTMFController> _logger;
        private readonly ConfigurationRequest _config; // final, bound object

        public DTMFController(
            CallAutomationService service,
            ILogger<DTMFController> logger, IOptions<ConfigurationRequest> configOptions)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = configOptions.Value ?? throw new ArgumentNullException(nameof(configOptions));
        }


        #region DTMF

        /// <summary>
        /// Send DTMF Tones to a specific target asynchronously.
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="targetUser">The user identifier</param>
        /// <returns>status result</returns>
        [HttpPost("/sendDTMFTonesAsync")]
        [ProducesResponseType(typeof(CallConnectionResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        [Tags("Send or Start DTMF APIs")]
        public async Task<IActionResult> SendDTMFTonesTargetAsync(string callConnectionId, string targetUser, bool isPstn)
        {
            try
            {
                if (string.IsNullOrEmpty(callConnectionId))
                {
                    return BadRequest("Call Connection ID is required");
                }

                if (string.IsNullOrEmpty(targetUser))
                {
                    return BadRequest("Target ID is required");
                }

                _logger.LogInformation($"Send DTMF Tones to target. CallConnectionId: {callConnectionId}, Target: {targetUser}");

                CommunicationIdentifier target;
                if (isPstn)
                {
                    target = new PhoneNumberIdentifier(targetUser);
                }
                else
                {
                    target = new CommunicationUserIdentifier(targetUser);
                }

                List<DtmfTone> tones = new List<DtmfTone>
                {
                    DtmfTone.Zero,
                    DtmfTone.One
                };

                CallMedia callMedia = _service.GetCallMedia(callConnectionId);
                await callMedia.SendDtmfTonesAsync(tones, target);
                var correlationId = (_service.GetCallConnectionProperties(callConnectionId)).CorrelationId;
                var callStatus = (_service.GetCallConnectionProperties(callConnectionId)).CallConnectionState.ToString();

                string successMessage = $"DTMF tones sent successfully to target. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, CallStatus: {callStatus}";
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
                string errorMessage = $"Error sending DTMF Tones to target. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
                _logger.LogError(errorMessage);

                return Problem($"Failed to send DTMF Tones: {ex.Message}");
            }
        }

        /// <summary>
        /// Send DTMF Tones to a specific target.
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="targetUser">The user identifier</param>
        /// <returns>status result</returns>
        [HttpPost("/sendDTMFTones")]
        [ProducesResponseType(typeof(CallConnectionResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        [Tags("Send or Start DTMF APIs")]
        public IActionResult SendDTMFTonesTarget(string callConnectionId, string targetUser, bool isPstn)
        {
            try
            {
                if (string.IsNullOrEmpty(callConnectionId))
                {
                    return BadRequest("Call Connection ID is required");
                }

                if (string.IsNullOrEmpty(targetUser))
                {
                    return BadRequest("Target ID is required");
                }

                _logger.LogInformation($"Send DTMF Tones to target. CallConnectionId: {callConnectionId}, Target: {targetUser}");

                CommunicationIdentifier target;
                if (isPstn)
                {
                    target = new PhoneNumberIdentifier(targetUser);
                }
                else
                {
                    target = new CommunicationUserIdentifier(targetUser);
                }
                List<DtmfTone> tones = new List<DtmfTone>
                {
                    DtmfTone.Zero,
                    DtmfTone.One
                };

                CallMedia callMedia = _service.GetCallMedia(callConnectionId);
                callMedia.SendDtmfTones(tones, target);
                var correlationId = (_service.GetCallConnectionProperties(callConnectionId)).CorrelationId;
                var callStatus = (_service.GetCallConnectionProperties(callConnectionId)).CallConnectionState.ToString();

                string successMessage = $"DTMF tones sent successfully to target. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, CallStatus: {callStatus}";
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
                string errorMessage = $"Error sending DTMF Tones to target. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
                _logger.LogError(errorMessage);

                return Problem($"Failed to send DTMF Tones: {ex.Message}");
            }
        }

        /// <summary>
        /// Start continuous DTMF recognition to a specific target asynchronously.
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="targetUser">The user identifier</param>
        /// <returns>status result</returns>
        [HttpPost("/startContinuousDTMFTonesAsync")]
        [ProducesResponseType(typeof(CallConnectionResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        [Tags("Send or Start DTMF APIs")]
        public async Task<IActionResult> StartContinuousDTMFTonesTargetAsync(string callConnectionId, string targetUser, bool isPstn)
        {
            try
            {
                if (string.IsNullOrEmpty(callConnectionId))
                {
                    return BadRequest("Call Connection ID is required");
                }

                if (string.IsNullOrEmpty(targetUser))
                {
                    return BadRequest("Target ID is required");
                }

                _logger.LogInformation($"Start continuous DTMF recognition to target. CallConnectionId: {callConnectionId}, Target: {targetUser}");

                CommunicationIdentifier target;
                if (isPstn)
                {
                    target = new PhoneNumberIdentifier(targetUser);
                }
                else
                {
                    target = new CommunicationUserIdentifier(targetUser);
                }

                CallMedia callMedia = _service.GetCallMedia(callConnectionId);
                await callMedia.StartContinuousDtmfRecognitionAsync(target);
                var correlationId = (_service.GetCallConnectionProperties(callConnectionId)).CorrelationId;
                var callStatus = (_service.GetCallConnectionProperties(callConnectionId)).CallConnectionState.ToString();

                string successMessage = $"Continuous DTMF recognition started successfully to target. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, CallStatus: {callStatus}";
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
                string errorMessage = $"Error starting continuous DTMF recognition to target. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
                _logger.LogError(errorMessage);

                return Problem($"Failed to start continuous DTMF recognition: {ex.Message}");
            }
        }

        /// <summary>
        /// Start continuous DTMF recognition to a specific target.
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="targetUser">The user identifier</param>
        /// <returns>status result</returns>
        [HttpPost("/startContinuousDTMFTones")]
        [ProducesResponseType(typeof(CallConnectionResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        [Tags("Send or Start DTMF APIs")]
        public IActionResult StartContinuousDTMFTonesTarget(string callConnectionId, string targetUser, bool isPstn)
        {
            try
            {
                if (string.IsNullOrEmpty(callConnectionId))
                {
                    return BadRequest("Call Connection ID is required");
                }

                if (string.IsNullOrEmpty(targetUser))
                {
                    return BadRequest("Target ID is required");
                }

                _logger.LogInformation($"Start continuous DTMF recognition to target. CallConnectionId: {callConnectionId}, Target: {targetUser}");

                CommunicationIdentifier target;
                if (isPstn)
                {
                    target = new PhoneNumberIdentifier(targetUser);
                }
                else
                {
                    target = new CommunicationUserIdentifier(targetUser);
                }

                CallMedia callMedia = _service.GetCallMedia(callConnectionId);
                callMedia.StartContinuousDtmfRecognition(target);
                var correlationId = (_service.GetCallConnectionProperties(callConnectionId)).CorrelationId;
                var callStatus = (_service.GetCallConnectionProperties(callConnectionId)).CallConnectionState.ToString();

                string successMessage = $"Continuous DTMF recognition started successfully to target. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, CallStatus: {callStatus}";
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
                string errorMessage = $"Error starting continuous DTMF recognition to target. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
                _logger.LogError(errorMessage);

                return Problem($"Failed to start continuous DTMF recognition: {ex.Message}");
            }
        }

        /// <summary>
        /// Stop continuous DTMF recognition to a specific target asynchronously.
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="targetUser">The user identifier</param>
        /// <returns>status result</returns>
        [HttpPost("/stopContinuousDTMFTonesAsync")]
        [ProducesResponseType(typeof(CallConnectionResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        [Tags("Send or Start DTMF APIs")]
        public async Task<IActionResult> StopContinuousDTMFTonesTargetAsync(string callConnectionId, string targetUser, bool isPstn)
        {
            try
            {
                if (string.IsNullOrEmpty(callConnectionId))
                {
                    return BadRequest("Call Connection ID is required");
                }

                if (string.IsNullOrEmpty(targetUser))
                {
                    return BadRequest("Target ID is required");
                }

                _logger.LogInformation($"Stop continuous DTMF recognition to target. CallConnectionId: {callConnectionId}, Target: {targetUser}");

                CommunicationIdentifier target;
                if (isPstn)
                {
                    target = new PhoneNumberIdentifier(targetUser);
                }
                else
                {
                    target = new CommunicationUserIdentifier(targetUser);
                }

                CallMedia callMedia = _service.GetCallMedia(callConnectionId);
                await callMedia.StopContinuousDtmfRecognitionAsync(target);
                var correlationId = (_service.GetCallConnectionProperties(callConnectionId)).CorrelationId;
                var callStatus = (_service.GetCallConnectionProperties(callConnectionId)).CallConnectionState.ToString();

                string successMessage = $"Continuous DTMF recognition stopped successfully to target. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, CallStatus: {callStatus}";
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
                string errorMessage = $"Error stopping continuous DTMF recognition to target. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
                _logger.LogError(errorMessage);

                return Problem($"Failed to stop continuous DTMF recognition: {ex.Message}");
            }
        }

        /// <summary>
        /// Stop continuous DTMF recognition to a specific target.
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="targetUser">The user identifier</param>
        /// <returns>status result</returns>
        [HttpPost("/stopContinuousDTMFTones")]
        [ProducesResponseType(typeof(CallConnectionResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        [Tags("Send or Start DTMF APIs")]
        public IActionResult StopContinuousDTMFTonesTarget(string callConnectionId, string targetUser, bool isPstn)
        {
            try
            {
                if (string.IsNullOrEmpty(callConnectionId))
                {
                    return BadRequest("Call Connection ID is required");
                }

                if (string.IsNullOrEmpty(targetUser))
                {
                    return BadRequest("Target ID is required");
                }

                _logger.LogInformation($"Stop continuous DTMF recognition to target. CallConnectionId: {callConnectionId}, Target: {targetUser}");

                CommunicationIdentifier target;
                if (isPstn)
                {
                    target = new PhoneNumberIdentifier(targetUser);
                }
                else
                {
                    target = new CommunicationUserIdentifier(targetUser);
                }

                CallMedia callMedia = _service.GetCallMedia(callConnectionId);
                callMedia.StopContinuousDtmfRecognition(target);
                var correlationId = (_service.GetCallConnectionProperties(callConnectionId)).CorrelationId;
                var callStatus = (_service.GetCallConnectionProperties(callConnectionId)).CallConnectionState.ToString();

                string successMessage = $"Continuous DTMF recognition stopped successfully to target. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, CallStatus: {callStatus}";
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
                string errorMessage = $"Error stopping continuous DTMF recognition to target. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
                _logger.LogError(errorMessage);

                return Problem($"Failed to stop continuous DTMF recognition: {ex.Message}");
            }
        }

        #endregion
    }
}


