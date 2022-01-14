// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace IncomingCallRouting
{
    /// <summary>
    /// Handling different callback events
    /// and perform operations
    /// </summary>

    using Azure.Communication;
    using Azure.Communication.CallingServer;
    using Azure.Communication.CallingServer.Models;
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    public class IncomingCallHandler
    {
        private CallingServerClient callingServerClient;
        private CallConfiguration callConfiguration;
        private CallConnection callConnection;
        private CancellationTokenSource reportCancellationTokenSource;
        private CancellationToken reportCancellationToken;
        private string targetParticipant;

        private TaskCompletionSource<bool> callEstablishedTask;
        private TaskCompletionSource<bool> playAudioCompletedTask;
        private TaskCompletionSource<bool> callTerminatedTask;
        private TaskCompletionSource<bool> toneReceivedCompleteTask;
        private TaskCompletionSource<bool> transferToParticipantCompleteTask;
        private readonly int maxRetryAttemptCount = 3;

        public IncomingCallHandler(CallingServerClient callingServerClient, CallConfiguration callConfiguration)
        {
            this.callConfiguration = callConfiguration;
            this.callingServerClient = callingServerClient;
            targetParticipant = callConfiguration.targetParticipant;
        }

        public async Task Report(string incomingCallContext)
        {
            reportCancellationTokenSource = new CancellationTokenSource();
            reportCancellationToken = reportCancellationTokenSource.Token;

            try
            {
                // Answer Call
                var response = await callingServerClient.AnswerCallAsync(
                    incomingCallContext,
                    new List<CallMediaType> { CallMediaType.Audio },
                    new List<CallingEventSubscriptionType> { CallingEventSubscriptionType.ParticipantsUpdated,
                    CallingEventSubscriptionType.ToneReceived},
                    new Uri(callConfiguration.AppCallbackUrl));

                Logger.LogMessage(Logger.MessageType.INFORMATION, $"AnswerCallAsync Response -----> {response.GetRawResponse()}");

                callConnection = response.Value;
                RegisterToCallStateChangeEvent(callConnection.CallConnectionId);

                //Wait for the call to get connected
                await callEstablishedTask.Task.ConfigureAwait(false);

                RegisterToDtmfResultEvent(callConnection.CallConnectionId);

                await PlayAudioAsync().ConfigureAwait(false);
                var playAudioCompleted = await playAudioCompletedTask.Task.ConfigureAwait(false);

                if (!playAudioCompleted)
                {
                    await HangupAsync().ConfigureAwait(false);
                }
                else
                {
                    var toneReceivedComplete = await toneReceivedCompleteTask.Task.ConfigureAwait(false);
                    if (toneReceivedComplete)
                    {
                        string participant = targetParticipant;
                        Logger.LogMessage(Logger.MessageType.INFORMATION, $"Tranferring call to participant {participant}");
                        var transferToParticipantCompleted = await TransferToParticipant(participant);
                        if (!transferToParticipantCompleted)
                        {
                            await RetryTransferToParticipantAsync(async () => await TransferToParticipant(participant));
                        }
                    }
                    await HangupAsync().ConfigureAwait(false);
                }

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

        private async Task PlayAudioAsync()
        {
            try
            {
                // Preparing data for request
                var playAudioOptions = new PlayAudioOptions()
                {
                    CallbackUri = new Uri(callConfiguration.AppCallbackUrl),
                    OperationContext = Guid.NewGuid().ToString(),
                    Loop = true,
                };

                var response = await callConnection.PlayAudioAsync(new Uri(callConfiguration.AudioFileUrl),
                    playAudioOptions).ConfigureAwait(false);

                Logger.LogMessage(Logger.MessageType.INFORMATION, $"PlayAudioAsync response --> {response.GetRawResponse()}, Id: {response.Value.OperationId}, Status: {response.Value.Status}, OperationContext: {response.Value.OperationContext}, ResultInfo: {response.Value.ResultDetails}");

                if (response.Value.Status == CallingOperationStatus.Running)
                {
                    Logger.LogMessage(Logger.MessageType.INFORMATION, $"Play Audio state: {response.Value.Status}");
                    // listen to play audio events
                    RegisterToPlayAudioResultEvent(playAudioOptions.OperationContext);

                    var completedTask = await Task.WhenAny(playAudioCompletedTask.Task, Task.Delay(30 * 1000)).ConfigureAwait(false);

                    if (completedTask != playAudioCompletedTask.Task)
                    {
                        playAudioCompletedTask.TrySetResult(false);
                        toneReceivedCompleteTask.TrySetResult(false);
                    }
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
            if (reportCancellationToken.IsCancellationRequested)
            {
                Logger.LogMessage(Logger.MessageType.INFORMATION, "Cancellation request, Hangup will not be performed");
                return;
            }

            Logger.LogMessage(Logger.MessageType.INFORMATION, "Performing Hangup operation");
            var hangupResponse = await callConnection.HangupAsync(reportCancellationToken).ConfigureAwait(false);

            Logger.LogMessage(Logger.MessageType.INFORMATION, $"HangupAsync response --> {hangupResponse}");

        }

        private async Task CancelAllMediaOperations()
        {
            if (reportCancellationToken.IsCancellationRequested)
            {
                Logger.LogMessage(Logger.MessageType.INFORMATION, "Cancellation request, CancelMediaProcessing will not be performed");
                return;
            }

            Logger.LogMessage(Logger.MessageType.INFORMATION, "Performing cancel media processing operation to stop playing audio");

            var operationContext = Guid.NewGuid().ToString();
            var response = await callConnection.CancelAllMediaOperationsAsync(operationContext, reportCancellationToken).ConfigureAwait(false);

            Logger.LogMessage(Logger.MessageType.INFORMATION, $"PlayAudioAsync response --> {response.ContentStream}, " +
                $"Id: {response.Content}, Status: {response.Status}");
        }

        private void RegisterToCallStateChangeEvent(string callConnectionId)
        {
            callEstablishedTask = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            reportCancellationToken.Register(() => callEstablishedTask.TrySetCanceled());

            callTerminatedTask = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            //Set the callback method
            var callStateChangeNotificaiton = new NotificationCallback((CallingServerEventBase callEvent) =>
            {
                var callStateChanged = (CallConnectionStateChangedEvent)callEvent;

                Logger.LogMessage(Logger.MessageType.INFORMATION, $"Call State changed to: {callStateChanged.CallConnectionState}");

                if (callStateChanged.CallConnectionState == CallConnectionState.Connected)
                {
                    callEstablishedTask.TrySetResult(true);
                }
                else if (callStateChanged.CallConnectionState == CallConnectionState.Disconnected)
                {
                    EventDispatcher.Instance.Unsubscribe(CallingServerEventType.CallConnectionStateChangedEvent.ToString(), callConnectionId);
                    reportCancellationTokenSource.Cancel();
                    callTerminatedTask.SetResult(true);
                }
            });

            //Subscribe to the event
            var eventId = EventDispatcher.Instance.Subscribe(CallingServerEventType.CallConnectionStateChangedEvent.ToString(), callConnectionId, callStateChangeNotificaiton);
        }

        private void RegisterToPlayAudioResultEvent(string operationContext)
        {
            playAudioCompletedTask = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            reportCancellationToken.Register(() => playAudioCompletedTask.TrySetCanceled());

            var playPromptResponseNotification = new NotificationCallback((CallingServerEventBase callEvent) =>
            {
                Task.Run(() =>
                {
                    var playAudioResultEvent = (PlayAudioResultEvent)callEvent;
                    Logger.LogMessage(Logger.MessageType.INFORMATION, $"Play audio status: {playAudioResultEvent.Status}");

                    if (playAudioResultEvent.Status == CallingOperationStatus.Completed)
                    {
                        playAudioCompletedTask.TrySetResult(true);
                        EventDispatcher.Instance.Unsubscribe(CallingServerEventType.PlayAudioResultEvent.ToString(), operationContext);
                    }
                    else if (playAudioResultEvent.Status == CallingOperationStatus.Failed)
                    {
                        playAudioCompletedTask.TrySetResult(false);
                    }
                });
            });

            //Subscribe to event
            EventDispatcher.Instance.Subscribe(CallingServerEventType.PlayAudioResultEvent.ToString(), operationContext, playPromptResponseNotification);
        }

        private void RegisterToDtmfResultEvent(string callConnectionId)
        {
            toneReceivedCompleteTask = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var dtmfReceivedEvent = new NotificationCallback((CallingServerEventBase callEvent) =>
            {
                Task.Run(async () =>
                {
                    var toneReceivedEvent = (ToneReceivedEvent)callEvent;
                    Logger.LogMessage(Logger.MessageType.INFORMATION, $"Tone received ---------> : {toneReceivedEvent.ToneInfo?.Tone}");

                    if (toneReceivedEvent?.ToneInfo?.Tone == ToneValue.Tone1)
                    {
                        toneReceivedCompleteTask.TrySetResult(true);
                    }
                    else
                    {
                        toneReceivedCompleteTask.TrySetResult(false);
                    }

                    EventDispatcher.Instance.Unsubscribe(CallingServerEventType.ToneReceivedEvent.ToString(), callConnectionId);
                    // cancel playing audio
                    await CancelAllMediaOperations().ConfigureAwait(false);
                });
            });
            //Subscribe to event
            EventDispatcher.Instance.Subscribe(CallingServerEventType.ToneReceivedEvent.ToString(), callConnectionId, dtmfReceivedEvent);
        }

        private async Task<bool> TransferToParticipant(string targetParticipant)
        {
            transferToParticipantCompleteTask = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            var identifierKind = GetIdentifierKind(targetParticipant);

            if (identifierKind == CommunicationIdentifierKind.UnknownIdentity)
            {
                Logger.LogMessage(Logger.MessageType.INFORMATION, "Unknown identity provided. Enter valid phone number or communication user id");
                transferToParticipantCompleteTask.TrySetResult(true);
            }
            else
            {
                var operationContext = Guid.NewGuid().ToString();

                RegisterToTransferParticipantsResultEvent(operationContext);

                if (identifierKind == CommunicationIdentifierKind.UserIdentity)
                {
                    var response = await callConnection.TransferToParticipantAsync(new CommunicationUserIdentifier(targetParticipant), null, null, operationContext).ConfigureAwait(false);
                    Logger.LogMessage(Logger.MessageType.INFORMATION, $"TransferParticipantAsync response --> {response.GetRawResponse()}, status: {response.Value.Status}  " +
                        $"OperationContext: {response.Value.OperationContext}, OperationId: {response.Value.OperationId}, ResultDetails: {response.Value.ResultDetails}");
                }
                else if (identifierKind == CommunicationIdentifierKind.PhoneIdentity)
                {
                    var response = await callConnection.TransferToParticipantAsync(new PhoneNumberIdentifier(targetParticipant), null, null, operationContext).ConfigureAwait(false);
                    Logger.LogMessage(Logger.MessageType.INFORMATION, $"TransferParticipantAsync response --> {response}");
                }
            }

            var transferToParticipantCompleted = await transferToParticipantCompleteTask.Task.ConfigureAwait(false);
            return transferToParticipantCompleted;
        }

        private void RegisterToTransferParticipantsResultEvent(string operationContext)
        {
            var transferToParticipantReceivedEvent = new NotificationCallback(async (CallingServerEventBase callEvent) =>
            {
                var transferParticipantUpdatedEvent = (ParticipantsUpdatedEvent)callEvent;
                if (transferParticipantUpdatedEvent.CallConnectionId != null)
                {
                    Logger.LogMessage(Logger.MessageType.INFORMATION, $"Transfer participant callconnection ID - {transferParticipantUpdatedEvent.CallConnectionId}");
                    EventDispatcher.Instance.Unsubscribe(CallingServerEventType.ParticipantsUpdatedEvent.ToString(), operationContext);

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
            EventDispatcher.Instance.Subscribe(CallingServerEventType.ParticipantsUpdatedEvent.ToString(), operationContext, transferToParticipantReceivedEvent);
        }

        private CommunicationIdentifierKind GetIdentifierKind(string participantnumber)
        {
            //checks the identity type returns as string
            return Regex.Match(participantnumber, Constants.userIdentityRegex, RegexOptions.IgnoreCase).Success ? CommunicationIdentifierKind.UserIdentity :
                   Regex.Match(participantnumber, Constants.phoneIdentityRegex, RegexOptions.IgnoreCase).Success ? CommunicationIdentifierKind.PhoneIdentity :
                   CommunicationIdentifierKind.UnknownIdentity;
        }
    }
}
