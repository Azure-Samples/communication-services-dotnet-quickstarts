// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Communication.CallingServer.Sample.CallPlayAudio
{
    using Azure.Communication;
    using Azure.Communication.CallAutomation;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public class CallPlayTerminate
    {
        private CallConfiguration callConfiguration;
        private CallAutomationClient callClient;
        private CallConnection callConnection;
        private CancellationTokenSource reportCancellationTokenSource;
        private CancellationToken reportCancellationToken;

        private TaskCompletionSource<bool> callEstablishedTask;
        private TaskCompletionSource<bool> playAudioCompletedTask;
        private TaskCompletionSource<bool> callTerminatedTask;
        public CallPlayTerminate(CallConfiguration callConfiguration)
        {
            this.callConfiguration = callConfiguration;
            callClient = new CallAutomationClient(this.callConfiguration.ConnectionString);
        }

        public async Task Report(string targetPhoneNumber)
        {
            reportCancellationTokenSource = new CancellationTokenSource();
            reportCancellationToken = reportCancellationTokenSource.Token;

            try
            {
                callConnection = await CreateCallAsync(targetPhoneNumber).ConfigureAwait(false);

                RegisterToPlayAudioResultEvent(callConnection.CallConnectionId);

                await PlayAudioAsync().ConfigureAwait(false);
              
                // Wait for the call to play audio
                await playAudioCompletedTask.Task.ConfigureAwait(false);

                // Hang up the call
                await HangupAsync().ConfigureAwait(false);

                // Wait for the call to terminate
                await callTerminatedTask.Task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogMessage(Logger.MessageType.ERROR, $"Call ended unexpectedly, reason: {ex.Message}");
            }
        }

        private async Task<CallConnection> CreateCallAsync(string targetPhoneNumber)
        {
            try
            {
                //Preparting request data
                CallSource source = new CallSource(new CommunicationUserIdentifier(callConfiguration.SourceIdentity));
                source.CallerId = new PhoneNumberIdentifier(callConfiguration.SourcePhoneNumber);
                var target = new PhoneNumberIdentifier(targetPhoneNumber);

                var createCallOption = new CreateCallOptions(source,
                    new List<CommunicationIdentifier>() { target },
                    new Uri(callConfiguration.AppCallbackUrl));

                Logger.LogMessage(Logger.MessageType.INFORMATION, "Performing CreateCall operation");
                var call = await callClient.CreateCallAsync(createCallOption).ConfigureAwait(false);

                Logger.LogMessage(Logger.MessageType.INFORMATION, $"CreateCallConnectionAsync response --> {call.GetRawResponse()}, Call Connection Id: { call.Value.CallConnection.CallConnectionId}");

                Logger.LogMessage(Logger.MessageType.INFORMATION, $"Call initiated with Call Connection id: { call.Value.CallConnection.CallConnectionId}");

                RegisterToCallStateChangeEvent(call.Value.CallConnection.CallConnectionId);

                //Wait for operation to complete
                await callEstablishedTask.Task.ConfigureAwait(false);

                return call.Value.CallConnection;
            }
            catch (Exception ex)
            {
                Logger.LogMessage(Logger.MessageType.ERROR, string.Format("Failure occured while creating/establishing the call. Exception: {0}", ex.Message));
                throw ex;
            }
        }

        private async Task PlayAudioAsync()
        {
            if (reportCancellationToken.IsCancellationRequested)
            {
                Logger.LogMessage(Logger.MessageType.INFORMATION, "Cancellation request, PlayAudio will not be performed");
                return;
            }

            try
            {
                // Preparing data for request
                var playAudioRequest = new PlayOptions()
                {
                    OperationContext = Guid.NewGuid().ToString(),
                    Loop = false,
                };

                Logger.LogMessage(Logger.MessageType.INFORMATION, "Performing PlayAudio operation");
                PlaySource audioFileUri = new FileSource(new Uri(callConfiguration.AudioFileUrl));
                var response = await callConnection.GetCallMedia().PlayToAllAsync(audioFileUri, playAudioRequest,
                    reportCancellationToken).ConfigureAwait(false);

                Logger.LogMessage(Logger.MessageType.INFORMATION, $"PlayAudioAsync response --> " +
                   $"{response}, Id: {response.ClientRequestId}, Status: {response.Status}");

                if (response.Status == 202)
                {
                    Logger.LogMessage(Logger.MessageType.INFORMATION, $"Play Audio state: {response.Status}");
                }
            }
            catch (TaskCanceledException)
            {
                Logger.LogMessage(Logger.MessageType.ERROR, "Play audio operation canceled");
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
            var hangupResponse = await callConnection.HangUpAsync(true).ConfigureAwait(false);

            Logger.LogMessage(Logger.MessageType.INFORMATION, $"HangupAsync response --> {hangupResponse}");
        }

        private void RegisterToCallStateChangeEvent(string callConnectionId)
        {
            callEstablishedTask = new TaskCompletionSource<bool>(TaskContinuationOptions.RunContinuationsAsynchronously);
            reportCancellationToken.Register(() => callEstablishedTask.TrySetCanceled());

            callTerminatedTask = new TaskCompletionSource<bool>(TaskContinuationOptions.RunContinuationsAsynchronously);

            //Set the callback method for call connected
            var callConnectedNotificaiton = new NotificationCallback((callEvent) =>
            {
                Logger.LogMessage(Logger.MessageType.INFORMATION, $"Call State changed to Connected");
                EventDispatcher.Instance.Unsubscribe("CallConnected", callConnectionId);
                callEstablishedTask.TrySetResult(true);
            });

            //Set the callback method for call Disconnected
            var callDisconnectedNotificaiton = new NotificationCallback((callEvent) =>
            {
                Logger.LogMessage(Logger.MessageType.INFORMATION, $"Call State changed to Disconnected");
                EventDispatcher.Instance.Unsubscribe("CallDisconnected", callConnectionId);
                reportCancellationTokenSource.Cancel();
                callTerminatedTask.SetResult(true);
            });

            //Subscribe to the call connected event
            var eventId = EventDispatcher.Instance.Subscribe("CallConnected", callConnectionId, callConnectedNotificaiton);

            //Subscribe to the call disconnected event
            var eventIdDisconnected = EventDispatcher.Instance.Subscribe("CallDisconnected", callConnectionId, callDisconnectedNotificaiton);
        }

        private void RegisterToPlayAudioResultEvent(string callConnectionId)
        {
            playAudioCompletedTask = new TaskCompletionSource<bool>(TaskContinuationOptions.RunContinuationsAsynchronously);
            reportCancellationToken.Register(() => playAudioCompletedTask.TrySetCanceled());

            var playCompletedNotification = new NotificationCallback((callEvent) =>
            {
                Task.Run(() =>
                {
                    Logger.LogMessage(Logger.MessageType.INFORMATION, $"Play audio status: Completed");
                    playAudioCompletedTask.TrySetResult(true);
                    EventDispatcher.Instance.Unsubscribe("PlayCompleted", callConnectionId);
                });
            });

            var playFailedNotification = new NotificationCallback((callEvent) =>
            {
                Task.Run(() =>
                {
                    Logger.LogMessage(Logger.MessageType.INFORMATION, $"Play audio status: Failed");
                    playAudioCompletedTask.TrySetResult(false);
                    EventDispatcher.Instance.Unsubscribe("PlayFailed", callConnectionId);
                });
            });

            var playCanceledNotification = new NotificationCallback((callEvent) =>
            {
                Task.Run(() =>
                {
                    Logger.LogMessage(Logger.MessageType.INFORMATION, $"Play audio status: Canceled");
                    playAudioCompletedTask.TrySetResult(false);
                    EventDispatcher.Instance.Unsubscribe("PlayCanceled", callConnectionId);
                });
            });

            //Subscribe to event
            EventDispatcher.Instance.Subscribe("PlayCompleted", callConnectionId, playCompletedNotification);
            EventDispatcher.Instance.Subscribe("PlayFailed", callConnectionId, playFailedNotification);
            EventDispatcher.Instance.Subscribe("PlayCanceled", callConnectionId, playCanceledNotification);
        }
    }
}
