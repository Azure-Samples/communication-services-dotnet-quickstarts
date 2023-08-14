// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Azure.Communication;
using Azure.Communication.CallAutomation;
using IncomingCallRouting.EventHandler;

namespace IncomingCallRouting.Utils
{
    /// <summary>
    /// Handling different callback events
    /// and perform operations
    /// </summary>
    public class IncomingCallHandler
    {
        private readonly CallAutomationClient _callAutomationClient;
        private readonly CallConfiguration _callConfiguration;
        private readonly CallRecording _callRecording;
        private CallConnection _callConnection;
        private CallConnectionProperties _callConnectionProperties;
        private CancellationTokenSource _reportCancellationTokenSource;
        private CancellationToken _reportCancellationToken;
        private readonly string _targetParticipant;
        private readonly string _consultTarget;
        private string _from;

        private TaskCompletionSource<bool> callEstablishedTask;
        private TaskCompletionSource<bool> playAudioCompletedTask;
        private TaskCompletionSource<bool> callTerminatedTask;
        private TaskCompletionSource<bool> toneReceivedCompleteTask;
        private TaskCompletionSource<bool> transferToParticipantCompleteTask;
        private readonly int maxRetryAttemptCount = 3;

        public IncomingCallHandler(CallAutomationClient callAutomationClient, CallConfiguration callConfiguration)
        {
            _callConfiguration = callConfiguration;
            _callAutomationClient = callAutomationClient;
            _callRecording = callAutomationClient.GetCallRecording();
            _targetParticipant = callConfiguration.TargetParticipant;
            _consultTarget = callConfiguration.ConsultTarget;
            _from = callConfiguration.IvrParticipants[0];
        }

        public async Task Report(string incomingCallContext)
        {
            _reportCancellationTokenSource = new CancellationTokenSource();
            _reportCancellationToken = _reportCancellationTokenSource.Token;

            try
            {
                // var create = await _callAutomationClient.CreateCallAsync(
                //     new CallInvite(new MicrosoftTeamsUserIdentifier(_targetParticipant)),
                //     new Uri(_callConfiguration.AppCallbackUrl));
                // Logger.LogMessage(Logger.MessageType.INFORMATION, $"CreateCallAsync Response -----> {create.GetRawResponse()}");

                // var redirect = await _callAutomationClient.RedirectCallAsync(incomingCallContext, new CallInvite(new MicrosoftTeamsUserIdentifier(_targetParticipant)));
                // Logger.LogMessage(Logger.MessageType.INFORMATION, $"RedirectCallAsync Response -----> {redirect.Status}");

                // Answer Call
                var response = await _callAutomationClient.AnswerCallAsync(incomingCallContext, new Uri(_callConfiguration.AppCallbackUrl));
                Logger.LogMessage(Logger.MessageType.INFORMATION, $"AnswerCallAsync Response -----> {response.GetRawResponse()}");

                _callConnection = response.Value.CallConnection;
                _callConnectionProperties = response.Value.CallConnectionProperties;
                RegisterToCallStateChangeEvent(_callConnectionProperties.CallConnectionId);
                
                // Wait for the call to get connected
                await callEstablishedTask.Task.ConfigureAwait(false);

                // Logger.LogMessage(Logger.MessageType.INFORMATION, $"Transferring to participant {_targetParticipant}");
                // var transfer = await _callConnection.TransferCallToParticipantAsync(GetIdentifier(_targetParticipant), cancellationToken: _reportCancellationToken);
                // Logger.LogMessage(Logger.MessageType.INFORMATION, $"Transfer Response -----> {transfer.GetRawResponse()}");

                // RegisterToDtmfResultEvent(_callConnectionProperties.CallConnectionId);

                // Logger.LogMessage(Logger.MessageType.INFORMATION, $"Adding participant {_targetParticipant} to call");
                // var addParticipant = await _callConnection.AddParticipantAsync(
                //     new CallInvite(new CommunicationUserIdentifier(_targetParticipant))
                //     {
                //         SourceDisplayName = "William"
                //     }, cancellationToken: _reportCancellationToken);
                // Logger.LogMessage(Logger.MessageType.INFORMATION, $"AddParticipant Response -----> {addParticipant.GetRawResponse()}");
                
                Logger.LogMessage(Logger.MessageType.INFORMATION, $"Start recording call on serverCallId: {_callConnectionProperties.ServerCallId}");
                var recording = await _callRecording.StartAsync(
                    new StartRecordingOptions(new ServerCallLocator(_callConnectionProperties.ServerCallId))
                    {
                        RecordingChannel = RecordingChannel.Mixed,
                        // RecordingStorageType = RecordingStorageType.External,
                        RecordingStateCallbackUri = new Uri(_callConfiguration.AppCallbackUrl),
                        // AudioChannelParticipantOrdering = { new CommunicationUserIdentifier(_targetParticipant), new CommunicationUserIdentifier("abc") }
                    }, _reportCancellationToken);
                Logger.LogMessage(Logger.MessageType.INFORMATION, $"Start Recording Response -----> {recording.GetRawResponse()}");

                // var pause = await _callRecording.PauseRecordingAsync(recording.Value.RecordingId);
                // Logger.LogMessage(Logger.MessageType.INFORMATION, $"Pause Recording Response -----> {pause.Status}");
                //
                // var resume = await _callRecording.ResumeRecordingAsync(recording.Value.RecordingId);
                // Logger.LogMessage(Logger.MessageType.INFORMATION, $"Resume Recording Response -----> {resume.Status}");
                //
                // var stop = await _callRecording.StopRecordingAsync(recording.Value.RecordingId);
                // Logger.LogMessage(Logger.MessageType.INFORMATION, $"Stop Recording Response -----> {stop.Status}");

                //
                // Logger.LogMessage(Logger.MessageType.INFORMATION, $"Tranferring call to participant {_targetParticipant}");
                // var transferToParticipantCompleted = await TransferToParticipant(_targetParticipant);
                // if (!transferToParticipantCompleted)
                // {
                //     await RetryTransferToParticipantAsync(async () => await TransferToParticipant(_targetParticipant));
                // }

                // var participants = await _callConnection.GetParticipantsAsync();

                // await _callConnection.TransferCallToParticipantAsync(
                //     new CallInvite(new MicrosoftTeamsUserIdentifier(_targetParticipant)));

                // await _callConnection.RemoveParticipantAsync(new MicrosoftTeamsUserIdentifier(_targetParticipant));Logger.LogMessage(Logger.MessageType.INFORMATION, $"Adding participant {_consultTarget} to call");

                var addParticipant2 = await _callConnection.AddParticipantAsync(
                    new CallInvite(new MicrosoftTeamsUserIdentifier(_targetParticipant))
                    {
                        SourceDisplayName = "Jack (Contoso Tech Support)"
                    }, cancellationToken: _reportCancellationToken);
                Logger.LogMessage(Logger.MessageType.INFORMATION, $"AddParticipant Response -----> {addParticipant2.GetRawResponse()}");

                // await PlayAudioAsync(_targetParticipant);

                // await _callConnection.TransferCallToParticipantAsync(new CallInvite(new MicrosoftTeamsUserIdentifier(_targetParticipant)), _reportCancellationToken);

                // await _callConnection.HangUpAsync(false);

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

                Logger.LogMessage(Logger.MessageType.INFORMATION, $"StartRecognizeAsync response --> {response.GetRawResponse().Status}, Id: {response.GetRawResponse().ClientRequestId}");
        
                if (response.GetRawResponse().Status == 202)
                {
                    // listen to play audio events
                    // RegisterToDtmfResultEvent(operationContext);
        
                    // var completedTask = await Task.WhenAny(toneReceivedCompleteTask.Task, Task.Delay(10 * 1000)).ConfigureAwait(false);
        
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
        
            Logger.LogMessage(Logger.MessageType.INFORMATION, $"PlayAudioAsync response --> {response.GetRawResponse().ContentStream}, " +
                $"Status: {response.GetRawResponse().Status}");
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
                    Logger.LogMessage(Logger.MessageType.INFORMATION, $"Tone received ---------> : {recognizeCompletedEvent?.RecognizeResult}");

                    // toneReceivedCompleteTask.TrySetResult(recognizeCompletedEvent?.RecognizeResult);

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

        // private async Task<bool> TransferToParticipant(string targetParticipant, string transfereeCallerId = null)
        // {
        //     transferToParticipantCompleteTask = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        //
        //     var identifier = GetIdentifier(targetParticipant);
        //
        //     if (identifier == null)
        //     {
        //         Logger.LogMessage(Logger.MessageType.INFORMATION, "Unknown identity provided. Enter valid phone number or communication user id");
        //         return true;
        //     }
        //     var operationContext = Guid.NewGuid().ToString();
        //
        //     var response = await _callConnection.TransferCallToParticipantAsync(new TransferToParticipantOptions(identifier)
        //     {
        //         OperationContext = operationContext,
        //         SourceCallerId = transfereeCallerId == null ? null : new PhoneNumberIdentifier(transfereeCallerId),
        //     }, cancellationToken: _reportCancellationToken);
        //
        //     var transferToParticipantCompleted = await transferToParticipantCompleteTask.Task.ConfigureAwait(false);
        //     return transferToParticipantCompleted;
        // }

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
