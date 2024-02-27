using Azure.Communication.Calling.WindowsClient;
using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace RawVideo
{
    internal class VideoStreamRenderer
    {
        private readonly MediaElement mediaElement;
        private readonly CallVideoStream stream;

        public VideoStreamRenderer(CallVideoStream stream)
        {
            this.stream = stream;

            mediaElement = new MediaElement
            {
                // Fit
                Stretch = Stretch.Uniform,
                AutoPlay = true
            };

            RelativePanel.SetAlignVerticalCenterWithPanel(mediaElement, true);
            RelativePanel.SetAlignHorizontalCenterWithPanel(mediaElement, true);
        }

        public async Task StartPreviewAsync()
        {
            Uri uri = null;
            switch (stream.Kind)
            {
                case VideoStreamKind.LocalOutgoing:
                    var localVideoStream = stream as LocalOutgoingVideoStream;
                    uri = await localVideoStream.StartPreviewAsync();
                    break;
                case VideoStreamKind.RemoteIncoming:
                    var remoteIncomingVideoStream = stream as RemoteIncomingVideoStream;
                    uri = await remoteIncomingVideoStream.StartPreviewAsync();
                    break;
            }

            mediaElement.Source = uri;
        }

        public async Task StopPreviewAsync()
        {
            switch (stream.Kind)
            {
                case VideoStreamKind.LocalOutgoing:
                    var localVideoStream = stream as LocalOutgoingVideoStream;
                    await localVideoStream.StopPreviewAsync();
                    break;
                case VideoStreamKind.RemoteIncoming:
                    var remoteIncomingVideoStream = stream as RemoteIncomingVideoStream;
                    await remoteIncomingVideoStream.StopPreviewAsync();
                    break;
            }

            mediaElement.Source = null;
        }

        public UIElement GetView()
        {
            return mediaElement;
        }
    }
}
