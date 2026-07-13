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

        var client = _callAutomationService.GetCallAutomationClient();
        var recording = client.GetCallRecording();
        await recording.DeleteAsync(new Uri(recordingLocation));

        return "RecordingDeleted";
    }

    public async Task<byte[]> DownloadRecordingAsync(string downloadLocation)
    {
        _logger.LogInformation("Downloading recording from {Location}", downloadLocation);

        var client = _callAutomationService.GetCallAutomationClient();
        var recording = client.GetCallRecording();
        var response = await recording.DownloadStreamingAsync(new Uri(downloadLocation));

        using var memoryStream = new MemoryStream();
        await response.Value.CopyToAsync(memoryStream);

        return memoryStream.ToArray();
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
