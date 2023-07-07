using Azure.Communication.Calling.WindowsClient;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace CallingTestApp
{
    internal class VideoFrameRenderer : IDisposable
    {
        private static readonly string MediaEncodingSubtypes_NV12 = "{3231564E-0000-0010-8000-00AA00389B71}";

        private MediaElement mediaElement;
        private Image imageElement;
        private MediaPlayerElement mediaPlayerElement;
        private StackPanel parentView;
        private UIElement childView;

        public enum RenderType
        {
            MediaElement,
            ImageElement,
            MediaPlayerElement
        }

        public VideoFrameRenderer(StackPanel parentView, int w, int h)
        {
            childView = imageElement = new Image
            {
                Width = w,
                Height = h,
                Visibility = Visibility.Visible
            };

            this.parentView = parentView;
            parentView.Children.Add(childView);
        }

        public unsafe void RenderRawVideoFrame(RawVideoFrameBuffer videoFrameBuffer)
        {
            VideoStreamFormat videoStreamFormat = videoFrameBuffer.StreamFormat;
            int w = videoStreamFormat.Width;
            int h = videoStreamFormat.Height;

            var writeableBitmap = new WriteableBitmap(videoStreamFormat.Width, videoStreamFormat.Height);

            byte* sourceArrayBuffer = BufferExtensions.GetArrayBuffer(videoFrameBuffer.Buffers[0]);
            byte* destArrayBuffer = BufferExtensions.GetArrayBuffer(writeableBitmap.PixelBuffer);

            int rgbaCapacity = w * h * 4;

            for (int i = 0; i < rgbaCapacity; i += 4)
            {
                destArrayBuffer[i + 0] = sourceArrayBuffer[i + 2];
                destArrayBuffer[i + 1] = sourceArrayBuffer[i + 1];
                destArrayBuffer[i + 2] = sourceArrayBuffer[i + 0];
                destArrayBuffer[i + 3] = sourceArrayBuffer[i + 3];
            }

            imageElement.Source = writeableBitmap;
        }

        public void ClearView()
        {
            imageElement.Source = null;
        }

        public void Dispose()
        {
            ClearView();
        }
    }
}
