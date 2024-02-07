using Azure.Communication.Calling.WindowsClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Graphics.Capture;
using Windows.Media.Capture.Frames;
using Windows.Security.Authorization.AppCapabilityAccess;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Xamarin.Essentials;

namespace RawVideo
{
    public sealed partial class MainPage : Page
    {
        // UI
        private int selectedVideoDeviceInfoListIndex = -1;
        private int selectedCameraListIndex = -1;
        private int selectedDisplayListIndex = -1;

        // App
        private IReadOnlyList<VideoDeviceDetails> videoDeviceInfoList;
        private List<Tuple<MediaFrameSourceGroup, MediaFrameSourceInfo>> cameraList;
        private List<GraphicsCaptureItem> displayList;
        private List<VideoStreamKind> outgoingVideoStreamKindList;
        private List<VideoStreamKind> incomingVideoStreamKindList;
        private CallClient callClient;
        private CallAgent callAgent;
        private CommunicationCall call;
        private DeviceManager deviceManager;
        private ScreenCaptureService screenCaptureService;
        private CameraCaptureService cameraCaptureService;
        private OutgoingVideoStream outgoingVideoStream;
        private LocalOutgoingVideoStream localOutgoingVideoStream;
        private VirtualOutgoingVideoStream virtualOutgoingVideoStream;
        private ScreenShareOutgoingVideoStream screenShareOutgoingVideoStream;
        private IncomingVideoStream incomingVideoStream;
        private RemoteIncomingVideoStream remoteIncomingVideoStream;
        private RawIncomingVideoStream rawIncomingVideoStream;
        private VideoStreamRenderer outgoingVideoStreamRenderer;
        private VideoStreamRenderer incomingVideoStreamRenderer;
        private VideoFrameRenderer incomingVideoFrameRenderer;
        private VideoFrameRenderer outgoingVideoFrameRenderer;
        private VideoStreamKind outgoingVideoStreamKind;
        private VideoStreamKind incomingVideoStreamKind;
        private RawVideoFrameKind outgoingVideoFrameKind;
        private RawVideoFrameKind incomingVideoFrameKind;
        private ApplicationDataContainer settings;
        private int w = 0;
        private int h = 0;
        private int framerate = 30;
        private bool callInProgress = false;

        public MainPage()
        {
            InitializeComponent();

            settings = ApplicationData.Current.LocalSettings;

            var savedToken = settings.Values["Token"];
            if (savedToken != null)
            {
                tokenTextBox.Text = savedToken.ToString();
            }
        }

        private async void InitResources()
        {
            outgoingVideoFrameKind = RawVideoFrameKind.Texture;
            incomingVideoFrameKind = RawVideoFrameKind.Texture;

            incomingVideoStreamKindList = new List<VideoStreamKind>
            {
                VideoStreamKind.RemoteIncoming,
                VideoStreamKind.RawIncoming,
            };

            outgoingVideoStreamKindList = new List<VideoStreamKind>
            {
                VideoStreamKind.LocalOutgoing,
                VideoStreamKind.VirtualOutgoing,
                VideoStreamKind.ScreenShareOutgoing
            };

            incomingVideoStreamKindList
                .ForEach(kind => incomingVideoStreamKindComboBox.Items.Add(kind.ToString()));

            outgoingVideoStreamKindList
                .ForEach(kind => outgoingVideoStreamKindComboBox.Items.Add(kind.ToString()));

            incomingVideoStreamKindComboBox.SelectedIndex = 1;
            outgoingVideoStreamKindComboBox.SelectedIndex = 1;

            await CreateCallAgent();

            if (deviceManager == null)
            {
                return;
            }

            videoDeviceInfoList = deviceManager.Cameras.OrderBy(item => item.Name).ToList();
            foreach (VideoDeviceDetails item in videoDeviceInfoList)
            {
                videoDeviceInfoComboBox.Items.Add(item.Name);
            }

            if (videoDeviceInfoList.Count > 0)
            {
                videoDeviceInfoComboBox.SelectedIndex = 0;
            }

            cameraList = await CameraCaptureService.GetCameraListAsync();
            foreach (Tuple<MediaFrameSourceGroup, MediaFrameSourceInfo> item in cameraList)
            {
                cameraComboBox.Items.Add(item.Item2.DeviceInformation.Name);
            }

            if (cameraList.Count > 0)
            {
                cameraComboBox.SelectedIndex = 0;
            }

            await GetScreenSharePermission();

            displayList = ScreenCaptureService.GetDisplayList();
            foreach (GraphicsCaptureItem item in displayList)
            {
                displayComboBox.Items.Add(string.Format("{0} ({1}x{2})",
                    item.DisplayName,
                    item.Size.Width,
                    item.Size.Height));
            }

            if (displayList.Count > 0)
            {
                displayComboBox.SelectedIndex = 0;
            }

            tokenContainer.Visibility = Visibility.Collapsed;
            callContainer.Visibility = Visibility.Visible;

            var savedMeetingLink = settings.Values["MeetingLink"];
            if (savedMeetingLink != null)
            {
                meetingLinkTextBox.Text = savedMeetingLink.ToString();
            }
        }

        private async void GetPermissions(object sender, RoutedEventArgs args)
        {
            if (string.IsNullOrEmpty(tokenTextBox.Text))
            {
                ShowMessage("Invalid token");
                return;
            }

            PermissionStatus cameraPermission = 
                await Permissions.RequestAsync<Permissions.Camera>();
            PermissionStatus microphonePermission = 
                await Permissions.RequestAsync<Permissions.Microphone>();

            if (cameraPermission == PermissionStatus.Granted && 
                microphonePermission == PermissionStatus.Granted)
            {
                InitResources();
            }
        }

        private async Task GetScreenSharePermission()
        {
            AppCapabilityAccessStatus screenSharePermission = 
                await GraphicsCaptureAccess.RequestAccessAsync(
                    GraphicsCaptureAccessKind.Programmatic);
        }

        private async Task CreateCallAgent()
        {
            try
            {
                var credential = new CallTokenCredential(tokenTextBox.Text);
                callClient = new CallClient();

                var options = new CallAgentOptions
                {
                    DisplayName = "Windows Quickstart User"
                };

                callAgent = await callClient.CreateCallAgentAsync(credential, options);

                deviceManager = await callClient.GetDeviceManagerAsync();

                settings.Values["Token"] = tokenTextBox.Text;
            }
            catch (Exception ex)
            {
                ShowMessage("Failed to create call agent");
                Console.WriteLine(ex.Message);
            }
        }

        private async void StartCall(object sender, RoutedEventArgs args)
        {
            if (callInProgress)
            {
                return;
            }

            if (!ValidateCallSettings())
            {
                return;
            }

            callInProgress = true;

            var incomingVideoOptions = new IncomingVideoOptions
            {
                StreamKind = incomingVideoStreamKind,
                FrameKind = incomingVideoFrameKind
            };

            OutgoingVideoOptions outgoingVideoOptions = CreateOutgoingVideoOptions();

            var joinCallOptions = new JoinCallOptions
            {
                IncomingVideoOptions = incomingVideoOptions,
                OutgoingVideoOptions = outgoingVideoOptions
            };

            var locator = new TeamsMeetingLinkLocator(meetingLinkTextBox.Text);

            loadingProgressRing.IsActive = true;
            try
            {
                call = await callAgent.JoinAsync(locator, joinCallOptions);

                await call.MuteOutgoingAudioAsync();
                await call.MuteIncomingAudioAsync();

                await this.RunOnUIThread(() =>
                {
                    callSettingsContainer.Visibility = Visibility.Collapsed;
                    videoContainer.Visibility = Visibility.Visible;
                });

                settings.Values["MeetingLink"] = meetingLinkTextBox.Text;
            }
            catch (Exception ex)
            {
                callInProgress = false;

                ShowMessage("Call failed to start");
                Console.WriteLine(ex.Message);
            }

            loadingProgressRing.IsActive = false;

            if (call != null)
            {
                call.RemoteParticipantsUpdated += OnRemoteParticipantsUpdated;

                AddRemoteParticipants(call.RemoteParticipants);
            }
        }

        private OutgoingVideoOptions CreateOutgoingVideoOptions()
        {
            VideoStreamFormat format = CreateVideoStreamFormat();

            var options = new RawOutgoingVideoStreamOptions
            {
                Formats = new VideoStreamFormat[] { format }
            };

            switch (outgoingVideoStreamKind)
            {
                case VideoStreamKind.LocalOutgoing:
                    localOutgoingVideoStream = new LocalOutgoingVideoStream(
                        videoDeviceInfoList[selectedVideoDeviceInfoListIndex]);
                    outgoingVideoStream = localOutgoingVideoStream;

                    break;
                case VideoStreamKind.VirtualOutgoing:
                    virtualOutgoingVideoStream = new VirtualOutgoingVideoStream(options);
                    outgoingVideoStream = virtualOutgoingVideoStream;

                    break;
                case VideoStreamKind.ScreenShareOutgoing:
                    screenShareOutgoingVideoStream = new ScreenShareOutgoingVideoStream(options);
                    outgoingVideoStream = screenShareOutgoingVideoStream;

                    break;
            }

            outgoingVideoStream.StateChanged += OnVideoStreamStateChanged;

            return new OutgoingVideoOptions()
            {
                Streams = new OutgoingVideoStream[] { outgoingVideoStream }
            };
        }

        private VideoStreamFormat CreateVideoStreamFormat()
        {
            var format = new VideoStreamFormat
            {
                PixelFormat = VideoStreamPixelFormat.Rgba,
                FramesPerSecond = framerate
            };

            switch (outgoingVideoStreamKind)
            {
                case VideoStreamKind.VirtualOutgoing:
                    format.Resolution = VideoStreamResolution.P360;
                    w = format.Width;
                    h = format.Height;
                    break;
                case VideoStreamKind.ScreenShareOutgoing:
                    GraphicsCaptureItem display = displayList[selectedDisplayListIndex];
                    w = display.Size.Width;
                    h = display.Size.Height;

                    format.Width = w;
                    format.Height = h;
                    break;
            }

            format.Stride1 = w * 4;

            return format;
        }

        private void OnRemoteParticipantsUpdated(object sender, ParticipantsUpdatedEventArgs args)
        {
            AddRemoteParticipants(args.AddedParticipants);

            foreach (RemoteParticipant remoteParticipant in args.RemovedParticipants)
            {
                remoteParticipant.VideoStreamStateChanged -= OnVideoStreamStateChanged;
            }
        }

        private void AddRemoteParticipants(IReadOnlyList<RemoteParticipant> remoteParticipantList)
        {
            foreach (RemoteParticipant remoteParticipant in remoteParticipantList)
            {
                remoteParticipant.VideoStreamStateChanged += OnVideoStreamStateChanged;
                foreach (IncomingVideoStream stream in remoteParticipant.IncomingVideoStreams)
                {
                    OnIncomingVideoStreamStateChanged(stream);
                }
            }
        }

        private void OnVideoStreamStateChanged(object sender, VideoStreamStateChangedEventArgs args)
        {
            CallVideoStream stream = args.Stream;
            switch (stream.Direction)
            {
                case StreamDirection.Outgoing:
                    OnOutgoingVideoStreamStateChanged(stream as OutgoingVideoStream);
                    break;
                case StreamDirection.Incoming:
                    OnIncomingVideoStreamStateChanged(stream as IncomingVideoStream);
                    break;
            }
        }

        private async void OnOutgoingVideoStreamStateChanged(OutgoingVideoStream stream)
        {
            switch (stream.State)
            {
                case VideoStreamState.Available:
                    if (stream.Kind == VideoStreamKind.LocalOutgoing)
                    {
                        await StartLocalPreview();
                    }

                    break;
                case VideoStreamState.Started:
                    switch (stream.Kind)
                    {
                        case VideoStreamKind.VirtualOutgoing:
                            await StartCameraCaptureService();
                            break;
                        case VideoStreamKind.ScreenShareOutgoing:
                            await StartScreenShareCaptureService();
                            break;
                    }

                    break;
                case VideoStreamState.Stopped:
                    switch (stream.Kind)
                    {
                        case VideoStreamKind.LocalOutgoing:
                            await StopLocalPreview();
                            break;
                        case VideoStreamKind.VirtualOutgoing:
                            await StopCameraCaptureService();
                            break;
                        case VideoStreamKind.ScreenShareOutgoing:
                            await StopScreenShareCaptureService();
                            break;
                    }

                    outgoingVideoStream = null;
                    break;
            }
        }

        private async void OnIncomingVideoStreamStateChanged(IncomingVideoStream stream)
        {
            if (incomingVideoStream != null && incomingVideoStream != stream)
            {
                if (stream.State == VideoStreamState.Available)
                {
                    ShowMessage("This app only support 1 incoming video stream from 1 remote participant");
                }

                return;
            }

            switch (stream.State)
            {
                case VideoStreamState.Available:
                    switch (stream.Kind)
                    {
                        case VideoStreamKind.RemoteIncoming:
                            remoteIncomingVideoStream = stream as RemoteIncomingVideoStream;
                            await StartRemotePreview();

                            break;
                        case VideoStreamKind.RawIncoming:
                            rawIncomingVideoStream = stream as RawIncomingVideoStream;
                            rawIncomingVideoStream.RawVideoFrameReceived += RawVideoFrameReceived;
                            rawIncomingVideoStream.Start();

                            break;
                    }

                    incomingVideoStream = stream;
                    break;
                case VideoStreamState.Started:
                    if (stream.Kind == VideoStreamKind.RawIncoming)
                    {
                        await StartRawIncomingPreview();
                    }

                    break;
                case VideoStreamState.Stopped:
                    switch (stream.Kind)
                    {
                        case VideoStreamKind.RemoteIncoming:
                            await StopRemotePreview();
                            break;
                        case VideoStreamKind.RawIncoming:
                            await StopRawIncomingPreview();
                            break;
                    }

                    break;
                case VideoStreamState.NotAvailable:
                    if (stream.Kind == VideoStreamKind.RawIncoming)
                    {
                        rawIncomingVideoStream.RawVideoFrameReceived -= RawVideoFrameReceived;
                    }

                    incomingVideoStream = null;
                    break;
            }
        }

        private async void RawVideoFrameReceived(object sender, RawVideoFrameReceivedEventArgs args)
        {
            using (RawVideoFrame frame = args.Frame)
            {
                await this.RunOnUIThread(() => incomingVideoFrameRenderer?.RenderRawVideoFrame(frame));
            }
        }

        private async void RawVideoFrameCaptured(object sender, RawVideoFrame frame)
        {
            using (frame)
            {
                await this.RunOnUIThread(() => outgoingVideoFrameRenderer?.RenderRawVideoFrame(frame));
            }
        }

        private async Task StartRemotePreview()
        {
            if (incomingVideoStreamRenderer == null)
            {
                await this.RunOnUIThread(async () =>
                {
                    incomingVideoStreamRenderer = new VideoStreamRenderer(remoteIncomingVideoStream);
                    incomingVideoContainer.Children.Add(incomingVideoStreamRenderer.GetView());
                    await incomingVideoStreamRenderer.StartPreviewAsync();
                });
            }
        }

        private async Task StopRemotePreview()
        {
            if (incomingVideoStreamRenderer != null)
            {
                await this.RunOnUIThread(async () =>
                {
                    incomingVideoContainer.Children.Remove(incomingVideoStreamRenderer.GetView());
                    await incomingVideoStreamRenderer.StopPreviewAsync();
                    incomingVideoStreamRenderer = null;
                });
            }
        }

        private async Task StartRawIncomingPreview()
        {
            if (incomingVideoFrameRenderer == null)
            {
                await this.RunOnUIThread(() =>
                {
                    incomingVideoFrameRenderer = new VideoFrameRenderer();
                    incomingVideoContainer.Background = new SolidColorBrush(Colors.Black);
                    incomingVideoContainer.Children.Add(incomingVideoFrameRenderer.GetView());
                });
            }
        }

        private async Task StopRawIncomingPreview()
        {
            if (incomingVideoFrameRenderer != null)
            {
                await this.RunOnUIThread(() =>
                {
                    incomingVideoContainer.Children.Remove(incomingVideoFrameRenderer.GetView());
                    incomingVideoContainer.Background = null;
                    incomingVideoFrameRenderer.ClearView();
                    incomingVideoFrameRenderer = null;
                });
            }
        }

        private async Task StartLocalPreview()
        {
            if (outgoingVideoStreamRenderer == null)
            {
                await this.RunOnUIThread(async () =>
                {
                    outgoingVideoStreamRenderer = new VideoStreamRenderer(localOutgoingVideoStream);
                    outgoingVideoContainer.Children.Add(outgoingVideoStreamRenderer.GetView());
                    await outgoingVideoStreamRenderer.StartPreviewAsync();
                });
            }
        }

        private async Task StopLocalPreview()
        {
            if (outgoingVideoStreamRenderer != null)
            {
                await this.RunOnUIThread(async () =>
                {
                    outgoingVideoContainer.Children.Remove(outgoingVideoStreamRenderer.GetView());
                    await outgoingVideoStreamRenderer.StopPreviewAsync();
                    outgoingVideoStreamRenderer = null;
                });
            }
        }

        private async Task StartCameraCaptureService()
        {
            if (cameraCaptureService == null)
            {
                cameraCaptureService = new CameraCaptureService(virtualOutgoingVideoStream,
                    cameraList[selectedCameraListIndex]);
                cameraCaptureService.FrameArrived += RawVideoFrameCaptured;
                await cameraCaptureService.StartAsync();

                await this.RunOnUIThread(() =>
                {
                    outgoingVideoFrameRenderer = new VideoFrameRenderer();
                    outgoingVideoContainer.Background = new SolidColorBrush(Colors.Black);
                    outgoingVideoContainer.Children.Add(outgoingVideoFrameRenderer.GetView());
                });
            }
        }

        private async Task StopCameraCaptureService()
        {
            if (cameraCaptureService != null)
            {
                await this.RunOnUIThread(() =>
                {
                    outgoingVideoContainer.Children.Remove(outgoingVideoFrameRenderer.GetView());
                    outgoingVideoContainer.Background = null;
                    outgoingVideoFrameRenderer.ClearView();
                    outgoingVideoFrameRenderer = null;
                });

                cameraCaptureService.FrameArrived -= RawVideoFrameCaptured;
                await cameraCaptureService.StopAsync();
                cameraCaptureService = null;
            }
        }

        private async Task StartScreenShareCaptureService()
        {
            if (screenCaptureService == null)
            {
                screenCaptureService = new ScreenCaptureService(screenShareOutgoingVideoStream,
                    displayList[selectedDisplayListIndex]);
                screenCaptureService.FrameArrived += RawVideoFrameCaptured;
                screenCaptureService.Start();

                await this.RunOnUIThread(() =>
                {
                    outgoingVideoFrameRenderer = new VideoFrameRenderer();
                    outgoingVideoContainer.Background = new SolidColorBrush(Colors.Black);
                    outgoingVideoContainer.Children.Add(outgoingVideoFrameRenderer.GetView());
                });
            }
        }

        private async Task StopScreenShareCaptureService()
        {
            if (screenCaptureService != null)
            {
                await this.RunOnUIThread(() =>
                {
                    outgoingVideoContainer.Children.Remove(outgoingVideoFrameRenderer.GetView());
                    outgoingVideoContainer.Background = null;
                    outgoingVideoFrameRenderer.ClearView();
                    outgoingVideoFrameRenderer = null;
                });

                screenCaptureService.FrameArrived -= RawVideoFrameCaptured;
                screenCaptureService.Stop();
                screenCaptureService = null;
            }
        }

        private async void EndCall(object sender, RoutedEventArgs args)
        {
            if (!callInProgress)
            {
                return;
            }

            loadingProgressRing.IsActive = true;
            try
            {
                if (call != null)
                {
                    foreach (RemoteParticipant remoteParticipant in call.RemoteParticipants)
                    {
                        remoteParticipant.VideoStreamStateChanged -= OnVideoStreamStateChanged;
                    }

                    call.RemoteParticipantsUpdated -= OnRemoteParticipantsUpdated;

                    await StopRemotePreview();
                    await StopRawIncomingPreview();

                    incomingVideoStream = null;
                    remoteIncomingVideoStream = null;
                    rawIncomingVideoStream = null;

                    await StopLocalPreview();
                    await StopCameraCaptureService();
                    await StopScreenShareCaptureService();

                    virtualOutgoingVideoStream = null;
                    screenShareOutgoingVideoStream = null;
                    localOutgoingVideoStream = null;

                    if (outgoingVideoStream != null)
                    {
                        outgoingVideoStream.StateChanged -= OnVideoStreamStateChanged;
                        await call.StopVideoAsync(outgoingVideoStream);
                        outgoingVideoStream = null;
                    }

                    await call.HangUpAsync(new HangUpOptions());
                    call = null;
                }

                callInProgress = false;

                await this.RunOnUIThread(() =>
                {
                    callSettingsContainer.Visibility = Visibility.Visible;
                    videoContainer.Visibility = Visibility.Collapsed;
                });
            }
            catch (Exception ex)
            {
                ShowMessage("Call failed to stop");
                Console.WriteLine(ex.Message);
            }

            loadingProgressRing.IsActive = false;
        }

        private bool ValidateCallSettings()
        {
            string meetingLink = meetingLinkTextBox.Text;
            if (string.IsNullOrEmpty(meetingLink) || !meetingLink.StartsWith("https://"))
            {
                ShowMessage("Invalid teams meeting link");
                return false;
            }

            bool isValid = true;
            switch (outgoingVideoStreamKind)
            {
                case VideoStreamKind.LocalOutgoing:
                    isValid = selectedVideoDeviceInfoListIndex != -1;
                    break;
                case VideoStreamKind.VirtualOutgoing:
                    isValid = selectedCameraListIndex != -1;
                    break;
                case VideoStreamKind.ScreenShareOutgoing:
                    isValid = selectedDisplayListIndex != -1;
                    break;
            }

            return isValid;
        }

        private async void ShowMessage(string message)
        {
            await this.RunOnUIThread(async () =>
            {
                await new MessageDialog(message).ShowAsync();
            });
        }

        private void IncomingVideoStreamKindSelected(object sender, SelectionChangedEventArgs args)
        {
            incomingVideoStreamKind =
                incomingVideoStreamKindList[incomingVideoStreamKindComboBox.SelectedIndex];
        }

        private void OutgoingVideoStreamKindSelected(object sender, SelectionChangedEventArgs args)
        {
            outgoingVideoStreamKind =
                outgoingVideoStreamKindList[outgoingVideoStreamKindComboBox.SelectedIndex];

            switch (outgoingVideoStreamKind)
            {
                case VideoStreamKind.LocalOutgoing:
                    videoDeviceInfoComboBox.Visibility = Visibility.Visible;
                    cameraComboBox.Visibility = Visibility.Collapsed;
                    displayComboBox.Visibility = Visibility.Collapsed;
                    break;
                case VideoStreamKind.VirtualOutgoing:
                    videoDeviceInfoComboBox.Visibility = Visibility.Collapsed;
                    cameraComboBox.Visibility = Visibility.Visible;
                    displayComboBox.Visibility = Visibility.Collapsed;
                    break;
                case VideoStreamKind.ScreenShareOutgoing:
                    videoDeviceInfoComboBox.Visibility = Visibility.Collapsed;
                    cameraComboBox.Visibility = Visibility.Collapsed;
                    displayComboBox.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void VideoDeviceInfoSelected(object sender, SelectionChangedEventArgs args)
        {
            selectedVideoDeviceInfoListIndex = videoDeviceInfoComboBox.SelectedIndex;
        }

        private void CameraSelected(object sender, SelectionChangedEventArgs args)
        {
            selectedCameraListIndex = cameraComboBox.SelectedIndex;
        }

        private void DisplaySelected(object sender, SelectionChangedEventArgs args)
        {
            selectedDisplayListIndex = displayComboBox.SelectedIndex;
        }
    }
}
