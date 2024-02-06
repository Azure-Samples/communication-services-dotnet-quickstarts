using Azure.Communication.Calling.WindowsClient;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace RawVideo
{
    internal class VideoFrameRenderer
    {
        private readonly Image imageElement;

        public VideoFrameRenderer()
        {
            imageElement = new Image
            {
                //Fit
                Stretch = Stretch.Uniform
            };

            RelativePanel.SetAlignVerticalCenterWithPanel(imageElement, true);
            RelativePanel.SetAlignHorizontalCenterWithPanel(imageElement, true);
        }

        public unsafe void RenderRawVideoFrame(RawVideoFrame frame)
        {
            VideoStreamFormat format = frame.StreamFormat;
            int w = format.Width;
            int h = format.Height;
            int rgbaCapacity = w * h * 4;

            var bitmap = new WriteableBitmap(w, h);

            byte* sourceArrayBuffer = null;
            byte* destArrayBuffer = BufferExtensions.GetArrayBuffer(bitmap.PixelBuffer);

            switch (frame.Kind)
            {
                case RawVideoFrameKind.Buffer:
                    sourceArrayBuffer = BufferExtensions.GetArrayBuffer((frame as RawVideoFrameBuffer).Buffers[0]);
                    break;
                case RawVideoFrameKind.Texture:
                    sourceArrayBuffer = BufferExtensions.GetArrayBuffer((frame as RawVideoFrameTexture).Texture.Buffer);
                    break;
            }

            for (int i = 0; i < rgbaCapacity; i += 4)
            {
                destArrayBuffer[i + 0] = sourceArrayBuffer[i + 2];
                destArrayBuffer[i + 1] = sourceArrayBuffer[i + 1];
                destArrayBuffer[i + 2] = sourceArrayBuffer[i + 0];
                destArrayBuffer[i + 3] = sourceArrayBuffer[i + 3];
            }

            imageElement.Source = bitmap;
            (imageElement.Parent as RelativePanel).Background = new SolidColorBrush(Colors.Black);
        }

        public void ClearView()
        {
            imageElement.Source = null;
            (imageElement.Parent as RelativePanel).Background = null;
        }

        public UIElement GetView()
        {
            return imageElement;
        }
    }
}
