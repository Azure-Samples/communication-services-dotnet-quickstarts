using Azure.Communication.Calling;
using Azure.WinRT.Communication;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Core;

namespace CallingQuickstart
{
    public sealed partial class MainWindow : Window
    {
        CallAgent callAgent;
        Call call;
        DeviceManager deviceManager;
        Dictionary<string, RemoteParticipant> remoteParticipantDictionary = new Dictionary<string, RemoteParticipant>();

        public MainWindow()
        {
            this.InitializeComponent();
            Task.Run(() => this.InitCallAgentAndDeviceManagerAsync()).Wait();
        }

        private async Task InitCallAgentAndDeviceManagerAsync()
        {
            var callClient = new CallClient();
            this.deviceManager = await callClient.GetDeviceManager();

            var tokenCredential = new CommunicationTokenCredential("<AUTHENTICATION_TOKEN>");
            var callAgentOptions = new CallAgentOptions()
            {
                DisplayName = "<DISPLAY_NAME>"
            };

            this.callAgent = await callClient.CreateCallAgent(tokenCredential, callAgentOptions);
            this.callAgent.OnCallsUpdated += Agent_OnCallsUpdatedAsync;
            this.callAgent.OnIncomingCall += Agent_OnIncomingCallAsync;
        }

        private async Task AddVideoStreamsAsync(IReadOnlyList<RemoteVideoStream> remoteVideoStreams)
        {
            foreach (var remoteVideoStream in remoteVideoStreams)
            {
                var remoteUri = await remoteVideoStream.Start();

                this.DispatcherQueue.TryEnqueue(() => {
                    RemoteVideo.Source = MediaSource.CreateFromUri(remoteUri);
                    RemoteVideo.MediaPlayer.Play();
                });
            }
        }

        private async void Agent_OnCallsUpdatedAsync(object sender, CallsUpdatedEventArgs args)
        {
            foreach (var call in args.AddedCalls)
            {
                foreach (var remoteParticipant in call.RemoteParticipants)
                {
                    var remoteParticipantMRI = remoteParticipant.Identifier.ToString();
                    this.remoteParticipantDictionary.TryAdd(remoteParticipantMRI, remoteParticipant);
                    await AddVideoStreamsAsync(remoteParticipant.VideoStreams);
                    remoteParticipant.OnVideoStreamsUpdated += Call_OnVideoStreamsUpdatedAsync;
                }
            }
        }

        private async void Agent_OnIncomingCallAsync(object sender, IncomingCall incomingCall)
        {
            var acceptCallOptions = new AcceptCallOptions();

            if (this.deviceManager.Cameras?.Count > 0)
            {
                var videoDeviceInfo = this.deviceManager.Cameras?.FirstOrDefault();
                if (videoDeviceInfo != null)
                {
                    var localVideoStream = new LocalVideoStream(videoDeviceInfo);

                    var localUri = await localVideoStream.MediaUriAsync();

                    this.DispatcherQueue.TryEnqueue(() => {
                        LocalVideo.Source = MediaSource.CreateFromUri(localUri);
                        LocalVideo.MediaPlayer.Play();
                    });

                    acceptCallOptions.VideoOptions = new VideoOptions(new[] { localVideoStream });
                }
            }

            call = await incomingCall.AcceptAsync(acceptCallOptions);
        }

        private async void CallButton_Click(object sender, RoutedEventArgs e)
        {
            var startCallOptions = new StartCallOptions();

            if (this.deviceManager.Cameras?.Count > 0)
            {
                var videoDeviceInfo = this.deviceManager.Cameras?.FirstOrDefault();
                if (videoDeviceInfo != null)
                {
                    var localVideoStream = new LocalVideoStream(videoDeviceInfo);

                    var localUri = await localVideoStream.MediaUriAsync();

                    this.DispatcherQueue.TryEnqueue(() => {
                        LocalVideo.Source = MediaSource.CreateFromUri(localUri);
                        LocalVideo.MediaPlayer.Play();
                    });

                    startCallOptions.VideoOptions = new VideoOptions(new[] { localVideoStream });
                }
            }

            var callees = new ICommunicationIdentifier[1]
            {
                new CommunicationUserIdentifier(CalleeTextBox.Text.Trim())
            };

            this.call = await this.callAgent.StartCallAsync(callees, startCallOptions);
            this.call.OnRemoteParticipantsUpdated += Call_OnRemoteParticipantsUpdatedAsync;
            this.call.OnStateChanged += Call_OnStateChangedAsync;
        }

        private async void HangupButton_Click(object sender, RoutedEventArgs e)
        {
            await this.call.HangUpAsync(new HangUpOptions());
        }

        private async void Call_OnVideoStreamsUpdatedAsync(object sender, RemoteVideoStreamsEventArgs args)
        {
            foreach (var remoteVideoStream in args.AddedRemoteVideoStreams)
            {
                this.DispatcherQueue.TryEnqueue(async () => {
                    RemoteVideo.Source = MediaSource.CreateFromUri(await remoteVideoStream.Start());
                });
            }

            foreach (var remoteVideoStream in args.RemovedRemoteVideoStreams)
            {
                remoteVideoStream.Stop();
            }
        }

        private async void Call_OnRemoteParticipantsUpdatedAsync(object sender, ParticipantsUpdatedEventArgs args)
        {
            foreach (var remoteParticipant in args.AddedParticipants)
            {
                String remoteParticipantMRI = remoteParticipant.Identifier.ToString();
                this.remoteParticipantDictionary.TryAdd(remoteParticipantMRI, remoteParticipant);
                await AddVideoStreamsAsync(remoteParticipant.VideoStreams);
                remoteParticipant.OnVideoStreamsUpdated += Call_OnVideoStreamsUpdatedAsync;
            }

            foreach (var remoteParticipant in args.RemovedParticipants)
            {
                String remoteParticipantMRI = remoteParticipant.Identifier.ToString();
                this.remoteParticipantDictionary.Remove(remoteParticipantMRI);
            }
        }

        private async void Call_OnStateChangedAsync(object sender, PropertyChangedEventArgs args)
        {
            var state = (sender as Call)?.State;
            this.DispatcherQueue.TryEnqueue(() => {
                State.Text = state.ToString();
            });
        }
    }
}
