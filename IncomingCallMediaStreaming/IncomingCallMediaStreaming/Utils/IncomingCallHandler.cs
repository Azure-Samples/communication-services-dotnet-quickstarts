// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace IncomingCallMediaStreaming
{
    /// <summary>
    /// Handling different callback events
    /// and perform operations
    /// </summary>

    using Azure.Communication.CallAutomation;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class IncomingCallHandler
    {
        private CallAutomationClient callAutomationClient;
        private CallConfiguration callConfiguration;
        private CallConnection callConnection;
        private CancellationTokenSource reportCancellationTokenSource;
        private CancellationToken reportCancellationToken;

        private TaskCompletionSource<bool> callEstablishedTask;
        private TaskCompletionSource<bool> callTerminatedTask;

        public IncomingCallHandler(CallAutomationClient callAutomationClient, CallConfiguration callConfiguration)
        {
            this.callConfiguration = callConfiguration;
            this.callAutomationClient = callAutomationClient;
        }

        public async Task Report(string incomingCallContext)
        {
            reportCancellationTokenSource = new CancellationTokenSource();
            reportCancellationToken = reportCancellationTokenSource.Token;

            try
            {
                AnswerCallOptions answerCallOptions = new AnswerCallOptions(incomingCallContext,
                    new Uri(callConfiguration.AppCallbackUrl));

                answerCallOptions.MediaStreamingOptions = new MediaStreamingOptions
                    (new Uri(callConfiguration.MediaStreamingTransportURI),
                    MediaStreamingTransport.Websocket, 
                    MediaStreamingContent.Audio, 
                    MediaStreamingAudioChannel.Unmixed);

                // Answer Call
                var response = await callAutomationClient.AnswerCallAsync(answerCallOptions);

                Logger.LogMessage(Logger.MessageType.INFORMATION, $"AnswerCallAsync Response -----> {response.GetRawResponse()}");

                callConnection = response.Value.CallConnection;
                RegisterToCallStateChangeEvent(callConnection.CallConnectionId);

                //Wait for the call to get connected
                await callEstablishedTask.Task.ConfigureAwait(false);

                // Wait for the call to terminate
                await callTerminatedTask.Task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogMessage(Logger.MessageType.ERROR, $"Call ended unexpectedly, reason: {ex.Message}");
            }
        }

        private void RegisterToCallStateChangeEvent(string callConnectionId)
        {
            callEstablishedTask = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            reportCancellationToken.Register(() => callEstablishedTask.TrySetCanceled());

            callTerminatedTask = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

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
    }
}
