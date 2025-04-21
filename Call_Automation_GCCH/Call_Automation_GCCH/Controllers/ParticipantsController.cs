using System;
using System.Threading.Tasks;
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
    [Route("api/participants")]
    [Produces("application/json")]
    public class ParticipantsController : ControllerBase
    {
        private readonly CallAutomationService _service;
        private readonly ILogger<ParticipantsController> _logger;
        private readonly ConfigurationRequest _config; // final, bound object


        public ParticipantsController(
            CallAutomationService service,
            ILogger<ParticipantsController> logger, IOptions<ConfigurationRequest> configOptions)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = configOptions.Value ?? throw new ArgumentNullException(nameof(configOptions));
        }

        /// <summary>
        /// Adds an ACS participant to a call asynchronously
        /// </summary>
        /// <param name="callConnectionId">Call connection ID</param>
        /// <param name="acsParticipant">ACS participant ID</param>
        /// <returns>Operation result</returns>
        [HttpPost("addAcsParticipantAsync")]
        [ProducesResponseType(typeof(CallConnectionResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        [Tags("Add/Remove Participant APIs")]
        public async Task<IActionResult> AddAcsParticipantAsync(string callConnectionId, string acsParticipant)
        {
            try
            {
                _logger.LogInformation($"Starting to add ACS participant async: {acsParticipant} for call {callConnectionId}");

                var correlationId = (_service.GetCallConnectionProperties(callConnectionId)).CorrelationId;
                var callStatus = (_service.GetCallConnectionProperties(callConnectionId)).CallConnectionState.ToString();

                CallConnection callConnection = _service.GetCallConnection(callConnectionId);
                CallInvite callInvite = new CallInvite(new CommunicationUserIdentifier(acsParticipant));
                var addParticipantOptions = new AddParticipantOptions(callInvite)
                {
                    OperationContext = "addAcsUserContext",
                    InvitationTimeoutInSeconds = 30,
                };

                _logger.LogInformation($"Executing AddParticipantAsync for ACS participant: {acsParticipant} on call {callConnectionId}");

                var result = await callConnection.AddParticipantAsync(addParticipantOptions);
                var operationStatus = result.GetRawResponse().ToString();

                string successMessage = $"Added ACS participant async. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, CallStatus: {callStatus}, OperationStatus: {operationStatus}";
                _logger.LogInformation(successMessage);

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = operationStatus
                });
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogInformation($"ACS participant validation error for call {callConnectionId}: {ex.Message}");
                return BadRequest($"Invalid ACS participant: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error adding ACS participant for call {callConnectionId}: {ex.Message}");
                return Problem($"Failed to add ACS participant: {ex.Message}");
            }
        }

        /// <summary>
        /// Adds an ACS participant to a call
        /// </summary>
        /// <param name="callConnectionId">Call connection ID</param>
        /// <param name="acsParticipant">ACS participant ID</param>
        /// <returns>Operation result</returns>
        [HttpPost("addAcsParticipant")]
        [ProducesResponseType(typeof(CallConnectionResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        [Tags("Add/Remove Participant APIs")]
        public IActionResult AddAcsParticipant(string callConnectionId, string acsParticipant)
        {
            try
            {
                _logger.LogInformation($"Starting to add ACS participant: {acsParticipant} for call {callConnectionId}");

                var correlationId = (_service.GetCallConnectionProperties(callConnectionId)).CorrelationId;
                var callStatus = (_service.GetCallConnectionProperties(callConnectionId)).CallConnectionState.ToString();

                CallConnection callConnection = _service.GetCallConnection(callConnectionId);
                CallInvite callInvite = new CallInvite(new CommunicationUserIdentifier(acsParticipant));
                var addParticipantOptions = new AddParticipantOptions(callInvite)
                {
                    OperationContext = "addPstnUserContext",
                    InvitationTimeoutInSeconds = 30,
                };

                _logger.LogInformation($"Executing AddParticipant for ACS participant: {acsParticipant} on call {callConnectionId}");

                var result = callConnection.AddParticipant(addParticipantOptions);
                var operationStatus = result.GetRawResponse().ToString();

                string successMessage = $"Added ACS participant. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, CallStatus: {callStatus}, OperationStatus: {operationStatus}";
                _logger.LogInformation(successMessage);

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = operationStatus
                });
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogInformation($"ACS participant validation error for call {callConnectionId}: {ex.Message}");
                return BadRequest($"Invalid ACS participant: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error adding ACS participant for call {callConnectionId}: {ex.Message}");
                return Problem($"Failed to add ACS participant: {ex.Message}");
            }
        }

        /// <summary>
        /// Removes an ACS participant from a call asynchronously
        /// </summary>
        /// <param name="callConnectionId">Call connection ID</param>
        /// <param name="acsTarget">ACS target ID to remove</param>
        /// <returns>Operation status</returns>
        [HttpPost("removeAcsParticipantAsync")]
        [ProducesResponseType(typeof(CallConnectionResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        [Tags("Add/Remove Participant APIs")]
        public async Task<IActionResult> RemoveAcsParticipantAsync(string callConnectionId, string acsTarget)
        {
            try
            {
                _logger.LogInformation($"Starting to remove ACS participant async: {acsTarget} from call {callConnectionId}");

                var correlationId = (_service.GetCallConnectionProperties(callConnectionId)).CorrelationId;
                var callStatus = (_service.GetCallConnectionProperties(callConnectionId)).CallConnectionState.ToString();

                CallConnection callConnection = _service.GetCallConnection(callConnectionId);
                RemoveParticipantOptions removeParticipantOptions = new RemoveParticipantOptions(new CommunicationUserIdentifier(acsTarget))
                {
                    OperationContext = "removeAcsParticipantContext"
                };

                _logger.LogInformation($"Executing RemoveParticipantAsync for ACS participant: {acsTarget} on call {callConnectionId}");

                var response = await callConnection.RemoveParticipantAsync(removeParticipantOptions);
                var operationStatus = response.GetRawResponse().ToString();

                string successMessage = $"Successfully removed ACS participant async. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, CallStatus: {callStatus}, OperationStatus: {operationStatus}";
                _logger.LogInformation(successMessage);

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = operationStatus
                });
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogInformation($"ACS target validation error for call {callConnectionId}: {ex.Message}");
                return BadRequest($"Invalid ACS target: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error removing ACS participant for call {callConnectionId}: {ex.Message}");
                return Problem($"Failed to remove ACS participant: {ex.Message}");
            }
        }

        /// <summary>
        /// Removes an ACS participant from a call
        /// </summary>
        /// <param name="callConnectionId">Call connection ID</param>
        /// <param name="acsTarget">ACS target ID to remove</param>
        /// <returns>Operation status</returns>
        [HttpPost("removeAcsParticipant")]
        [ProducesResponseType(typeof(CallConnectionResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        [Tags("Add/Remove Participant APIs")]
        public IActionResult RemoveAcsParticipant(string callConnectionId, string acsTarget)
        {
            try
            {
                _logger.LogInformation($"Starting to remove ACS participant: {acsTarget} from call {callConnectionId}");

                var correlationId = (_service.GetCallConnectionProperties(callConnectionId)).CorrelationId;
                var callStatus = (_service.GetCallConnectionProperties(callConnectionId)).CallConnectionState.ToString();

                CallConnection callConnection = _service.GetCallConnection(callConnectionId);
                RemoveParticipantOptions removeParticipantOptions = new RemoveParticipantOptions(new CommunicationUserIdentifier(acsTarget))
                {
                    OperationContext = "removeAcsParticipantContext"
                };

                _logger.LogInformation($"Executing RemoveParticipant for ACS participant: {acsTarget} on call {callConnectionId}");

                var response = callConnection.RemoveParticipant(removeParticipantOptions);
                var operationStatus = response.GetRawResponse().ToString();

                string successMessage = $"Successfully removed ACS participant. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, CallStatus: {callStatus}, OperationStatus: {operationStatus}";
                _logger.LogInformation(successMessage);

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = operationStatus
                });
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogInformation($"ACS target validation error for call {callConnectionId}: {ex.Message}");
                return BadRequest($"Invalid ACS target: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error removing ACS participant for call {callConnectionId}: {ex.Message}");
                return Problem($"Failed to remove ACS participant: {ex.Message}");
            }
        }

        /// <summary>
        /// Cancels adding a participant to a call asynchronously
        /// </summary>
        /// <param name="callConnectionId">Call connection ID</param>
        /// <param name="invitationId">Invitation ID to cancel</param>
        /// <returns>Operation result</returns>
        [HttpPost("cancelAddParticipantAsync")]
        [ProducesResponseType(typeof(CallConnectionResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        [Tags("Add/Remove Participant APIs")]
        public async Task<IActionResult> CancelAddParticipantAsync(string callConnectionId, string invitationId)
        {
            try
            {
                _logger.LogInformation($"Starting to cancel add participant async with invitation ID: {invitationId} for call {callConnectionId}");

                var correlationId = (_service.GetCallConnectionProperties(callConnectionId)).CorrelationId;
                var callStatus = (_service.GetCallConnectionProperties(callConnectionId)).CallConnectionState.ToString();

                CallConnection callConnection = _service.GetCallConnection(callConnectionId);

                CancelAddParticipantOperationOptions cancelAddParticipantOperationOptions = new CancelAddParticipantOperationOptions(invitationId)
                {
                    OperationContext = "CancelAddingParticipantContext"
                };

                _logger.LogInformation($"Executing CancelAddParticipantOperationAsync for invitation: {invitationId} on call {callConnectionId}");

                var result = await callConnection.CancelAddParticipantOperationAsync(cancelAddParticipantOperationOptions);
                var operationStatus = result.GetRawResponse().ToString();
                
                string successMessage = $"Successfully canceled add participant operation async. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, CallStatus: {callStatus}, OperationStatus: {operationStatus}";
                _logger.LogInformation(successMessage);

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = operationStatus
                });
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogInformation($"Invitation ID validation error for call {callConnectionId}: {ex.Message}");
                return BadRequest($"Invalid invitation ID: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error canceling add participant operation for call {callConnectionId}: {ex.Message}");
                return Problem($"Failed to cancel add participant operation: {ex.Message}");
            }
        }

        /// <summary>
        /// Cancels adding a participant to a call
        /// </summary>
        /// <param name="callConnectionId">Call connection ID</param>
        /// <param name="invitationId">Invitation ID to cancel</param>
        /// <returns>Operation result</returns>
        [HttpPost("cancelAddParticipant")]
        [ProducesResponseType(typeof(CallConnectionResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        [Tags("Add/Remove Participant APIs")]
        public IActionResult CancelAddParticipant(string callConnectionId, string invitationId)
        {
            try
            {
                _logger.LogInformation($"Starting to cancel add participant with invitation ID: {invitationId} for call {callConnectionId}");
                
                var correlationId = (_service.GetCallConnectionProperties(callConnectionId)).CorrelationId;
                var callStatus = (_service.GetCallConnectionProperties(callConnectionId)).CallConnectionState.ToString();

                CallConnection callConnection = _service.GetCallConnection(callConnectionId);
                CancelAddParticipantOperationOptions cancelAddParticipantOperationOptions = new CancelAddParticipantOperationOptions(invitationId)
                {
                    OperationContext = "CancelAddingParticipantContext"
                };

                _logger.LogInformation($"Executing CancelAddParticipantOperation for invitation: {invitationId} on call {callConnectionId}");

                var result = callConnection.CancelAddParticipantOperationAsync(cancelAddParticipantOperationOptions);
                var operationInfo = $"Task type: {result.GetType()}";
                
                string successMessage = $"Successfully canceled add participant operation. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, CallStatus: {callStatus}, OperationInfo: {operationInfo}";
                _logger.LogInformation(successMessage);
                
                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = "Operation Started"
                });
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogInformation($"Invitation ID validation error for call {callConnectionId}: {ex.Message}");
                return BadRequest($"Invalid invitation ID: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error canceling add participant operation for call {callConnectionId}: {ex.Message}");
                return Problem($"Failed to cancel add participant operation: {ex.Message}");
            }
        }
        /// <summary>
        /// Gets an ACS participant information asynchronously
        /// </summary>
        /// <param name="callConnectionId">Call connection ID</param>
        /// <param name="acsTarget">ACS participant ID to retrieve</param>
        /// <returns>Participant information</returns>
        [HttpGet("getAcsParticipantAsync/{callConnectionId}/{acsTarget}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        [Tags("Get Participant APIs")]
        public async Task<IActionResult> GetAcsParticipantAsync(string callConnectionId, string acsTarget)
        {
            try
            {
                _logger.LogInformation($"Starting to get ACS participant: {acsTarget} for call {callConnectionId}");

                var correlationId = (_service.GetCallConnectionProperties(callConnectionId)).CorrelationId;
                var callStatus = (_service.GetCallConnectionProperties(callConnectionId)).CallConnectionState.ToString();

                CallConnection callConnection = _service.GetCallConnection(callConnectionId);
                CallParticipant participant = await callConnection.GetParticipantAsync(new CommunicationUserIdentifier(acsTarget));

                if (participant != null)
                {
                    string participantInfo = $"Participant: {participant.Identifier.RawId}, IsOnHold: {participant.IsOnHold}";
                    _logger.LogInformation($"{participantInfo}. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}");

                    return Ok(new
                    {
                        CallConnectionId = callConnectionId,
                        CorrelationId = correlationId,
                        Participant = new
                        {
                            Id = participant.Identifier.RawId,
                            IsOnHold = participant.IsOnHold,
                            IsMuted = participant.IsMuted,
                            Type = "ACS User"
                        }
                    });
                }
                else
                {
                    _logger.LogInformation($"No participant found with target: {acsTarget}. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}");

                    return Ok(new
                    {
                        CallConnectionId = callConnectionId,
                        CorrelationId = correlationId,
                        Message = "Participant not found"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting participant: {ex.Message}. CallConnectionId: {callConnectionId}");
                return Problem($"Failed to get participant: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets an ACS participant information synchronously
        /// </summary>
        /// <param name="callConnectionId">Call connection ID</param>
        /// <param name="acsTarget">ACS participant ID to retrieve</param>
        /// <returns>Participant information</returns>
        [HttpGet("getAcsParticipant/{callConnectionId}/{acsTarget}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        [Tags("Get Participant APIs")]
        public IActionResult GetAcsParticipant(string callConnectionId, string acsTarget)
        {
            try
            {
                _logger.LogInformation($"Starting to get ACS participant: {acsTarget} for call {callConnectionId}");

                var correlationId = (_service.GetCallConnectionProperties(callConnectionId)).CorrelationId;
                var callStatus = (_service.GetCallConnectionProperties(callConnectionId)).CallConnectionState.ToString();

                CallConnection callConnection = _service.GetCallConnection(callConnectionId);
                CallParticipant participant = callConnection.GetParticipant(new CommunicationUserIdentifier(acsTarget));

                if (participant != null)
                {
                    string participantInfo = $"Participant: {participant.Identifier.RawId}, IsOnHold: {participant.IsOnHold}";
                    _logger.LogInformation($"{participantInfo}. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}");

                    return Ok(new
                    {
                        CallConnectionId = callConnectionId,
                        CorrelationId = correlationId,
                        Participant = new
                        {
                            Id = participant.Identifier.RawId,
                            IsOnHold = participant.IsOnHold,
                            IsMuted = participant.IsMuted,
                            Type = "ACS User"
                        }
                    });
                }
                else
                {
                    _logger.LogInformation($"No participant found with target: {acsTarget}. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}");

                    return Ok(new
                    {
                        CallConnectionId = callConnectionId,
                        CorrelationId = correlationId,
                        Message = "Participant not found"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting participant: {ex.Message}. CallConnectionId: {callConnectionId}");
                return Problem($"Failed to get participant: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets all participants in a call asynchronously
        /// </summary>
        /// <param name="callConnectionId">Call connection ID</param>
        /// <returns>List of participants</returns>
        [HttpGet("getParticipantListAsync/{callConnectionId}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        [Tags("Get Participant APIs")]
        public async Task<IActionResult> GetParticipantListAsync(string callConnectionId)
        {
            try
            {
                _logger.LogInformation($"Starting to get participant list for call {callConnectionId}");

                var correlationId = (_service.GetCallConnectionProperties(callConnectionId)).CorrelationId;
                var callStatus = (_service.GetCallConnectionProperties(callConnectionId)).CallConnectionState.ToString();

                CallConnection callConnection = _service.GetCallConnection(callConnectionId);
                var list = await callConnection.GetParticipantsAsync();

                var participants = list.Value.Select(p => new
                {
                    Id = p.Identifier.RawId,
                    IsOnHold = p.IsOnHold,
                    IsMuted = p.IsMuted,
                    IdentifierType = p.Identifier.GetType().ToString()
                }).ToList();

                int participantCount = participants.Count;
                _logger.LogInformation($"Found {participantCount} participants. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}");

                foreach (var participant in list.Value)
                {
                    _logger.LogInformation("----------------------------------------------------------------------");
                    _logger.LogInformation($"Participant: {participant.Identifier.RawId}, IsOnHold: {participant.IsOnHold}, IsMuted: {participant.IsMuted}. CallConnectionId: {callConnectionId}");
                    _logger.LogInformation("----------------------------------------------------------------------");
                }

                return Ok(new
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    CallStatus = callStatus,
                    ParticipantCount = participantCount,
                    Participants = participants
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting participant list: {ex.Message}. CallConnectionId: {callConnectionId}");
                return Problem($"Failed to get participant list: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets all participants in a call synchronously
        /// </summary>
        /// <param name="callConnectionId">Call connection ID</param>
        /// <returns>List of participants</returns>
        [HttpGet("getParticipantList/{callConnectionId}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        [Tags("Get Participant APIs")]
        public IActionResult GetParticipantList(string callConnectionId)
        {
            try
            {
                _logger.LogInformation($"Starting to get participant list for call {callConnectionId}");

                var correlationId = (_service.GetCallConnectionProperties(callConnectionId)).CorrelationId;
                var callStatus = (_service.GetCallConnectionProperties(callConnectionId)).CallConnectionState.ToString();

                CallConnection callConnection = _service.GetCallConnection(callConnectionId);
                var list = callConnection.GetParticipants();

                var participants = list.Value.Select(p => new
                {
                    Id = p.Identifier.RawId,
                    IsOnHold = p.IsOnHold,
                    IsMuted = p.IsMuted,
                    IdentifierType = p.Identifier.GetType().ToString()
                }).ToList();

                int participantCount = participants.Count;
                _logger.LogInformation($"Found {participantCount} participants. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}");

                foreach (var participant in list.Value)
                {
                    _logger.LogInformation("----------------------------------------------------------------------");
                    _logger.LogInformation($"Participant: {participant.Identifier.RawId}, IsOnHold: {participant.IsOnHold}, IsMuted: {participant.IsMuted}. CallConnectionId: {callConnectionId}");
                    _logger.LogInformation("----------------------------------------------------------------------");
                }

                return Ok(new
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    CallStatus = callStatus,
                    ParticipantCount = participantCount,
                    Participants = participants
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting participant list: {ex.Message}. CallConnectionId: {callConnectionId}");
                return Problem($"Failed to get participant list: {ex.Message}");
            }
        }

        [HttpPost("muteAcsParticipantAsync")]
        [ProducesResponseType(typeof(CallConnectionResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        [Tags("Mute Participant APIs")]
        public async Task<IActionResult> MuteAcsParticipantAsync(string callConnectionId, string acsTarget)
        {
            string correlationId = Guid.NewGuid().ToString();
            _logger.LogInformation($"Starting to mute ACS participant async: {acsTarget} for call {callConnectionId}, CorrelationId: {correlationId}");
            
            try
            {
                CommunicationIdentifier target = new CommunicationUserIdentifier(acsTarget);
                
                CallConnection callConnection = _service.GetCallConnection(callConnectionId);
                var result = await callConnection.MuteParticipantAsync(target);
                
                string successMessage = $"Participant muted successfully. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, Status: {result.GetRawResponse().Status}";
                _logger.LogInformation(successMessage);
                
                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = result.GetRawResponse().Status.ToString()
                });
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error muting participant. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, Error: {ex.Message}";
                _logger.LogError(errorMessage);
                return Problem($"Failed to mute participant: {ex.Message}");
            }
        }

        [HttpPost("muteAcsParticipant")]
        [ProducesResponseType(typeof(CallConnectionResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        [Tags("Mute Participant APIs")]
        public IActionResult MuteAcsParticipant(string callConnectionId, string acsTarget)
        {
            string correlationId = Guid.NewGuid().ToString();
            _logger.LogInformation($"Starting to mute ACS participant: {acsTarget} for call {callConnectionId}, CorrelationId: {correlationId}");
            
            try
            {
                CommunicationIdentifier target = new CommunicationUserIdentifier(acsTarget);
                
                CallConnection callConnection = _service.GetCallConnection(callConnectionId);
                var result = callConnection.MuteParticipant(target);
                
                string successMessage = $"Participant muted successfully. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, Status: {result.GetRawResponse().Status}";
                _logger.LogInformation(successMessage);
                
                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = result.GetRawResponse().Status.ToString()
                });
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error muting participant. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, Error: {ex.Message}";
                _logger.LogError(errorMessage);
                return Problem($"Failed to mute participant: {ex.Message}");
            }
        }
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



#region Add/Remove Participant to PSTN
/**********************************************************************************************
app.MapPost("/addPstnParticipantAsync", async (string callConnectionId, string pstnParticipant, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation($"Starting to add PSTN participant async: {pstnParticipant} for call {callConnectionId}");
        LogCollector.Log($"Starting to add PSTN participant async: {pstnParticipant} for call {callConnectionId}");
        
        CallConnection callConnection = GetConnection(callConnectionId);
        CallInvite callInvite = new CallInvite(new PhoneNumberIdentifier(pstnParticipant),
               new PhoneNumberIdentifier(acsPhoneNumber));
        var addParticipantOptions = new AddParticipantOptions(callInvite)
        {
            OperationContext = "addPstnUserContext",
            InvitationTimeoutInSeconds = 30,
        };

        logger.LogInformation($"Executing AddParticipantAsync for PSTN participant: {pstnParticipant} on call {callConnectionId}");
        LogCollector.Log($"Executing AddParticipantAsync for PSTN participant: {pstnParticipant} on call {callConnectionId}");
        
        var result = await callConnection.AddParticipantAsync(addParticipantOptions);
        
        logger.LogInformation($"Successfully added PSTN participant async: {pstnParticipant} to call {callConnectionId}");
        LogCollector.Log($"Successfully added PSTN participant async: {pstnParticipant} to call {callConnectionId}");
        
        return Results.Ok(new { Result = result, CallConnectionId = callConnectionId });
    }
    catch (ArgumentNullException ex)
    {
        logger.LogInformation($"PSTN participant validation error for call {callConnectionId}: {ex.Message}");
        LogCollector.Log($"PSTN participant validation error for call {callConnectionId}: {ex.Message}");
        return Results.BadRequest($"Invalid PSTN participant: {ex.Message}");
    }
    catch (Exception ex)
    {
        logger.LogInformation($"Error adding PSTN participant for call {callConnectionId}: {ex.Message}");
        LogCollector.Log($"Error adding PSTN participant for call {callConnectionId}: {ex.Message}");
        return Results.Problem($"Failed to add PSTN participant: {ex.Message}");
    }
}).WithTags("Add/Remove Participant APIs");

app.MapPost("/addPstnParticipant", (string callConnectionId, string pstnParticipant, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation($"Starting to add PSTN participant: {pstnParticipant} for call {callConnectionId}");
        LogCollector.Log($"Starting to add PSTN participant: {pstnParticipant} for call {callConnectionId}");
        
        CallConnection callConnection = GetConnection(callConnectionId);
        CallInvite callInvite = new CallInvite(new PhoneNumberIdentifier(pstnParticipant),
               new PhoneNumberIdentifier(acsPhoneNumber));
        var addParticipantOptions = new AddParticipantOptions(callInvite)
        {
            OperationContext = "addPstnUserContext",
            InvitationTimeoutInSeconds = 30,
        };

        logger.LogInformation($"Executing AddParticipant for PSTN participant: {pstnParticipant} on call {callConnectionId}");
        LogCollector.Log($"Executing AddParticipant for PSTN participant: {pstnParticipant} on call {callConnectionId}");
        
        var result = callConnection.AddParticipant(addParticipantOptions);
        
        logger.LogInformation($"Successfully added PSTN participant: {pstnParticipant} to call {callConnectionId}");
        LogCollector.Log($"Successfully added PSTN participant: {pstnParticipant} to call {callConnectionId}");
        
        return Results.Ok(new { Result = result, CallConnectionId = callConnectionId });
    }
    catch (ArgumentNullException ex)
    {
        logger.LogInformation($"PSTN participant validation error for call {callConnectionId}: {ex.Message}");
        LogCollector.Log($"PSTN participant validation error for call {callConnectionId}: {ex.Message}");
        return Results.BadRequest($"Invalid PSTN participant: {ex.Message}");
    }
    catch (Exception ex)
    {
        logger.LogInformation($"Error adding PSTN participant for call {callConnectionId}: {ex.Message}");
        LogCollector.Log($"Error adding PSTN participant for call {callConnectionId}: {ex.Message}");
        return Results.Problem($"Failed to add PSTN participant: {ex.Message}");
    }
}).WithTags("Add/Remove Participant APIs");

app.MapPost("/removePstnParticipantAsync", async (string callConnectionId, string pstnTarget, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation($"Starting to remove PSTN participant async: {pstnTarget} from call {callConnectionId}");
        LogCollector.Log($"Starting to remove PSTN participant async: {pstnTarget} from call {callConnectionId}");
        
        CallConnection callConnection = GetConnection(callConnectionId);
        RemoveParticipantOptions removeParticipantOptions = new RemoveParticipantOptions(new PhoneNumberIdentifier(pstnTarget))
        {
            OperationContext = "removePstnParticipantContext"
        };

        logger.LogInformation($"Executing RemoveParticipantAsync for PSTN participant: {pstnTarget} on call {callConnectionId}");
        LogCollector.Log($"Executing RemoveParticipantAsync for PSTN participant: {pstnTarget} on call {callConnectionId}");
        
        await callConnection.RemoveParticipantAsync(removeParticipantOptions);
        
        logger.LogInformation($"Successfully removed PSTN participant async: {pstnTarget} from call {callConnectionId}");
        LogCollector.Log($"Successfully removed PSTN participant async: {pstnTarget} from call {callConnectionId}");
        
        return Results.Ok(new { Status = "Success", CallConnectionId = callConnectionId });
    }
    catch (ArgumentNullException ex)
    {
        logger.LogInformation($"PSTN target validation error for call {callConnectionId}: {ex.Message}");
        LogCollector.Log($"PSTN target validation error for call {callConnectionId}: {ex.Message}");
        return Results.BadRequest($"Invalid PSTN target: {ex.Message}");
    }
    catch (Exception ex)
    {
        logger.LogInformation($"Error removing PSTN participant for call {callConnectionId}: {ex.Message}");
        LogCollector.Log($"Error removing PSTN participant for call {callConnectionId}: {ex.Message}");
        return Results.Problem($"Failed to remove PSTN participant: {ex.Message}");
    }
}).WithTags("Add/Remove Participant APIs");

app.MapPost("/removePstnParticipant", (string callConnectionId, string pstnTarget, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation($"Starting to remove PSTN participant: {pstnTarget} from call {callConnectionId}");
        LogCollector.Log($"Starting to remove PSTN participant: {pstnTarget} from call {callConnectionId}");
        
        CallConnection callConnection = GetConnection(callConnectionId);
        RemoveParticipantOptions removeParticipantOptions = new RemoveParticipantOptions(new PhoneNumberIdentifier(pstnTarget))
        {
            OperationContext = "removePstnParticipantContext"
        };

        logger.LogInformation($"Executing RemoveParticipant for PSTN participant: {pstnTarget} on call {callConnectionId}");
        LogCollector.Log($"Executing RemoveParticipant for PSTN participant: {pstnTarget} on call {callConnectionId}");
        
        callConnection.RemoveParticipant(removeParticipantOptions);
        
        logger.LogInformation($"Successfully removed PSTN participant: {pstnTarget} from call {callConnectionId}");
        LogCollector.Log($"Successfully removed PSTN participant: {pstnTarget} from call {callConnectionId}");
        
        return Results.Ok(new { Status = "Success", CallConnectionId = callConnectionId });
    }
    catch (ArgumentNullException ex)
    {
        logger.LogInformation($"PSTN target validation error for call {callConnectionId}: {ex.Message}");
        LogCollector.Log($"PSTN target validation error for call {callConnectionId}: {ex.Message}");
        return Results.BadRequest($"Invalid PSTN target: {ex.Message}");
    }
    catch (Exception ex)
    {
        logger.LogInformation($"Error removing PSTN participant for call {callConnectionId}: {ex.Message}");
        LogCollector.Log($"Error removing PSTN participant for call {callConnectionId}: {ex.Message}");
        return Results.Problem($"Failed to remove PSTN participant: {ex.Message}");
    }
}).WithTags("Add/Remove Participant APIs");
*/

#endregion

#region Add/Remove Teams Participant
/**********************************************************************************************
app.MapPost("/addTeamsParticipantAsync", async (string teamsObjectId, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation($"Starting to add Teams participant async: {teamsObjectId} for call {callConnectionId}");
        LogCollector.Log($"Starting to add Teams participant async: {teamsObjectId} for call {callConnectionId}");
        
        CallConnection callConnection = GetConnection(callConnectionId);
        CallInvite callInvite = new CallInvite(new MicrosoftTeamsUserIdentifier(teamsObjectId));
        var addParticipantOptions = new AddParticipantOptions(callInvite)
        {
            OperationContext = "addTeamsUserContext",
            InvitationTimeoutInSeconds = 30,
        };

        logger.LogInformation($"Executing AddParticipantAsync for Teams participant: {teamsObjectId} on call {callConnection.CallConnectionId}");
        LogCollector.Log($"Executing AddParticipantAsync for Teams participant: {teamsObjectId} on call {callConnectionId}");
        
        var result = await callConnection.AddParticipantAsync(addParticipantOptions);
        
        logger.LogInformation($"Successfully added Teams participant async: {teamsObjectId} to call {callConnectionId}");
        LogCollector.Log($"Successfully added Teams participant async: {teamsObjectId} to call {callConnectionId}");
        
        return Results.Ok(new { Result = result, CallConnectionId = callConnectionId });
    }
    catch (ArgumentNullException ex)
    {
        logger.LogInformation($"Teams participant validation error for call {callConnectionId}: {ex.Message}");
        LogCollector.Log($"Teams participant validation error for call {callConnectionId}: {ex.Message}");
        return Results.BadRequest($"Invalid Teams participant: {ex.Message}");
    }
    catch (Exception ex)
    {
        logger.LogInformation($"Error adding Teams participant for call {callConnectionId}: {ex.Message}");
        LogCollector.Log($"Error adding Teams participant for call {callConnectionId}: {ex.Message}");
        return Results.Problem($"Failed to add Teams participant: {ex.Message}");
    }
}).WithTags("Add/Remove Participant APIs");

app.MapPost("/addTeamsParticipant", (string callConnectionId, string teamsObjectId, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation($"Starting to add Teams participant: {teamsObjectId} for call {callConnectionId}");
        LogCollector.Log($"Starting to add Teams participant: {teamsObjectId} for call {callConnectionId}");
        
        CallConnection callConnection = GetConnection(callConnectionId);
        CallInvite callInvite = new CallInvite(new MicrosoftTeamsUserIdentifier(teamsObjectId));
        var addParticipantOptions = new AddParticipantOptions(callInvite)
        {
            OperationContext = "addTeamsUserContext",
            InvitationTimeoutInSeconds = 30,
        };

        logger.LogInformation($"Executing AddParticipant for Teams participant: {teamsObjectId} on call {callConnectionId}");
        LogCollector.Log($"Executing AddParticipant for Teams participant: {teamsObjectId} on call {callConnectionId}");
        
        var result = callConnection.AddParticipant(addParticipantOptions);
        
        logger.LogInformation($"Successfully added Teams participant: {teamsObjectId} to call {callConnectionId}");
        LogCollector.Log($"Successfully added Teams participant: {teamsObjectId} to call {callConnectionId}");
        
        return Results.Ok(new { Result = result, CallConnectionId = callConnectionId });
    }
    catch (ArgumentNullException ex)
    {
        logger.LogInformation($"Teams participant validation error for call {callConnectionId}: {ex.Message}");
        LogCollector.Log($"Teams participant validation error for call {callConnectionId}: {ex.Message}");
        return Results.BadRequest($"Invalid Teams participant: {ex.Message}");
    }
    catch (Exception ex)
    {
        logger.LogInformation($"Error adding Teams participant for call {callConnectionId}: {ex.Message}");
        LogCollector.Log($"Error adding Teams participant for call {callConnectionId}: {ex.Message}");
        return Results.Problem($"Failed to add Teams participant: {ex.Message}");
    }
}).WithTags("Add/Remove Participant APIs");

app.MapPost("/removeTeamsParticipantAsync", async (string callConnectionId, string teamsObjectId, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation($"Starting to remove Teams participant async: {teamsObjectId} from call {callConnectionId}");
        LogCollector.Log($"Starting to remove Teams participant async: {teamsObjectId} from call {callConnectionId}");
        
        CallConnection callConnection = GetConnection(callConnectionId);
        RemoveParticipantOptions removeParticipantOptions = new RemoveParticipantOptions(new MicrosoftTeamsUserIdentifier(teamsObjectId))
        {
            OperationContext = "removeTeamsParticipantContext"
        };

        logger.LogInformation($"Executing RemoveParticipantAsync for Teams participant: {teamsObjectId} on call {callConnectionId}");
        LogCollector.Log($"Executing RemoveParticipantAsync for Teams participant: {teamsObjectId} on call {callConnectionId}");
        
        await callConnection.RemoveParticipantAsync(removeParticipantOptions);
        
        logger.LogInformation($"Successfully removed Teams participant async: {teamsObjectId} from call {callConnectionId}");
        LogCollector.Log($"Successfully removed Teams participant async: {teamsObjectId} from call {callConnectionId}");
        
        return Results.Ok(new { Status = "Success", CallConnectionId = callConnectionId });
    }
    catch (ArgumentNullException ex)
    {
        logger.LogInformation($"Teams object ID validation error for call {callConnectionId}: {ex.Message}");
        LogCollector.Log($"Teams object ID validation error for call {callConnectionId}: {ex.Message}");
        return Results.BadRequest($"Invalid Teams object ID: {ex.Message}");
    }
    catch (Exception ex)
    {
        logger.LogInformation($"Error removing Teams participant for call {callConnectionId}: {ex.Message}");
        LogCollector.Log($"Error removing Teams participant for call {callConnectionId}: {ex.Message}");
        return Results.Problem($"Failed to remove Teams participant: {ex.Message}");
    }
}).WithTags("Add/Remove Participant APIs");

app.MapPost("/removeTeamsParticipant", (string callConnectionId, string teamsObjectId, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation($"Starting to remove Teams participant: {teamsObjectId} from call {callConnectionId}");
        LogCollector.Log($"Starting to remove Teams participant: {teamsObjectId} from call {callConnectionId}");
        
        CallConnection callConnection = GetConnection(callConnectionId);
        RemoveParticipantOptions removeParticipantOptions = new RemoveParticipantOptions(new MicrosoftTeamsUserIdentifier(teamsObjectId))
        {
            OperationContext = "removeTeamsParticipantContext"
        };

        logger.LogInformation($"Executing RemoveParticipant for Teams participant: {teamsObjectId} on call {callConnectionId}");
        LogCollector.Log($"Executing RemoveParticipant for Teams participant: {teamsObjectId} on call {callConnectionId}");
        
        callConnection.RemoveParticipantAsync(removeParticipantOptions);
        
        logger.LogInformation($"Successfully removed Teams participant: {teamsObjectId} from call {callConnectionId}");
        LogCollector.Log($"Successfully removed Teams participant: {teamsObjectId} from call {callConnectionId}");
        
        return Results.Ok(new { Status = "Success", CallConnectionId = callConnectionId });
    }
    catch (ArgumentNullException ex)
    {
        logger.LogInformation($"Teams object ID validation error for call {callConnectionId}: {ex.Message}");
        LogCollector.Log($"Teams object ID validation error for call {callConnectionId}: {ex.Message}");
        return Results.BadRequest($"Invalid Teams object ID: {ex.Message}");
    }
    catch (Exception ex)
    {
        logger.LogInformation($"Error removing Teams participant for call {callConnectionId}: {ex.Message}");
        LogCollector.Log($"Error removing Teams participant for call {callConnectionId}: {ex.Message}");
        return Results.Problem($"Failed to remove Teams participant: {ex.Message}");
    }
}).WithTags("Add/Remove Participant APIs");
*/
#endregion

#region Get Participant
/*
app.MapPost("/getPstnParticipantAsync", async (string callConnectionId, string pstnTarget, ILogger<Program> logger) =>
{
    try
    {
        CallConnection callConnection = GetConnection(callConnectionId);
        CallParticipant participant = await callConnection.GetParticipantAsync(new PhoneNumberIdentifier(pstnTarget));

        if (participant != null)
        {
            logger.LogInformation($"Participant:-->{participant.Identifier.RawId.ToString()}. CallConnectionId: {callConnectionId}");
            LogCollector.Log($"Participant:-->{participant.Identifier.RawId.ToString()}. CallConnectionId: {callConnectionId}");
            logger.LogInformation($"Is Participant on hold:-->{participant.IsOnHold}. CallConnectionId: {callConnectionId}");
            LogCollector.Log($"Is Participant on hold:-->{participant.IsOnHold}. CallConnectionId: {callConnectionId}");
        }
        else
        {
            logger.LogInformation($"No participant found with target: {pstnTarget}. CallConnectionId: {callConnectionId}");
            LogCollector.Log($"No participant found with target: {pstnTarget}. CallConnectionId: {callConnectionId}");
        }
        return Results.Ok();
    }
    catch (Exception ex)
    {
        logger.LogError($"Error getting participant: {ex.Message}. CallConnectionId: {callConnectionId}");
        LogCollector.Log($"Error getting participant: {ex.Message}. CallConnectionId: {callConnectionId}");
        return Results.Problem($"Failed to get participant: {ex.Message}. CallConnectionId: {callConnectionId}");
    }
}).WithTags("Get Participant APIs");

app.MapPost("/getPstnParticipant", (string callConnectionId, string pstnTarget, ILogger<Program> logger) =>
{
    try
    {
        CallConnection callConnection = GetConnection(callConnectionId);
        CallParticipant participant = callConnection.GetParticipant(new PhoneNumberIdentifier(pstnTarget));

        if (participant != null)
        {
            logger.LogInformation($"Participant:-->{participant.Identifier.RawId.ToString()}. CallConnectionId: {callConnectionId}");
            LogCollector.Log($"Participant:-->{participant.Identifier.RawId.ToString()}. CallConnectionId: {callConnectionId}");
            logger.LogInformation($"Is Participant on hold:-->{participant.IsOnHold}. CallConnectionId: {callConnectionId}");
            LogCollector.Log($"Is Participant on hold:-->{participant.IsOnHold}. CallConnectionId: {callConnectionId}");
        }
        else
        {
            logger.LogInformation($"No participant found with target: {pstnTarget}. CallConnectionId: {callConnectionId}");
            LogCollector.Log($"No participant found with target: {pstnTarget}. CallConnectionId: {callConnectionId}");
        }
        return Results.Ok();
    }
    catch (Exception ex)
    {
        logger.LogError($"Error getting participant: {ex.Message}. CallConnectionId: {callConnectionId}");
        LogCollector.Log($"Error getting participant: {ex.Message}. CallConnectionId: {callConnectionId}");
        return Results.Problem($"Failed to get participant: {ex.Message}. CallConnectionId: {callConnectionId}");
    }
}).WithTags("Get Participant APIs");
*/
#region Get Participants to Teams

///// <summary>
///// Gets a Teams participant information asynchronously
///// </summary>
///// <param name="callConnectionId">Call connection ID</param>
///// <param name="teamsObjectId">Teams user ID to retrieve</param>
///// <returns>Participant information</returns>
//[HttpGet("getTeamsParticipantAsync/{callConnectionId}/{teamsObjectId}")]
//[ProducesResponseType(typeof(object), 200)]
//[ProducesResponseType(typeof(ProblemDetails), 500)]
//[Tags("Get Participant APIs")]
//public async Task<IActionResult> GetTeamsParticipantAsync(string callConnectionId, string teamsObjectId)
//{
//    try
//    {
//        _logger.LogInformation($"Starting to get Teams participant: {teamsObjectId} for call {callConnectionId}");

//        var correlationId = (_service.GetCallConnectionProperties(callConnectionId)).CorrelationId;
//        var callStatus = (_service.GetCallConnectionProperties(callConnectionId)).CallConnectionState.ToString();

//        CallConnection callConnection = _service.GetCallConnection(callConnectionId);
//        CallParticipant participant = await callConnection.GetParticipantAsync(new MicrosoftTeamsUserIdentifier(teamsObjectId));

//        if (participant != null)
//        {
//            string participantInfo = $"Participant: {participant.Identifier.RawId}, IsOnHold: {participant.IsOnHold}";
//            _logger.LogInformation($"{participantInfo}. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}");

//            return Ok(new
//            {
//                CallConnectionId = callConnectionId,
//                CorrelationId = correlationId,
//                Participant = new
//                {
//                    Id = participant.Identifier.RawId,
//                    IsOnHold = participant.IsOnHold,
//                    IsMuted = participant.IsMuted,
//                    Type = "Teams User"
//                }
//            });
//        }
//        else
//        {
//            _logger.LogInformation($"No participant found with target: {teamsObjectId}. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}");

//            return Ok(new
//            {
//                CallConnectionId = callConnectionId,
//                CorrelationId = correlationId,
//                Message = "Participant not found"
//            });
//        }
//    }
//    catch (Exception ex)
//    {
//        _logger.LogError($"Error getting participant: {ex.Message}. CallConnectionId: {callConnectionId}");
//        return Problem($"Failed to get participant: {ex.Message}");
//    }
//}

///// <summary>
///// Gets a Teams participant information synchronously
///// </summary>
///// <param name="callConnectionId">Call connection ID</param>
///// <param name="teamsObjectId">Teams user ID to retrieve</param>
///// <returns>Participant information</returns>
//[HttpGet("getTeamsParticipant/{callConnectionId}/{teamsObjectId}")]
//[ProducesResponseType(typeof(object), 200)]
//[ProducesResponseType(typeof(ProblemDetails), 500)]
//[Tags("Get Participant APIs")]
//public IActionResult GetTeamsParticipant(string callConnectionId, string teamsObjectId)
//{
//    try
//    {
//        _logger.LogInformation($"Starting to get Teams participant: {teamsObjectId} for call {callConnectionId}");

//        var correlationId = (_service.GetCallConnectionProperties(callConnectionId)).CorrelationId;
//        var callStatus = (_service.GetCallConnectionProperties(callConnectionId)).CallConnectionState.ToString();

//        CallConnection callConnection = _service.GetCallConnection(callConnectionId);
//        CallParticipant participant = callConnection.GetParticipant(new MicrosoftTeamsUserIdentifier(teamsObjectId));

//        if (participant != null)
//        {
//            string participantInfo = $"Participant: {participant.Identifier.RawId}, IsOnHold: {participant.IsOnHold}";
//            _logger.LogInformation($"{participantInfo}. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}");

//            return Ok(new
//            {
//                CallConnectionId = callConnectionId,
//                CorrelationId = correlationId,
//                Participant = new
//                {
//                    Id = participant.Identifier.RawId,
//                    IsOnHold = participant.IsOnHold,
//                    IsMuted = participant.IsMuted,
//                    Type = "Teams User"
//                }
//            });
//        }
//        else
//        {
//            _logger.LogInformation($"No participant found with target: {teamsObjectId}. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}");

//            return Ok(new
//            {
//                CallConnectionId = callConnectionId,
//                CorrelationId = correlationId,
//                Message = "Participant not found"
//            });
//        }
//    }
//    catch (Exception ex)
//    {
//        _logger.LogError($"Error getting participant: {ex.Message}. CallConnectionId: {callConnectionId}");
//        return Problem($"Failed to get participant: {ex.Message}");
//    }
//}
#endregion
#endregion