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
        private TaskCompletionSource<bool> inviteParticipantCompleteTask;
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
                        Console.WriteLine($"Initiating invite participant from number {targetPhoneNumber} and participant identifier is {participant}");

                        var inviteParticipantCompleted = await InviteParticipant(callConnection.CallConnectionId, participant);
                        if (!inviteParticipantCompleted)
                        {
                            await RetryInviteParticipantAsync(async () => await InviteParticipant(callConnection.CallConnectionId, participant));
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
                Console.WriteLine("Call ended unexpectedly, reason: " + ex.Message);
            }
        }

        private async Task RetryInviteParticipantAsync(Func<Task<bool>> action)
        {
            int retryAttemptCount = 1;
            while (retryAttemptCount <= maxRetryAttemptCount)
            {
                Console.WriteLine($"Retrying invite participant attempt {retryAttemptCount} is in progress");
                var inviteParticipantResult = await action();
                if (inviteParticipantResult)
                {
                    return;
                }
                else
                {
                    Console.WriteLine($"Retry invite participant attempt {retryAttemptCount} has failed");
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
                    new List<CallModality> { CallModality.Audio },
                    new List<EventSubscriptionType> { EventSubscriptionType.ParticipantsUpdated, EventSubscriptionType.DtmfReceived }
                    );
                createCallOption.AlternateCallerId = new PhoneNumberIdentifier(callConfiguration.SourcePhoneNumber);

                Console.WriteLine("Performing CreateCall operation");
                var call = await callClient.CreateCallConnectionAsync(source, 
                    new List<CommunicationIdentifier>() { target }, 
                    createCallOption, reportCancellationToken)
                    .ConfigureAwait(false);

                Console.WriteLine("Call initiated with Call Leg id: {0}", call.Value.CallConnectionId);

                RegisterToCallStateChangeEvent(call.Value.CallConnectionId);

                //Wait for operation to complete
                await callEstablishedTask.Task.ConfigureAwait(false);

                return call.Value;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failure occured while creating/establishing the call. Exception: {0}", ex.Message);
                throw ex;
            }
        }

        private async Task PlayAudioAsync(string callLegId)
        {
            if (reportCancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("Cancellation request, PlayAudio will not be performed");
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

                Console.WriteLine("Performing PlayAudio operation");
                var response = await callConnection.PlayAudioAsync(playAudioRequest, reportCancellationToken).ConfigureAwait(false);

                if (response.Value.Status == OperationStatus.Running)
                {
                    Console.WriteLine("Play Audio state: {0}", response.Value.Status);

                    // listen to play audio events
                    RegisterToPlayAudioResultEvent(playAudioRequest.OperationContext);

                    var completedTask = await Task.WhenAny(playAudioCompletedTask.Task, Task.Delay(30 * 1000)).ConfigureAwait(false);

                    if (completedTask != playAudioCompletedTask.Task)
                    {
                        Console.WriteLine("No response from user in 30 sec, initiating hangup");
                        playAudioCompletedTask.TrySetResult(false);
                        toneReceivedCompleteTask.TrySetResult(false);
                    }
                }
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Play audio operation cancelled");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failure occured while playing audio on the call. Exception: {0}", ex.Message);
            }
        }

        private async Task HangupAsync(string callLegId)
        {
            if (reportCancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("Cancellation request, Hangup will not be performed");
                return;
            }

            Console.WriteLine("Performing Hangup operation");
            await callConnection.HangupAsync(reportCancellationToken).ConfigureAwait(false);
        }

        private async Task CancelMediaProcessing(string callLegId)
        {
            if (reportCancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("Cancellation request, CancelMediaProcessing will not be performed");
                return;
            }

            Console.WriteLine("Performing cancel media processing operation to stop playing audio");

            var operationContext = Guid.NewGuid().ToString();
            await callConnection.CancelAllMediaOperationsAsync(operationContext, reportCancellationToken).ConfigureAwait(false);
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

                Console.WriteLine("Call State changed to: {0}", callStateChanged.CallConnectionState);

                if (callStateChanged.CallConnectionState == CallConnectionState.Established)
                {
                    callEstablishedTask.TrySetResult(true);
                }
                else if (callStateChanged.CallConnectionState == CallConnectionState.Terminated)
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
                    Console.WriteLine("Play audio status: {0}", playAudioResultEvent.Status);

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
                    Console.WriteLine("Tone received --------- : {0}", toneReceivedEvent.ToneInfo?.Tone);

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

        private async Task<bool> InviteParticipant(string callLegId, string invitedParticipant)
        {
            inviteParticipantCompleteTask = new TaskCompletionSource<bool>(TaskContinuationOptions.RunContinuationsAsynchronously);
          
            var identifierKind = GetIdentifierKind(invitedParticipant);

            if (identifierKind == CommunicationIdentifierKind.UnknownIdentity)
            {
                Console.WriteLine("Unknown identity provided. Enter valid phone number or communication user id");
                inviteParticipantCompleteTask.TrySetResult(true);
            }
            else
            {
                var operationContext = Guid.NewGuid().ToString();
                var alternartCallerid = new PhoneNumberIdentifier(ConfigurationManager.AppSettings["SourcePhone"]).ToString();

                RegisterToInviteParticipantsResultEvent(operationContext);

                if (identifierKind == CommunicationIdentifierKind.UserIdentity)
                {
                    await callConnection.AddParticipantAsync(new CommunicationUserIdentifier(invitedParticipant), alternartCallerid, operationContext).ConfigureAwait(false);
                }
                else if (identifierKind == CommunicationIdentifierKind.PhoneIdentity)
                {
                    await callConnection.AddParticipantAsync(new PhoneNumberIdentifier(invitedParticipant), alternartCallerid, operationContext).ConfigureAwait(false);
                }
            }

            var inviteParticipantCompleted = await inviteParticipantCompleteTask.Task.ConfigureAwait(false);
            return inviteParticipantCompleted;
        }

        private void RegisterToInviteParticipantsResultEvent(string operationContext)
        {
            var inviteParticipantReceivedEvent = new NotificationCallback((CallingServerEventBase callEvent) =>
            {
                var inviteParticipantsUpdatedEvent = (InviteParticipantsResultEvent)callEvent;
                if (inviteParticipantsUpdatedEvent.Status == "Completed")
                {
                    Console.WriteLine($"Invite participant status - {inviteParticipantsUpdatedEvent.Status}");
                    EventDispatcher.Instance.Unsubscribe(CallingServerEventType.InviteParticipantsResultEvent.ToString(), operationContext);
                    inviteParticipantCompleteTask.TrySetResult(true);
                }
                else if (inviteParticipantsUpdatedEvent.Status == "Failed")
                {
                    inviteParticipantCompleteTask.TrySetResult(false);
                }
            });

            //Subscribe to event
            EventDispatcher.Instance.Subscribe(CallingServerEventType.InviteParticipantsResultEvent.ToString(), operationContext, inviteParticipantReceivedEvent);
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
