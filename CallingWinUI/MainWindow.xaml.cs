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

            //{"token":"eyJhbGciOiJSUzI1NiIsImtpZCI6IjEwNiIsIng1dCI6Im9QMWFxQnlfR3hZU3pSaXhuQ25zdE5PU2p2cyIsInR5cCI6IkpXVCJ9.eyJza3lwZWlkIjoiYWNzOmI2YWFkYTFmLTBiMWQtNDdhYy04NjZmLTkxYWFlMDBhMWQwMV8wMDAwMDAxNC0zYmM0LWE5MGEtYmRkMC00NDQ4MjIwMDllOGUiLCJzY3AiOjE3OTIsImNzaSI6IjE2NjQ3Njc2OTAiLCJleHAiOjE2NjQ4NTQwOTAsImFjc1Njb3BlIjoidm9pcCIsInJlc291cmNlSWQiOiJiNmFhZGExZi0wYjFkLTQ3YWMtODY2Zi05MWFhZTAwYTFkMDEiLCJpYXQiOjE2NjQ3Njc2OTB9.Fm5L4Sy_MD8YfkpblXzHD83f3ruAeqbv1r8kvQBaWBwWNss08G5sgls90wUQHbxlwb_N3lE82yFTH3tKZB75N1mjLplAUhbha_LJLSJrNHjRu5H0MvuFx0LBvsHwQVpASW7P-o_ows7-H8zT-hjje_pB_KONTjJVmbiFAiHUmN2kaIjDYPdcP_yWOCvJ4-Iq4pXXdbl0perbDT98t6jIc5PSbqpkYvA0-ck73roWG8mGATir55xffaR6o_BABz6yzuzgSI274kYdsH0NtzR9ArWTyTRvTcSE3Vn4LicgoWxt97NY6d-ylaUmK5HCeQbMMN0CqfY5RYXCWV0JQBSllA","expiresOn":"2022-10-04T03:28:10.262Z","user":{"communicationUserId":"8:acs:b6aada1f-0b1d-47ac-866f-91aae00a1d01_00000014-3bc4-a90a-bdd0-444822009e8e"}}
            var tokenCredential = new CommunicationTokenCredential("eyJhbGciOiJSUzI1NiIsImtpZCI6IjEwNiIsIng1dCI6Im9QMWFxQnlfR3hZU3pSaXhuQ25zdE5PU2p2cyIsInR5cCI6IkpXVCJ9.eyJza3lwZWlkIjoiYWNzOmI2YWFkYTFmLTBiMWQtNDdhYy04NjZmLTkxYWFlMDBhMWQwMV8wMDAwMDAxNC0zYmM0LWE5MGEtYmRkMC00NDQ4MjIwMDllOGUiLCJzY3AiOjE3OTIsImNzaSI6IjE2NjQ3Njc2OTAiLCJleHAiOjE2NjQ4NTQwOTAsImFjc1Njb3BlIjoidm9pcCIsInJlc291cmNlSWQiOiJiNmFhZGExZi0wYjFkLTQ3YWMtODY2Zi05MWFhZTAwYTFkMDEiLCJpYXQiOjE2NjQ3Njc2OTB9.Fm5L4Sy_MD8YfkpblXzHD83f3ruAeqbv1r8kvQBaWBwWNss08G5sgls90wUQHbxlwb_N3lE82yFTH3tKZB75N1mjLplAUhbha_LJLSJrNHjRu5H0MvuFx0LBvsHwQVpASW7P-o_ows7-H8zT-hjje_pB_KONTjJVmbiFAiHUmN2kaIjDYPdcP_yWOCvJ4-Iq4pXXdbl0perbDT98t6jIc5PSbqpkYvA0-ck73roWG8mGATir55xffaR6o_BABz6yzuzgSI274kYdsH0NtzR9ArWTyTRvTcSE3Vn4LicgoWxt97NY6d-ylaUmK5HCeQbMMN0CqfY5RYXCWV0JQBSllA");
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
