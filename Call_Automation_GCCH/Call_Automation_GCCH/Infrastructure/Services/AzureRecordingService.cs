using Azure;
using Azure.Communication;
using Azure.Communication.CallAutomation;
using Call_Automation_GCCH.Core.Interfaces;
using Call_Automation_GCCH.Services;
using Microsoft.Extensions.Logging;

namespace Call_Automation_GCCH.Infrastructure.Services;

/// <summary>
/// Infrastructure layer implementation of IRecordingService
/// Delegates to existing ICallAutomationService for Azure SDK operations
/// </summary>
public class AzureRecordingService : IRecordingService
{
    private readonly ICallAutomationService _callAutomationService;
    private readonly ILogger<AzureRecordingService> _logger;
    private readonly string _callbackUri;

    public AzureRecordingService(
        ICallAutomationService callAutomationService,
        ILogger<AzureRecordingService> logger,
        IConfiguration configuration)
    {
        _callAutomationService = callAutomationService ?? throw new ArgumentNullException(nameof(callAutomationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _callbackUri = configuration["CommunicationSettings:CallbackUriHost"] ?? string.Empty;
    }

    public async Task<RecordingInfo> StartRecordingAsync(
        string callConnectionId,
        RecordingConfiguration? options = null)
    {
        _logger.LogInformation("Starting recording for call {CallConnectionId}", callConnectionId);

        var callProps = _callAutomationService.GetCallConnectionProperties(callConnectionId);
        var client = _callAutomationService.GetCallAutomationClient();

        var callLocator = new ServerCallLocator(callProps.ServerCallId);
        var startOptions = new StartRecordingOptions(callLocator);

        if (options != null)
        {
            if (options.IsAudioVideo)
            {
                startOptions.RecordingContent = RecordingContent.AudioVideo;
            }

            startOptions.RecordingChannel = options.IsMixed
                ? RecordingChannel.Mixed
                : RecordingChannel.Unmixed;

            startOptions.RecordingFormat = options.RecordingFormat switch
            {
                "Mp3" => RecordingFormat.Mp3,
                "Mp4" => RecordingFormat.Mp4,
                "Wav" => RecordingFormat.Wav,
                _ => RecordingFormat.Mp3
            };

            startOptions.PauseOnStart = options.PauseOnStart;

            if (!string.IsNullOrWhiteSpace(_callbackUri))
            {
                startOptions.RecordingStateCallbackUri = new Uri(new Uri(_callbackUri), "/api/callbacks");
            }

            if (!string.IsNullOrWhiteSpace(options.ExternalStorageUri))
            {
                startOptions.RecordingStorage = RecordingStorage.CreateAzureBlobContainerRecordingStorage(
                    new Uri(options.ExternalStorageUri));
            }

            if (options.ChannelAffinity != null && options.ChannelAffinity.Any())
            {
                foreach (var ca in options.ChannelAffinity)
                {
                    var participant = ca.Participant.StartsWith("+")
                        ? (CommunicationIdentifier)new PhoneNumberIdentifier(ca.Participant)
                        : new CommunicationUserIdentifier(ca.Participant);

                    startOptions.ChannelAffinity.Add(new Azure.Communication.CallAutomation.ChannelAffinity(participant)
                    {
                        Channel = ca.Channel
                    });
                }
            }
        }

        var response = await client.GetCallRecording().StartAsync(startOptions);

        return new RecordingInfo
        {
            RecordingId = response.Value.RecordingId,
            Status = response.Value.RecordingState.ToString(),
            Format = startOptions.RecordingFormat.ToString()
        };
    }

    public async Task<string> PauseRecordingAsync(string recordingId)
    {
        _logger.LogInformation("Pausing recording {RecordingId}", recordingId);

        var client = _callAutomationService.GetCallAutomationClient();
        var recording = client.GetCallRecording();
        await recording.PauseAsync(recordingId);

        return "RecordingPaused";
    }

    public async Task<string> ResumeRecordingAsync(string recordingId)
    {
        _logger.LogInformation("Resuming recording {RecordingId}", recordingId);

        var client = _callAutomationService.GetCallAutomationClient();
        var recording = client.GetCallRecording();
        await recording.ResumeAsync(recordingId);

        return "RecordingResumed";
    }

    public async Task<string> StopRecordingAsync(string recordingId)
    {
        _logger.LogInformation("Stopping recording {RecordingId}", recordingId);

        var client = _callAutomationService.GetCallAutomationClient();
        var recording = client.GetCallRecording();
        await recording.StopAsync(recordingId);

        return "RecordingStopped";
    }

    public async Task<RecordingStateInfo> GetRecordingStateAsync(string recordingId)
    {
        _logger.LogInformation("Getting state for recording {RecordingId}", recordingId);

        var client = _callAutomationService.GetCallAutomationClient();
        var recording = client.GetCallRecording();
        var response = await recording.GetStateAsync(recordingId);

        return new RecordingStateInfo
        {
            RecordingId = recordingId,
            State = response.Value.RecordingState.ToString()
        };
    }

    public async Task<string> DeleteRecordingAsync(string recordingLocation)
    {
        _logger.LogInformation("Deleting recording at {Location}", recordingLocation);

        var client = _callAutomationService.GetRecordingDownloadClient();
        var recording = client.GetCallRecording();
        await recording.DeleteAsync(new Uri(recordingLocation));

        return "RecordingDeleted";
    }

    public async Task<byte[]> DownloadRecordingAsync(string downloadLocation)
    {
        _logger.LogInformation("[AzureRecordingService] Downloading recording from {Location}", downloadLocation);
        _logger.LogInformation("[AzureRecordingService] Validating recording service client...");

        try
        {
            var client = _callAutomationService.GetRecordingDownloadClient();
            if (client == null)
            {
                _logger.LogError("[AzureRecordingService] Call Automation Client is null");
                throw new InvalidOperationException("Call Automation Client is not initialized");
            }

            // Log which ACS resource the current client is authenticated against
            try
            {
                var pmaEndpoint = _callAutomationService.GetCurrentPmaEndpoint();
                _logger.LogInformation("[AzureRecordingService] Current PMA endpoint: {PmaEndpoint}",
                    string.IsNullOrEmpty(pmaEndpoint) ? "(none - using default)" : pmaEndpoint);
            }
            catch { /* ignore if not available */ }

            _logger.LogInformation("[AzureRecordingService] Creating recording client...");
            var recording = client.GetCallRecording();
            if (recording == null)
            {
                _logger.LogError("[AzureRecordingService] Recording client is null");
                throw new InvalidOperationException("Recording client is not initialized");
            }

            _logger.LogInformation("[AzureRecordingService] Calling DownloadStreamingAsync...");
            var recordingUri = new Uri(downloadLocation);
            _logger.LogInformation("[AzureRecordingService] Recording URI Scheme: {Scheme}", recordingUri.Scheme);
            _logger.LogInformation("[AzureRecordingService] Recording URI Host: {Host}", recordingUri.Host);
            _logger.LogInformation("[AzureRecordingService] Recording URI Path: {Path}", recordingUri.AbsolutePath);
            _logger.LogInformation("[AzureRecordingService] Recording URI Query length: {QueryLen}", recordingUri.Query?.Length ?? 0);

            var startTime = DateTime.UtcNow;
            var response = await recording.DownloadStreamingAsync(recordingUri);
            var downloadTime = DateTime.UtcNow - startTime;
            _logger.LogInformation("[AzureRecordingService] DownloadStreamingAsync returned in {Elapsed}ms", downloadTime.TotalMilliseconds);

            _logger.LogInformation("[AzureRecordingService] Response is null: {IsNull}", response == null);
            _logger.LogInformation("[AzureRecordingService] Response Value is null: {IsNull}", response?.Value == null);

            using var memoryStream = new MemoryStream();
            var copyStartTime = DateTime.UtcNow;
            await response.Value.CopyToAsync(memoryStream);
            var copyTime = DateTime.UtcNow - copyStartTime;
            _logger.LogInformation("[AzureRecordingService] Stream copied to memory in {Elapsed}ms", copyTime.TotalMilliseconds);

            var result = memoryStream.ToArray();
            _logger.LogInformation("[AzureRecordingService] Successfully downloaded {ByteCount} bytes", result.Length);
            _logger.LogInformation("[AzureRecordingService] Total operation time: {Elapsed}ms", (DateTime.UtcNow - startTime).TotalMilliseconds);

            return result;
        }
        catch (Azure.RequestFailedException rfx) when (rfx.Status == 401)
        {
            _logger.LogError(rfx, "[AzureRecordingService] AUTHORIZATION FAILED (401 Unauthorized)");
            _logger.LogError("[AzureRecordingService] ACS Error Code: {ErrorCode}", rfx.ErrorCode);
            _logger.LogError("[AzureRecordingService] ACS Status: {Status}", rfx.Status);
            _logger.LogError("[AzureRecordingService] ACS Message: {Message}", rfx.Message);
            _logger.LogError("[AzureRecordingService] === LIKELY CAUSES OF 401 ===");
            _logger.LogError("[AzureRecordingService] 1. The ACS connection string in the app does NOT match the ACS resource that owns this recording.");
            _logger.LogError("[AzureRecordingService] 2. The ACS access key has been rotated/regenerated after recording was created.");
            _logger.LogError("[AzureRecordingService] 3. The recording URL is expired (recordings expire 48 hours after creation).");
            _logger.LogError("[AzureRecordingService] 4. The recording URL is from a different environment (e.g., commercial vs GCCH).");
            _logger.LogError("[AzureRecordingService] === RECOMMENDED FIX ===");
            _logger.LogError("[AzureRecordingService] Verify AcsConnectionString points to the same ACS resource used to CREATE the recording.");
            _logger.LogError("[AzureRecordingService] Check current config via: GET /api/v2/configuration");
            throw;
        }
        catch (Azure.RequestFailedException rfx)
        {
            _logger.LogError(rfx, "[AzureRecordingService] ACS REQUEST FAILED - Status: {Status}, ErrorCode: {ErrorCode}", rfx.Status, rfx.ErrorCode);
            _logger.LogError("[AzureRecordingService] Message: {Message}", rfx.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AzureRecordingService] DOWNLOAD FAILED - Exception Type: {ExceptionType}", ex.GetType().Name);
            _logger.LogError("[AzureRecordingService] Exception Message: {Message}", ex.Message);
            _logger.LogError("[AzureRecordingService] Stack Trace: {StackTrace}", ex.StackTrace);
            throw;
        }
    }

    public async Task<string> StartRecordingAsync(string callConnectionId)
    {
        var result = await StartRecordingAsync(callConnectionId, null);
        return result.RecordingId;
    }

    public async Task StopRecordingAsync(string callConnectionId, string recordingId)
    {
        await StopRecordingAsync(recordingId);
    }

    public async Task PauseRecordingAsync(string callConnectionId, string recordingId)
    {
        await PauseRecordingAsync(recordingId);
    }

    public async Task ResumeRecordingAsync(string callConnectionId, string recordingId)
    {
        await ResumeRecordingAsync(recordingId);
    }
}
