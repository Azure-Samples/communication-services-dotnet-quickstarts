using Azure;
using Azure.Communication;
using Azure.Communication.CallAutomation;
using Call_Automation_GCCH.Core.Entities;
using Call_Automation_GCCH.Core.Interfaces;
using Microsoft.Extensions.Logging;
using AzureCallConnection = Azure.Communication.CallAutomation.CallConnection;
using DomainCallConnection = Call_Automation_GCCH.Core.Entities.CallConnection;

namespace Call_Automation_GCCH.Infrastructure.Services;

/// <summary>
/// Infrastructure layer implementation of ICallService
/// Wraps Azure Communication Services SDK
/// </summary>
public class AzureCallService : ICallService
{
    private readonly CallAutomationClient _client;
    private readonly ILogger<AzureCallService> _logger;
    private readonly string _sourcePhoneNumber;

    public AzureCallService(
        CallAutomationClient client,
        ILogger<AzureCallService> logger,
        string sourcePhoneNumber)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sourcePhoneNumber = sourcePhoneNumber;
    }

    public async Task<DomainCallConnection> CreateCallAsync(CallOptions options, string callbackUri)
    {
        _logger.LogInformation("AzureCallService: Creating call to {Target}", options.Target);

        // Build call invite
        var invite = options.IsPstn
            ? new CallInvite(
                new PhoneNumberIdentifier(options.Target),
                new PhoneNumberIdentifier(_sourcePhoneNumber))
            : new CallInvite(new CommunicationUserIdentifier(options.Target));

        // Build create call options
        var createOptions = new CreateCallOptions(invite, new Uri(callbackUri))
        {
            OperationContext = options.OperationContext
        };

        // Add transcription if configured
        if (options.Transcription != null)
        {
            var wsUri = BuildWebSocketUri(callbackUri);
            createOptions.TranscriptionOptions = new TranscriptionOptions(
                new Uri(wsUri),
                options.Transcription.Locale,
                options.Transcription.StartTranscription,
                TranscriptionTransport.Websocket)
            {
                EnableIntermediateResults = options.Transcription.EnableIntermediateResults
            };
        }

        // Add media streaming if configured
        if (options.MediaStreaming != null)
        {
            var wsUri = BuildWebSocketUri(callbackUri);
            var audioChannel = options.MediaStreaming.MediaStreamingAudioChannel.Equals("Unmixed", StringComparison.OrdinalIgnoreCase)
                ? MediaStreamingAudioChannel.Unmixed
                : MediaStreamingAudioChannel.Mixed;
            var format = options.MediaStreaming.AudioFormat.Equals("Pcm24KMono", StringComparison.OrdinalIgnoreCase)
                ? AudioFormat.Pcm24KMono
                : AudioFormat.Pcm16KMono;

            createOptions.MediaStreamingOptions = new MediaStreamingOptions(
                new Uri(wsUri),
                MediaStreamingContent.Audio,
                audioChannel,
                MediaStreamingTransport.Websocket,
                options.MediaStreaming.StartMediaStreaming)
            {
                EnableBidirectional = options.MediaStreaming.EnableBidirectional,
                AudioFormat = format
            };
        }

        // Add call intelligence if configured
        if (options.CallIntelligence != null)
        {
            if (!Uri.TryCreate(options.CallIntelligence.CognitiveServicesEndpoint, UriKind.Absolute, out var endpoint))
            {
                throw new ArgumentException($"Invalid cognitive services endpoint: {options.CallIntelligence.CognitiveServicesEndpoint}");
            }

            createOptions.CallIntelligenceOptions = new CallIntelligenceOptions
            {
                CognitiveServicesEndpoint = endpoint
            };
        }

        // Execute SDK call
        var result = await _client.CreateCallAsync(createOptions);
        var props = result.Value.CallConnectionProperties;

        return new DomainCallConnection
        {
            CallConnectionId = props.CallConnectionId,
            CorrelationId = props.CorrelationId,
            Status = props.CallConnectionState.ToString()
        };
    }

    public async Task<DomainCallConnection> CreateGroupCallAsync(
        List<string> targets,
        string callbackUri,
        CallOptions? options = null)
    {
        _logger.LogInformation("AzureCallService: Creating group call with {Count} targets", targets.Count);

        var identifiers = new List<CommunicationIdentifier>();
        foreach (var target in targets)
        {
            if (target.StartsWith("8:"))
                identifiers.Add(new CommunicationUserIdentifier(target));
            else if (target.StartsWith("+"))
                identifiers.Add(new PhoneNumberIdentifier(target));
            else
                throw new ArgumentException($"Invalid target format: {target}");
        }

        var groupOptions = new CreateGroupCallOptions(identifiers, new Uri(callbackUri))
        {
            SourceCallerIdNumber = new PhoneNumberIdentifier(_sourcePhoneNumber),
            OperationContext = options?.OperationContext
        };

        // Add optional configurations (similar to CreateCallAsync)
        if (options?.Transcription != null)
        {
            var wsUri = BuildWebSocketUri(callbackUri);
            groupOptions.TranscriptionOptions = new TranscriptionOptions(
                new Uri(wsUri),
                options.Transcription.Locale,
                options.Transcription.StartTranscription,
                TranscriptionTransport.Websocket)
            {
                EnableIntermediateResults = options.Transcription.EnableIntermediateResults
            };
        }

        if (options?.MediaStreaming != null)
        {
            var wsUri = BuildWebSocketUri(callbackUri);
            var audioChannel = options.MediaStreaming.MediaStreamingAudioChannel.Equals("Unmixed", StringComparison.OrdinalIgnoreCase)
                ? MediaStreamingAudioChannel.Unmixed
                : MediaStreamingAudioChannel.Mixed;
            var format = options.MediaStreaming.AudioFormat.Equals("Pcm24KMono", StringComparison.OrdinalIgnoreCase)
                ? AudioFormat.Pcm24KMono
                : AudioFormat.Pcm16KMono;

            groupOptions.MediaStreamingOptions = new MediaStreamingOptions(
                new Uri(wsUri),
                MediaStreamingContent.Audio,
                audioChannel,
                MediaStreamingTransport.Websocket,
                options.MediaStreaming.StartMediaStreaming)
            {
                EnableBidirectional = options.MediaStreaming.EnableBidirectional,
                AudioFormat = format
            };
        }

        if (options?.CallIntelligence != null)
        {
            if (!Uri.TryCreate(options.CallIntelligence.CognitiveServicesEndpoint, UriKind.Absolute, out var endpoint))
            {
                throw new ArgumentException($"Invalid cognitive services endpoint");
            }

            groupOptions.CallIntelligenceOptions = new CallIntelligenceOptions
            {
                CognitiveServicesEndpoint = endpoint
            };
        }

        var result = await _client.CreateGroupCallAsync(groupOptions);
        var props = result.Value.CallConnectionProperties;

        return new DomainCallConnection
        {
            CallConnectionId = props.CallConnectionId,
            CorrelationId = props.CorrelationId,
            Status = props.CallConnectionState.ToString()
        };
    }

    public async Task<DomainCallConnection> TransferCallAsync(
        string callConnectionId,
        string targetParticipant,
        string? transferee = null,
        bool isPstn = true)
    {
        _logger.LogInformation("AzureCallService: Transferring call {CallId}", callConnectionId);

        var connection = _client.GetCallConnection(callConnectionId);
        var props = await connection.GetCallConnectionPropertiesAsync();

        var transferOptions = isPstn
            ? new TransferToParticipantOptions(new PhoneNumberIdentifier(targetParticipant))
            {
                OperationContext = "TransferCallContext",
                Transferee = string.IsNullOrEmpty(transferee) ? null : new PhoneNumberIdentifier(transferee)
            }
            : new TransferToParticipantOptions(new CommunicationUserIdentifier(targetParticipant))
            {
                OperationContext = "TransferCallContext",
                Transferee = string.IsNullOrEmpty(transferee) ? null : new CommunicationUserIdentifier(transferee)
            };

        var response = await connection.TransferCallToParticipantAsync(transferOptions);

        return new DomainCallConnection
        {
            CallConnectionId = callConnectionId,
            CorrelationId = props.Value.CorrelationId,
            Status = response.GetRawResponse().Status.ToString()
        };
    }

    public async Task<bool> HangupCallAsync(string callConnectionId, bool forEveryone = false)
    {
        _logger.LogInformation("AzureCallService: Hanging up call {CallId}", callConnectionId);

        var connection = _client.GetCallConnection(callConnectionId);
        var response = await connection.HangUpAsync(forEveryone);

        return response.Status == 204; // No Content = Success
    }

    public async Task<DomainCallConnection> GetCallPropertiesAsync(string callConnectionId)
    {
        var connection = _client.GetCallConnection(callConnectionId);
        var props = await connection.GetCallConnectionPropertiesAsync();

        return new DomainCallConnection
        {
            CallConnectionId = props.Value.CallConnectionId,
            CorrelationId = props.Value.CorrelationId,
            Status = props.Value.CallConnectionState.ToString()
        };
    }

    private string BuildWebSocketUri(string httpUri)
    {
        return httpUri.Replace("https://", "wss://")
            .Replace("http://", "ws://")
            .TrimEnd('/') + "/ws";
    }
}
