// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Communication.CallingServer.Sample.CallPlayAudio
{
    using Azure.Communication;
    using Azure.Communication.CallingServer;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public class CallPlayTerminate
    {
        private CallConfiguration callConfiguration;
        private CallingServerClient callClient;
        private CallConnection callConnection;
        private CancellationTokenSource reportCancellationTokenSource;
        private CancellationToken reportCancellationToken;

        private TaskCompletionSource<bool> callEstablishedTask;
        private TaskCompletionSource<bool> callTerminatedTask;
        public CallPlayTerminate(CallConfiguration callConfiguration)
        {
            this.callConfiguration = callConfiguration;
            callClient = new CallingServerClient(this.callConfiguration.ConnectionString);
        }

        public async Task Report(string targetPhoneNumber)
        {
            reportCancellationTokenSource = new CancellationTokenSource();
            reportCancellationToken = reportCancellationTokenSource.Token;

            try
            {
                callConnection = await CreateCallAsync(targetPhoneNumber).ConfigureAwait(false);

                await PlayAudioAsync().ConfigureAwait(false);
              
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
                var source = new CommunicationUserIdentifier(callConfiguration.SourceIdentity);
                var target = new PhoneNumberIdentifier(targetPhoneNumber);
                var createCallOption = new CreateCallOptions(
                    new Uri(callConfiguration.AppCallbackUrl),
                    new List<MediaType> { MediaType.Audio },
                    new List<EventSubscriptionType> { EventSubscriptionType.ParticipantsUpdated }
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
                }

                // wait for 10 seconds and then terminate the call              
                await Task.Delay(10 * 1000);

                await HangupAsync().ConfigureAwait(false);
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

        private void RegisterToCallStateChangeEvent(string callConnectionId)
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
                    EventDispatcher.Instance.Unsubscribe(CallingServerEventType.CallConnectionStateChangedEvent.ToString(), callConnectionId);
                    reportCancellationTokenSource.Cancel();
                    callTerminatedTask.SetResult(true);
                }
            });

            //Subscribe to the event
            var eventId = EventDispatcher.Instance.Subscribe(CallingServerEventType.CallConnectionStateChangedEvent.ToString(), callConnectionId, callStateChangeNotificaiton);
        }
    }
}
