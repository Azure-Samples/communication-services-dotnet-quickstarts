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
        /// Send DTMF Tones to a specific ACS target asynchronously.
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="acsTarget">The ACS user identifier</param>
        /// <returns>status result</returns>
        [HttpPost("/sendDTMFTonesAsync")]
        [ProducesResponseType(typeof(CallConnectionResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        [Tags("Send or Start DTMF APIs")]
        public async Task<IActionResult> SendDTMFTonesAcsTargetAsync(string callConnectionId, string acsTarget)
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

                _logger.LogInformation($"Send DTMF Tones to ACS target. CallConnectionId: {callConnectionId}, Target: {acsTarget}");

                CommunicationIdentifier target = new CommunicationUserIdentifier(acsTarget);

                List<DtmfTone> tones = new List<DtmfTone>
                {
                    DtmfTone.Zero,
                    DtmfTone.One
                };

                CallMedia callMedia = _service.GetCallMedia(callConnectionId);
                await callMedia.SendDtmfTonesAsync(tones, target);
                var correlationId = (_service.GetCallConnectionProperties(callConnectionId)).CorrelationId;
                var callStatus = (_service.GetCallConnectionProperties(callConnectionId)).CallConnectionState.ToString();

                string successMessage = $"DTMF tones sent successfully to ACS target. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, CallStatus: {callStatus}";
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
                string errorMessage = $"Error sending DTMF Tones to ACS target. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
                _logger.LogError(errorMessage);

                return Problem($"Failed to send DTMF Tones: {ex.Message}");
            }
        }

        /// <summary>
        /// Send DTMF Tones to a specific ACS target.
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="acsTarget">The ACS user identifier</param>
        /// <returns>status result</returns>
        [HttpPost("/sendDTMFTones")]
        [ProducesResponseType(typeof(CallConnectionResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        [Tags("Send or Start DTMF APIs")]
        public IActionResult SendDTMFTonesAcsTarget(string callConnectionId, string acsTarget)
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

                _logger.LogInformation($"Send DTMF Tones to ACS target. CallConnectionId: {callConnectionId}, Target: {acsTarget}");

                CommunicationIdentifier target = new CommunicationUserIdentifier(acsTarget);

                List<DtmfTone> tones = new List<DtmfTone>
                {
                    DtmfTone.Zero,
                    DtmfTone.One
                };

                CallMedia callMedia = _service.GetCallMedia(callConnectionId);
                callMedia.SendDtmfTones(tones, target);
                var correlationId = (_service.GetCallConnectionProperties(callConnectionId)).CorrelationId;
                var callStatus = (_service.GetCallConnectionProperties(callConnectionId)).CallConnectionState.ToString();

                string successMessage = $"DTMF tones sent successfully to ACS target. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, CallStatus: {callStatus}";
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
                string errorMessage = $"Error sending DTMF Tones to ACS target. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
                _logger.LogError(errorMessage);

                return Problem($"Failed to send DTMF Tones: {ex.Message}");
            }
        }

        /// <summary>
        /// Start continuous DTMF recognition to a specific ACS target asynchronously.
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="acsTarget">The ACS user identifier</param>
        /// <returns>status result</returns>
        [HttpPost("/startContinuousDTMFTonesAsync")]
        [ProducesResponseType(typeof(CallConnectionResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        [Tags("Send or Start DTMF APIs")]
        public async Task<IActionResult> StartContinuousDTMFTonesAcsTargetAsync(string callConnectionId, string acsTarget)
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

                _logger.LogInformation($"Start continuous DTMF recognition to ACS target. CallConnectionId: {callConnectionId}, Target: {acsTarget}");

                CommunicationIdentifier target = new CommunicationUserIdentifier(acsTarget);

                CallMedia callMedia = _service.GetCallMedia(callConnectionId);
                await callMedia.StartContinuousDtmfRecognitionAsync(target);
                var correlationId = (_service.GetCallConnectionProperties(callConnectionId)).CorrelationId;
                var callStatus = (_service.GetCallConnectionProperties(callConnectionId)).CallConnectionState.ToString();

                string successMessage = $"Continuous DTMF recognition started successfully to ACS target. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, CallStatus: {callStatus}";
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
                string errorMessage = $"Error starting continuous DTMF recognition to ACS target. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
                _logger.LogError(errorMessage);

                return Problem($"Failed to start continuous DTMF recognition: {ex.Message}");
            }
        }

        /// <summary>
        /// Start continuous DTMF recognition to a specific ACS target.
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="acsTarget">The ACS user identifier</param>
        /// <returns>status result</returns>
        [HttpPost("/startContinuousDTMFTones")]
        [ProducesResponseType(typeof(CallConnectionResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        [Tags("Send or Start DTMF APIs")]
        public IActionResult StartContinuousDTMFTonesAcsTarget(string callConnectionId, string acsTarget)
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

                _logger.LogInformation($"Start continuous DTMF recognition to ACS target. CallConnectionId: {callConnectionId}, Target: {acsTarget}");

                CommunicationIdentifier target = new CommunicationUserIdentifier(acsTarget);

                CallMedia callMedia = _service.GetCallMedia(callConnectionId);
                callMedia.StartContinuousDtmfRecognition(target);
                var correlationId = (_service.GetCallConnectionProperties(callConnectionId)).CorrelationId;
                var callStatus = (_service.GetCallConnectionProperties(callConnectionId)).CallConnectionState.ToString();

                string successMessage = $"Continuous DTMF recognition started successfully to ACS target. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, CallStatus: {callStatus}";
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
                string errorMessage = $"Error starting continuous DTMF recognition to ACS target. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
                _logger.LogError(errorMessage);

                return Problem($"Failed to start continuous DTMF recognition: {ex.Message}");
            }
        }

        /// <summary>
        /// Stop continuous DTMF recognition to a specific ACS target asynchronously.
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="acsTarget">The ACS user identifier</param>
        /// <returns>status result</returns>
        [HttpPost("/stopContinuousDTMFTonesAsync")]
        [ProducesResponseType(typeof(CallConnectionResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        [Tags("Send or Start DTMF APIs")]
        public async Task<IActionResult> StopContinuousDTMFTonesAcsTargetAsync(string callConnectionId, string acsTarget)
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

                _logger.LogInformation($"Stop continuous DTMF recognition to ACS target. CallConnectionId: {callConnectionId}, Target: {acsTarget}");

                CommunicationIdentifier target = new CommunicationUserIdentifier(acsTarget);

                CallMedia callMedia = _service.GetCallMedia(callConnectionId);
                await callMedia.StopContinuousDtmfRecognitionAsync(target);
                var correlationId = (_service.GetCallConnectionProperties(callConnectionId)).CorrelationId;
                var callStatus = (_service.GetCallConnectionProperties(callConnectionId)).CallConnectionState.ToString();

                string successMessage = $"Continuous DTMF recognition stopped successfully to ACS target. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, CallStatus: {callStatus}";
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
                string errorMessage = $"Error stopping continuous DTMF recognition to ACS target. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
                _logger.LogError(errorMessage);

                return Problem($"Failed to stop continuous DTMF recognition: {ex.Message}");
            }
        }

        /// <summary>
        /// Stop continuous DTMF recognition to a specific ACS target.
        /// </summary>
        /// <param name="callConnectionId">The call connection ID</param>
        /// <param name="acsTarget">The ACS user identifier</param>
        /// <returns>status result</returns>
        [HttpPost("/stopContinuousDTMFTones")]
        [ProducesResponseType(typeof(CallConnectionResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        [Tags("Send or Start DTMF APIs")]
        public IActionResult StopContinuousDTMFTonesAcsTarget(string callConnectionId, string acsTarget)
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

                _logger.LogInformation($"Stop continuous DTMF recognition to ACS target. CallConnectionId: {callConnectionId}, Target: {acsTarget}");

                CommunicationIdentifier target = new CommunicationUserIdentifier(acsTarget);

                CallMedia callMedia = _service.GetCallMedia(callConnectionId);
                callMedia.StopContinuousDtmfRecognition(target);
                var correlationId = (_service.GetCallConnectionProperties(callConnectionId)).CorrelationId;
                var callStatus = (_service.GetCallConnectionProperties(callConnectionId)).CallConnectionState.ToString();

                string successMessage = $"Continuous DTMF recognition stopped successfully to ACS target. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, CallStatus: {callStatus}";
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
                string errorMessage = $"Error stopping continuous DTMF recognition to ACS target. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
                _logger.LogError(errorMessage);

                return Problem($"Failed to stop continuous DTMF recognition: {ex.Message}");
            }
        }

        #endregion
    }
}


