using Azure.Communication.CallAutomation;
using Call_Automation_GCCH.Models;
using Call_Automation_GCCH.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Call_Automation_GCCH.Controllers
{
    [ApiController]
    [Route("api/dialog")]
    [Produces("application/json")]
    public class DialogController : ControllerBase
    {
        private readonly CallAutomationService _service;
        private readonly ILogger<CallController> _logger;
        private readonly ConfigurationRequest _config;
        public DialogController(CallAutomationService service,
            ILogger<CallController> logger, IOptions<ConfigurationRequest> configOptions)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = configOptions.Value ?? throw new ArgumentNullException(nameof(configOptions));
        }

        ///// <summary>
        ///// Start dialog asynchronous operation.
        ///// </summary>
        ///// <param name="callConnectionId"></param>
        ///// <returns>DialogResponse</returns>
        //[HttpPost("/startDialogAsync")]
        //[Tags("Dialog APIs")]
        //public async Task<IActionResult> StartDialogAsync(string callConnectionId)
        //{
        //    try
        //    {
        //        if (string.IsNullOrEmpty(callConnectionId))
        //        {
        //            return BadRequest("Call Connection ID is required");
        //        }

        //        if (string.IsNullOrEmpty(_config.DefaultBotId))
        //        {
        //            return BadRequest("Default bot id is not configured.");
        //        }

        //        _logger.LogInformation($"Start Dialog async. CallConnectionId: {callConnectionId}");

        //        Dictionary<string, object> dialogContext = new Dictionary<string, object>();

        //        CallDialog callDialog = _service.GetCallConnection(callConnectionId).GetCallDialog();
        //        string dialogId = Guid.NewGuid().ToString();
        //        string botAppId = _config.DefaultBotId;
        //        var dialogOptions = new StartDialog(dialogId, new PowerVirtualAgentsDialog(botAppId, dialogContext))
        //        {
        //            OperationContext = "DialogStart"
        //        };

        //        var response = await callDialog.StartDialogAsync(dialogOptions);

        //        var correlationId = (_service.GetCallConnectionProperties(callConnectionId)).CorrelationId;
        //        var callStatus = (_service.GetCallConnectionProperties(callConnectionId)).CallConnectionState.ToString();

        //        string successMessage = $" Dialog started successfully. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, CallStatus: {callStatus}";
        //        _logger.LogInformation(successMessage);
        //        var dialogResponse = new DialogResponse
        //        {
        //            CallConnectionId = callConnectionId,
        //            CorrelationId = correlationId,
        //            Status = callStatus,
        //            DialogId = dialogId
        //        };

        //        return Ok(dialogResponse);
        //    }
        //    catch (Exception ex)
        //    {
        //        string errorMessage = $"Error starting Dialog. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
        //        _logger.LogError(errorMessage);

        //        return Problem($"Failed to start dialog: {ex.Message}");
        //    }
        //}

        ///// <summary>
        ///// Start dialog synchronous operation.
        ///// </summary>
        ///// <param name="callConnectionId"></param>
        ///// <returns>DialogResponse</returns>
        //[HttpPost("/startDialog")]
        //[Tags("Dialog APIs")]
        //public IActionResult StartDialog(string callConnectionId)
        //{
        //    try
        //    {
        //        if (string.IsNullOrEmpty(callConnectionId))
        //        {
        //            return BadRequest("Call Connection ID is required");
        //        }

        //        if (string.IsNullOrEmpty(_config.DefaultBotId))
        //        {
        //            return BadRequest("Default bot id is not configured.");
        //        }

        //        _logger.LogInformation($"Start Dialog. CallConnectionId: {callConnectionId}");

        //        Dictionary<string, object> dialogContext = new Dictionary<string, object>();

        //        CallDialog callDialog = _service.GetCallConnection(callConnectionId).GetCallDialog();
        //        string dialogId = Guid.NewGuid().ToString();
        //        string botAppId = _config.DefaultBotId;
        //        var dialogOptions = new StartDialog(dialogId, new PowerVirtualAgentsDialog(botAppId, dialogContext))
        //        {
        //            OperationContext = "DialogStart"
        //        };

        //        var response = callDialog.StartDialog(dialogOptions);

        //        var correlationId = (_service.GetCallConnectionProperties(callConnectionId)).CorrelationId;
        //        var callStatus = (_service.GetCallConnectionProperties(callConnectionId)).CallConnectionState.ToString();

        //        string successMessage = $" Dialog started successfully. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, CallStatus: {callStatus}";
        //        _logger.LogInformation(successMessage);
        //        DialogResponse dialogResponse = new DialogResponse
        //        {
        //            CallConnectionId = callConnectionId,
        //            CorrelationId = correlationId,
        //            Status = callStatus,
        //            DialogId = dialogId
        //        };

        //        return Ok(dialogResponse);
        //    }
        //    catch (Exception ex)
        //    {
        //        string errorMessage = $"Error starting Dialog. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
        //        _logger.LogError(errorMessage);

        //        return Problem($"Failed to start dialog: {ex.Message}");
        //    }
        //}

        ///// <summary>
        ///// Stop dialog asynchronous operation.
        ///// </summary>
        ///// <param name="callConnectionId"></param>
        ///// <param name="dialogId"></param>
        ///// <returns>DialogResponse</returns>
        //[HttpPost("/stopDialogAsync")]
        //[Tags("Dialog APIs")]
        //public async Task<IActionResult> StopDialogAsync(string callConnectionId, string dialogId)
        //{
        //    try
        //    {
        //        if (string.IsNullOrEmpty(callConnectionId))
        //        {
        //            return BadRequest("Call Connection ID is required");
        //        }

        //        if (string.IsNullOrEmpty(dialogId))
        //        {
        //            return BadRequest("Dialog ID is required");
        //        }

        //        _logger.LogInformation($"Stop Dialog async. CallConnectionId: {callConnectionId}");

        //        CallDialog callDialog = _service.GetCallConnection(callConnectionId).GetCallDialog();

        //        var response = await callDialog.StopDialogAsync(dialogId);

        //        var correlationId = (_service.GetCallConnectionProperties(callConnectionId)).CorrelationId;
        //        var callStatus = (_service.GetCallConnectionProperties(callConnectionId)).CallConnectionState.ToString();

        //        string successMessage = $" Dialog stopped successfully. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, CallStatus: {callStatus}";
        //        _logger.LogInformation(successMessage);
        //        DialogResponse dialogResponse = new DialogResponse
        //        {
        //            CallConnectionId = callConnectionId,
        //            CorrelationId = correlationId,
        //            Status = callStatus,
        //            DialogId = dialogId
        //        };

        //        return Ok(dialogResponse);
        //    }
        //    catch (Exception ex)
        //    {
        //        string errorMessage = $"Error stoping Dialog. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
        //        _logger.LogError(errorMessage);

        //        return Problem($"Failed to stop dialog: {ex.Message}");
        //    }
        //}

        ///// <summary>
        ///// Stop dialog synchronous operation.
        ///// </summary>
        ///// <param name="callConnectionId"></param>
        ///// <param name="dialogId"></param>
        ///// <returns>DialogResponse</returns>
        //[HttpPost("/stopDialog")]
        //[Tags("Dialog APIs")]
        //public IActionResult StopDialog(string callConnectionId, string dialogId)
        //{
        //    try
        //    {
        //        if (string.IsNullOrEmpty(callConnectionId))
        //        {
        //            return BadRequest("Call Connection ID is required");
        //        }

        //        if (string.IsNullOrEmpty(dialogId))
        //        {
        //            return BadRequest("Dialog ID is required");
        //        }

        //        _logger.LogInformation($"Stop Dialog sync. CallConnectionId: {callConnectionId}");

        //        CallDialog callDialog = _service.GetCallConnection(callConnectionId).GetCallDialog();

        //        var response = callDialog.StopDialog(dialogId);

        //        var correlationId = (_service.GetCallConnectionProperties(callConnectionId)).CorrelationId;
        //        var callStatus = (_service.GetCallConnectionProperties(callConnectionId)).CallConnectionState.ToString();

        //        string successMessage = $" Dialog stopped successfully. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, CallStatus: {callStatus}";
        //        _logger.LogInformation(successMessage);
        //        DialogResponse dialogResponse = new DialogResponse
        //        {
        //            CallConnectionId = callConnectionId,
        //            CorrelationId = correlationId,
        //            Status = callStatus,
        //            DialogId = dialogId
        //        };

        //        return Ok(dialogResponse);
        //    }
        //    catch (Exception ex)
        //    {
        //        string errorMessage = $"Error stoping Dialog to ACS target. CallConnectionId: {callConnectionId}. Error: {ex.Message}";
        //        _logger.LogError(errorMessage);

        //        return Problem($"Failed to stop dialog: {ex.Message}");
        //    }
        //}
    }
}
