using System;
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

namespace Call_Automation_GCCH.Controllers
{
    [ApiController]
    [Route("api/recordings")]
    [Produces("application/json")]
    public class RecordingsController : ControllerBase
    {
        private readonly ICallAutomationService _service;
        private readonly ILogger<RecordingsController> _logger;
        private readonly AcsCommunicationSettings _config;
        private readonly IWebHostEnvironment _env;

        public RecordingsController(
            ICallAutomationService service,
            ILogger<RecordingsController> logger,
            IOptions<AcsCommunicationSettings> configOptions,
            IWebHostEnvironment env)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = configOptions.Value ?? throw new ArgumentNullException(nameof(configOptions));
            _env = env ?? throw new ArgumentNullException(nameof(env));
        }

        /// <summary>Starts a recording. Include "recordingOptions" to customize format/channel. Omit for defaults.</summary>
        [HttpPost("startRecordingAsync")]
        [Tags("Recording APIs")]
        public Task<IActionResult> StartRecordingAsync([FromBody] StartRecordingRequest request) => HandleStartRecording(request, async: true);
        [HttpPost("startRecording")]
        [Tags("Recording APIs")]
        public IActionResult StartRecording([FromBody] StartRecordingRequest request) => HandleStartRecording(request, async: false).Result;

        /// <summary>Pauses a recording.</summary>
        [HttpPost("pauseRecordingAsync")]
        [Tags("Recording APIs")]
        public Task<IActionResult> PauseRecordingAsync(string callConnectionId, string recordingId) => HandlePauseRecording(callConnectionId, recordingId, async: true);
        [HttpPost("pauseRecording")]
        [Tags("Recording APIs")]
        public IActionResult PauseRecording(string callConnectionId, string recordingId) => HandlePauseRecording(callConnectionId, recordingId, async: false).Result;

        /// <summary>Resumes a recording.</summary>
        [HttpPost("resumeRecordingAsync")]
        [Tags("Recording APIs")]
        public Task<IActionResult> ResumeRecordingAsync(string callConnectionId, string recordingId) => HandleResumeRecording(callConnectionId, recordingId, async: true);
        [HttpPost("resumeRecording")]
        [Tags("Recording APIs")]
        public IActionResult ResumeRecording(string callConnectionId, string recordingId) => HandleResumeRecording(callConnectionId, recordingId, async: false).Result;

        /// <summary>Stops a recording.</summary>
        [HttpPost("stopRecordingAsync")]
        [Tags("Recording APIs")]
        public Task<IActionResult> StopRecordingAsync(string callConnectionId, string recordingId) => HandleStopRecording(callConnectionId, recordingId, async: true);
        [HttpPost("stopRecording")]
        [Tags("Recording APIs")]
        public IActionResult StopRecording(string callConnectionId, string recordingId) => HandleStopRecording(callConnectionId, recordingId, async: false).Result;

        /// <summary>Downloads the recording to a local temp file.</summary>
        [HttpPost("downloadRecordingAsync")]
        [Tags("Recording APIs")]
        public Task<IActionResult> DownloadRecordingAsync(string callConnectionId) => HandleDownloadRecording(callConnectionId, async: true);
        [HttpPost("downloadRecording")]
        [Tags("Recording APIs")]
        public IActionResult DownloadRecording(string callConnectionId) => HandleDownloadRecording(callConnectionId, async: false).Result;

        /// <summary>
        /// Downloads the recording using DownloadTo to the App Service wwwroot/downloads folder and returns a browsable URL.
        /// Uses the recording location captured from the AcsRecordingFileStatusUpdated event.
        /// </summary>
        [HttpPost("downloadToFileAsync")]
        [Tags("Recording APIs")]
        public Task<IActionResult> DownloadToFileAsync(string? callConnectionId = null, [FromQuery] string? location = null) =>
            HandleDownloadToFile(callConnectionId, location, async: true);

        [HttpPost("downloadToFile")]
        [Tags("Recording APIs")]
        public IActionResult DownloadToFile(string? callConnectionId = null, [FromQuery] string? location = null) =>
            HandleDownloadToFile(callConnectionId, location, async: false).Result;

        /// <summary>
        /// Lists all recordings currently saved to the App Service wwwroot/downloads folder with browsable URLs.
        /// </summary>
        [HttpGet("listDownloadedFiles")]
        [Tags("Recording APIs")]
        public IActionResult ListDownloadedFiles()
        {
            try
            {
                var downloadsPath = Path.Combine(_env.WebRootPath ?? _env.ContentRootPath, "downloads");
                if (!Directory.Exists(downloadsPath))
                {
                    return Ok(new { totalFiles = 0, files = Array.Empty<object>() });
                }

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var files = Directory.GetFiles(downloadsPath)
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(fi => fi.CreationTimeUtc)
                    .Select(fi => new
                    {
                        fileName = fi.Name,
                        sizeInBytes = fi.Length,
                        sizeInMB = Math.Round(fi.Length / 1024.0 / 1024.0, 2),
                        createdAt = fi.CreationTimeUtc,
                        downloadUrl = $"{baseUrl}/downloads/{fi.Name}"
                    })
                    .ToArray();

                return Ok(new { totalFiles = files.Length, downloadsFolderPath = downloadsPath, files });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing downloaded files");
                return Problem($"Failed to list downloaded files: {ex.Message}");
            }
        }

        /// <summary>Gets the state of a recording.</summary>
        [HttpGet("getRecordingStateAsync")]
        [Tags("Recording APIs")]
        public Task<IActionResult> GetRecordingStateAsync(string recordingId) => HandleGetRecordingState(recordingId, async: true);
        [HttpGet("getRecordingState")]
        [Tags("Recording APIs")]
        public IActionResult GetRecordingState(string recordingId) => HandleGetRecordingState(recordingId, async: false).Result;

        /// <summary>Deletes a recording.</summary>
        [HttpDelete("deleteRecordingAsync")]
        [Tags("Recording APIs")]
        public Task<IActionResult> DeleteRecordingAsync(string recordingLocation) => HandleDeleteRecording(recordingLocation, async: true);
        [HttpDelete("deleteRecording")]
        [Tags("Recording APIs")]
        public IActionResult DeleteRecording(string recordingLocation) => HandleDeleteRecording(recordingLocation, async: false).Result;

        // ??? HANDLERS ???????????????????????????????????????????????????????????

        private async Task<IActionResult> HandleStartRecording(StartRecordingRequest request, bool async)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.CallConnectionId))
                    return BadRequest("CallConnectionId is required.");

                var opts = request.RecordingOptions ?? new RecordingOptionsRequest();
                var recordingContent = opts.IsAudioVideo ? RecordingContent.AudioVideo : RecordingContent.Audio;
                var recordingChannel = opts.IsMixed ? RecordingChannel.Mixed : RecordingChannel.Unmixed;

                RecordingFormat format = (opts.RecordingFormat?.Trim() ?? "Mp3").ToLowerInvariant() switch
                {
                    "mp3" => RecordingFormat.Mp3,
                    "mp4" => RecordingFormat.Mp4,
                    "wav" => RecordingFormat.Wav,
                    _ => throw new ArgumentException($"Invalid recording format '{opts.RecordingFormat}'. Valid: Mp3, Mp4, Wav")
                };

                if (recordingContent == RecordingContent.AudioVideo && format != RecordingFormat.Mp4)
                    return BadRequest("AudioVideo content is only supported with Mp4 format.");

                var callProps = _service.GetCallConnectionProperties(request.CallConnectionId);

                var locatorType = (request.CallLocatorType?.Trim() ?? "CallConnectionId").ToLowerInvariant();
                StartRecordingOptions recordingOptions = locatorType switch
                {
                    "callconnectionid" => new StartRecordingOptions(callProps.CallConnectionId),
                    "servercalllocator" => new StartRecordingOptions(
                        new ServerCallLocator(!string.IsNullOrEmpty(request.ServerCallId)
                            ? request.ServerCallId
                            : callProps.ServerCallId)),
                    "groupcalllocator" => string.IsNullOrEmpty(request.GroupCallId)
                        ? throw new ArgumentException("groupCallId is required when callLocatorType is 'GroupCallLocator'.")
                        : new StartRecordingOptions(new GroupCallLocator(request.GroupCallId)),
                    _ => throw new ArgumentException(
                        $"Invalid callLocatorType '{request.CallLocatorType}'. " +
                        "Valid values: 'CallConnectionId', 'ServerCallLocator', 'GroupCallLocator'.")
                };

                recordingOptions.RecordingContent = recordingContent;
                recordingOptions.RecordingFormat = format;
                recordingOptions.RecordingChannel = recordingChannel;
                recordingOptions.RecordingStateCallbackUri = new Uri(new Uri(_config.CallbackUriHost), "/api/callbacks");
                recordingOptions.PauseOnStart = opts.PauseOnStart;

                if (!string.IsNullOrEmpty(opts.ExternalStorageContainerUri))
                    recordingOptions.RecordingStorage = RecordingStorage.CreateAzureBlobContainerRecordingStorage(new Uri(opts.ExternalStorageContainerUri));

                if (opts.ChannelAffinity != null && opts.ChannelAffinity.Count > 0)
                {
                    foreach (var ca in opts.ChannelAffinity)
                    {
                        CommunicationIdentifier participant = ca.Participant.StartsWith("+")
                            ? new PhoneNumberIdentifier(ca.Participant)
                            : new CommunicationUserIdentifier(ca.Participant);
                        recordingOptions.ChannelAffinity.Add(new ChannelAffinity(participant) { Channel = ca.Channel });
                    }
                }

                _service.RecordingFileFormat = format.ToString();

                var result = async
                    ? await _service.GetCallAutomationClient().GetCallRecording().StartAsync(recordingOptions)
                    : _service.GetCallAutomationClient().GetCallRecording().Start(recordingOptions);

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = request.CallConnectionId, CorrelationId = callProps.CorrelationId,
                    Status = $"Recording started. RecordingId: {result.Value.RecordingId}. Content: {recordingContent}, Format: {format}, Channel: {recordingChannel}"
                });
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (Exception ex) { _logger.LogError(ex, "Error starting recording"); return Problem($"Failed to start recording: {ex.Message}"); }
        }

        private async Task<IActionResult> HandlePauseRecording(string callConnectionId, string recordingId, bool async)
        {
            try
            {
                var correlationId = _service.GetCallConnectionProperties(callConnectionId).CorrelationId;
                var result = async
                    ? await _service.GetCallAutomationClient().GetCallRecording().PauseAsync(recordingId)
                    : _service.GetCallAutomationClient().GetCallRecording().Pause(recordingId);
                return Ok(new CallConnectionResponse { CallConnectionId = callConnectionId, CorrelationId = correlationId, Status = $"Recording paused. RecordingId: {recordingId}. Status: {result.Status}" });
            }
            catch (Exception ex) { _logger.LogError(ex, "Error pausing recording"); return Problem($"Failed to pause recording: {ex.Message}"); }
        }

        private async Task<IActionResult> HandleResumeRecording(string callConnectionId, string recordingId, bool async)
        {
            try
            {
                var correlationId = _service.GetCallConnectionProperties(callConnectionId).CorrelationId;
                var result = async
                    ? await _service.GetCallAutomationClient().GetCallRecording().ResumeAsync(recordingId)
                    : _service.GetCallAutomationClient().GetCallRecording().Resume(recordingId);
                return Ok(new CallConnectionResponse { CallConnectionId = callConnectionId, CorrelationId = correlationId, Status = $"Recording resumed. RecordingId: {recordingId}. Status: {result.Status}" });
            }
            catch (Exception ex) { _logger.LogError(ex, "Error resuming recording"); return Problem($"Failed to resume recording: {ex.Message}"); }
        }

        private async Task<IActionResult> HandleStopRecording(string callConnectionId, string recordingId, bool async)
        {
            try
            {
                var correlationId = _service.GetCallConnectionProperties(callConnectionId).CorrelationId;
                var result = async
                    ? await _service.GetCallAutomationClient().GetCallRecording().StopAsync(recordingId)
                    : _service.GetCallAutomationClient().GetCallRecording().Stop(recordingId);
                return Ok(new CallConnectionResponse { CallConnectionId = callConnectionId, CorrelationId = correlationId, Status = $"Recording stopped. RecordingId: {recordingId}. Status: {result.Status}" });
            }
            catch (Exception ex) { _logger.LogError(ex, "Error stopping recording"); return Problem($"Failed to stop recording: {ex.Message}"); }
        }

        private async Task<IActionResult> HandleDownloadRecording(string callConnectionId, bool async)
        {
            try
            {
                var location = _service.RecordingLocation;
                var format = _service.RecordingFileFormat;
                if (string.IsNullOrEmpty(location)) return Problem("Recording location is not available from the events.");
                if (string.IsNullOrEmpty(format)) return Problem("Recording file format is not available from the events.");

                var correlationId = _service.GetCallConnectionProperties(callConnectionId).CorrelationId;
                var fileName = $"Recording_{callConnectionId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{format}";
                var tempDir = Path.Combine(Path.GetTempPath(), "call-recordings");
                Directory.CreateDirectory(tempDir);
                var localFilePath = Path.Combine(tempDir, fileName);

                using var fileStream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write);
                var result = async
                    ? await _service.GetRecordingDownloadClient().GetCallRecording().DownloadToAsync(new Uri(location), fileStream)
                    : _service.GetRecordingDownloadClient().GetCallRecording().DownloadTo(new Uri(location), fileStream);

                return Ok(new CallConnectionResponse { CallConnectionId = callConnectionId, CorrelationId = correlationId, Status = $"Recording downloaded. Path: {localFilePath}, Status: {result.Status}" });
            }
            catch (Exception ex) { _logger.LogError(ex, "Error downloading recording"); return Problem($"Failed to download recording: {ex.Message}"); }
        }

        private async Task<IActionResult> HandleDownloadToFile(string? callConnectionId, string? locationOverride, bool async)
        {
            try
            {
                _logger.LogInformation("=== DOWNLOAD-TO-FILE (App Service) INITIATED ===");
                _logger.LogInformation("[DownloadToFile] Async: {Async}, CallConnectionId: {CallConnectionId}, LocationOverride: {LocationOverride}",
                    async, callConnectionId ?? "(none)", string.IsNullOrEmpty(locationOverride) ? "(none)" : "(provided)");

                // Prefer explicit location, otherwise use captured location from event
                var location = !string.IsNullOrEmpty(locationOverride) ? locationOverride : _service.RecordingLocation;
                var format = _service.RecordingFileFormat;

                if (string.IsNullOrEmpty(location))
                {
                    _logger.LogWarning("[DownloadToFile] No location provided and no location captured from events");
                    return Problem("Recording location is not available. Either pass ?location=<url> or wait for AcsRecordingFileStatusUpdated event.");
                }
                if (string.IsNullOrEmpty(format)) format = "mp4";

                _logger.LogInformation("[DownloadToFile] Using location: {Location}", location);
                _logger.LogInformation("[DownloadToFile] Format: {Format}", format);

                // Save to wwwroot/downloads so it's served as static content
                var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
                var downloadsDir = Path.Combine(webRoot, "downloads");
                Directory.CreateDirectory(downloadsDir);

                var suffix = string.IsNullOrEmpty(callConnectionId) ? Guid.NewGuid().ToString("N").Substring(0, 8) : callConnectionId;
                var fileName = $"Recording_{suffix}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{format}";
                var localFilePath = Path.Combine(downloadsDir, fileName);
                _logger.LogInformation("[DownloadToFile] Saving to: {Path}", localFilePath);

                var startTime = DateTime.UtcNow;
                using (var fileStream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write))
                {
                    var client = _service.GetRecordingDownloadClient();
                    var result = async
                        ? await client.GetCallRecording().DownloadToAsync(new Uri(location), fileStream)
                        : client.GetCallRecording().DownloadTo(new Uri(location), fileStream);
                    _logger.LogInformation("[DownloadToFile] SDK Status: {Status}", result.Status);
                }
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

                var fi = new FileInfo(localFilePath);
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var downloadUrl = $"{baseUrl}/downloads/{fileName}";

                _logger.LogInformation("[DownloadToFile] File saved: {Size} bytes in {Elapsed}ms", fi.Length, elapsed);
                _logger.LogInformation("[DownloadToFile] Accessible at: {Url}", downloadUrl);
                _logger.LogInformation("=== DOWNLOAD-TO-FILE COMPLETED ===");

                return Ok(new
                {
                    status = "Success",
                    fileName,
                    localFilePath,
                    sizeInBytes = fi.Length,
                    sizeInMB = Math.Round(fi.Length / 1024.0 / 1024.0, 2),
                    downloadUrl,
                    elapsedMs = elapsed,
                    message = "File saved to App Service. Click downloadUrl to access."
                });
            }
            catch (Azure.RequestFailedException rfx) when (rfx.Status == 401)
            {
                _logger.LogError(rfx, "[DownloadToFile] 401 UNAUTHORIZED - ACS credentials don't match recording resource");
                return Problem($"401 Unauthorized: The ACS connection string does not match the resource that owns this recording, or the access key has been rotated. Details: {rfx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DownloadToFile] Error: {Message}", ex.Message);
                return Problem($"Failed to download recording to file: {ex.Message}");
            }
        }

        private async Task<IActionResult> HandleGetRecordingState(string recordingId, bool async)
        {
            try
            {
                if (string.IsNullOrEmpty(recordingId)) return BadRequest("RecordingId is required");
                var result = async
                    ? await _service.GetCallAutomationClient().GetCallRecording().GetStateAsync(recordingId)
                    : _service.GetCallAutomationClient().GetCallRecording().GetState(recordingId);
                return Ok(new { RecordingId = recordingId, RecordingState = result.Value.RecordingState?.ToString(), Status = result.GetRawResponse().Status.ToString() });
            }
            catch (Exception ex) { _logger.LogError(ex, "Error getting recording state"); return Problem($"Failed to get recording state: {ex.Message}"); }
        }

        private async Task<IActionResult> HandleDeleteRecording(string recordingLocation, bool async)
        {
            try
            {
                if (string.IsNullOrEmpty(recordingLocation)) return BadRequest("Recording location URL is required");
                var result = async
                    ? await _service.GetRecordingDownloadClient().GetCallRecording().DeleteAsync(new Uri(recordingLocation))
                    : _service.GetRecordingDownloadClient().GetCallRecording().Delete(new Uri(recordingLocation));
                return Ok(new { RecordingLocation = recordingLocation, Status = result.Status.ToString() });
            }
            catch (Exception ex) { _logger.LogError(ex, "Error deleting recording"); return Problem($"Failed to delete recording: {ex.Message}"); }
        }
    }
}
