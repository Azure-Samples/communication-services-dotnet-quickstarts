using Azure.Communication.Calling.WindowsClient;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Media.Core;
using Windows.Networking.PushNotifications;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace CallingQuickstart
{
    public sealed partial class MainPage : Page
    {
        private const string authToken = "<ACS auth token>";

        private CallClient callClient;
        private CallTokenRefreshOptions callTokenRefreshOptions;
        private CallAgent callAgent;

        private LocalOutgoingAudioStream micStream;
        private LocalOutgoingVideoStream cameraStream;

        private BackgroundBlurEffect backgroundBlurVideoEffect = new BackgroundBlurEffect();

        #region Page initialization
        public MainPage()
        {
            this.InitializeComponent();

            // Hide default title bar.
            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;

            QuickstartTitle.Text = $"{Package.Current.DisplayName} - Ready";
            Window.Current.SetTitleBar(AppTitleBar);

            CallButton.IsEnabled = true;
            HangupButton.IsEnabled = !CallButton.IsEnabled;
            MuteLocal.IsChecked = MuteLocal.IsEnabled = !CallButton.IsEnabled;
            BackgroundBlur.IsChecked = BackgroundBlur.IsEnabled = !CallButton.IsEnabled;

            ApplicationView.PreferredLaunchViewSize = new Windows.Foundation.Size(800, 600);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await InitCallAgentAndDeviceManagerAsync();

            base.OnNavigatedTo(e);
        }
#endregion

        #region UI event handlers
        private async void CallButton_Click(object sender, RoutedEventArgs e)
        {
            CommunicationCall call = null;

            var callString = CalleeTextBox.Text.Trim();

            if (!string.IsNullOrEmpty(callString))
            {
                if (callString.StartsWith("8:")) // 1:1 ACS call
                {
                    call = await StartAcsCallAsync(callString);
                }
                else if (callString.StartsWith("+")) // 1:1 phone call
                {
                    call = await StartPhoneCallAsync(callString, "+12133947338");
                }
                else if (Guid.TryParse(callString, out Guid groupId))// Join group call by group guid
                {
                    call = await JoinGroupCallByIdAsync(groupId);
                }
                else if (Uri.TryCreate(callString, UriKind.Absolute, out Uri teamsMeetinglink)) //Teams meeting link
                {
                    call = await JoinTeamsMeetingByLinkAsync(teamsMeetinglink);
                }
            }

            if (call != null)
            {
                call.RemoteParticipantsUpdated += OnRemoteParticipantsUpdatedAsync;
                call.StateChanged += OnStateChangedAsync;
            }
        }

        private async void HangupButton_Click(object sender, RoutedEventArgs e)
        {
            var call = this.callAgent?.Calls?.FirstOrDefault();
            if (call != null)
            {
                //var incoingVideoStream = call.RemoteParticipants[0].IncomingVideoStreams[0];
                //var remoteVideoStream = incoingVideoStream as RemoteIncomingVideoStream;
                //await remoteVideoStream.StopPreviewAsync();

                foreach (var localVideoStream in call.OutgoingVideoStreams)
                {
                    await call.StopVideoAsync(localVideoStream);
                }

                try
                {
                    // This failed because RemoteVideoStream is enable
                    await call.HangUpAsync(new HangUpOptions() { ForEveryone = true });
                }
                catch(Exception ex) 
                { 
                    var errorCode = unchecked((int)(0x0000FFFFU & ex.HResult));
                    if (errorCode != 98) // sam_status_failed_to_hangup_for_everyone (98)
                    {
                        throw;
                    }
                }
            }
        }

        private async void MuteLocal_Click(object sender, RoutedEventArgs e)
        {
            var muteCheckbox = sender as CheckBox;

            if (muteCheckbox != null)
            {
                var call = this.callAgent?.Calls?.FirstOrDefault();
                if (call != null)
                {
                    if ((bool)muteCheckbox.IsChecked)
                    {
                        await call.MuteOutgoingAudioAsync();
                    }
                    else
                    {
                        await call.UnmuteOutgoingAudioAsync();
                    }
                }

                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    AppTitleBar.Background = call.IsOutgoingAudioMuted ? new SolidColorBrush(Colors.PaleVioletRed) : new SolidColorBrush(Colors.SeaGreen);
                });
            }
        }

        private async void BackgroundBlur_Click(object sender, RoutedEventArgs e)
        {
            var muteBackgroundBlurCheckbox = sender as CheckBox;
            if (muteBackgroundBlurCheckbox != null)
            {
                var localVideoEffectsFeature = cameraStream.Features.VideoEffects;

                if ((localVideoEffectsFeature != null) &&
                    (localVideoEffectsFeature.IsEffectSupported(backgroundBlurVideoEffect)))
                {
                    if (muteBackgroundBlurCheckbox.IsChecked.Value)
                    {
                        localVideoEffectsFeature.EnableEffect(backgroundBlurVideoEffect);
                    }
                    else
                    {
                        localVideoEffectsFeature.DisableEffect(backgroundBlurVideoEffect);
                    }
                }
            }
        }
        #endregion

        #region API event handlers

        private async void OnTokenRefreshRequestedAsync(object sender, CallTokenRefreshRequestedEventArgs e)
        {
            e.CallToken = new CallToken(
                authToken,
                DateTimeOffset.Now.AddHours(-8));  // Parse the expiration from the token
        }

        private async void OnCallsUpdatedAsync(object sender, CallsUpdatedEventArgs args)
        {
            var removedParticipants = new List<RemoteParticipant>();
            var addedParticipants = new List<RemoteParticipant>();

            foreach(var call in args.RemovedCalls)
            {
                removedParticipants.AddRange(call.RemoteParticipants.ToList<RemoteParticipant>());
            }

            foreach (var call in args.AddedCalls)
            {
                addedParticipants.AddRange(call.RemoteParticipants.ToList<RemoteParticipant>());
            }

            await OnParticipantChangedAsync(removedParticipants, addedParticipants);
        }

        private async void OnIncomingCallAsync(object sender, IncomingCallReceivedEventArgs args)
        {
            var incomingCall = args.IncomingCall;

            var acceptCallOptions = new AcceptCallOptions() { 
                IncomingVideoOptions = new IncomingVideoOptions()
                {
                    IncomingVideoStreamKind = VideoStreamKind.RemoteIncoming
                } 
            };

            _ = await incomingCall.AcceptAsync(acceptCallOptions);
        }

        private async void OnStateChangedAsync(object sender, PropertyChangedEventArgs args)
        {
            var call = sender as CommunicationCall;

            if (call != null)
            {
                var state = call.State;

                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    QuickstartTitle.Text = $"{Package.Current.DisplayName} - {state.ToString()}";
                    Window.Current.SetTitleBar(AppTitleBar);

                    HangupButton.IsEnabled = state == CallState.Connected || state == CallState.Ringing;
                    CallButton.IsEnabled = !HangupButton.IsEnabled;
                    MuteLocal.IsEnabled = !CallButton.IsEnabled;
                    BackgroundBlur.IsEnabled = !CallButton.IsEnabled;
                });

                switch (state)
                {
                    case CallState.Connected:
                        {
                            await call.StartAudioAsync(micStream);
                            await call.StartVideoAsync(cameraStream);
                            var localUri = await cameraStream.StartPreviewAsync();

                            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                            {
                                Stats.Text = $"Call id: {Guid.Parse(call.Id).ToString("D")}, Remote caller id: {call.RemoteParticipants.FirstOrDefault()?.Identifier.RawId}";
                            });

                            break;
                        }
                    case CallState.Disconnected:
                        {
                            call.RemoteParticipantsUpdated -= OnRemoteParticipantsUpdatedAsync;
                            call.StateChanged -= OnStateChangedAsync;

                            // This crashes
                            // await cameraStream.StopPreviewAsync();

                            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                            {
                                Stats.Text = $"Call ended: {call.CallEndReason.ToString()}";
                            });

                            call.Dispose();

                            break;
                        }
                    default: break;
                }
            }
        }

        private async void OnRemoteParticipantsUpdatedAsync(object sender, ParticipantsUpdatedEventArgs args)
        {
            await OnParticipantChangedAsync(
                args.RemovedParticipants.ToList<RemoteParticipant>(),
                args.AddedParticipants.ToList<RemoteParticipant>());
        }


        private async Task OnParticipantChangedAsync(IEnumerable<RemoteParticipant> removedParticipants, IEnumerable<RemoteParticipant> addedParticipants)
        {
            foreach (var participant in removedParticipants)
            {
                foreach(var incomingVideoStream in  participant.IncomingVideoStreams)
                {
                    var remoteVideoStream = incomingVideoStream as RemoteIncomingVideoStream;
                    if (remoteVideoStream != null)
                    {
                        await remoteVideoStream.StopPreviewAsync();
                    }
                }
                participant.VideoStreamStateChanged -= OnVideoStreamStateChanged;
            }

            foreach (var participant in addedParticipants)
            {
                participant.VideoStreamStateChanged += OnVideoStreamStateChanged;
            }
        }

        private void OnVideoStreamStateChanged(object sender, VideoStreamStateChangedEventArgs e)
        {
            CallVideoStream callVideoStream = e.CallVideoStream;

            switch (callVideoStream.StreamDirection)
            {
                case StreamDirection.Outgoing:
                    //OnOutgoingVideoStreamStateChanged(callVideoStream as OutgoingVideoStream);
                    break;
                case StreamDirection.Incoming:
                    OnIncomingVideoStreamStateChanged(callVideoStream as IncomingVideoStream);
                    break;
            }
        }

        private async void OnIncomingVideoStreamStateChanged(IncomingVideoStream incomingVideoStream)
        {
            switch (incomingVideoStream.State)
            {
                case VideoStreamState.Available:
                    {
                        switch (incomingVideoStream.Kind)
                        {
                            case VideoStreamKind.RemoteIncoming:
                                var remoteVideoStream = incomingVideoStream as RemoteIncomingVideoStream;
                                var uri = await remoteVideoStream.StartPreviewAsync();

                                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                                {
                                    RemoteVideo.Source = MediaSource.CreateFromUri(uri);
                                });
                                break;

                            case VideoStreamKind.RawIncoming:
                                break;
                        }

                        break;
                    }
                case VideoStreamState.Started:
                    break;
                case VideoStreamState.Stopping:
                    break;
                case VideoStreamState.Stopped:
                    if (incomingVideoStream.Kind == VideoStreamKind.RemoteIncoming)
                    {
                        var remoteVideoStream = incomingVideoStream as RemoteIncomingVideoStream;
                        await remoteVideoStream.StopPreviewAsync();
                    }
                    break;
                case VideoStreamState.NotAvailable:
                    break;
            }

        }

        private async void OnPushNotificationReceived(PushNotificationChannel sender, PushNotificationReceivedEventArgs args)
        {
            switch (args.NotificationType) {
                case PushNotificationType.Toast:
                case PushNotificationType.Tile:
                case PushNotificationType.TileFlyout:
                case PushNotificationType.Badge: 
                    break;
                case PushNotificationType.Raw:
                    RawNotification rawNotification = args.RawNotification;
                    string channelId = rawNotification.ChannelId;
                    string content = rawNotification.Content;

                    var pushNotificationDetails = PushNotificationDetails.Parse(content);
                    await this.callAgent.HandlePushNotificationAsync(pushNotificationDetails);
                    break;
                default: break;
            }
        }
        #endregion

        #region Helpers
        private async Task InitCallAgentAndDeviceManagerAsync()
        {
            this.callClient = new CallClient(new CallClientOptions() {
                Diagnostics = new CallDiagnosticsOptions() { 
                    AppName = "CallingQuickstart",
                    AppVersion="1.0",
                    Tags = new[] { "Calling", "ACS", "Windows" }
                    }
                });

            // Set up local video stream using the first camera enumerated
            var deviceManager = await this.callClient.GetDeviceManagerAsync();
            var camera = deviceManager?.Cameras?.FirstOrDefault();
            var mic = deviceManager?.Microphones?.FirstOrDefault();
            if (camera != null)
            {
                micStream = new LocalOutgoingAudioStream();

                cameraStream = new LocalOutgoingVideoStream(camera);
                var localUri = await cameraStream.StartPreviewAsync();
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    LocalVideo.Source = MediaSource.CreateFromUri(localUri);
                });
            }

            callTokenRefreshOptions = new CallTokenRefreshOptions(false);
            callTokenRefreshOptions.TokenRefreshRequested += OnTokenRefreshRequestedAsync;

            var tokenCredential = new CallTokenCredential(authToken, callTokenRefreshOptions);

            var callAgentOptions = new CallAgentOptions()
            {
                DisplayName = "Contoso",
                //https://github.com/lukes/ISO-3166-Countries-with-Regional-Codes/blob/master/all/all.csv
                EmergencyCallOptions = new EmergencyCallOptions() { CountryCode = "840" }
            };


            try
            {
                this.callAgent = await this.callClient.CreateCallAgentAsync(tokenCredential, callAgentOptions);
                //await this.callAgent.RegisterForPushNotificationAsync(await this.RegisterWNS());
                this.callAgent.CallsUpdated += OnCallsUpdatedAsync;
                this.callAgent.IncomingCallReceived += OnIncomingCallAsync;

            }
            catch(Exception ex)
            {
                if (ex.HResult == -2147024809)
                {
                    // E_INVALIDARG
                    // Handle possible invalid token
                }
            }
        }

        private async Task<CommunicationCall> StartAcsCallAsync(string acsCallee)
        {
            var options = await GetStartCallOptionsAsynnc();
            var call = await this.callAgent.StartCallAsync( new [] { new UserCallIdentifier(acsCallee) }, options);
            return call;
        }

        private async Task<CommunicationCall> StartPhoneCallAsync(string acsCallee, string alternateCallerId)
        {
            var options = await GetStartCallOptionsAsynnc();
            options.AlternateCallerId = new PhoneNumberCallIdentifier(alternateCallerId);

            var call = await this.callAgent.StartCallAsync( new [] { new PhoneNumberCallIdentifier(acsCallee) }, options);
            return call;
        }

        private async Task<CommunicationCall> JoinGroupCallByIdAsync(Guid groupId)
        {
            var joinCallOptions = await GetJoinCallOptionsAsync();

            var groupCallLocator = new GroupCallLocator(groupId);
            var call = await this.callAgent.JoinAsync(groupCallLocator, joinCallOptions);
            return call;
        }

        private async Task<CommunicationCall> JoinTeamsMeetingByLinkAsync(Uri teamsCallLink)
        {
            var joinCallOptions = await GetJoinCallOptionsAsync();

            var teamsMeetingLinkLocator = new TeamsMeetingLinkLocator(teamsCallLink.AbsoluteUri);
            var call = await callAgent.JoinAsync(teamsMeetingLinkLocator, joinCallOptions);
            return call;
        }

        private async Task<StartCallOptions> GetStartCallOptionsAsynnc()
        {
            return new StartCallOptions() {
                OutgoingAudioOptions = new OutgoingAudioOptions() { IsOutgoingAudioMuted = true, OutgoingAudioStream = micStream  },
                OutgoingVideoOptions = new OutgoingVideoOptions() { OutgoingVideoStreams = new OutgoingVideoStream[] { cameraStream } }
            };
        }

        private async Task<JoinCallOptions> GetJoinCallOptionsAsync()
        {
            return new JoinCallOptions() {
                OutgoingAudioOptions = new OutgoingAudioOptions() { IsOutgoingAudioMuted = true },
                OutgoingVideoOptions = new OutgoingVideoOptions() { OutgoingVideoStreams = new OutgoingVideoStream[] { cameraStream } }
            };
        }

        private async Task<string> RegisterWNS()
        {
            // Register to WNS

            var channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
            channel.PushNotificationReceived += OnPushNotificationReceived;
            var hub = new Microsoft.WindowsAzure.Messaging.NotificationHub("{CHANNEL_NAME}", "{SECRET_FROM_PNHUB_RESOURCE}");
            var result = await hub.RegisterNativeAsync(channel.Uri);

            return string.Empty;
        }
#endregion
    }
}
