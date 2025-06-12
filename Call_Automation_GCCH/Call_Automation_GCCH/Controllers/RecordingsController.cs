using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Communication;
using Azure.Communication.CallAutomation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Call_Automation_GCCH.Models;
using Call_Automation_GCCH.Services;
using System.ComponentModel.DataAnnotations;

namespace Call_Automation_GCCH.Controllers
{
    [ApiController]
    [Route("api/recordings")]
    [Produces("application/json")]
    public class RecordingsController : ControllerBase
    {
        private readonly CallAutomationService _service;
        private readonly ILogger<RecordingsController> _logger;
        private readonly ConfigurationRequest _config;
        private readonly IStorageService _storageService;

        public RecordingsController(
            CallAutomationService service,
            ILogger<RecordingsController> logger,
            IOptions<ConfigurationRequest> configOptions,
            IStorageService storageService)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = configOptions.Value ?? throw new ArgumentNullException(nameof(configOptions));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        }

        /// <summary>
        /// Starts a recording with configurable options asynchronously
        /// </summary>
        /// <param name="callConnectionId">The call connection ID for the call to record</param>
        /// <param name="isAudioVideo">True for AudioVideo content, false for Audio only</param>
        /// <param name="recordingFormat">Recording format (valid options: Mp3, Mp4, Wav)</param>
        /// <param name="isMixed">True for mixed channel (all participants combined), false for unmixed (separate streams)</param>
        /// <param name="isRecordingWithCallConnectionId">Whether to use call connection ID for recording</param>
        /// <param name="isPauseOnStart">Whether to pause recording on start</param>
        [HttpPost("startRecordingAsync")]
        [Tags("Recording APIs")]
        public async Task<IActionResult> StartRecordingAsync(
            string callConnectionId,
            bool isAudioVideo = false,
            string recordingFormat = "Mp3",
            bool isMixed = true,
            bool isRecordingWithCallConnectionId = true,
            bool isPauseOnStart = false)
        {
            try
            {
                // Validate required parameter
                if (string.IsNullOrWhiteSpace(callConnectionId))
                {
                    return BadRequest("CallConnectionId is required.");
                }

                // Trim whitespace from text inputs (except callConnectionId)
                recordingFormat = recordingFormat?.Trim() ?? "Mp3";

                // Convert bool parameters to enums
                var recordingContent = isAudioVideo ? RecordingContent.AudioVideo : RecordingContent.Audio;
                var recordingChannel = isMixed ? RecordingChannel.Mixed : RecordingChannel.Unmixed;

                // Parse and validate recording format
                if (!Enum.TryParse<RecordingFormat>(recordingFormat, true, out var format))
                {
                    return BadRequest($"Invalid recording format '{recordingFormat}'. Valid options: Mp3, Mp4, Wav");
                }

                // Validate format compatibility
                if (recordingContent == RecordingContent.AudioVideo && format != RecordingFormat.Mp4)
                {
                    return BadRequest("AudioVideo content is only supported with Mp4 format.");
                }

                CallConnectionProperties callConnectionProperties = _service.GetCallConnectionProperties(callConnectionId);
                var serverCallId = callConnectionProperties.ServerCallId;
                var correlationId = callConnectionProperties.CorrelationId;
                CallLocator callLocator = new ServerCallLocator(serverCallId);

                var recordingOptions = isRecordingWithCallConnectionId
                    ? new StartRecordingOptions(callConnectionProperties.CallConnectionId)
                    : new StartRecordingOptions(callLocator);

                recordingOptions.RecordingContent = recordingContent;
                recordingOptions.RecordingFormat = format;
                recordingOptions.RecordingChannel = recordingChannel;
                recordingOptions.RecordingStateCallbackUri = new Uri(new Uri(_config.CallbackUriHost), "/api/callbacks");
                recordingOptions.PauseOnStart = isPauseOnStart;
                CallAutomationService.SetRecordingFileFormat(format.ToString());

                var recordingResult = await _service.GetCallAutomationClient().GetCallRecording().StartAsync(recordingOptions);
                var recordingId = recordingResult.Value.RecordingId;

                string successMessage = $"Recording started. RecordingId: {recordingId}. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, Content: {recordingContent}, Format: {format}, Channel: {recordingChannel}, Status: {recordingResult.GetRawResponse().Status.ToString()}";
                _logger.LogInformation(successMessage);

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = $"Recording started. RecordingId: {recordingId}. Content: {recordingContent}, Format: {format}, Channel: {recordingChannel}, Status: {recordingResult.GetRawResponse().Status.ToString()}"
                });
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error starting recording: {ex.Message}. CallConnectionId: {callConnectionId}";
                _logger.LogError(errorMessage);
                return Problem($"Failed to start recording: {ex.Message}. CallConnectionId: {callConnectionId}");
            }
        }

        /// <summary>
        /// Starts a recording with configurable options synchronously
        /// </summary>
        /// <param name="callConnectionId">The call connection ID for the call to record</param>
        /// <param name="isAudioVideo">True for AudioVideo content, false for Audio only</param>
        /// <param name="recordingFormat">Recording format (valid options: Mp3, Mp4, Wav)</param>
        /// <param name="isMixed">True for mixed channel (all participants combined), false for unmixed (separate streams)</param>
        /// <param name="isRecordingWithCallConnectionId">Whether to use call connection ID for recording</param>
        /// <param name="isPauseOnStart">Whether to pause recording on start</param>
        [HttpPost("startRecording")]
        [Tags("Recording APIs")]
        public IActionResult StartRecording(
            string callConnectionId,
            bool isAudioVideo = false,
            string recordingFormat = "Mp3",
            bool isMixed = true,
            bool isRecordingWithCallConnectionId = true,
            bool isPauseOnStart = false)
        {
            try
            {
                // Validate required parameter
                if (string.IsNullOrWhiteSpace(callConnectionId))
                {
                    return BadRequest("CallConnectionId is required.");
                }

                // Trim whitespace from text inputs (except callConnectionId)
                recordingFormat = recordingFormat?.Trim() ?? "Mp3";

                // Convert bool parameters to enums
                var recordingContent = isAudioVideo ? RecordingContent.AudioVideo : RecordingContent.Audio;
                var recordingChannel = isMixed ? RecordingChannel.Mixed : RecordingChannel.Unmixed;

                // Parse and validate recording format
                if (!Enum.TryParse<RecordingFormat>(recordingFormat, true, out var format))
                {
                    return BadRequest($"Invalid recording format '{recordingFormat}'. Valid options: Mp3, Mp4, Wav");
                }

                // Validate format compatibility
                if (recordingContent == RecordingContent.AudioVideo && format != RecordingFormat.Mp4)
                {
                    return BadRequest("AudioVideo content is only supported with Mp4 format.");
                }

                CallConnectionProperties callConnectionProperties = _service.GetCallConnectionProperties(callConnectionId);
                var serverCallId = callConnectionProperties.ServerCallId;
                var correlationId = callConnectionProperties.CorrelationId;
                CallLocator callLocator = new ServerCallLocator(serverCallId);

                var recordingOptions = isRecordingWithCallConnectionId
                    ? new StartRecordingOptions(callConnectionProperties.CallConnectionId)
                    : new StartRecordingOptions(callLocator);

                recordingOptions.RecordingContent = recordingContent;
                recordingOptions.RecordingFormat = format;
                recordingOptions.RecordingChannel = recordingChannel;
                recordingOptions.RecordingStateCallbackUri = new Uri(new Uri(_config.CallbackUriHost), "/api/callbacks");
                recordingOptions.PauseOnStart = isPauseOnStart;
                CallAutomationService.SetRecordingFileFormat(format.ToString());

                var recordingResult = _service.GetCallAutomationClient().GetCallRecording().Start(recordingOptions);
                var recordingId = recordingResult.Value.RecordingId;

                string successMessage = $"Recording started. RecordingId: {recordingId}. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, Content: {recordingContent}, Format: {format}, Channel: {recordingChannel}, Status: {recordingResult.GetRawResponse().Status.ToString()}";
                _logger.LogInformation(successMessage);

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = $"Recording started. RecordingId: {recordingId}. Content: {recordingContent}, Format: {format}, Channel: {recordingChannel}, Status: {recordingResult.GetRawResponse().Status.ToString()}"
                });
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error starting recording: {ex.Message}. CallConnectionId: {callConnectionId}";
                _logger.LogError(errorMessage);
                return Problem($"Failed to start recording: {ex.Message}. CallConnectionId: {callConnectionId}");
            }
        }

        /// <summary>
        /// Pauses a recording asynchronously
        /// </summary>
        [HttpPost("pauseRecordingAsync")]
        [Tags("Recording APIs")]
        public async Task<IActionResult> PauseRecordingAsync(
            string callConnectionId,
            string recordingId)
        {
            try
            {
                var callConnection = _service.GetCallConnection(callConnectionId);
                var correlationId = _service.GetCallConnectionProperties(callConnectionId).CorrelationId;

                var pauseResult = await _service.GetCallAutomationClient().GetCallRecording().PauseAsync(recordingId);

                string successMessage = $"Recording paused successfully. RecordingId: {recordingId}. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, Status: {pauseResult.Status}";
                _logger.LogInformation(successMessage);

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = $"Recording paused. RecordingId: {recordingId}. Status: {pauseResult.Status}"
                });
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error pausing recording: {ex.Message}. CallConnectionId: {callConnectionId}";
                _logger.LogError(errorMessage);
                return Problem($"Failed to pause recording: {ex.Message}. CallConnectionId: {callConnectionId}");
            }
        }

        /// <summary>
        /// Pauses a recording synchronously
        /// </summary>
        [HttpPost("pauseRecording")]
        [Tags("Recording APIs")]
        public IActionResult PauseRecording(
            string callConnectionId,
            string recordingId)
        {
            try
            {
                var callConnection = _service.GetCallConnection(callConnectionId);
                var correlationId = _service.GetCallConnectionProperties(callConnectionId).CorrelationId;

                var pauseResult = _service.GetCallAutomationClient().GetCallRecording().Pause(recordingId);

                string successMessage = $"Recording paused successfully. RecordingId: {recordingId}. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, Status: {pauseResult.Status}";
                _logger.LogInformation(successMessage);

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = $"Recording paused. RecordingId: {recordingId}. Status: {pauseResult.Status}"
                });
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error pausing recording: {ex.Message}. CallConnectionId: {callConnectionId}";
                _logger.LogError(errorMessage);
                return Problem($"Failed to pause recording: {ex.Message}. CallConnectionId: {callConnectionId}");
            }
        }

        /// <summary>
        /// Resumes a recording asynchronously
        /// </summary>
        [HttpPost("resumeRecordingAsync")]
        [Tags("Recording APIs")]
        public async Task<IActionResult> ResumeRecordingAsync(
            string callConnectionId,
            string recordingId)
        {
            try
            {
                var callConnection = _service.GetCallConnection(callConnectionId);
                var correlationId = _service.GetCallConnectionProperties(callConnectionId).CorrelationId;

                var resumeRecordingResult = await _service.GetCallAutomationClient().GetCallRecording().ResumeAsync(recordingId);

                string successMessage = $"Recording resumed successfully. RecordingId: {recordingId}. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, Status: {resumeRecordingResult.Status}";
                _logger.LogInformation(successMessage);

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = $"Recording resumed. RecordingId: {recordingId}. Status: {resumeRecordingResult.Status}"
                });
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error resuming recording: {ex.Message}. CallConnectionId: {callConnectionId}";
                _logger.LogError(errorMessage);
                return Problem($"Failed to resume recording: {ex.Message}. CallConnectionId: {callConnectionId}");
            }
        }

        /// <summary>
        /// Resumes a recording synchronously
        /// </summary>
        [HttpPost("resumeRecording")]
        [Tags("Recording APIs")]
        public IActionResult ResumeRecording(
            string callConnectionId,
            string recordingId)
        {
            try
            {
                var callConnection = _service.GetCallConnection(callConnectionId);
                var correlationId = _service.GetCallConnectionProperties(callConnectionId).CorrelationId;

                var resumeRecordingResult = _service.GetCallAutomationClient().GetCallRecording().Resume(recordingId);

                string successMessage = $"Recording resumed successfully. RecordingId: {recordingId}. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, Status: {resumeRecordingResult.Status}";
                _logger.LogInformation(successMessage);

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = $"Recording resumed. RecordingId: {recordingId}. Status: {resumeRecordingResult.Status}"
                });
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error resuming recording: {ex.Message}. CallConnectionId: {callConnectionId}";
                _logger.LogError(errorMessage);
                return Problem($"Failed to resume recording: {ex.Message}. CallConnectionId: {callConnectionId}");
            }
        }

        /// <summary>
        /// Stops a recording asynchronously
        /// </summary>
        [HttpPost("stopRecordingAsync")]
        [Tags("Recording APIs")]
        public async Task<IActionResult> StopRecordingAsync(
            string callConnectionId,
            string recordingId)
        {
            try
            {
                var callConnection = _service.GetCallConnection(callConnectionId);
                var correlationId = _service.GetCallConnectionProperties(callConnectionId).CorrelationId;

                var stopRecordingResult = await _service.GetCallAutomationClient().GetCallRecording().StopAsync(recordingId);

                string successMessage = $"Recording stopped successfully. RecordingId: {recordingId}. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, Status: {stopRecordingResult.Status}";
                _logger.LogInformation(successMessage);

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = $"Recording stopped. RecordingId: {recordingId}. Status: {stopRecordingResult.Status}"
                });
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error stopping recording: {ex.Message}. CallConnectionId: {callConnectionId}";
                _logger.LogError(errorMessage);
                return Problem($"Failed to stop recording: {ex.Message}. CallConnectionId: {callConnectionId}");
            }
        }

        /// <summary>
        /// Stops a recording synchronously
        /// </summary>
        [HttpPost("stopRecording")]
        [Tags("Recording APIs")]
        public IActionResult StopRecording(
            string callConnectionId,
            string recordingId)
        {
            try
            {
                var callConnection = _service.GetCallConnection(callConnectionId);
                var correlationId = _service.GetCallConnectionProperties(callConnectionId).CorrelationId;

                var stopRecordingResult = _service.GetCallAutomationClient().GetCallRecording().Stop(recordingId);

                string successMessage = $"Recording stopped successfully. RecordingId: {recordingId}. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, Status: {stopRecordingResult.Status}";
                _logger.LogInformation(successMessage);

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = $"Recording stopped. RecordingId: {recordingId}. Status: {stopRecordingResult.Status}"
                });
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error stopping recording: {ex.Message}. CallConnectionId: {callConnectionId}";
                _logger.LogError(errorMessage);
                return Problem($"Failed to stop recording: {ex.Message}. CallConnectionId: {callConnectionId}");
            }
        }

        /// <summary>
        /// Downloads the recording to VM and uploads to storage account asynchronously
        /// </summary>
        [HttpPost("downloadRecordingAsync")]
        [Tags("Recording APIs")]
        public async Task<IActionResult> DownloadRecordingAsync(string callConnectionId)
        {
            try
            {
                var location = CallAutomationService.GetRecordingLocation();
                var format = CallAutomationService.GetRecordingFileFormat();

                if (string.IsNullOrEmpty(location))
                {
                    _logger.LogError($"Recording location is not available from the events. CallConnectionId: {callConnectionId}");
                    return Problem("Recording Location from Azure Events is Null");
                }

                if (string.IsNullOrEmpty(format))
                {
                    _logger.LogError($"Recording File Format is not available from the events. CallConnectionId: {callConnectionId}");
                    return Problem("Recording File Format from Azure Events is Null");
                }

                if (string.IsNullOrEmpty(_config.StorageConnectionString))
                {
                    _logger.LogError($"Storage connection string is not configured. CallConnectionId: {callConnectionId}");
                    return Problem("Storage account is not configured");
                }

                var correlationId = _service.GetCallConnectionProperties(callConnectionId).CorrelationId;
                var date = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                var fileName = $"Recording_{callConnectionId}_{date}.{format}";
                var mimeType = format.ToLower() switch
                {
                    "mp4" => "video/mp4",
                    "wav" => "audio/wav",
                    "mp3" => "audio/mpeg",
                    _ => "application/octet-stream"
                };

                // Create local temporary directory for recordings
                var tempDir = Path.Combine(Path.GetTempPath(), "call-recordings");
                Directory.CreateDirectory(tempDir);
                var localFilePath = Path.Combine(tempDir, fileName);

                // Download recording to local VM storage first
                using var fileStream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write);
                var recordingClient = _service.GetCallAutomationClient().GetCallRecording();
                var downloadResult = await recordingClient.DownloadToAsync(new Uri(location), fileStream);

                _logger.LogInformation($"Recording downloaded to VM. Path: {localFilePath}, CallConnectionId: {callConnectionId}, Status: {downloadResult.Status}");

                // Upload to Azure Storage Account
                using var uploadStream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read);
                var storageUrl = await _storageService.UploadRecordingAsync(fileName, uploadStream, mimeType);

                // Clean up local file
                if (System.IO.File.Exists(localFilePath))
                {
                    System.IO.File.Delete(localFilePath);
                }

                string successMessage = $"Recording downloaded and uploaded to storage. StorageUrl: {storageUrl}. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}";
                _logger.LogInformation(successMessage);

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = $"Recording downloaded and uploaded to storage. StorageUrl: {storageUrl}"
                });
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error downloading and uploading recording: {ex.Message}. CallConnectionId: {callConnectionId}";
                _logger.LogError(errorMessage);
                return Problem($"Failed to download and upload recording: {ex.Message}. CallConnectionId: {callConnectionId}");
            }
        }

        /// <summary>
        /// Downloads the recording to VM and uploads to storage account synchronously
        /// </summary>
        [HttpPost("downloadRecording")]
        [Tags("Recording APIs")]
        public IActionResult DownloadRecording(string callConnectionId)
        {
            try
            {
                var location = CallAutomationService.GetRecordingLocation();
                var format = CallAutomationService.GetRecordingFileFormat();

                if (string.IsNullOrEmpty(location))
                {
                    _logger.LogError($"Recording location is not available from the events. CallConnectionId: {callConnectionId}");
                    return Problem("Recording Location from Azure Events is Null");
                }

                if (string.IsNullOrEmpty(format))
                {
                    _logger.LogError($"Recording File Format is not available from the events. CallConnectionId: {callConnectionId}");
                    return Problem("Recording File Format from Azure Events is Null");
                }

                if (string.IsNullOrEmpty(_config.StorageConnectionString))
                {
                    _logger.LogError($"Storage connection string is not configured. CallConnectionId: {callConnectionId}");
                    return Problem("Storage account is not configured");
                }

                var correlationId = _service.GetCallConnectionProperties(callConnectionId).CorrelationId;
                var date = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                var fileName = $"Recording_{callConnectionId}_{date}.{format}";
                var mimeType = format.ToLower() switch
                {
                    "mp4" => "video/mp4",
                    "wav" => "audio/wav",
                    "mp3" => "audio/mpeg",
                    _ => "application/octet-stream"
                };

                // Create local temporary directory for recordings
                var tempDir = Path.Combine(Path.GetTempPath(), "call-recordings");
                Directory.CreateDirectory(tempDir);
                var localFilePath = Path.Combine(tempDir, fileName);

                // Download recording to local VM storage first
                using var fileStream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write);
                var recordingClient = _service.GetCallAutomationClient().GetCallRecording();
                var downloadResult = recordingClient.DownloadTo(new Uri(location), fileStream);

                _logger.LogInformation($"Recording downloaded to VM. Path: {localFilePath}, CallConnectionId: {callConnectionId}, Status: {downloadResult.Status}");

                // Upload to Azure Storage Account
                using var uploadStream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read);
                var storageUrl = _storageService.UploadRecording(fileName, uploadStream, mimeType);

                // Clean up local file
                if (System.IO.File.Exists(localFilePath))
                {
                    System.IO.File.Delete(localFilePath);
                }

                string successMessage = $"Recording downloaded and uploaded to storage. StorageUrl: {storageUrl}. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}";
                _logger.LogInformation(successMessage);

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = $"Recording downloaded and uploaded to storage. StorageUrl: {storageUrl}"
                });
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error downloading and uploading recording: {ex.Message}. CallConnectionId: {callConnectionId}";
                _logger.LogError(errorMessage);
                return Problem($"Failed to download and upload recording: {ex.Message}. CallConnectionId: {callConnectionId}");
            }
        }
    }
}
