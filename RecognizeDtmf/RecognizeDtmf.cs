// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Calling.RecognizeDTMF
{
    using Azure.Communication;
    using Azure.Communication.CallAutomation;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public class RecognizeDtmf
    {
        private CallConfiguration callConfiguration;
        private CallAutomationClient callClient;
        private CallConnection callConnection;
        private CancellationTokenSource reportCancellationTokenSource;
        private CancellationToken reportCancellationToken;

        private TaskCompletionSource<bool> callEstablishedTask;
        private TaskCompletionSource<bool> playAudioCompletedTask;
        private TaskCompletionSource<bool> callTerminatedTask;
        private TaskCompletionSource<bool> recognizeDtmfCompletedTask;
        private DtmfTone toneInputValue = DtmfTone.Zero;
        private int toneCount = 0;

        public RecognizeDtmf(CallConfiguration callConfiguration)
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
                RegisterToDtmfResultEvent(callConnection.CallConnectionId);

                await StartRecognizingDtmf(targetPhoneNumber).ConfigureAwait(false);
                var recognizeDtmfCompleted = await recognizeDtmfCompletedTask.Task.ConfigureAwait(false);

                if (!recognizeDtmfCompleted || toneCount == 0)
                {
                    // dtmf recognize failed, let's hang up.
                    await HangupAsync().ConfigureAwait(false);
                }
                else
                {
                    // dtmf tone found, let's play message based on key press.
                    Logger.LogMessage(Logger.MessageType.INFORMATION, $"Play Audio for input {toneInputValue.ToString()}");
                    await PlayAudioAsInput().ConfigureAwait(false);
                    await playAudioCompletedTask.Task.ConfigureAwait(false);

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

                Logger.LogMessage(Logger.MessageType.INFORMATION, $"CreateCallConnectionAsync response --> {call.GetRawResponse()}, Call Connection Id: {call.Value.CallConnectionProperties.CallConnectionId}");

                Logger.LogMessage(Logger.MessageType.INFORMATION, $"Call initiated with Call Connection id: {call.Value.CallConnectionProperties.CallConnectionId}");

                RegisterToCallStateChangeEvent(call.Value.CallConnectionProperties.CallConnectionId);

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

        private async Task StartRecognizingDtmf(string targetPhoneNumber)
        {
            if (reportCancellationToken.IsCancellationRequested)
            {
                Logger.LogMessage(Logger.MessageType.INFORMATION, "Cancellation request, PlayAudio will not be performed");
                return;
            }

            try
            {
                string audioFilePath = callConfiguration.AudioFileUrl + callConfiguration.AudioFileName;
                PlaySource audioFileUri = new FileSource(new Uri(audioFilePath));

                //Start recognizing Dtmf Tone
                var recognizeOptions = new CallMediaRecognizeDtmfOptions(new PhoneNumberIdentifier(targetPhoneNumber), 1)
                {
                    InitialSilenceTimeout = TimeSpan.FromSeconds(15),
                    InterToneTimeout = TimeSpan.FromSeconds(5),
                    InterruptPrompt = true,
                    InterruptCallMediaOperation = true,
                    Prompt = audioFileUri,
                    StopTones = new List<DtmfTone> { DtmfTone.Pound }
                };

                var resp = await callConnection.GetCallMedia().StartRecognizingAsync(recognizeOptions, reportCancellationToken);

                Logger.LogMessage(Logger.MessageType.INFORMATION, $"StartRecognizingAsync response --> " +
                $"{resp}, Id: {resp.ClientRequestId}, Status: {resp.Status}");
            }
            catch (TaskCanceledException)
            {
                Logger.LogMessage(Logger.MessageType.ERROR, " Start recognizing with Play audio prompt for Custom message got cancelled");
            }
            catch (Exception ex)
            {
                Logger.LogMessage(Logger.MessageType.ERROR, $"Failure occured while start recognizing with Play audio prompt. Exception: {ex.Message}");
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

            var playCancelledNotification = new NotificationCallback((callEvent) =>
            {
                Task.Run(() =>
                {
                    Logger.LogMessage(Logger.MessageType.INFORMATION, $"Play audio status: Cancelled");
                    playAudioCompletedTask.TrySetResult(false);
                    EventDispatcher.Instance.Unsubscribe("PlayCancelled", callConnectionId);
                });
            });

            //Subscribe to event
            EventDispatcher.Instance.Subscribe("PlayCompleted", callConnectionId, playCompletedNotification);
            EventDispatcher.Instance.Subscribe("PlayFailed", callConnectionId, playFailedNotification);
            EventDispatcher.Instance.Subscribe("PlayCancelled", callConnectionId, playCancelledNotification);
        }

        private void RegisterToDtmfResultEvent(string callConnectionId)
        {
            recognizeDtmfCompletedTask = new TaskCompletionSource<bool>(TaskContinuationOptions.RunContinuationsAsynchronously);

            var dtmfReceivedEvent = new NotificationCallback((callEvent) =>
            {
                Task.Run(() =>
                {
                    var toneReceivedEvent = (RecognizeCompleted)callEvent;

                    if (toneReceivedEvent.CollectTonesResult.Tones.Count != 0)
                    {
                        Logger.LogMessage(Logger.MessageType.INFORMATION, $"Tone received --------- : {toneReceivedEvent.CollectTonesResult.Tones[0]}");
                        toneCount = toneReceivedEvent.CollectTonesResult.Tones.Count;
                        toneInputValue = toneReceivedEvent.CollectTonesResult.Tones[0];
                    }
                    EventDispatcher.Instance.Unsubscribe("RecognizeCompleted", callConnectionId);
                    recognizeDtmfCompletedTask.TrySetResult(true);
                });
            });

            var dtmfFailedEvent = new NotificationCallback((callEvent) =>
            {
                Task.Run(() =>
                {
                    Logger.LogMessage(Logger.MessageType.INFORMATION, $"Failed to recognize any Dtmf tone");
                    recognizeDtmfCompletedTask.TrySetResult(false);
                    EventDispatcher.Instance.Unsubscribe("Recognizefailed", callConnectionId);
                });
            });

            //Subscribe to event
            EventDispatcher.Instance.Subscribe("RecognizeCompleted", callConnectionId, dtmfReceivedEvent);
            EventDispatcher.Instance.Subscribe("Recognizefailed", callConnectionId, dtmfFailedEvent);
        }

        private async Task PlayAudioAsInput()
        {
            if (reportCancellationToken.IsCancellationRequested)
            {
                Logger.LogMessage(Logger.MessageType.INFORMATION, "Cancellation request, PlayAudio will not be performed");
                return;
            }

            try
            {
                var audioFileName = callConfiguration.InvalidAudioFileName;

                if (toneInputValue == DtmfTone.One)
                {
                    audioFileName = callConfiguration.SalesAudioFileName;
                }
                else if (toneInputValue == DtmfTone.Two)
                {
                    audioFileName = callConfiguration.MarketingAudioFileName;
                }
                else if (toneInputValue == DtmfTone.Three)
                {
                    audioFileName = callConfiguration.CustomerCareAudioFileName;
                }

                string audioFilePath = callConfiguration.AudioFileUrl + audioFileName;

                // Preparing data for request
                var playAudioRequest = new PlayOptions()
                {
                    OperationContext = Guid.NewGuid().ToString(),
                    Loop = false,
                };

                PlaySource audioFileUri = new FileSource(new Uri(audioFilePath));
                Logger.LogMessage(Logger.MessageType.INFORMATION, "Performing PlayAudio operation");

                var response = await callConnection.GetCallMedia().PlayToAllAsync(audioFileUri, playAudioRequest,
                    reportCancellationToken).ConfigureAwait(false);

                Logger.LogMessage(Logger.MessageType.INFORMATION, $"PlayAudioAsync response --> " +
                    $"{response}, Id: {response.ClientRequestId}, Status: {response.Status}");

                if (response.Status == 202)
                {
                    Logger.LogMessage(Logger.MessageType.INFORMATION, $"Play Audio state: {response.Status}");

                    // listen to play audio events
                    RegisterToPlayAudioResultEvent(callConnection.CallConnectionId);
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
