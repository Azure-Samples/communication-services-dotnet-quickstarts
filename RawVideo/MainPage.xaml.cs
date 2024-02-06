using Azure.Communication.Calling.WindowsClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Graphics.Capture;
using Windows.Media.Capture.Frames;
using Windows.Security.Authorization.AppCapabilityAccess;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

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
        private AppCapabilityAccessStatus screenSharePermissionStatus;
        private int w = 0;
        private int h = 0;
        private int framerate = 0;
        private bool callInProgress = false;

        public MainPage()
        {
            InitializeComponent();
            InitializeTestCase();
        }

        private async void InitializeTestCase()
        {
            framerate = 30;
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

            incomingVideoStreamKindComboBox.SelectedIndex = 1;
            outgoingVideoStreamKindComboBox.SelectedIndex = 1;

            await CreateCallAgent();

            videoDeviceInfoList = deviceManager.Cameras.OrderBy(item => item.Name).ToList();
            foreach (VideoDeviceDetails item in videoDeviceInfoList)
            {
                videoDeviceInfoComboBox.Items.Add(item.Name);
            }

            if (videoDeviceInfoList.Count > 0)
            {
                videoDeviceInfoComboBox.SelectedIndex = 0;
            }

            cameraList = await CameraCaptureService.GetCameraList();
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
                displayComboBox.Items.Add(string.Format("{0}  ({1}x{2})",
                    item.DisplayName,
                    item.Size.Width,
                    item.Size.Height));
            }

            if (displayList.Count > 0)
            {
                displayComboBox.SelectedIndex = 0;
            }
        }

        private async Task GetScreenSharePermission()
        {
            screenSharePermissionStatus = AppCapabilityAccessStatus.UserPromptRequired;
            try
            {
                screenSharePermissionStatus = await GraphicsCaptureAccess.RequestAccessAsync(
                    GraphicsCaptureAccessKind.Programmatic);
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
            }
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
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
            }
        }

        private async void StartCall(object sender, RoutedEventArgs e)
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

            try
            {
                call = await callAgent.JoinAsync(locator, joinCallOptions);

                await call.MuteOutgoingAudioAsync();
                await call.MuteIncomingAudioAsync();

                await this.RunOnUIThread(() =>
                {
                    settingsContainer.Visibility = Visibility.Collapsed;
                    videoContainer.Visibility = Visibility.Visible;
                });
            }
            catch
            {
                callInProgress = false;
            }

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
                    GraphicsCaptureItem item = displayList[selectedDisplayListIndex];
                    w = item.Size.Width;
                    h = item.Size.Height;

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

                    break;
            }
        }

        private async void OnIncomingVideoStreamStateChanged(IncomingVideoStream stream)
        {
            switch (stream.State)
            {
                case VideoStreamState.Available:
                    if (incomingVideoStream == null)
                    {
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
                    }

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

        private async void FrameArrived(object sender, RawVideoFrame frame)
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
                    await incomingVideoStreamRenderer.StopPreviewAsync();
                    incomingVideoContainer.Children.Remove(incomingVideoStreamRenderer.GetView());
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
                    incomingVideoFrameRenderer.ClearView();
                    incomingVideoContainer.Children.Remove(incomingVideoFrameRenderer.GetView());
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
                ;
                await this.RunOnUIThread(async () =>
                {
                    await outgoingVideoStreamRenderer.StopPreviewAsync();
                    outgoingVideoContainer.Children.Remove(outgoingVideoStreamRenderer.GetView());
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
                cameraCaptureService.FrameArrived += FrameArrived;
                await cameraCaptureService.StartAsync();

                await this.RunOnUIThread(() =>
                {
                    outgoingVideoFrameRenderer = new VideoFrameRenderer();
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
                    outgoingVideoFrameRenderer.ClearView();
                    outgoingVideoContainer.Children.Remove(outgoingVideoFrameRenderer.GetView());
                    outgoingVideoFrameRenderer = null;
                });

                cameraCaptureService.FrameArrived -= FrameArrived;
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
                screenCaptureService.FrameArrived += FrameArrived;
                screenCaptureService.Start();

                await this.RunOnUIThread(() =>
                {
                    outgoingVideoFrameRenderer = new VideoFrameRenderer();
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
                    outgoingVideoFrameRenderer.ClearView();
                    outgoingVideoContainer.Children.Remove(outgoingVideoFrameRenderer.GetView());
                    outgoingVideoFrameRenderer = null;
                });

                screenCaptureService.FrameArrived -= FrameArrived;
                screenCaptureService.Stop();
                screenCaptureService = null;
            }
        }

        private async void EndCall(object sender, RoutedEventArgs e)
        {
            if (!callInProgress)
            {
                return;
            }

            try
            {
                if (call != null)
                {
                    call.RemoteParticipantsUpdated -= OnRemoteParticipantsUpdated;

                    await StopRemotePreview();
                    await StopRawIncomingPreview();

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
                    settingsContainer.Visibility = Visibility.Visible;
                    videoContainer.Visibility = Visibility.Collapsed;
                });
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
            }
        }

        private bool ValidateCallSettings()
        {
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
