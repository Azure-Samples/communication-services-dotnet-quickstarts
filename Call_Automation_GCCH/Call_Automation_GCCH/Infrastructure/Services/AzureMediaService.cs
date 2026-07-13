using Azure;
using Azure.Communication;
using Azure.Communication.CallAutomation;
using Call_Automation_GCCH.Core.Interfaces;
using Call_Automation_GCCH.Services;
using Microsoft.Extensions.Logging;

namespace Call_Automation_GCCH.Infrastructure.Services;

/// <summary>
/// Infrastructure layer implementation of IMediaService
/// Delegates to existing ICallAutomationService for Azure SDK operations
/// </summary>
public class AzureMediaService : IMediaService
{
    private readonly ICallAutomationService _callAutomationService;
    private readonly ILogger<AzureMediaService> _logger;
    private readonly string _audioFileUrl;

    public AzureMediaService(
        ICallAutomationService callAutomationService,
        ILogger<AzureMediaService> logger,
        IConfiguration configuration)
    {
        _callAutomationService = callAutomationService ?? throw new ArgumentNullException(nameof(callAutomationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _audioFileUrl = configuration["CommunicationSettings:AudioFileUrl"] ?? string.Empty;
    }

    public async Task<string> PlayAsync(
        string callConnectionId,
        string targetParticipant,
        bool isPstn,
        string? audioFileUrl = null,
        string? textToPlay = null,
        bool loop = false,
        bool interruptCallMediaOperation = false,
        string? operationContext = null)
    {
        _logger.LogInformation("Playing media to participant {Participant} in call {CallConnectionId}",
            targetParticipant, callConnectionId);

        var callMedia = _callAutomationService.GetCallMedia(callConnectionId);

        // Build play source
        var playSources = new List<PlaySource>();
        if (!string.IsNullOrWhiteSpace(audioFileUrl))
        {
            playSources.Add(new FileSource(new Uri(audioFileUrl)));
        }
        else if (!string.IsNullOrWhiteSpace(textToPlay))
        {
            playSources.Add(new TextSource(textToPlay) { VoiceName = "en-US-NancyNeural" });
        }
        else if (!string.IsNullOrWhiteSpace(_audioFileUrl))
        {
            playSources.Add(new FileSource(new Uri(_audioFileUrl)));
        }

        // Build target
        var target = isPstn
            ? new PhoneNumberIdentifier(targetParticipant)
            : new CommunicationUserIdentifier(targetParticipant) as CommunicationIdentifier;

        var playOptions = new PlayOptions(playSources.First(), new[] { target })
        {
            Loop = loop,
            OperationContext = operationContext
        };

        await callMedia.PlayAsync(playOptions);
        return operationContext ?? "PlayCompleted";
    }

    public async Task<string> PlayToAllAsync(
        string callConnectionId,
        string? audioFileUrl = null,
        string? textToPlay = null,
        bool loop = false,
        bool interruptCallMediaOperation = false,
        string? operationContext = null)
    {
        _logger.LogInformation("Playing media to all participants in call {CallConnectionId}", callConnectionId);

        var callMedia = _callAutomationService.GetCallMedia(callConnectionId);

        // Build play source
        PlaySource playSource;
        if (!string.IsNullOrWhiteSpace(audioFileUrl))
        {
            playSource = new FileSource(new Uri(audioFileUrl));
        }
        else if (!string.IsNullOrWhiteSpace(textToPlay))
        {
            playSource = new TextSource(textToPlay) { VoiceName = "en-US-NancyNeural" };
        }
        else if (!string.IsNullOrWhiteSpace(_audioFileUrl))
        {
            playSource = new FileSource(new Uri(_audioFileUrl));
        }
        else
        {
            throw new ArgumentException("Either audioFileUrl or textToPlay must be provided");
        }

        var playOptions = new PlayToAllOptions(playSource)
        {
            Loop = loop,
            InterruptCallMediaOperation = interruptCallMediaOperation,
            OperationContext = operationContext
        };

        await callMedia.PlayToAllAsync(playOptions);
        return operationContext ?? "PlayToAllCompleted";
    }

    public async Task<string> HoldAsync(
        string callConnectionId,
        string participantId,
        bool isPstn,
        string? playSourceId = null,
        string? operationContext = null)
    {
        _logger.LogInformation("Holding participant {Participant} in call {CallConnectionId}",
            participantId, callConnectionId);

        var callMedia = _callAutomationService.GetCallMedia(callConnectionId);

        var target = isPstn
            ? new PhoneNumberIdentifier(participantId)
            : new CommunicationUserIdentifier(participantId) as CommunicationIdentifier;

        var holdOptions = new HoldOptions(target)
        {
            OperationContext = operationContext
        };

        if (!string.IsNullOrWhiteSpace(playSourceId))
        {
            holdOptions.PlaySource = new FileSource(new Uri(playSourceId));
        }

        await callMedia.HoldAsync(holdOptions);
        return operationContext ?? "HoldCompleted";
    }

    public async Task<string> UnholdAsync(
        string callConnectionId,
        string participantId,
        bool isPstn,
        string? operationContext = null)
    {
        _logger.LogInformation("Unholding participant {Participant} in call {CallConnectionId}",
            participantId, callConnectionId);

        var callMedia = _callAutomationService.GetCallMedia(callConnectionId);

        var target = isPstn
            ? new PhoneNumberIdentifier(participantId)
            : new CommunicationUserIdentifier(participantId) as CommunicationIdentifier;

        var unholdOptions = new UnholdOptions(target)
        {
            OperationContext = operationContext
        };

        await callMedia.UnholdAsync(unholdOptions);
        return operationContext ?? "UnholdCompleted";
    }

    public async Task<string> StartMediaStreamingAsync(
        string callConnectionId,
        string? operationContext = null)
    {
        _logger.LogInformation("Starting media streaming for call {CallConnectionId}", callConnectionId);

        var callMedia = _callAutomationService.GetCallMedia(callConnectionId);

        var options = new StartMediaStreamingOptions
        {
            OperationContext = operationContext
        };

        await callMedia.StartMediaStreamingAsync(options);
        return operationContext ?? "MediaStreamingStarted";
    }

    public async Task<string> StopMediaStreamingAsync(
        string callConnectionId,
        string? operationContext = null)
    {
        _logger.LogInformation("Stopping media streaming for call {CallConnectionId}", callConnectionId);

        var callMedia = _callAutomationService.GetCallMedia(callConnectionId);

        var options = new StopMediaStreamingOptions
        {
            OperationContext = operationContext
        };

        await callMedia.StopMediaStreamingAsync(options);
        return operationContext ?? "MediaStreamingStopped";
    }

    public async Task<string> CancelAllMediaOperationsAsync(string callConnectionId)
    {
        _logger.LogInformation("Cancelling all media operations for call {CallConnectionId}", callConnectionId);

        var callMedia = _callAutomationService.GetCallMedia(callConnectionId);
        await callMedia.CancelAllMediaOperationsAsync();
        return "AllMediaOperationsCancelled";
    }

    public async Task<string> StartTranscriptionAsync(
        string callConnectionId,
        string locale = "en-US",
        string? operationContext = null)
    {
        _logger.LogInformation("Starting transcription for call {CallConnectionId} with locale {Locale}",
            callConnectionId, locale);

        var callMedia = _callAutomationService.GetCallMedia(callConnectionId);

        var options = new StartTranscriptionOptions
        {
            Locale = locale,
            OperationContext = operationContext
        };

        await callMedia.StartTranscriptionAsync(options);
        return operationContext ?? "TranscriptionStarted";
    }

    public async Task<string> StopTranscriptionAsync(
        string callConnectionId,
        string? operationContext = null)
    {
        _logger.LogInformation("Stopping transcription for call {CallConnectionId}", callConnectionId);

        var callMedia = _callAutomationService.GetCallMedia(callConnectionId);

        var options = new StopTranscriptionOptions
        {
            OperationContext = operationContext
        };

        await callMedia.StopTranscriptionAsync(options);
        return operationContext ?? "TranscriptionStopped";
    }

    public async Task<string> UpdateTranscriptionAsync(
        string callConnectionId,
        string locale,
        string? operationContext = null)
    {
        _logger.LogInformation("Updating transcription for call {CallConnectionId} to locale {Locale}",
            callConnectionId, locale);

        var callMedia = _callAutomationService.GetCallMedia(callConnectionId);

        var options = new UpdateTranscriptionOptions(locale)
        {
            OperationContext = operationContext
        };

        await callMedia.UpdateTranscriptionAsync(options);
        return operationContext ?? "TranscriptionUpdated";
    }

    public async Task PlayAudioAsync(string callConnectionId, string audioUrl, bool loop = false)
    {
        await PlayToAllAsync(callConnectionId, audioFileUrl: audioUrl, loop: loop);
    }

    public async Task CancelMediaOperationsAsync(string callConnectionId)
    {
        await CancelAllMediaOperationsAsync(callConnectionId);
    }
}
