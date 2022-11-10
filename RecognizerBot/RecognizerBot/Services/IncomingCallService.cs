// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Azure.Communication;
using Azure.Communication.CallAutomation;
using IncomingCallRouting.EventHandler;
using IncomingCallRouting.Interfaces;
using IncomingCallRouting.Utils;
using Microsoft.Extensions.Configuration;

namespace IncomingCallRouting.Services
{
    /// <summary>
    /// Handling different callback events
    /// and perform operations
    /// </summary>
    public class IncomingCallService : IIncomingCallService
    {
        private readonly IRegonizeService _recognizeService;
        private readonly CallAutomationClient _callingServerClient;
        private readonly CallConfiguration _callConfiguration;
        private readonly CallRecording _callRecording;
        private CallConnection _callConnection;
        private CallConnectionProperties _callConnectionProperties;
        private CancellationTokenSource _reportCancellationTokenSource;
        private CancellationToken _reportCancellationToken;
        private readonly string _streamUri;

        private TaskCompletionSource<bool> callEstablishedTask;
        private TaskCompletionSource<bool> playAudioCompletedTask;
        private TaskCompletionSource<bool> callTerminatedTask;
        private TaskCompletionSource<bool> toneReceivedCompleteTask;
        private TaskCompletionSource<bool> transferToParticipantCompleteTask;
        private readonly int maxRetryAttemptCount = 3;

        public IncomingCallService(IConfiguration configuration, IRegonizeService recognizeService)
        {
            _recognizeService = recognizeService;
            var options = new CallAutomationClientOptions { Diagnostics = { LoggedHeaderNames = { "*" } } };
            _callingServerClient = new CallAutomationClient(new Uri(configuration["PmaUri"]), configuration["ResourceConnectionString"], options);
            // callingServerClient = new CallAutomationClient(configuration["ResourceConnectionString"], options);

            _callConfiguration = CallConfiguration.GetCallConfiguration(configuration);
            _callRecording = _callingServerClient.GetCallRecording();
            _streamUri = _callConfiguration.StreamUri;
        }

        public async Task HandleCall(string incomingCallContext)
        {
            _reportCancellationTokenSource = new CancellationTokenSource();
            _reportCancellationToken = _reportCancellationTokenSource.Token;

            try
            {
                // Answer Call
                var response = await _callingServerClient.AnswerCallAsync(new AnswerCallOptions(incomingCallContext,
                    new Uri("https://3a1c-167-220-2-8.ngrok.io"))
                {
                    MediaStreamingOptions = new MediaStreamingOptions(new Uri(_streamUri), MediaStreamingTransport.Websocket, MediaStreamingContent.Audio, MediaStreamingAudioChannel.Mixed)
                }, _reportCancellationToken);
                Logger.LogMessage(Logger.MessageType.INFORMATION, $"AnswerCallAsync Response -----> {response.GetRawResponse()}");

                // var createCall = await _callingServerClient.CreateCallAsync(new CreateCallOptions(
                //     new CallSource(new CommunicationUserIdentifier("SOURCE")),
                //     new[] { new CommunicationUserIdentifier("TARGET") }, new Uri("CallingServerAPICallBacks")));

                _callConnection = response.Value.CallConnection;
                _callConnectionProperties = response.Value.CallConnectionProperties;
                RegisterToCallStateChangeEvent(_callConnectionProperties.CallConnectionId);

                CurrentCall.CallConnection = _callConnection;
                CurrentCall.CallConnectionProperties = _callConnectionProperties;

                // Wait for the call to get connected
                await callEstablishedTask.Task.ConfigureAwait(false);

                // Start recognizing intents from audio stream
                await _recognizeService.RecognizeIntentFromStream(true);

                // Wait for the call to terminate
                await callTerminatedTask.Task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogMessage(Logger.MessageType.ERROR, $"Call ended unexpectedly, reason: {ex.Message}");
            }
        }

        private async Task RetryTransferToParticipantAsync(Func<Task<bool>> action)
        {
            int retryAttemptCount = 1;
            while (retryAttemptCount <= maxRetryAttemptCount)
            {
                Logger.LogMessage(Logger.MessageType.INFORMATION, $"Retrying Transfer participant attempt {retryAttemptCount} is in progress");
                var transferToParticipantResult = await action();
                if (transferToParticipantResult)
                {
                    return;
                }
                else
                {
                    Logger.LogMessage(Logger.MessageType.INFORMATION, $"Retry transfer participant attempt {retryAttemptCount} has failed");
                    retryAttemptCount++;
                }
            }
        }

        private async Task PlayAudioAsync(string target)
        {
            try
            {
                var playSource = new FileSource(new Uri(_callConfiguration.AudioFileUrl));

                var operationContext = Guid.NewGuid().ToString();
                // var response = await _callConnection.GetCallMedia().PlayAsync(playSource, new[] { GetIdentifier(target) }, cancellationToken: _reportCancellationToken).ConfigureAwait(false);
                var response = await _callConnection.GetCallMedia().PlayToAllAsync(playSource, cancellationToken: _reportCancellationToken).ConfigureAwait(false);

                // var response = await _callConnection.GetCallMedia().StartRecognizingAsync(new CallMediaRecognizeDtmfOptions(GetIdentifier(target), 1)
                // {
                //     Prompt = playSource,
                //     OperationContext = operationContext
                // }).ConfigureAwait(false);

                Logger.LogMessage(Logger.MessageType.INFORMATION, $"StartRecognizeAsync response --> {response.Status}, Id: {response.ClientRequestId}");
        
                if (response.Status == 202)
                {
                    // listen to play audio events
                    RegisterToDtmfResultEvent(operationContext);
        
                    var completedTask = await Task.WhenAny(toneReceivedCompleteTask.Task, Task.Delay(10 * 1000)).ConfigureAwait(false);
        
                    // if (completedTask != toneReceivedCompleteTask.Task)
                    // {
                    //     // playAudioCompletedTask.TrySetResult(false);
                    //     toneReceivedCompleteTask.TrySetResult(false);
                    //     await CancelAllMediaOperations().ConfigureAwait(false);
                    // }
                }
            }
            catch (TaskCanceledException)
            {
                Logger.LogMessage(Logger.MessageType.ERROR, "Play audio operation cancelled");
            }
            catch (Exception ex)
            {
                Logger.LogMessage(Logger.MessageType.ERROR, $"Failure occured while playing audio on the call. Exception: {ex.Message}");
            }
        }

        private async Task HangupAsync()
        {
            if (_reportCancellationToken.IsCancellationRequested)
            {
                Logger.LogMessage(Logger.MessageType.INFORMATION, "Cancellation request, Hangup will not be performed");
                return;
            }

            Logger.LogMessage(Logger.MessageType.INFORMATION, "Performing Hangup operation");
            var hangupResponse = await _callConnection.HangUpAsync(false).ConfigureAwait(false);

            Logger.LogMessage(Logger.MessageType.INFORMATION, $"HangupAsync response --> {hangupResponse}");

        }

        private async Task CancelAllMediaOperations()
        {
            if (_reportCancellationToken.IsCancellationRequested)
            {
                Logger.LogMessage(Logger.MessageType.INFORMATION, "Cancellation request, CancelMediaProcessing will not be performed");
                return;
            }
        
            Logger.LogMessage(Logger.MessageType.INFORMATION, "Performing cancel media processing operation to stop playing audio");
        
            var operationContext = Guid.NewGuid().ToString();
            var response = await _callConnection.GetCallMedia().CancelAllMediaOperationsAsync(_reportCancellationToken).ConfigureAwait(false);
        
            Logger.LogMessage(Logger.MessageType.INFORMATION, $"PlayAudioAsync response --> {response.ContentStream}, " +
                $"Id: {response.Content}, Status: {response.Status}");
        }

        private void RegisterToCallStateChangeEvent(string callConnectionId)
        {
            callEstablishedTask = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _reportCancellationToken.Register(() => callEstablishedTask.TrySetCanceled());

            callTerminatedTask = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            //Set the callback method
            var callStateChangeNotificaiton = new NotificationCallback((CallAutomationEventBase callEvent) =>
            {
                var callStateChanged = (CallConnected)callEvent;

                Logger.LogMessage(Logger.MessageType.INFORMATION, $"Call State changed to connected");

                if (callEvent is CallConnected callConnectedEvent)
                {
                    callEstablishedTask.TrySetResult(true);
                }
            });

            //Subscribe to the event
            var eventId = EventDispatcher.Instance.Subscribe(nameof(CallConnected), callConnectionId, callStateChangeNotificaiton);
        }

        private void RegisterToPlayAudioResultEvent(string operationContext)
        {
            playAudioCompletedTask = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _reportCancellationToken.Register(() => playAudioCompletedTask.TrySetCanceled());

            var playPromptResponseNotification = new NotificationCallback((CallAutomationEventBase callEvent) =>
            {
                Task.Run(() =>
                {
                    var playAudioResultEvent = (PlayCompleted)callEvent;
                    Logger.LogMessage(Logger.MessageType.INFORMATION, $"Play audio status: {playAudioResultEvent}");

                    playAudioCompletedTask.TrySetResult(true);
                    EventDispatcher.Instance.Unsubscribe(nameof(PlayCompleted), operationContext);
                });
            });

            //Subscribe to event
            EventDispatcher.Instance.Subscribe(nameof(PlayCompleted), operationContext, playPromptResponseNotification);
        }

        private void RegisterToDtmfResultEvent(string callConnectionId)
        {
            toneReceivedCompleteTask = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var dtmfReceivedEvent = new NotificationCallback((CallAutomationEventBase callEvent) =>
            {
                Task.Run(async () =>
                {
                    var recognizeCompletedEvent = (RecognizeCompleted)callEvent;
                    Logger.LogMessage(Logger.MessageType.INFORMATION, $"Tone received ---------> : {recognizeCompletedEvent.CollectTonesResult?.Tones}");

                    toneReceivedCompleteTask.TrySetResult(recognizeCompletedEvent?.CollectTonesResult?.Tones[0] == DtmfTone.One);

                    EventDispatcher.Instance.Unsubscribe(nameof(RecognizeCompleted), callConnectionId);
                    // cancel playing audio
                    await CancelAllMediaOperations().ConfigureAwait(false);
                });
            });
            //Subscribe to event
            EventDispatcher.Instance.Subscribe(nameof(RecognizeCompleted), callConnectionId, dtmfReceivedEvent);
        }

        private CommunicationIdentifier GetIdentifier(string targetParticipant, string rawId = null)
        {

            if (GetIdentifierKind(targetParticipant) == CommunicationIdentifierKind.UserIdentity)
            {
                return new CommunicationUserIdentifier(targetParticipant);
            }
            else if (GetIdentifierKind(targetParticipant) == CommunicationIdentifierKind.PhoneIdentity)
            {
                return new PhoneNumberIdentifier(targetParticipant, rawId);
            }
            else if (GetIdentifierKind(targetParticipant) == CommunicationIdentifierKind.TeamsIdentity)
            {
                return new MicrosoftTeamsUserIdentifier(targetParticipant, rawId: rawId);
            }
            else
            {
                return null;
            }
        }

        private async Task<bool> TransferToParticipant(string targetParticipant, string transfereeCallerId = null)
        {
            transferToParticipantCompleteTask = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            var identifier = GetIdentifier(targetParticipant);

            if (identifier == null)
            {
                Logger.LogMessage(Logger.MessageType.INFORMATION, "Unknown identity provided. Enter valid phone number or communication user id");
                return true;
            }
            var operationContext = Guid.NewGuid().ToString();

            var response = await _callConnection.TransferCallToParticipantAsync(new TransferToParticipantOptions(identifier)
            {
                OperationContext = operationContext,
                SourceCallerId = transfereeCallerId == null ? null : new PhoneNumberIdentifier(transfereeCallerId),
                UserToUserInformation = "user1"
            }, cancellationToken: _reportCancellationToken);

            var transferToParticipantCompleted = await transferToParticipantCompleteTask.Task.ConfigureAwait(false);
            return transferToParticipantCompleted;
        }

        private void RegisterToTransferParticipantsResultEvent(string operationContext)
        {
            var transferToParticipantReceivedEvent = new NotificationCallback(async (CallAutomationEventBase callEvent) =>
            {
                var transferParticipantUpdatedEvent = (ParticipantsUpdated)callEvent;
                if (transferParticipantUpdatedEvent.CallConnectionId != null)
                {
                    Logger.LogMessage(Logger.MessageType.INFORMATION, $"Transfer participant callconnection ID - {transferParticipantUpdatedEvent.CallConnectionId}");
                    EventDispatcher.Instance.Unsubscribe(nameof(ParticipantsUpdated), operationContext);

                    Logger.LogMessage(Logger.MessageType.INFORMATION, "Sleeping for 60 seconds before proceeding further");
                    await Task.Delay(60 * 1000);

                    transferToParticipantCompleteTask.TrySetResult(true);
                }
                else
                {
                    transferToParticipantCompleteTask.TrySetResult(false);
                }
            });

            //Subscribe to event
            EventDispatcher.Instance.Subscribe(nameof(ParticipantsUpdated), operationContext, transferToParticipantReceivedEvent);
        }

        private CommunicationIdentifierKind GetIdentifierKind(string participantnumber)
        {
            //checks the identity type returns as string
            return Regex.Match(participantnumber, Constants.userIdentityRegex, RegexOptions.IgnoreCase).Success ? CommunicationIdentifierKind.UserIdentity :
                   Regex.Match(participantnumber, Constants.phoneIdentityRegex, RegexOptions.IgnoreCase).Success ? CommunicationIdentifierKind.PhoneIdentity :
                   Regex.Match(participantnumber, Constants.teamsIdentityRegex, RegexOptions.IgnoreCase).Success ? CommunicationIdentifierKind.TeamsIdentity :
                   CommunicationIdentifierKind.UnknownIdentity;
        }

        /// <summary>
        /// Type of communication identifiers.
        /// </summary>
        public enum CommunicationIdentifierKind
        {
            UserIdentity,
            PhoneIdentity,
            TeamsIdentity,
            UnknownIdentity
        }
    }
}
