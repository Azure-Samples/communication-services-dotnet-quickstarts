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
    [Route("api/connect")]
    [Produces("application/json")]
    public class ConnectController : ControllerBase
    {
        private readonly CallAutomationService _service;
        private readonly ILogger<ConnectController> _logger;
        private readonly ConfigurationRequest _config; // final, bound object

        public ConnectController(
            CallAutomationService service,
            ILogger<ConnectController> logger, IOptions<ConfigurationRequest> configOptions)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = configOptions.Value ?? throw new ArgumentNullException(nameof(configOptions));
        }

    //    /// <summary>
    //    /// Connects to a room call asynchronously
    //    /// </summary>
    //    /// <param name="roomId">The room ID to connect to</param>
    //    /// <returns>Call connection information</returns>
    //    [HttpPost("/ConnectRoomCallAsync")]
    //    [Tags("Connect Call APIs")]
    //    public async Task<IActionResult> ConnectRoomCallAsync(string roomId)
    //    {
    //        try
    //        {
    //            if (string.IsNullOrEmpty(roomId))
    //            {
    //                return BadRequest("Room ID is required");
    //            }

    //            _logger.LogInformation($"Starting async room call connection to: {roomId}");

    //            RoomCallLocator roomCallLocator = new RoomCallLocator(roomId);
    //            var callbackUri = new Uri(new Uri(_config.CallbackUriHost), "/api/callbacks");
    //            var websocketUri = _config.CallbackUriHost.Replace("https", "wss") + "/ws";
    //            MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(new Uri(websocketUri), MediaStreamingContent.Audio,
    //                MediaStreamingAudioChannel.Unmixed, MediaStreamingTransport.Websocket, false);
    //            TranscriptionOptions transcriptionOptions = new TranscriptionOptions(new Uri(websocketUri), TranscriptionTransport.Websocket,
    //                "en-us", false);
    //            ConnectCallOptions connectCallOptions = new ConnectCallOptions(roomCallLocator, callbackUri)
    //            {
    //                // ACS GCCH Phase 2
    //                // CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
    //                OperationContext = "ConnectRoomCallContext",
    //                MediaStreamingOptions = mediaStreamingOptions,
    //                TranscriptionOptions = transcriptionOptions
    //            };

    //            _logger.LogInformation("Initiating ConnectCallAsync operation for room call");

    //            ConnectCallResult connectCallResult = await _service.GetCallAutomationClient().ConnectCallAsync(connectCallOptions);
    //            var operationStatus = connectCallResult.CallConnectionProperties.CallConnectionState.ToString();

    //            _logger.LogInformation($"Connected room async call with connection id: {connectCallResult.CallConnectionProperties.CallConnectionId}, CorrelationId: {connectCallResult.CallConnectionProperties.CorrelationId}, Status: {operationStatus}");

    //            return Ok(new CallConnectionResponse
    //            {
    //                CallConnectionId = connectCallResult.CallConnectionProperties.CallConnectionId,
    //                CorrelationId = connectCallResult.CallConnectionProperties.CorrelationId,
    //                Status = operationStatus
    //            });
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError($"Error connecting room call: {ex.Message}");
    //            return Problem($"Failed to connect room call: {ex.Message}");
    //        }
    //    }

    //    /// <summary>
    //    /// Connects to a room call synchronously
    //    /// </summary>
    //    /// <param name="roomId">The room ID to connect to</param>
    //    /// <returns>Call connection information</returns>
    //    [HttpPost("/ConnectRoomCall")]
    //    [Tags("Connect Call APIs")]
    //    public IActionResult ConnectRoomCall(string roomId)
    //    {
    //        try
    //        {
    //            if (string.IsNullOrEmpty(roomId))
    //            {
    //                return BadRequest("Room ID is required");
    //            }

    //            _logger.LogInformation($"Starting room call connection to: {roomId}");

    //            RoomCallLocator roomCallLocator = new RoomCallLocator(roomId);
    //            var callbackUri = new Uri(new Uri(_config.CallbackUriHost), "/api/callbacks");
    //            var websocketUri = _config.CallbackUriHost.Replace("https", "wss") + "/ws";
    //            MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(new Uri(websocketUri), MediaStreamingContent.Audio,
    //                MediaStreamingAudioChannel.Unmixed, MediaStreamingTransport.Websocket, false);
    //            TranscriptionOptions transcriptionOptions = new TranscriptionOptions(new Uri(websocketUri), TranscriptionTransport.Websocket,
    //                "en-us", false);
    //            ConnectCallOptions connectCallOptions = new ConnectCallOptions(roomCallLocator, callbackUri)
    //            {
    //                // ACS GCCH Phase 2
    //                // CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
    //                OperationContext = "ConnectRoomCallContext",
    //                MediaStreamingOptions = mediaStreamingOptions,
    //                TranscriptionOptions = transcriptionOptions
    //            };

    //            _logger.LogInformation("Initiating ConnectCall operation for room call");

    //            ConnectCallResult connectCallResult = _service.GetCallAutomationClient().ConnectCall(connectCallOptions);
    //            var operationStatus = connectCallResult.CallConnectionProperties.CallConnectionState.ToString();

    //            _logger.LogInformation($"Connected room call with connection id: {connectCallResult.CallConnectionProperties.CallConnectionId}, CorrelationId: {connectCallResult.CallConnectionProperties.CorrelationId}, Status: {operationStatus}");

    //            return Ok(new CallConnectionResponse
    //            {
    //                CallConnectionId = connectCallResult.CallConnectionProperties.CallConnectionId,
    //                CorrelationId = connectCallResult.CallConnectionProperties.CorrelationId,
    //                Status = operationStatus
    //            });
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError($"Error connecting room call: {ex.Message}");
    //            return Problem($"Failed to connect room call: {ex.Message}");
    //        }
    //    }

    //    /// <summary>
    //    /// Connects to a group call asynchronously
    //    /// </summary>
    //    /// <param name="groupId">The group ID to connect to</param>
    //    /// <returns>Call connection information</returns>
    //    [HttpPost("/ConnectGroupCallAsync")]
    //    [Tags("Connect Call APIs")]
    //    public async Task<IActionResult> ConnectGroupCallAsync(string groupId)
    //    {
    //        try
    //        {
    //            if (string.IsNullOrEmpty(groupId))
    //            {
    //                return BadRequest("Group ID is required");
    //            }

    //            _logger.LogInformation($"Starting async group call connection to: {groupId}");

    //            GroupCallLocator groupCallLocator = new GroupCallLocator(groupId);
    //            var callbackUri = new Uri(new Uri(_config.CallbackUriHost), "/api/callbacks");
    //            var websocketUri = _config.CallbackUriHost.Replace("https", "wss") + "/ws";
    //            MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(new Uri(websocketUri), MediaStreamingContent.Audio,
    //                MediaStreamingAudioChannel.Unmixed, MediaStreamingTransport.Websocket, false);
    //            TranscriptionOptions transcriptionOptions = new TranscriptionOptions(new Uri(websocketUri), TranscriptionTransport.Websocket,
    //                "en-us", false);
    //            ConnectCallOptions connectCallOptions = new ConnectCallOptions(groupCallLocator, callbackUri)
    //            {
    //                // ACS GCCH Phase 2
    //                // CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
    //                OperationContext = "ConnectGroupCallContext",
    //                MediaStreamingOptions = mediaStreamingOptions,
    //                TranscriptionOptions = transcriptionOptions
    //            };

    //            _logger.LogInformation("Initiating ConnectCallAsync operation for group call");

    //            ConnectCallResult connectCallResult = await _service.GetCallAutomationClient().ConnectCallAsync(connectCallOptions);
    //            var operationStatus = connectCallResult.CallConnectionProperties.CallConnectionState.ToString();

    //            _logger.LogInformation($"Connected group async call with connection id: {connectCallResult.CallConnectionProperties.CallConnectionId}, CorrelationId: {connectCallResult.CallConnectionProperties.CorrelationId}, Status: {operationStatus}");

    //            return Ok(new CallConnectionResponse
    //            {
    //                CallConnectionId = connectCallResult.CallConnectionProperties.CallConnectionId,
    //                CorrelationId = connectCallResult.CallConnectionProperties.CorrelationId,
    //                Status = operationStatus
    //            });
    //        }
    //        catch (ArgumentNullException ex)
    //        {
    //            _logger.LogError($"Group ID validation error: {ex.Message}");
    //            return BadRequest($"Invalid group ID: {ex.Message}");
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError($"Error connecting group call: {ex.Message}");
    //            return Problem($"Failed to connect group call: {ex.Message}");
    //        }
    //    }

    //    /// <summary>
    //    /// Connects to a group call synchronously
    //    /// </summary>
    //    /// <param name="groupId">The group ID to connect to</param>
    //    /// <returns>Call connection information</returns>
    //    [HttpPost("/ConnectGroupCall")]
    //    [Tags("Connect Call APIs")]
    //    public IActionResult ConnectGroupCall(string groupId)
    //    {
    //        try
    //        {
    //            if (string.IsNullOrEmpty(groupId))
    //            {
    //                return BadRequest("Group ID is required");
    //            }

    //            _logger.LogInformation($"Starting group call connection to: {groupId}");

    //            GroupCallLocator groupCallLocator = new GroupCallLocator(groupId);
    //            var callbackUri = new Uri(new Uri(_config.CallbackUriHost), "/api/callbacks");
    //            var websocketUri = _config.CallbackUriHost.Replace("https", "wss") + "/ws";
    //            MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(new Uri(websocketUri), MediaStreamingContent.Audio,
    //                MediaStreamingAudioChannel.Unmixed, MediaStreamingTransport.Websocket, false);
    //            TranscriptionOptions transcriptionOptions = new TranscriptionOptions(new Uri(websocketUri), TranscriptionTransport.Websocket,
    //                "en-us", false);
    //            ConnectCallOptions connectCallOptions = new ConnectCallOptions(groupCallLocator, callbackUri)
    //            {
    //                // ACS GCCH Phase 2
    //                // CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
    //                OperationContext = "ConnectGroupCallContext",
    //                MediaStreamingOptions = mediaStreamingOptions,
    //                TranscriptionOptions = transcriptionOptions
    //            };

    //            _logger.LogInformation("Initiating ConnectCall operation for group call");

    //            ConnectCallResult connectCallResult = _service.GetCallAutomationClient().ConnectCall(connectCallOptions);
    //            var operationStatus = connectCallResult.CallConnectionProperties.CallConnectionState.ToString();

    //            _logger.LogInformation($"Connected group call with connection id: {connectCallResult.CallConnectionProperties.CallConnectionId}, CorrelationId: {connectCallResult.CallConnectionProperties.CorrelationId}, Status: {operationStatus}");

    //            return Ok(new CallConnectionResponse
    //            {
    //                CallConnectionId = connectCallResult.CallConnectionProperties.CallConnectionId,
    //                CorrelationId = connectCallResult.CallConnectionProperties.CorrelationId,
    //                Status = operationStatus
    //            });
    //        }
    //        catch (ArgumentNullException ex)
    //        {
    //            _logger.LogError($"Group ID validation error: {ex.Message}");
    //            return BadRequest($"Invalid group ID: {ex.Message}");
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError($"Error connecting group call: {ex.Message}");
    //            return Problem($"Failed to connect group call: {ex.Message}");
    //        }
    //    }

    //    /// <summary>
    //    /// Connects to a one-to-N call asynchronously
    //    /// </summary>
    //    /// <param name="serverCallId">The server call ID to connect to</param>
    //    /// <returns>Call connection information</returns>
    //    [HttpPost("/ConnectOneToNCallAsync")]
    //    [Tags("Connect Call APIs")]
    //    public async Task<IActionResult> ConnectOneToNCallAsync(string serverCallId)
    //    {
    //        try
    //        {
    //            if (string.IsNullOrEmpty(serverCallId))
    //            {
    //                return BadRequest("Server Call ID is required");
    //            }

    //            _logger.LogInformation($"Starting async one-to-N call connection to: {serverCallId}");

    //            ServerCallLocator serverCallLocator = new ServerCallLocator(serverCallId);
    //            var callbackUri = new Uri(new Uri(_config.CallbackUriHost), "/api/callbacks");
    //            var websocketUri = _config.CallbackUriHost.Replace("https", "wss") + "/ws";
    //            MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(new Uri(websocketUri), MediaStreamingContent.Audio,
    //                MediaStreamingAudioChannel.Unmixed, MediaStreamingTransport.Websocket, false);
    //            TranscriptionOptions transcriptionOptions = new TranscriptionOptions(new Uri(websocketUri), TranscriptionTransport.Websocket,
    //                "en-us", false);
    //            ConnectCallOptions connectCallOptions = new ConnectCallOptions(serverCallLocator, callbackUri)
    //            {
    //                // ACS GCCH Phase 2
    //                // CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
    //                OperationContext = "ConnectOneToNCallContext",
    //                MediaStreamingOptions = mediaStreamingOptions,
    //                TranscriptionOptions = transcriptionOptions
    //            };

    //            _logger.LogInformation("Initiating ConnectCallAsync operation for one-to-N call");

    //            ConnectCallResult connectCallResult = await _service.GetCallAutomationClient().ConnectCallAsync(connectCallOptions);
    //            var operationStatus = connectCallResult.CallConnectionProperties.CallConnectionState.ToString();

    //            _logger.LogInformation($"Connected 1 to N async call with connection id: {connectCallResult.CallConnectionProperties.CallConnectionId}, CorrelationId: {connectCallResult.CallConnectionProperties.CorrelationId}, Status: {operationStatus}");

    //            return Ok(new CallConnectionResponse
    //            {
    //                CallConnectionId = connectCallResult.CallConnectionProperties.CallConnectionId,
    //                CorrelationId = connectCallResult.CallConnectionProperties.CorrelationId,
    //                Status = operationStatus
    //            });
    //        }
    //        catch (ArgumentNullException ex)
    //        {
    //            _logger.LogError($"Server call ID validation error: {ex.Message}");
    //            return BadRequest($"Invalid server call ID: {ex.Message}");
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError($"Error connecting one-to-N call: {ex.Message}");
    //            return Problem($"Failed to connect one-to-N call: {ex.Message}");
    //        }
    //    }

    //    /// <summary>
    //    /// Connects to a one-to-N call synchronously
    //    /// </summary>
    //    /// <param name="serverCallId">The server call ID to connect to</param>
    //    /// <returns>Call connection information</returns>
    //    [HttpPost("/ConnectOneToNCall")]
    //    [Tags("Connect Call APIs")]
    //    public IActionResult ConnectOneToNCall(string serverCallId)
    //    {
    //        try
    //        {
    //            if (string.IsNullOrEmpty(serverCallId))
    //            {
    //                return BadRequest("Server Call ID is required");
    //            }

    //            _logger.LogInformation($"Starting one-to-N call connection to: {serverCallId}");

    //            ServerCallLocator serverCallLocator = new ServerCallLocator(serverCallId);
    //            var callbackUri = new Uri(new Uri(_config.CallbackUriHost), "/api/callbacks");
    //            var websocketUri = _config.CallbackUriHost.Replace("https", "wss") + "/ws";
    //            MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(new Uri(websocketUri), MediaStreamingContent.Audio,
    //                MediaStreamingAudioChannel.Unmixed, MediaStreamingTransport.Websocket, false);
    //            TranscriptionOptions transcriptionOptions = new TranscriptionOptions(new Uri(websocketUri), TranscriptionTransport.Websocket,
    //                "en-us", false);
    //            ConnectCallOptions connectCallOptions = new ConnectCallOptions(serverCallLocator, callbackUri)
    //            {
    //                // ACS GCCH Phase 2
    //                // CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint) },
    //                OperationContext = "ConnectOneToNCallContext",
    //                MediaStreamingOptions = mediaStreamingOptions,
    //                TranscriptionOptions = transcriptionOptions
    //            };

    //            _logger.LogInformation("Initiating ConnectCall operation for one-to-N call");

    //            ConnectCallResult connectCallResult = _service.GetCallAutomationClient().ConnectCall(connectCallOptions);
    //            var operationStatus = connectCallResult.CallConnectionProperties.CallConnectionState.ToString();

    //            _logger.LogInformation($"Connected 1 to N call with connection id: {connectCallResult.CallConnectionProperties.CallConnectionId}, CorrelationId: {connectCallResult.CallConnectionProperties.CorrelationId}, Status: {operationStatus}");

    //            return Ok(new CallConnectionResponse
    //            {
    //                CallConnectionId = connectCallResult.CallConnectionProperties.CallConnectionId,
    //                CorrelationId = connectCallResult.CallConnectionProperties.CorrelationId,
    //                Status = operationStatus
    //            });
    //        }
    //        catch (ArgumentNullException ex)
    //        {
    //            _logger.LogError($"Server call ID validation error: {ex.Message}");
    //            return BadRequest($"Invalid server call ID: {ex.Message}");
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError($"Error connecting one-to-N call: {ex.Message}");
    //            return Problem($"Failed to connect one-to-N call: {ex.Message}");
    //        }
    //    }
    }
}