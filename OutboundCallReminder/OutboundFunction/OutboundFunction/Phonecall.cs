using System;
using System.Collections.Generic;
using Azure.Communication;
using Azure.Communication.CallingServer;
using Azure.Communication.Identity;
using System.Threading;
using System.Threading.Tasks;

namespace OutboundFunction
{
    class Phonecall
    {
        private CallingServerClient callClient;
        private CallConnection callConnection;
        private string appCallbackUrl;

        private CancellationTokenSource reportCancellationTokenSource;
        private CancellationToken reportCancellationToken;

        private TaskCompletionSource<bool> callEstablishedTask;
        private TaskCompletionSource<bool> callTerminatedTask;

        public Phonecall(string callbackUrl)
        {
            var connectionString = Environment.GetEnvironmentVariable("Connectionstring");
            callClient = new CallingServerClient(connectionString);
            appCallbackUrl = callbackUrl;
        }

        public async void InitiatePhoneCall(string sourcePhoneNumber, string targetPhoneNumber, string audioFileUrl)
        {
            reportCancellationTokenSource = new CancellationTokenSource();
            reportCancellationToken = reportCancellationTokenSource.Token;
            try
            {
                await CreateCallAsync(sourcePhoneNumber, targetPhoneNumber).ConfigureAwait(false);
                await PlayAudioAsync(audioFileUrl).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogMessage(Logger.MessageType.ERROR, $"Call ended unexpectedly, reason: {ex.Message}");
            }
        }

        private async Task CreateCallAsync(string sourcePhoneNumber, string targetPhoneNumber)
        {
            try
            {
                var connectionString = Environment.GetEnvironmentVariable("Connectionstring");
                string appCallbackUrl = $"{this.appCallbackUrl}outboundcall/callback?{EventAuthHandler.GetSecretQuerystring}";

                //Preparing request data
                var source = await CreateUser(connectionString);
                var target = new PhoneNumberIdentifier(targetPhoneNumber);
                CreateCallOptions createCallOption = new CreateCallOptions(
                    new Uri(appCallbackUrl),
                    new List<MediaType> { MediaType.Audio },
                    new List<EventSubscriptionType> { EventSubscriptionType.ParticipantsUpdated, EventSubscriptionType.DtmfReceived }
                    );
                createCallOption.AlternateCallerId = new PhoneNumberIdentifier(sourcePhoneNumber);

                Logger.LogMessage(Logger.MessageType.INFORMATION, "Performing CreateCall operation");

                callConnection = await callClient.CreateCallConnectionAsync(source,
                    new List<CommunicationIdentifier>() { target },
                    createCallOption, reportCancellationToken)
                    .ConfigureAwait(false);

                Logger.LogMessage(Logger.MessageType.INFORMATION, $"CreateCallConnectionAsync response --> {callConnection.ToString()}, Call Connection Id: { callConnection.CallConnectionId}");
                Logger.LogMessage(Logger.MessageType.INFORMATION, $"Call initiated with Call Connection id: { callConnection.CallConnectionId}");

                RegisterToCallStateChangeEvent(callConnection.CallConnectionId);

                //Wait for operation to complete
                await callEstablishedTask.Task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogMessage(Logger.MessageType.ERROR, string.Format("Failure occured while creating/establishing the call. Exception: {0}", ex.Message));
                throw ex;
            }
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

        private static async Task<CommunicationUserIdentifier> CreateUser(string connectionString)
        {
            var client = new CommunicationIdentityClient(connectionString);
            var user = await client.CreateUserAsync().ConfigureAwait(false);
            return new CommunicationUserIdentifier(user.Value.Id);
        }

        private async Task PlayAudioAsync(string audioFileUrl)
        {
            if (reportCancellationToken.IsCancellationRequested)
            {
                Logger.LogMessage(Logger.MessageType.INFORMATION, "Cancellation request, PlayAudio will not be performed");
                return;
            }

            try
            {
                if (string.IsNullOrEmpty(audioFileUrl))
                {
                    audioFileUrl = Environment.GetEnvironmentVariable("AudioFileUrl");
                }

                Logger.LogMessage(Logger.MessageType.INFORMATION, $"audioFileUrl = {audioFileUrl}");

                // Preparing data for request
                var playAudioRequest = new PlayAudioOptions()
                {
                    AudioFileUri = new Uri(audioFileUrl),
                    OperationContext = Guid.NewGuid().ToString(),
                    Loop = true,
                    CallbackUri = new Uri($"{this.appCallbackUrl}outboundcall/callback?{EventAuthHandler.GetSecretQuerystring}"),
                    AudioFileId = "",
                };

                Logger.LogMessage(Logger.MessageType.INFORMATION, "Performing PlayAudio operation");
                var response = await callConnection.PlayAudioAsync(playAudioRequest, reportCancellationToken).ConfigureAwait(false);

                Logger.LogMessage(Logger.MessageType.INFORMATION, $"PlayAudioAsync response --> " +
                    $"{response.GetRawResponse()}, Id: {response.Value.OperationId}, Status: " +
                    $"{response.Value.Status}, OperationContext: {response.Value.OperationContext}, " +
                    $"ResultInfo: {response.Value.ResultInfo}");

                if (response.Value.Status == OperationStatus.Running)
                {
                    Logger.LogMessage(Logger.MessageType.INFORMATION, $"Play Audio state: {response.Value.Status}");
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
    }
}
