using System;
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

        // ─ Add ───────────────────────────────────────────────────────────────────────

        [HttpPost("addParticipant")]
        [Tags("Add/Remove Participant APIs")]
        public IActionResult AddParticipant(
            string callConnectionId,
            string participantId,
            bool isPstn = false)
            => HandleAddParticipant(callConnectionId, participantId, isPstn, async: false).Result;

        [HttpPost("addParticipantAsync")]
        [Tags("Add/Remove Participant APIs")]
        public Task<IActionResult> AddParticipantAsync(
            string callConnectionId,
            string participantId,
            bool isPstn = false)
            => HandleAddParticipant(callConnectionId, participantId, isPstn, async: true);

        // ─ Remove ────────────────────────────────────────────────────────────────────

        [HttpPost("removeParticipant")]
        [Tags("Add/Remove Participant APIs")]
        public IActionResult RemoveParticipant(
            string callConnectionId,
            string participantId,
            bool isPstn = false)
            => HandleRemoveParticipant(callConnectionId, participantId, isPstn, async: false).Result;

        [HttpPost("removeParticipantAsync")]
        [Tags("Add/Remove Participant APIs")]
        public Task<IActionResult> RemoveParticipantAsync(
            string callConnectionId,
            string participantId,
            bool isPstn = false)
            => HandleRemoveParticipant(callConnectionId, participantId, isPstn, async: true);

        // ─ Get ───────────────────────────────────────────────────────────────────────

        [HttpGet("getParticipant")]
        [Tags("Get Participant APIs")]
        public IActionResult GetParticipant(
            string callConnectionId,
            string participantId,
            bool isPstn = false)
            => HandleGetParticipant(callConnectionId, participantId, isPstn, async: false).Result;

        [HttpGet("getParticipantAsync")]
        [Tags("Get Participant APIs")]
        public Task<IActionResult> GetParticipantAsync(
            string callConnectionId,
            string participantId,
            bool isPstn = false)
            => HandleGetParticipant(callConnectionId, participantId, isPstn, async: true);

        // ─ Mute ──────────────────────────────────────────────────────────────────────

        [HttpPost("muteParticipant")]
        [Tags("Mute Participant APIs")]
        public IActionResult MuteParticipant(
            string callConnectionId,
            string participantId,
            bool isPstn = false)
            => HandleMuteParticipant(callConnectionId, participantId, isPstn, async: false).Result;

        [HttpPost("muteParticipantAsync")]
        [Tags("Mute Participant APIs")]
        public Task<IActionResult> MuteParticipantAsync(
            string callConnectionId,
            string participantId,
            bool isPstn = false)
            => HandleMuteParticipant(callConnectionId, participantId, isPstn, async: true);

        // ─────────────── Shared Handlers ────────────────────────────────────────────

        private async Task<IActionResult> HandleAddParticipant(
            string callConnectionId,
            string participantId,
            bool isPstn,
            bool async)
        {
            var opName = isPstn ? "PSTN" : "ACS";
            _logger.LogInformation($"Starting to add {opName} participant: {participantId} for call {callConnectionId}");

            try
            {
                var props = _service.GetCallConnectionProperties(callConnectionId);
                var connection = _service.GetCallConnection(callConnectionId);

                // build invite
                CallInvite invite = isPstn
                    ? new CallInvite(
                          new PhoneNumberIdentifier(participantId),
                          new PhoneNumberIdentifier(_config.AcsPhoneNumber))
                    : new CallInvite(new CommunicationUserIdentifier(participantId));

                var options = new AddParticipantOptions(invite)
                {
                    OperationContext = isPstn
                        ? "addPstnUserContext"
                        : "addAcsUserContext",
                    InvitationTimeoutInSeconds = 30
                };

                Response<AddParticipantResult> result = async
                    ? await connection.AddParticipantAsync(options)
                    : connection.AddParticipant(options);

                _logger.LogInformation(
                    $"{opName} participant added: Call={callConnectionId}, CorrId={props.CorrelationId}, " +
                    $"Status={result.GetRawResponse().Status}, InviteId={result.Value.InvitationId}");

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = props.CorrelationId,
                    Status = $"{result.GetRawResponse().Status}; InviteId={result.Value.InvitationId}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding {opName} participant");
                return Problem($"Failed to add participant: {ex.Message}");
            }
        }

        private async Task<IActionResult> HandleRemoveParticipant(
            string callConnectionId,
            string participantId,
            bool isPstn,
            bool async)
        {
            var opName = isPstn ? "PSTN" : "ACS";
            _logger.LogInformation($"Starting to remove {opName} participant: {participantId} from call {callConnectionId}");

            try
            {
                var props = _service.GetCallConnectionProperties(callConnectionId);
                var connection = _service.GetCallConnection(callConnectionId);

                var target = isPstn
                    ? (CommunicationIdentifier)new PhoneNumberIdentifier(participantId)
                    : new CommunicationUserIdentifier(participantId);

                var options = new RemoveParticipantOptions(target)
                {
                    OperationContext = isPstn
                        ? "removePstnParticipantContext"
                        : "removeAcsParticipantContext"
                };

                Response<RemoveParticipantResult> result = async
                     ? await connection.RemoveParticipantAsync(options)
                     : connection.RemoveParticipant(options);

                _logger.LogInformation(
                    $"{opName} participant removed: Call={callConnectionId}, CorrId={props.CorrelationId}, " +
                    $"Status={result.GetRawResponse().Status}");

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = props.CorrelationId,
                    Status = $"{result.GetRawResponse().Status}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing {opName} participant");
                return Problem($"Failed to remove participant: {ex.Message}");
            }
        }

        private async Task<IActionResult> HandleGetParticipant(
            string callConnectionId,
            string participantId,
            bool isPstn,
            bool async)
        {
            var opName = isPstn ? "PSTN" : "ACS";
            _logger.LogInformation($"Starting to get {opName} participant: {participantId} for call {callConnectionId}");

            try
            {
                var props = _service.GetCallConnectionProperties(callConnectionId);
                var connection = _service.GetCallConnection(callConnectionId);

                CallParticipant participant = async
                    ? await connection.GetParticipantAsync(
                          isPstn
                            ? (CommunicationIdentifier)new PhoneNumberIdentifier(participantId)
                            : new CommunicationUserIdentifier(participantId))
                    : connection.GetParticipant(
                          isPstn
                            ? (CommunicationIdentifier)new PhoneNumberIdentifier(participantId)
                            : new CommunicationUserIdentifier(participantId));

                if (participant == null)
                    return NotFound(new { callConnectionId, correlationId = props.CorrelationId, Message = "Not found" });

                return Ok(new
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = props.CorrelationId,
                    Participant = new
                    {
                        RawId = participant.Identifier.RawId,
                        IsOnHold = participant.IsOnHold,
                        IsMuted = participant.IsMuted
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting {opName} participant");
                return Problem($"Failed to get participant: {ex.Message}");
            }
        }

        private async Task<IActionResult> HandleMuteParticipant(
            string callConnectionId,
            string participantId,
            bool isPstn,
            bool async)
        {
            var opName = isPstn ? "PSTN" : "ACS";
            _logger.LogInformation($"Starting to mute {opName} participant: {participantId} for call {callConnectionId}");

            try
            {
                var props = _service.GetCallConnectionProperties(callConnectionId);
                var connection = _service.GetCallConnection(callConnectionId);

                var target = isPstn
                    ? (CommunicationIdentifier)new PhoneNumberIdentifier(participantId)
                    : new CommunicationUserIdentifier(participantId);

                Response<MuteParticipantResult> result = async
                    ? await connection.MuteParticipantAsync(target)
                    : connection.MuteParticipant(target);

                _logger.LogInformation(
                    $"{opName} participant muted: Call={callConnectionId}, CorrId={props.CorrelationId}, " +
                    $"Status={result.GetRawResponse().Status}");

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = props.CorrelationId,
                    Status = $"{result.GetRawResponse().Status}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error muting {opName} participant");
                return Problem($"Failed to mute participant: {ex.Message}");
            }
        }
    }
}