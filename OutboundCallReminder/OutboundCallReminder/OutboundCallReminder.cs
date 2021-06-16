// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Communication.Server.Calling.Sample.OutboundCallReminder
{
    using Azure.Communication;
    using Azure.Communication.CallingServer;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    public class OutboundCallReminder
    {
        private CallConfiguration callConfiguration;
        private CallingServerClient callClient;
        private CallConnection callConnection;
        private CancellationTokenSource reportCancellationTokenSource;
        private CancellationToken reportCancellationToken;

        private TaskCompletionSource<bool> callEstablishedTask;
        private TaskCompletionSource<bool> playAudioCompletedTask;
        private TaskCompletionSource<bool> callTerminatedTask;
        private TaskCompletionSource<bool> toneReceivedCompleteTask;
        private TaskCompletionSource<bool> addParticipantCompleteTask;
        private readonly int maxRetryAttemptCount = Convert.ToInt32(ConfigurationManager.AppSettings["MaxRetryCount"]);

        public OutboundCallReminder(CallConfiguration callConfiguration)
        {
            this.callConfiguration = callConfiguration;
            callClient = new CallingServerClient(this.callConfiguration.ConnectionString);
        }

        public async Task Report(string targetPhoneNumber, string participant)
        {
            reportCancellationTokenSource = new CancellationTokenSource();
            reportCancellationToken = reportCancellationTokenSource.Token;

            try
            {
                callConnection = await CreateCallAsync(targetPhoneNumber).ConfigureAwait(false);
                RegisterToDtmfResultEvent(callConnection.CallConnectionId);

                await PlayAudioAsync(callConnection.CallConnectionId).ConfigureAwait(false);
                var playAudioCompleted = await playAudioCompletedTask.Task.ConfigureAwait(false);

                if (!playAudioCompleted)
                {
                    await HangupAsync(callConnection.CallConnectionId).ConfigureAwait(false);
                }
                else
                {
                    var toneReceivedComplete = await toneReceivedCompleteTask.Task.ConfigureAwait(false);
                    if (toneReceivedComplete)
                    {
                        Logger.LogMessage(Logger.MessageType.INFORMATION, $"Initiating add participant from number {targetPhoneNumber} and participant identifier is {participant}");
                        var addParticipantCompleted = await AddParticipant(callConnection.CallConnectionId, participant);
                        if (!addParticipantCompleted)
                        {
                            await RetryAddParticipantAsync(async () => await AddParticipant(callConnection.CallConnectionId, participant));
                        }

                        await HangupAsync(callConnection.CallConnectionId).ConfigureAwait(false);
                    }
                    else
                    {
                        await HangupAsync(callConnection.CallConnectionId).ConfigureAwait(false);
                    }
                }

                // Wait for the call to terminate
                await callTerminatedTask.Task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogMessage(Logger.MessageType.ERROR, $"Call ended unexpectedly, reason: {ex.Message}");
            }
        }

        private async Task RetryAddParticipantAsync(Func<Task<bool>> action)
        {
            int retryAttemptCount = 1;
            while (retryAttemptCount <= maxRetryAttemptCount)
            {
                Logger.LogMessage(Logger.MessageType.INFORMATION, $"Retrying add participant attempt {retryAttemptCount} is in progress");
                var addParticipantResult = await action();
                if (addParticipantResult)
                {
                    return;
                }
                else
                {
                    Logger.LogMessage(Logger.MessageType.INFORMATION, $"Retry add participant attempt {retryAttemptCount} has failed");
                    retryAttemptCount++;
                }
            }
        }

        private async Task<CallConnection> CreateCallAsync(string targetPhoneNumber)
        {
            try
            {
                //Preparting request data
                var source = new CommunicationUserIdentifier(callConfiguration.SourceIdentity);
                var target = new PhoneNumberIdentifier(targetPhoneNumber);
                var createCallOption = new CreateCallOptions(
                    new Uri(callConfiguration.AppCallbackUrl),
                    new List<MediaType> { MediaType.Audio },
                    new List<EventSubscriptionType> { EventSubscriptionType.ParticipantsUpdated, EventSubscriptionType.DtmfReceived }
                    );
                createCallOption.AlternateCallerId = new PhoneNumberIdentifier(callConfiguration.SourcePhoneNumber);

                Logger.LogMessage(Logger.MessageType.INFORMATION, "Performing CreateCall operation");
                var call = await callClient.CreateCallConnectionAsync(source,
                    new List<CommunicationIdentifier>() { target },
                    createCallOption, reportCancellationToken)
                    .ConfigureAwait(false);

                Logger.LogMessage(Logger.MessageType.INFORMATION, $"CreateCallConnectionAsync response --> {call.GetRawResponse()}, Call Connection Id: { call.Value.CallConnectionId}");

                Logger.LogMessage(Logger.MessageType.INFORMATION, $"Call initiated with Call Connection id: { call.Value.CallConnectionId}");

                RegisterToCallStateChangeEvent(call.Value.CallConnectionId);

                //Wait for operation to complete
                await callEstablishedTask.Task.ConfigureAwait(false);

                return call.Value;
            }
            catch (Exception ex)
            {
                Logger.LogMessage(Logger.MessageType.ERROR, string.Format("Failure occured while creating/establishing the call. Exception: {0}", ex.Message));
                throw ex;
            }
        }

        private async Task PlayAudioAsync(string callLegId)
        {
            if (reportCancellationToken.IsCancellationRequested)
            {
                Logger.LogMessage(Logger.MessageType.INFORMATION, "Cancellation request, PlayAudio will not be performed");
                return;
            }

            try
            {
                // Preparing data for request
                var playAudioRequest = new PlayAudioOptions()
                {
                    AudioFileUri = new Uri(callConfiguration.AudioFileUrl),
                    OperationContext = Guid.NewGuid().ToString(),
                    Loop = true,
                };

                Logger.LogMessage(Logger.MessageType.INFORMATION, "Performing PlayAudio operation");
                var response = await callConnection.PlayAudioAsync(playAudioRequest, reportCancellationToken).ConfigureAwait(false);

                Logger.LogMessage(Logger.MessageType.INFORMATION, $"PlayAudioAsync response --> {response.GetRawResponse()}, Id: {response.Value.OperationId}, Status: {response.Value.Status}, OperationContext: {response.Value.OperationContext}, ResultInfo: {response.Value.ResultInfo}");

                if (response.Value.Status == OperationStatus.Running)
                {
                    Logger.LogMessage(Logger.MessageType.INFORMATION, $"Play Audio state: {response.Value.Status}");
                    // listen to play audio events
                    RegisterToPlayAudioResultEvent(playAudioRequest.OperationContext);

                    var completedTask = await Task.WhenAny(playAudioCompletedTask.Task, Task.Delay(30 * 1000)).ConfigureAwait(false);

                    if (completedTask != playAudioCompletedTask.Task)
                    {
                        Logger.LogMessage(Logger.MessageType.INFORMATION, "No response from user in 30 sec, initiating hangup");
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

        private async Task HangupAsync(string callLegId)
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

        private async Task CancelMediaProcessing(string callLegId)
        {
            if (reportCancellationToken.IsCancellationRequested)
            {
                Logger.LogMessage(Logger.MessageType.INFORMATION, "Cancellation request, CancelMediaProcessing will not be performed");
                return;
            }

            Logger.LogMessage(Logger.MessageType.INFORMATION, "Performing cancel media processing operation to stop playing audio");

            var operationContext = Guid.NewGuid().ToString();
            var response = await callConnection.CancelAllMediaOperationsAsync(operationContext, reportCancellationToken).ConfigureAwait(false);

            Logger.LogMessage(Logger.MessageType.INFORMATION, $"PlayAudioAsync response --> {response.GetRawResponse()}, Id: {response.Value.OperationId}, Status: {response.Value.Status}, OperationContext: {response.Value.OperationContext}, ResultInfo: {response.Value.ResultInfo}");
        }

        private void RegisterToCallStateChangeEvent(string callLegId)
        {
            callEstablishedTask = new TaskCompletionSource<bool>(TaskContinuationOptions.RunContinuationsAsynchronously);
            reportCancellationToken.Register(() => callEstablishedTask.TrySetCanceled());

            callTerminatedTask = new TaskCompletionSource<bool>(TaskContinuationOptions.RunContinuationsAsynchronously);

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
                    EventDispatcher.Instance.Unsubscribe(CallingServerEventType.CallConnectionStateChangedEvent.ToString(), callLegId);
                    reportCancellationTokenSource.Cancel();
                    callTerminatedTask.SetResult(true);
                }
            });

            //Subscribe to the event
            var eventId = EventDispatcher.Instance.Subscribe(CallingServerEventType.CallConnectionStateChangedEvent.ToString(), callLegId, callStateChangeNotificaiton);
        }

        private void RegisterToPlayAudioResultEvent(string operationContext)
        {
            playAudioCompletedTask = new TaskCompletionSource<bool>(TaskContinuationOptions.RunContinuationsAsynchronously);
            reportCancellationToken.Register(() => playAudioCompletedTask.TrySetCanceled());

            var playPromptResponseNotification = new NotificationCallback((CallingServerEventBase callEvent) =>
            {
                Task.Run(() =>
                {
                    var playAudioResultEvent = (PlayAudioResultEvent)callEvent;
                    Logger.LogMessage(Logger.MessageType.INFORMATION, $"Play audio status: {playAudioResultEvent.Status}");

                    if (playAudioResultEvent.Status == OperationStatus.Completed)
                    {
                        playAudioCompletedTask.TrySetResult(true);
                        EventDispatcher.Instance.Unsubscribe(CallingServerEventType.PlayAudioResultEvent.ToString(), operationContext);
                    }
                    else if (playAudioResultEvent.Status == OperationStatus.Failed)
                    {
                        playAudioCompletedTask.TrySetResult(false);
                    }
                });
            });

            //Subscribe to event
            EventDispatcher.Instance.Subscribe(CallingServerEventType.PlayAudioResultEvent.ToString(), operationContext, playPromptResponseNotification);
        }

        private void RegisterToDtmfResultEvent(string callLegId)
        {
            toneReceivedCompleteTask = new TaskCompletionSource<bool>(TaskContinuationOptions.RunContinuationsAsynchronously);
            var dtmfReceivedEvent = new NotificationCallback((CallingServerEventBase callEvent) =>
            {
                Task.Run(async () =>
                {
                    var toneReceivedEvent = (ToneReceivedEvent)callEvent;
                    Logger.LogMessage(Logger.MessageType.INFORMATION, $"Tone received --------- : {toneReceivedEvent.ToneInfo?.Tone}");

                    if (toneReceivedEvent?.ToneInfo?.Tone == ToneValue.Tone1)
                    {
                        toneReceivedCompleteTask.TrySetResult(true);
                    }
                    else
                    {
                        toneReceivedCompleteTask.TrySetResult(false);
                    }

                    EventDispatcher.Instance.Unsubscribe(CallingServerEventType.ToneReceivedEvent.ToString(), callLegId);
                    // cancel playing audio
                    await CancelMediaProcessing(callLegId).ConfigureAwait(false);
                });
            });
            //Subscribe to event
            EventDispatcher.Instance.Subscribe(CallingServerEventType.ToneReceivedEvent.ToString(), callLegId, dtmfReceivedEvent);
        }

        private async Task<bool> AddParticipant(string callLegId, string addedParticipant)
        {
            addParticipantCompleteTask = new TaskCompletionSource<bool>(TaskContinuationOptions.RunContinuationsAsynchronously);

            var identifierKind = GetIdentifierKind(addedParticipant);

            if (identifierKind == CommunicationIdentifierKind.UnknownIdentity)
            {
                Logger.LogMessage(Logger.MessageType.INFORMATION, "Unknown identity provided. Enter valid phone number or communication user id");
                addParticipantCompleteTask.TrySetResult(true);
            }
            else
            {
                var operationContext = Guid.NewGuid().ToString();
                var alternartCallerid = new PhoneNumberIdentifier(ConfigurationManager.AppSettings["SourcePhone"]).ToString();

                RegisterToAddParticipantsResultEvent(operationContext);

                if (identifierKind == CommunicationIdentifierKind.UserIdentity)
                {
                    var response = await callConnection.AddParticipantAsync(new CommunicationUserIdentifier(addedParticipant), alternartCallerid, operationContext).ConfigureAwait(false);
                    Logger.LogMessage(Logger.MessageType.INFORMATION, $"PlayAudioAsync response --> {response}");
                }
                else if (identifierKind == CommunicationIdentifierKind.PhoneIdentity)
                {
                    var response = await callConnection.AddParticipantAsync(new PhoneNumberIdentifier(addedParticipant), alternartCallerid, operationContext).ConfigureAwait(false);
                    Logger.LogMessage(Logger.MessageType.INFORMATION, $"PlayAudioAsync response --> {response}");
                }
            }

            var addParticipantCompleted = await addParticipantCompleteTask.Task.ConfigureAwait(false);
            return addParticipantCompleted;
        }

        private void RegisterToAddParticipantsResultEvent(string operationContext)
        {
            var addParticipantReceivedEvent = new NotificationCallback((CallingServerEventBase callEvent) =>
            {
                var addParticipantUpdatedEvent = (AddParticipantResultEvent)callEvent;
                if (addParticipantUpdatedEvent.Status == "Completed")
                {
                    Logger.LogMessage(Logger.MessageType.INFORMATION, $"Add participant status - {addParticipantUpdatedEvent.Status}");
                    EventDispatcher.Instance.Unsubscribe(CallingServerEventType.AddParticipantResultEvent.ToString(), operationContext);
                    addParticipantCompleteTask.TrySetResult(true);
                }
                else if (addParticipantUpdatedEvent.Status == "Failed")
                {
                    addParticipantCompleteTask.TrySetResult(false);
                }
            });

            //Subscribe to event
            EventDispatcher.Instance.Subscribe(CallingServerEventType.AddParticipantResultEvent.ToString(), operationContext, addParticipantReceivedEvent);
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
