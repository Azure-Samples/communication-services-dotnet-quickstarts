using Azure.Communication.Calling.WindowsClient;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Media.Core;
using Windows.UI.WindowManagement;
using WinRT.Interop;

namespace CallingQuickstart
{
    public partial class MainPage : Page
    {
#if CTE
        private const string teamsAuthToken = "<TEAMS_AUTHENTICATION_TOKEN>";

        private TeamsCallAgent teamsCallAgent;
        private TeamsCommunicationCall teamsCall;

        private async Task InitCallAgentAndDeviceManagerAsync()
        {
            this.callClient = new CallClient(new CallClientOptions() {
                Diagnostics = new CallDiagnosticsOptions() { 
                        AppName = "CallingQuickstart",
                        AppVersion="1.0",
                        Tags = new[] { "Calling", "Teams", "Windows" }
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

        private async void CameraList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedCamerea = CameraList.SelectedItem as VideoDeviceDetails;
            if (cameraStream != null)
            {
                await cameraStream.StopPreviewAsync();
            }
            
            cameraStream = new LocalOutgoingVideoStream(selectedCamerea);
            var localUri = await cameraStream.StartPreviewAsync();
            LocalVideo.Source = MediaSource.CreateFromUri(localUri);

            if (teamsCall != null)
            {
                await teamsCall.StartVideoAsync(cameraStream);
            }
        }

        private async void CallButton_Click(object sender, RoutedEventArgs e)
        {
            var callString = CalleeTextBox.Text.Trim();

            if (!string.IsNullOrEmpty(callString))
            {
                if (callString.StartsWith("8:")) // 1:1 ACS call
                {
                    teamsCall = await StartCteCallAsync(callString);
                }
                else if (callString.All(char.IsDigit)) // rooms call
                { 
                }
                else if (callString.StartsWith("+")) // 1:1 phone call
                {
                    teamsCall = await StartPhoneCallAsync(callString);
                }
                else if (Uri.TryCreate(callString, UriKind.Absolute, out Uri teamsMeetinglink)) //Teams meeting link
                {
                    teamsCall = await JoinTeamsMeetingByLinkWithCteAsync(teamsMeetinglink);
                }
            }

            teamsCall.RemoteParticipantsUpdated += OnRemoteParticipantsUpdatedAsync;
            teamsCall.StateChanged += OnStateChangedAsync;
        }

        private async void HangupButton_Click(object sender, RoutedEventArgs e)
        {
            var call = this.teamsCallAgent?.Calls?.FirstOrDefault();
            foreach (var localVideoStream in call?.OutgoingVideoStreams)
            {
                await call.StopVideoAsync(localVideoStream);
            }

            try
            {
                if (cameraStream != null)
                {
                    await cameraStream.StopPreviewAsync();
                }

                await call.HangUpAsync(new HangUpOptions() { ForEveryone = false });
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

        private async void MuteLocal_Click(object sender, RoutedEventArgs e)
        {
            var muteCheckbox = sender as CheckBox;

            if (muteCheckbox != null)
            {
                var call = this.teamsCallAgent?.Calls?.FirstOrDefault();

                if ((bool)muteCheckbox.IsChecked)
                {
                    await call?.MuteOutgoingAudioAsync();
                }
                else
                {
                    await call?.UnmuteOutgoingAudioAsync();
                }

                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    AppTitleBar.Background = call.IsOutgoingAudioMuted ? new SolidColorBrush(Colors.PaleVioletRed) : new SolidColorBrush(Colors.SeaGreen);
                });
            }
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

        private async void OnStateChangedAsync(object sender, PropertyChangedEventArgs args)
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

        private async Task<TeamsCommunicationCall> JoinTeamsMeetingByLinkWithCteAsync(Uri teamsCallLink)
        {
            var joinCallOptions = GetJoinCallOptions();

            var teamsMeetingLinkLocator = new TeamsMeetingLinkLocator(teamsCallLink.AbsoluteUri);
            var call = await teamsCallAgent.JoinAsync(teamsMeetingLinkLocator, joinCallOptions);
            return call;
        }

        private StartTeamsCallOptions GetStartTeamsCallOptions()
        {
            var startCallOptions = new StartTeamsCallOptions()
            {
                OutgoingAudioOptions = new OutgoingAudioOptions() { IsMuted = true, Stream = micStream },
                OutgoingVideoOptions = new OutgoingVideoOptions() { Streams = new OutgoingVideoStream[] { cameraStream } }
            };

            return startCallOptions;
        }

        private async Task<TeamsCommunicationCall> StartCteCallAsync(string callee)
        {
            var options = GetStartTeamsCallOptions();
            var call = await this.teamsCallAgent.StartCallAsync(new MicrosoftTeamsUserCallIdentifier(callee), options);
            return call;
        }

        private async Task<TeamsCommunicationCall> StartPhoneCallAsync(string callee)
        {
            var options = GetStartTeamsCallOptions();

            var call = await this.teamsCallAgent.StartCallAsync(new PhoneNumberCallIdentifier(callee), options);
            return call;
        }
#endif
    }
}
