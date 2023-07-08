using Azure.Communication.Calling.WindowsClient;
using CallingTestApp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Graphics.Capture;
using Windows.Security.Authorization.AppCapabilityAccess;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace CallingQuickstart
{
    public sealed partial class MainPage : Page
    {
        private Dictionary<int, IncomingVideoStream> incomingVideoStreamDictionary;
        private List<RemoteParticipant> remoteParticipantList = new List<RemoteParticipant>();
        private List<GraphicsCaptureItem> displayList;
        private List<VideoStreamKind> outgoingVideoStreamKindList;
        private CallClient callClient;
        private CallAgent callAgent;
        private CommunicationCall call;
        private RawOutgoingVideoStream rawOutgoingVideoStream;
        private VideoFrameRenderer incomingVideoFrameRenderer;
        private VideoStreamKind outgoingVideoStreamKind;
        private VideoStreamKind incomingVideoStreamKind;
        private ScreenCaptureService screenCaptureService;
        private VideoFrameSender videoFrameSender;
        private AppCapabilityAccessStatus screenSharePermissionStatus;
        private int w = 0;
        private int h = 0;
        private int framerate = 0;
        private bool callInProgress = false;
        private int displayListIndex = -1;

        public MainPage()
        {
            InitializeComponent();

            InitializeUIVariables();

            incomingVideoFrameRenderer = new VideoFrameRenderer(incomingVideoContainer, 640, 360);
        }

        private async void InitializeUIVariables()
        {
            incomingVideoStreamDictionary = new Dictionary<int, IncomingVideoStream>();

            outgoingVideoStreamKindList = new List<VideoStreamKind>
            {
                VideoStreamKind.VirtualOutgoing,
                VideoStreamKind.ScreenShareOutgoing
            };

            outgoingVideoStreamKindPicker.Items.Add("Virtual");
            outgoingVideoStreamKindPicker.Items.Add("ScreenShare");
            outgoingVideoStreamKindPicker.SelectedIndex = 0;

            displayList = ScreenCaptureService.GetDisplayList();
            if (displayList.Count == 0)
            {
                await GetScreenSharePermission();

                if (screenSharePermissionStatus == AppCapabilityAccessStatus.Allowed)
                {
                    displayList = ScreenCaptureService.GetDisplayList();
                }
            }

            foreach (GraphicsCaptureItem item in displayList)
            {
                displayPicker.Items.Add(string.Format("{0}  ({1}x{2})",
                    item.DisplayName,
                    item.Size.Width,
                    item.Size.Height));
            }

            if (displayList.Count > 0)
            {
                displayPicker.SelectedIndex = 0;
            }
        }

        private async Task CreateCallAgent()
        {
            try
            {
                var credential = new CallTokenCredential(TokenTextBox.Text);

                callClient = new CallClient();

                var callAgentOptions = new CallAgentOptions
                {
                    DisplayName = "Windows User"
                };

                callAgent = await callClient.CreateCallAgentAsync(credential, callAgentOptions);
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
            }
        }

        private async Task GetScreenSharePermission()
        {
            screenSharePermissionStatus = AppCapabilityAccessStatus.UserPromptRequired;

            try
            {
                screenSharePermissionStatus = await GraphicsCaptureAccess.RequestAccessAsync(GraphicsCaptureAccessKind.Programmatic);
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
            }
        }

        private async void StartCall(object sender, RoutedEventArgs e)
        {
            if (outgoingVideoStreamKind == VideoStreamKind.ScreenShareOutgoing && displayListIndex == -1)
            {
                return;
            }

            if (callInProgress)
            {
                return;
            }

            callInProgress = true;
            if (callClient == null)
            {
                await CreateCallAgent();
            }

            var incomingVideoOptions = new IncomingVideoOptions
            {
                StreamKind = VideoStreamKind.RawIncoming,
                FrameKind = RawVideoFrameKind.Buffer
            };

            OutgoingVideoOptions outgoingVideoOptions = CreateOutgoingVideoOptions();

            var joinCallOptions = new JoinCallOptions
            {
                IncomingVideoOptions = incomingVideoOptions,
                OutgoingVideoOptions = outgoingVideoOptions
            };

            var locator = new TeamsMeetingLinkLocator(MeetingLinkTextBox.Text);

            try
            {
                call = await callAgent.JoinAsync(locator, joinCallOptions);
                await call.MuteOutgoingAudioAsync();
                await call.MuteIncomingAudioAsync();
            }
            catch (Exception ex)
            {
                callInProgress = false;
            }

            if (call != null)
            {
                AddRemoteParticipants(call.RemoteParticipants);

                call.RemoteParticipantsUpdated += OnRemoteParticipantsUpdated;
            }
        }

        private void OnRemoteParticipantsUpdated(object sender, ParticipantsUpdatedEventArgs args)
        {
            AddRemoteParticipants(args.AddedParticipants);

            foreach (RemoteParticipant remoteParticipant in args.RemovedParticipants)
            {
                remoteParticipant.VideoStreamStateChanged -= OnVideoStreamStateChanged;
                remoteParticipantList.Remove(remoteParticipant);
            }
        }

        private void AddRemoteParticipants(IReadOnlyList<RemoteParticipant> remoteParticipantList)
        {
            foreach (RemoteParticipant remoteParticipant in remoteParticipantList)
            {
                IReadOnlyList<IncomingVideoStream> incomingVideoStreamList = remoteParticipant.IncomingVideoStreams;
                foreach (IncomingVideoStream incomingVideoStream in incomingVideoStreamList)
                {
                    OnIncomingVideoStreamStateChanged(incomingVideoStream);
                }

                remoteParticipant.VideoStreamStateChanged += OnVideoStreamStateChanged;
                this.remoteParticipantList.Add(remoteParticipant);
            }
        }

        private VideoStreamFormat CreateVideoStreamFormat()
        {
            framerate = 15;

            var format = new VideoStreamFormat
            {
                PixelFormat = VideoStreamPixelFormat.Rgba,
                FramesPerSecond = framerate
            };

            switch (outgoingVideoStreamKind)
            {
                case VideoStreamKind.VirtualOutgoing:
                    w = 640;
                    h = 360;
                    format.Resolution = VideoStreamResolution.P360;
                    break;
                case VideoStreamKind.ScreenShareOutgoing:
                    GetDisplaySize();
                    format.Width = w;
                    format.Height = h;
                    break;
            }

            format.Stride1 = w * 4;

            return format;
        }

        private OutgoingVideoOptions CreateOutgoingVideoOptions()
        {
            VideoStreamFormat videoFormat = CreateVideoStreamFormat();

            var rawOutgoingVideoStreamOptions = new RawOutgoingVideoStreamOptions
            {
                Formats = new VideoStreamFormat[] { videoFormat }
            };

            switch (outgoingVideoStreamKind)
            {
                case VideoStreamKind.VirtualOutgoing:
                    rawOutgoingVideoStream = new VirtualOutgoingVideoStream(rawOutgoingVideoStreamOptions);
                    break;
                case VideoStreamKind.ScreenShareOutgoing:
                    rawOutgoingVideoStream = new ScreenShareOutgoingVideoStream(rawOutgoingVideoStreamOptions);
                    break;
            }

            rawOutgoingVideoStream.FormatChanged += OnVideoStreamFormatChanged;
            rawOutgoingVideoStream.StateChanged += OnVideoStreamStateChanged;

            return new OutgoingVideoOptions()
            {
                Streams = new OutgoingVideoStream[] { rawOutgoingVideoStream }
            };
        }

        private void OnVideoStreamStateChanged(object sender, VideoStreamStateChangedEventArgs args)
        {
            CallVideoStream callVideoStream = args.Stream;

            switch (callVideoStream.Direction)
            {
                case StreamDirection.Outgoing:
                    OnOutgoingVideoStreamStateChanged(callVideoStream as OutgoingVideoStream);
                    break;
                case StreamDirection.Incoming:
                    OnIncomingVideoStreamStateChanged(callVideoStream as IncomingVideoStream);
                    break;
            }
        }

        private async void OnOutgoingVideoStreamStateChanged(OutgoingVideoStream outgoingVideoStream)
        {
            switch (outgoingVideoStream.State)
            {
                case VideoStreamState.Started:
                    switch (outgoingVideoStream.Kind)
                    {
                        case VideoStreamKind.VirtualOutgoing:
                            if (videoFrameSender == null)
                            {
                                videoFrameSender = new VideoFrameSender(outgoingVideoStream as RawOutgoingVideoStream);
                            }

                            videoFrameSender.Start();
                            break;
                        case VideoStreamKind.ScreenShareOutgoing:
                            await GetScreenSharePermission();

                            if (screenSharePermissionStatus == AppCapabilityAccessStatus.Allowed)
                            {
                                screenCaptureService = new ScreenCaptureService(
                                    rawOutgoingVideoStream,
                                    displayList[displayListIndex]);

                                screenCaptureService?.Start();
                            }

                            break;
                    }

                    break;
                case VideoStreamState.Stopped:
                    switch (outgoingVideoStream.Kind)
                    {
                        case VideoStreamKind.VirtualOutgoing:
                            videoFrameSender?.Stop();
                            break;
                        case VideoStreamKind.ScreenShareOutgoing:
                            screenCaptureService?.Stop();
                            break;
                    }

                    break;
            }
        }

        private async void OnIncomingVideoStreamStateChanged(IncomingVideoStream incomingVideoStream)
        {
            switch (incomingVideoStream.State)
            {
                case VideoStreamState.Available:
                    {
                        if (!incomingVideoStreamDictionary.ContainsKey(incomingVideoStream.Id))
                        {
                            var rawIncomingVideoStream = incomingVideoStream as RawIncomingVideoStream;
                            rawIncomingVideoStream.RawVideoFrameReceived += RawVideoFrameReceived;
                            rawIncomingVideoStream.Start();

                            incomingVideoStreamDictionary.Add(incomingVideoStream.Id, incomingVideoStream);
                        }

                        break;
                    }
                case VideoStreamState.Stopped:
                    await this.RunOnUIThread(() => incomingVideoFrameRenderer.ClearView());
                    break;
                case VideoStreamState.NotAvailable:
                    if (incomingVideoStreamDictionary.ContainsKey(incomingVideoStream.Id))
                    {
                        if (incomingVideoStreamKind == VideoStreamKind.RawIncoming)
                        {
                            var rawIncomingVideoStream = incomingVideoStreamDictionary[incomingVideoStream.Id] as RawIncomingVideoStream;
                            rawIncomingVideoStream.RawVideoFrameReceived -= RawVideoFrameReceived;
                        }

                        incomingVideoStreamDictionary.Remove(incomingVideoStream.Id);
                    }

                    break;
            }
        }

        private async void RawVideoFrameReceived(object sender, RawVideoFrameReceivedEventArgs args)
        {
            using (RawVideoFrame rawVideoFrame = args.Frame)
            {
                await this.RunOnUIThread(() => incomingVideoFrameRenderer.RenderRawVideoFrame(rawVideoFrame as RawVideoFrameBuffer));
            }
        }

        private void OnVideoStreamFormatChanged(object sender, VideoStreamFormatChangedEventArgs args)
        {
            VideoStreamFormat videoStreamFormat = args.Format;
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
                    if (videoFrameSender != null)
                    {
                        videoFrameSender.Stop();
                        videoFrameSender = null;
                    }

                    if (screenCaptureService != null)
                    {
                        screenCaptureService.Stop();
                        screenCaptureService = null;
                    }

                    rawOutgoingVideoStream.StateChanged -= OnVideoStreamStateChanged;
                    call.RemoteParticipantsUpdated -= OnRemoteParticipantsUpdated;

                    if (rawOutgoingVideoStream != null)
                    {
                        await call.StopVideoAsync(rawOutgoingVideoStream);
                    }

                    await call.HangUpAsync(new HangUpOptions());
                    call = null;
                }

                incomingVideoStreamDictionary.Clear();
                callInProgress = false;

                await this.RunOnUIThread(() => incomingVideoFrameRenderer.ClearView());
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
            }
        }

        private void GetDisplaySize()
        {
            GraphicsCaptureItem item = displayList[displayListIndex];
            w = item.Size.Width;
            h = item.Size.Height;
        }

        private void OutgoingVideoStreamKindSelected(object sender, SelectionChangedEventArgs args)
        {
            outgoingVideoStreamKind = outgoingVideoStreamKindList[outgoingVideoStreamKindPicker.SelectedIndex];

            switch (outgoingVideoStreamKindList[outgoingVideoStreamKindPicker.SelectedIndex])
            {
                case VideoStreamKind.VirtualOutgoing:
                    displayPicker.Visibility = Visibility.Collapsed;
                    break;
                case VideoStreamKind.ScreenShareOutgoing:
                    displayPicker.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void DisplaySelected(object sender, SelectionChangedEventArgs args)
        {
            displayListIndex = displayPicker.SelectedIndex;
        }
    }

    public static class UIHelper
    {
        public static Task RunOnUIThread(this Page p, Action a)
        {
            return p.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { a(); }).AsTask();
        }
    }
}
