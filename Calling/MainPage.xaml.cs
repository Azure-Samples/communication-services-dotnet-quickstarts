using Azure.Communication.Calling.WindowsClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Media.Core;
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
        private const string authToken = "<AUTHENTICATION_TOKEN>";
        private const string teamsAuthToken = "<TEAMS_AUTHENTICATION_TOKEN>";

        private CallClient callClient;
        private CallTokenRefreshOptions callTokenRefreshOptions = new CallTokenRefreshOptions(false);
        private CallAgent callAgent;
        private TeamsCallAgent teamsCallAgent;
        private CommunicationCall call;
        private TeamsCommunicationCall teamsCall;

        private LocalOutgoingAudioStream micStream;
        private LocalOutgoingVideoStream cameraStream;

        private BackgroundBlurEffect backgroundBlurVideoEffect = new BackgroundBlurEffect();
        private LocalVideoEffectsFeature localVideoEffectsFeature;

        private bool isCTE = false;

        #region Page initialization
        public MainPage()
        {
            this.InitializeComponent();

            // Hide default title bar.
            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;

            coreTitleBar.LayoutMetricsChanged += (CoreApplicationViewTitleBar sender, object args) => { MainGrid.RowDefinitions[0].Height = new GridLength(sender.Height, GridUnitType.Pixel); };

            QuickstartTitle.Text = $"{Package.Current.DisplayName} - Ready";

            CallButton.IsEnabled = true;
            HangupButton.IsEnabled = !CallButton.IsEnabled;
            MuteLocal.IsChecked = MuteLocal.IsEnabled = !CallButton.IsEnabled;

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
        private async void CallMethodList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((CallMethodList.SelectedItem as string).Equals("ACS"))
            {
                isCTE = false;
            }else if ((CallMethodList.SelectedItem as string).Equals("CTE"))
            {
                isCTE= true;
            }
        }

        private async void CameraList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tryRawMedia) return;

            if (isCTE)
            {
                if (cameraStream != null)
                {
                    await cameraStream?.StopPreviewAsync();
                    if (teamsCall != null)
                    {
                        await teamsCall?.StopVideoAsync(cameraStream);
                    }
                }
                var selectedCamerea = CameraList.SelectedItem as VideoDeviceDetails;
                cameraStream = new LocalOutgoingVideoStream(selectedCamerea);

                InitVideoEffectsFeature(cameraStream);

                var localUri = await cameraStream.StartPreviewAsync();
                LocalVideo.Source = MediaSource.CreateFromUri(localUri);

                if (teamsCall != null) {
                    await teamsCall?.StartVideoAsync(cameraStream);
                }
            }
            else
            {
                if (cameraStream != null)
                {
                    await cameraStream?.StopPreviewAsync();
                    if (call != null)
                    {
                        await call?.StopVideoAsync(cameraStream);
                    }
                }
                var selectedCamerea = CameraList.SelectedItem as VideoDeviceDetails;
                cameraStream = new LocalOutgoingVideoStream(selectedCamerea);

                InitVideoEffectsFeature(cameraStream);

                var localUri = await cameraStream.StartPreviewAsync();
                LocalVideo.Source = MediaSource.CreateFromUri(localUri);

                if (call != null) {
                    await call?.StartVideoAsync(cameraStream);
                }
            }
        }

        private async void CallButton_Click(object sender, RoutedEventArgs e)
        {
            var callString = CalleeTextBox.Text.Trim();

            if (!string.IsNullOrEmpty(callString))
            {
                if (callString.StartsWith("8:")) // 1:1 ACS call
                {
                    if (isCTE)
                    {
                        teamsCall = await StartCteCallAsync(callString);
                    }
                    else
                    {
                        call = await StartAcsCallAsync(callString);
                    }
                }
                else if (callString.All(char.IsDigit)) // rooms call
                { 
                    if (isCTE)
                    {

                    }
                    else
                    {
                        call = await StartRoomsCallAsync(callString);
                    }
                }
                else if (callString.StartsWith("+")) // 1:1 phone call
                {
                    if (isCTE)
                    {
                        teamsCall = await StartPhoneCallAsync(callString);
                    }
                    else
                    {
                        call = await StartPhoneCallAsync(callString, "+12000000000");
                    }
                }
                else if (Guid.TryParse(callString, out Guid groupId))// Join group call by group guid
                {
                    if (isCTE)
                    {

                    }else
                    {
                        call = await JoinGroupCallByIdAsync(groupId);
                    }
                }
                else if (Uri.TryCreate(callString, UriKind.Absolute, out Uri teamsMeetinglink)) //Teams meeting link
                {
                    if (isCTE)
                    {
                        teamsCall = await JoinTeamsMeetingByLinkWithCteAsync(teamsMeetinglink);
                    }
                    else
                    {
                        call = await JoinTeamsMeetingByLinkWithAcsAsync(teamsMeetinglink);
                    }
                }
            }

            if(isCTE && teamsCall != null)
            {
                teamsCall.RemoteParticipantsUpdated += OnRemoteParticipantsUpdatedAsync;
                teamsCall.StateChanged += OnStateChangedAsync;
                CallMethodList.IsEnabled = false;
            }
            else if (call != null)
            {
                call.RemoteParticipantsUpdated += OnRemoteParticipantsUpdatedAsync;
                call.StateChanged += OnStateChangedAsync;
                CallMethodList.IsEnabled = false;
            }
        }

        private async void HangupButton_Click(object sender, RoutedEventArgs e)
        {
            var acsCall = this.callAgent?.Calls?.FirstOrDefault();
            var cteCall = this.teamsCallAgent?.Calls?.FirstOrDefault();

            if (acsCall != null)
            {
                foreach (var localVideoStream in acsCall.OutgoingVideoStreams)
                {
                    await acsCall.StopVideoAsync(localVideoStream);
                }

                try
                {
                    if (cameraStream != null)
                    {
                        await cameraStream.StopPreviewAsync();
                    }

                    await acsCall.HangUpAsync(new HangUpOptions() { ForEveryone = false });
                    CallMethodList.IsEnabled = true;
                }
                catch(Exception ex) 
                { 
                    var errorCode = unchecked((int)(0x0000FFFFU & ex.HResult));
                    if (errorCode != 98) // sam_status_failed_to_hangup_for_everyone (98)
                    {
                        throw;
                    }
                }
            }else if (cteCall != null){
                
                foreach (var localVideoStream in cteCall.OutgoingVideoStreams)
                {
                    await cteCall.StopVideoAsync(localVideoStream);
                }

                try
                {
                    if (cameraStream != null)
                    {
                        await cameraStream.StopPreviewAsync();
                    }

                    await cteCall.HangUpAsync(new HangUpOptions() { ForEveryone = false });
                    CallMethodList.IsEnabled = true;
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
                var acsCall = this.callAgent?.Calls?.FirstOrDefault();
                var cteCall = this.teamsCallAgent?.Calls?.FirstOrDefault();

                if (acsCall != null)
                {
                    if ((bool)muteCheckbox.IsChecked)
                    {
                        await acsCall.MuteOutgoingAudioAsync();
                    }
                    else
                    {
                        await acsCall.UnmuteOutgoingAudioAsync();
                    }
                }
                else if(cteCall != null)
                {
                    if ((bool)muteCheckbox.IsChecked)
                    {
                        await cteCall.MuteOutgoingAudioAsync();
                    }
                    else
                    {
                        await cteCall.UnmuteOutgoingAudioAsync();
                    }
                }

                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    AppTitleBar.Background = (isCTE ? cteCall.IsOutgoingAudioMuted : acsCall.IsOutgoingAudioMuted) ? new SolidColorBrush(Colors.PaleVioletRed) : new SolidColorBrush(Colors.SeaGreen);
                });
            }
        }

        private async void BackgroundBlur_Click(object sender, RoutedEventArgs e)
        {
            if (localVideoEffectsFeature.IsEffectSupported(backgroundBlurVideoEffect))
            {
                var backgroundBlurCheckbox = sender as CheckBox;
                if (backgroundBlurCheckbox.IsChecked.Value)
                {
                    localVideoEffectsFeature.EnableEffect(backgroundBlurVideoEffect);
                }
                else
                {
                    localVideoEffectsFeature.DisableEffect(backgroundBlurVideoEffect);
                }
            }
        }
        #endregion

        #region Video Effects Event Handlers
        private void OnVideoEffectError(object sender, VideoEffectErrorEventArgs e)
        {
        }

        private void OnVideoEffectDisabled(object sender, VideoEffectDisabledEventArgs e)
        {
            Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                BackgroundBlur.IsChecked = false;
            });
        }

        private void OnVideoEffectEnabled(object sender, VideoEffectEnabledEventArgs e)
        {
            Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                BackgroundBlur.IsChecked = true;
            });
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

        private async void OnCallsUpdatedAsync(object sender, TeamsCallsUpdatedEventArgs args)
        {
            var removedParticipants = new List<RemoteParticipant>();
            var addedParticipants = new List<RemoteParticipant>();

            foreach (var teamsCall in args.RemovedCalls)
            {
                removedParticipants.AddRange(teamsCall.RemoteParticipants.ToList<RemoteParticipant>());
            }

            foreach (var teamsCall in args.AddedCalls)
            {
                addedParticipants.AddRange(teamsCall.RemoteParticipants.ToList<RemoteParticipant>());
            }

            await OnParticipantChangedAsync(removedParticipants, addedParticipants);
        }

        private async void OnIncomingCallAsync(object sender, IncomingCallReceivedEventArgs args)
        {
            var incomingCall = args.IncomingCall;

            var acceptCallOptions = new AcceptCallOptions() { 
                IncomingVideoOptions = new IncomingVideoOptions()
                {
                    StreamKind = VideoStreamKind.RemoteIncoming
                } 
            };

            call = await incomingCall.AcceptAsync(acceptCallOptions);
            call.StateChanged += OnStateChangedAsync;
            call.RemoteParticipantsUpdated += OnRemoteParticipantsUpdatedAsync;
        }

        private async void OnIncomingCallAsync(object sender, TeamsIncomingCallReceivedEventArgs args)
        {
            var teamsIncomingCall = args.IncomingCall;

            var acceptCallOptions = new AcceptCallOptions()
            {
                IncomingVideoOptions = new IncomingVideoOptions()
                {
                    StreamKind = VideoStreamKind.RemoteIncoming
                }
            };

            teamsCall = await teamsIncomingCall.AcceptAsync(acceptCallOptions);
            teamsCall.StateChanged += OnStateChangedAsync;
            teamsCall.RemoteParticipantsUpdated += OnRemoteParticipantsUpdatedAsync;
        }

        private async void OnStateChangedAsync(object sender, PropertyChangedEventArgs args)
        {
            if (isCTE)
            {
                var call = sender as TeamsCommunicationCall;

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
                    });

                    switch (state)
                    {
                        case CallState.Connected:
                            {
                                await call.StartAudioAsync(micStream);
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
            else
            {
                if (call != null)
                {
                    var call = sender as CommunicationCall;

                    var state = call.State;

                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        QuickstartTitle.Text = $"{Package.Current.DisplayName} - {state.ToString()}";
                        Window.Current.SetTitleBar(AppTitleBar);

                        HangupButton.IsEnabled = state == CallState.Connected || state == CallState.Ringing;
                        CallButton.IsEnabled = !HangupButton.IsEnabled;
                        MuteLocal.IsEnabled = !CallButton.IsEnabled;
                    });

                    switch (state)
                    {
                        case CallState.Connected:
                            {
                                await call.StartAudioAsync(micStream);
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
                foreach(var incomingVideoStream in participant.IncomingVideoStreams)
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
            CallVideoStream callVideoStream = e.Stream;

            switch (callVideoStream.Direction)
            {
                case StreamDirection.Outgoing:
                    OnOutgoingVideoStreamStateChanged(callVideoStream as OutgoingVideoStream);
                    break;
                case StreamDirection.Incoming:
                    OnIncomingVideoStreamStateChangedAsync(callVideoStream as IncomingVideoStream);
                    break;
            }
        }

        private async void OnIncomingVideoStreamStateChangedAsync(IncomingVideoStream incomingVideoStream)
        {
            switch (incomingVideoStream.State)
            {
                case VideoStreamState.Available:
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

                case VideoStreamState.Started:
                    break;

                case VideoStreamState.Stopping:
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
            micStream = new LocalOutgoingAudioStream();

            CameraList.ItemsSource = deviceManager.Cameras.ToList();

            if (camera != null)
            {
                CameraList.SelectedIndex = 0;
            }

            callTokenRefreshOptions.TokenRefreshRequested += OnTokenRefreshRequestedAsync;

            var tokenCredential = new CallTokenCredential(authToken, callTokenRefreshOptions);
            var teamsTokenCredential = new CallTokenCredential(teamsAuthToken);

            var callAgentOptions = new CallAgentOptions()
            {
                DisplayName = $"{Environment.MachineName}/{Environment.UserName}",
                //https://github.com/lukes/ISO-3166-Countries-with-Regional-Codes/blob/master/all/all.csv
                EmergencyCallOptions = new EmergencyCallOptions() { CountryCode = "840" }
            };

            var teamsCallAgentOptions = new TeamsCallAgentOptions();

            try
            {
                this.callAgent = await this.callClient.CreateCallAgentAsync(tokenCredential, callAgentOptions);
                //await this.callAgent.RegisterForPushNotificationAsync(await this.RegisterWNS());
                this.callAgent.CallsUpdated += OnCallsUpdatedAsync;
                this.callAgent.IncomingCallReceived += OnIncomingCallAsync;

                this.teamsCallAgent = await this.callClient.CreateTeamsCallAgentAsync(teamsTokenCredential, teamsCallAgentOptions);
                this.teamsCallAgent.CallsUpdated += OnCallsUpdatedAsync;
                this.teamsCallAgent.IncomingCallReceived += OnIncomingCallAsync;
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

        private void InitVideoEffectsFeature(LocalOutgoingVideoStream videoStream) {
            localVideoEffectsFeature = videoStream.Features.VideoEffects;
            localVideoEffectsFeature.VideoEffectEnabled += OnVideoEffectEnabled;
            localVideoEffectsFeature.VideoEffectDisabled += OnVideoEffectDisabled;
            localVideoEffectsFeature.VideoEffectError += OnVideoEffectError;
        }

        private async Task<CommunicationCall> StartAcsCallAsync(string acsCallee)
        {
            var options = GetStartCallOptions();
            var call = await this.callAgent.StartCallAsync( new [] { new UserCallIdentifier(acsCallee) }, options);
            return call;
        }

        private async Task<TeamsCommunicationCall> StartCteCallAsync(string cteCallee)
        {
            var options = GetStartTeamsCallOptions();
            var call = await this.teamsCallAgent.StartCallAsync(new MicrosoftTeamsUserCallIdentifier(cteCallee), options);
            return call;
        }

        private async Task<CommunicationCall> StartPhoneCallAsync(string acsCallee, string alternateCallerId)
        {
            var options = GetStartCallOptions();
            options.AlternateCallerId = new PhoneNumberCallIdentifier(alternateCallerId);

            var call = await this.callAgent.StartCallAsync( new [] { new PhoneNumberCallIdentifier(acsCallee) }, options);
            return call;
        }

        private async Task<TeamsCommunicationCall> StartPhoneCallAsync(string cteCallee)
        {
            var options = GetStartTeamsCallOptions();

            var call = await this.teamsCallAgent.StartCallAsync(new PhoneNumberCallIdentifier(cteCallee), options);
            return call;
        }

        private async Task<CommunicationCall> JoinGroupCallByIdAsync(Guid groupId)
        {
            var joinCallOptions = GetJoinCallOptions();

            var groupCallLocator = new GroupCallLocator(groupId);
            var call = await this.callAgent.JoinAsync(groupCallLocator, joinCallOptions);
            return call;
        }

        private async Task<CommunicationCall> JoinTeamsMeetingByLinkWithAcsAsync(Uri teamsCallLink)
        {
            var joinCallOptions = GetJoinCallOptions();

            var teamsMeetingLinkLocator = new TeamsMeetingLinkLocator(teamsCallLink.AbsoluteUri);
            var call = await callAgent.JoinAsync(teamsMeetingLinkLocator, joinCallOptions);
            return call;
        }

        private async Task<TeamsCommunicationCall> JoinTeamsMeetingByLinkWithCteAsync(Uri teamsCallLink)
        {
            var joinCallOptions = GetJoinCallOptions();

            var teamsMeetingLinkLocator = new TeamsMeetingLinkLocator(teamsCallLink.AbsoluteUri);
            var call = await teamsCallAgent.JoinAsync(teamsMeetingLinkLocator, joinCallOptions);
            return call;
        }

        private async Task<CommunicationCall> StartRoomsCallAsync(String roomId) 
        {
            var joinCallOptions = GetJoinCallOptions();

            var roomCallLocator = new RoomCallLocator(roomId);

            call = await callAgent.JoinAsync(roomCallLocator, joinCallOptions);
            return call;

        }

        private StartCallOptions GetStartCallOptions()
        {
            var startCallOptions = GetStartCallOptionsWithRawMedia();

            if (startCallOptions == null)
            {
                startCallOptions = new StartCallOptions() {
                    OutgoingAudioOptions = new OutgoingAudioOptions() { IsMuted = true, Stream = micStream  },
                    OutgoingVideoOptions = new OutgoingVideoOptions() { Streams = new OutgoingVideoStream[] { cameraStream } }
                };
            }

            return startCallOptions;
        }

        private StartTeamsCallOptions GetStartTeamsCallOptions()
        {
            var startCallOptions = GetStartTeamsCallOptionsWithRawMedia();

            if (startCallOptions == null)
            {
                startCallOptions = new StartTeamsCallOptions()
                {
                    OutgoingAudioOptions = new OutgoingAudioOptions() { IsMuted = true, Stream = micStream },
                    OutgoingVideoOptions = new OutgoingVideoOptions() { Streams = new OutgoingVideoStream[] { cameraStream } }
                };
            }

            return startCallOptions;
        }

        private JoinCallOptions GetJoinCallOptions()
        {
            return new JoinCallOptions() {
                OutgoingAudioOptions = new OutgoingAudioOptions() { IsMuted = true },
                OutgoingVideoOptions = new OutgoingVideoOptions() { Streams = new OutgoingVideoStream[] { cameraStream } }
            };
        }
        #endregion
    }
}
