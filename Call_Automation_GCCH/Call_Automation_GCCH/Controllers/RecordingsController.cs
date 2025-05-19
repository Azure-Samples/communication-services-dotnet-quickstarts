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
        private readonly CallAutomationService _service;
        private readonly ILogger<RecordingsController> _logger;
        private readonly ConfigurationRequest _config;

        public RecordingsController(
            CallAutomationService service,
            ILogger<RecordingsController> logger,
            IOptions<ConfigurationRequest> configOptions)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = configOptions.Value ?? throw new ArgumentNullException(nameof(configOptions));
        }

        /// <summary>
        /// Starts a recording with video in MP4 format and mixed channel asynchronously
        /// </summary>
        [HttpPost("startRecordingWithVideoMp4MixedAsync")]
        [Tags("Recording APIs")]
        public async Task<IActionResult> StartRecordingWithVideoMp4MixedAsync(
            string callConnectionId,
            bool isPauseOnStart)
        {
            try
            {
                CallConnectionProperties callConnectionProperties = _service.GetCallConnectionProperties(callConnectionId);
                var serverCallId = callConnectionProperties.ServerCallId;
                var correlationId = callConnectionProperties.CorrelationId;
                CallLocator callLocator = new ServerCallLocator(serverCallId);

                //var recordingOptions = isRecordingWithCallConnectionId
                //    ? new StartRecordingOptions(callConnectionProperties.CallConnectionId)
                //    : new StartRecordingOptions(callLocator);

                var recordingOptions = new StartRecordingOptions(callLocator);

                recordingOptions.RecordingContent = RecordingContent.AudioVideo;
                recordingOptions.RecordingFormat = RecordingFormat.Mp4;
                recordingOptions.RecordingChannel = RecordingChannel.Mixed;
                recordingOptions.RecordingStateCallbackUri = new Uri(new Uri(_config.CallbackUriHost), "/api/callbacks");
                recordingOptions.PauseOnStart = isPauseOnStart;
                CallAutomationService.SetRecordingFileFormat(RecordingFormat.Mp4.ToString());

                var recordingResult = await _service.GetCallAutomationClient().GetCallRecording().StartAsync(recordingOptions);
                var recordingId = recordingResult.Value.RecordingId;

                string successMessage = $"Recording started. RecordingId: {recordingId}. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, Status: {recordingResult.GetRawResponse().Status.ToString()}";
                _logger.LogInformation(successMessage);

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = $"Recording started. RecordingId: {recordingId}. Status: {recordingResult.GetRawResponse().Status.ToString()}"
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
        /// Starts a recording with video in MP4 format and mixed channel synchronously
        /// </summary>
        [HttpPost("startRecordingWithVideoMp4Mixed")]
        [Tags("Recording APIs")]
        public IActionResult StartRecordingWithVideoMp4Mixed(
            string callConnectionId,
            
            bool isPauseOnStart)
        {
            try
            {
                CallConnectionProperties callConnectionProperties = _service.GetCallConnectionProperties(callConnectionId);
                var serverCallId = callConnectionProperties.ServerCallId;
                var correlationId = callConnectionProperties.CorrelationId;
                CallLocator callLocator = new ServerCallLocator(serverCallId);

                var recordingOptions = new StartRecordingOptions(callLocator);

                recordingOptions.RecordingContent = RecordingContent.AudioVideo;
                recordingOptions.RecordingFormat = RecordingFormat.Mp4;
                recordingOptions.RecordingChannel = RecordingChannel.Mixed;
                recordingOptions.RecordingStateCallbackUri = new Uri(new Uri(_config.CallbackUriHost), "/api/callbacks");
                recordingOptions.PauseOnStart = isPauseOnStart;

                var recordingResult = _service.GetCallAutomationClient().GetCallRecording().Start(recordingOptions);
                var recordingId = recordingResult.Value.RecordingId;

                string successMessage = $"Recording started. RecordingId: {recordingId}. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, Status: {recordingResult.GetRawResponse().Status.ToString()}";
                _logger.LogInformation(successMessage);

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = $"Recording started. RecordingId: {recordingId}. Status: {recordingResult.GetRawResponse().Status.ToString()}"
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
        /// Starts a recording with audio in MP3 format and mixed channel asynchronously
        /// </summary>
        [HttpPost("startRecordingWithAudioMp3MixedAsync")]
        [Tags("Recording APIs")]
        public async Task<IActionResult> StartRecordingWithAudioMp3MixedAsync(
            string callConnectionId,
            
            bool isPauseOnStart)
        {
            try
            {
                CallConnectionProperties callConnectionProperties = _service.GetCallConnectionProperties(callConnectionId);
                var serverCallId = callConnectionProperties.ServerCallId;
                var correlationId = callConnectionProperties.CorrelationId;
                CallLocator callLocator = new ServerCallLocator(serverCallId);

                var recordingOptions = new StartRecordingOptions(callLocator);

                recordingOptions.RecordingContent = RecordingContent.Audio;
                recordingOptions.RecordingFormat = RecordingFormat.Mp3;
                recordingOptions.RecordingChannel = RecordingChannel.Mixed;
                recordingOptions.RecordingStateCallbackUri = new Uri(new Uri(_config.CallbackUriHost), "/api/callbacks");
                recordingOptions.PauseOnStart = isPauseOnStart;
                CallAutomationService.SetRecordingFileFormat(RecordingFormat.Mp3.ToString());
                var recordingResult = await _service.GetCallAutomationClient().GetCallRecording().StartAsync(recordingOptions);
                var recordingId = recordingResult.Value.RecordingId;

                string successMessage = $"Recording started. RecordingId: {recordingId}. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, Status: {recordingResult.GetRawResponse().Status.ToString()}";
                _logger.LogInformation(successMessage);

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = $"Recording started. RecordingId: {recordingId}. Status: {recordingResult.GetRawResponse().Status.ToString()}"
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
        /// Starts a recording with audio in MP3 format and mixed channel synchronously
        /// </summary>
        [HttpPost("startRecordingWithAudioMp3Mixed")]
        [Tags("Recording APIs")]
        public IActionResult StartRecordingWithAudioMp3Mixed(
            string callConnectionId,
            bool isPauseOnStart)
        {
            try
            {
                CallConnectionProperties callConnectionProperties = _service.GetCallConnectionProperties(callConnectionId);
                var serverCallId = callConnectionProperties.ServerCallId;
                var correlationId = callConnectionProperties.CorrelationId;
                CallLocator callLocator = new ServerCallLocator(serverCallId);

                var recordingOptions = new StartRecordingOptions(callLocator);

                recordingOptions.RecordingContent = RecordingContent.Audio;
                recordingOptions.RecordingFormat = RecordingFormat.Mp3;
                recordingOptions.RecordingChannel = RecordingChannel.Mixed;
                recordingOptions.RecordingStateCallbackUri = new Uri(new Uri(_config.CallbackUriHost), "/api/callbacks");
                recordingOptions.PauseOnStart = isPauseOnStart;
                CallAutomationService.SetRecordingFileFormat(RecordingFormat.Mp3.ToString());

                var recordingResult = _service.GetCallAutomationClient().GetCallRecording().Start(recordingOptions);
                var recordingId = recordingResult.Value.RecordingId;

                string successMessage = $"Recording started. RecordingId: {recordingId}. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, Status: {recordingResult.GetRawResponse().Status.ToString()}";
                _logger.LogInformation(successMessage);

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = $"Recording started. RecordingId: {recordingId}. Status: {recordingResult.GetRawResponse().Status.ToString()}"
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
        /// Starts a recording with audio in Wav format and mixed channel asynchronously
        /// </summary>
        [HttpPost("startRecordingWithAudioWavMixedAsync")]
        [Tags("Recording APIs")]
        public async Task<IActionResult> StartRecordingWithAudioWavMixedAsync(
            string callConnectionId,

            bool isPauseOnStart)
        {
            try
            {
                CallConnectionProperties callConnectionProperties = _service.GetCallConnectionProperties(callConnectionId);
                var serverCallId = callConnectionProperties.ServerCallId;
                var correlationId = callConnectionProperties.CorrelationId;
                CallLocator callLocator = new ServerCallLocator(serverCallId);

                var recordingOptions = new StartRecordingOptions(callLocator);

                recordingOptions.RecordingContent = RecordingContent.Audio;
                recordingOptions.RecordingFormat = RecordingFormat.Wav;
                recordingOptions.RecordingChannel = RecordingChannel.Mixed;
                recordingOptions.RecordingStateCallbackUri = new Uri(new Uri(_config.CallbackUriHost), "/api/callbacks");
                recordingOptions.PauseOnStart = isPauseOnStart;
                CallAutomationService.SetRecordingFileFormat(RecordingFormat.Wav.ToString());
                var recordingResult = await _service.GetCallAutomationClient().GetCallRecording().StartAsync(recordingOptions);
                var recordingId = recordingResult.Value.RecordingId;

                string successMessage = $"Recording started. RecordingId: {recordingId}. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, Status: {recordingResult.GetRawResponse().Status.ToString()}";
                _logger.LogInformation(successMessage);

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = $"Recording started. RecordingId: {recordingId}. Status: {recordingResult.GetRawResponse().Status.ToString()}"
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
        /// Starts a recording with audio in Wav format and mixed channel synchronously
        /// </summary>
        [HttpPost("startRecordingWithAudioWavMixed")]
        [Tags("Recording APIs")]
        public IActionResult StartRecordingWithAudioWavMixed(
            string callConnectionId,
            bool isPauseOnStart)
        {
            try
            {
                CallConnectionProperties callConnectionProperties = _service.GetCallConnectionProperties(callConnectionId);
                var serverCallId = callConnectionProperties.ServerCallId;
                var correlationId = callConnectionProperties.CorrelationId;
                CallLocator callLocator = new ServerCallLocator(serverCallId);

                var recordingOptions = new StartRecordingOptions(callLocator);

                recordingOptions.RecordingContent = RecordingContent.Audio;
                recordingOptions.RecordingFormat = RecordingFormat.Wav;
                recordingOptions.RecordingChannel = RecordingChannel.Mixed;
                recordingOptions.RecordingStateCallbackUri = new Uri(new Uri(_config.CallbackUriHost), "/api/callbacks");
                recordingOptions.PauseOnStart = isPauseOnStart;
                CallAutomationService.SetRecordingFileFormat(RecordingFormat.Wav.ToString());

                var recordingResult = _service.GetCallAutomationClient().GetCallRecording().Start(recordingOptions);
                var recordingId = recordingResult.Value.RecordingId;

                string successMessage = $"Recording started. RecordingId: {recordingId}. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, Status: {recordingResult.GetRawResponse().Status.ToString()}";
                _logger.LogInformation(successMessage);

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = $"Recording started. RecordingId: {recordingId}. Status: {recordingResult.GetRawResponse().Status.ToString()}"
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
        /// Starts a recording with audio in MP3 format and unmixed channel asynchronously
        /// </summary>
        [HttpPost("startRecordingWithAudioWavUnmixedAsync")]
        [Tags("Recording APIs")]
        public async Task<IActionResult> StartRecordingWithAudioWavUnmixedAsync(
            string callConnectionId,
            bool isPauseOnStart)
        {
            try
            {
                CallConnectionProperties callConnectionProperties = _service.GetCallConnectionProperties(callConnectionId);
                var serverCallId = callConnectionProperties.ServerCallId;
                var correlationId = callConnectionProperties.CorrelationId;
                CallLocator callLocator = new ServerCallLocator(serverCallId);

                var recordingOptions = new StartRecordingOptions(callLocator);

                recordingOptions.RecordingContent = RecordingContent.Audio;
                recordingOptions.RecordingFormat = RecordingFormat.Wav;
                recordingOptions.RecordingChannel = RecordingChannel.Unmixed;
                recordingOptions.RecordingStateCallbackUri = new Uri(new Uri(_config.CallbackUriHost), "/api/callbacks");
                recordingOptions.PauseOnStart = isPauseOnStart;
                CallAutomationService.SetRecordingFileFormat(RecordingFormat.Wav.ToString());

                var recordingResult = await _service.GetCallAutomationClient().GetCallRecording().StartAsync(recordingOptions);
                var recordingId = recordingResult.Value.RecordingId;

                string successMessage = $"Recording started. RecordingId: {recordingId}. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, Status: {recordingResult.GetRawResponse().Status.ToString()}";
                _logger.LogInformation(successMessage);

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = $"Recording started. RecordingId: {recordingId}. Status: {recordingResult.GetRawResponse().Status.ToString()}"
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
        /// Starts a recording with audio in MP3 format and unmixed channel synchronously
        /// </summary>
        [HttpPost("startRecordingWithAudioWavUnmixed")]
        [Tags("Recording APIs")]
        public IActionResult StartRecordingWithAudioWavUnmixed(
            string callConnectionId,
            bool isPauseOnStart)
        {
            try
            {
                CallConnectionProperties callConnectionProperties = _service.GetCallConnectionProperties(callConnectionId);
                var serverCallId = callConnectionProperties.ServerCallId;
                var correlationId = callConnectionProperties.CorrelationId;
                CallLocator callLocator = new ServerCallLocator(serverCallId);

                var recordingOptions = new StartRecordingOptions(callLocator);

                recordingOptions.RecordingContent = RecordingContent.Audio;
                recordingOptions.RecordingFormat = RecordingFormat.Wav;
                recordingOptions.RecordingChannel = RecordingChannel.Unmixed;
                recordingOptions.RecordingStateCallbackUri = new Uri(new Uri(_config.CallbackUriHost), "/api/callbacks");
                recordingOptions.PauseOnStart = isPauseOnStart;
                CallAutomationService.SetRecordingFileFormat(RecordingFormat.Wav.ToString());

                var recordingResult = _service.GetCallAutomationClient().GetCallRecording().Start(recordingOptions);
                var recordingId = recordingResult.Value.RecordingId;

                string successMessage = $"Recording started. RecordingId: {recordingId}. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, Status: {recordingResult.GetRawResponse().Status.ToString()}";
                _logger.LogInformation(successMessage);

                return Ok(new CallConnectionResponse
                {
                    CallConnectionId = callConnectionId,
                    CorrelationId = correlationId,
                    Status = $"Recording started. RecordingId: {recordingId}. Status: {recordingResult.GetRawResponse().Status.ToString()}"
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

        // Commenting as we have to create a storage account to store the download file and redirect it
        ///// <summary>
        ///// Streams the recording to the user's browser for download (synchronous version)
        ///// </summary>
        //[HttpGet("downloadRecording")]
        //[Tags("Recording APIs")]
        //public IActionResult DownloadRecording(string callConnectionId)
        //{
        //    try
        //    {
        //        var location = CallAutomationService.GetRecordingLocation();
        //        var format = CallAutomationService.GetRecordingFileFormat();

        //        if (string.IsNullOrEmpty(location))
        //        {
        //            _logger.LogError($"Recording location is not available from the events. CallConnectionId: {callConnectionId}");
        //            return Problem("Recording Location from Azure Events is Null");
        //        }

        //        if (string.IsNullOrEmpty(format))
        //        {
        //            _logger.LogError($"Recording File Format is not available from the events. CallConnectionId: {callConnectionId}");
        //            return Problem("Recording File Format from Azure Events is Null");
        //        }

        //        var correlationId = _service.GetCallConnectionProperties(callConnectionId).CorrelationId;
        //        var date = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        //        var fileName = $"Recording_{date}.{format}";
        //        var mimeType = format.ToLower() switch
        //        {
        //            "mp4" => "video/mp4",
        //            "wav" => "audio/wav",
        //            _ => "application/octet-stream"
        //        };

        //        // Set response headers for browser download
        //        Response.ContentType = mimeType;
        //        Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{fileName}\"");

        //        // Use MemoryStream with DownloadTo (sync)
        //        using var memoryStream = new MemoryStream();
        //        var recordingClient = _service.GetCallAutomationClient().GetCallRecording();
        //        var result = recordingClient.DownloadTo(new Uri(location), memoryStream);

        //        memoryStream.Position = 0; // Reset position for reading
        //        _logger.LogInformation($"Recording streamed to browser. CallConnectionId: {callConnectionId}, CorrelationId: {correlationId}, Status: {result.Status}");

        //        return File(memoryStream, mimeType, fileName); // ASP.NET handles stream flushing
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"Error streaming recording: {ex.Message}. CallConnectionId: {callConnectionId}");
        //        return Problem($"Failed to stream recording: {ex.Message}. CallConnectionId: {callConnectionId}");
        //    }
        //}
    }
}
