using Azure.Communication.Calling.WindowsClient;
using System;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Core;
using Buffer = Windows.Storage.Streams.Buffer;

namespace CallingTestApp
{
    internal abstract class CaptureService
    {
        protected readonly RawOutgoingVideoStream rawOutgoingVideoStream;

        protected CaptureService(RawOutgoingVideoStream rawOutgoingVideoStream) 
        {
            this.rawOutgoingVideoStream = rawOutgoingVideoStream;
        }

        protected async Task SendRawVideoFrame(SoftwareBitmap bitmap)
        {
            if (bitmap != null && CanSendRawVideoFrames())
            {
                RawVideoFrame rawVideoFrame = ConvertSoftwareBitmapToRawVideoFrame(bitmap);

                using (rawVideoFrame)
                {
                    await rawOutgoingVideoStream.SendRawVideoFrameAsync(rawVideoFrame);
                }
            }
        }

        private unsafe RawVideoFrame ConvertSoftwareBitmapToRawVideoFrame(SoftwareBitmap bitmap)
        {
            bitmap = SoftwareBitmap.Convert(bitmap, BitmapPixelFormat.Rgba8);

            int w = bitmap.PixelWidth;
            int h = bitmap.PixelHeight;
            uint rgbaCapacity = (uint)(w * h * 4);

            var rgbaBuffer = new Buffer(rgbaCapacity)
            {
                Length = rgbaCapacity
            };

            bitmap.CopyToBuffer(rgbaBuffer);

            var timeSpan = new TimeSpan(rawOutgoingVideoStream.TimestampInTicks);
            var mediaStreamSample = MediaStreamSample.CreateFromBuffer(rgbaBuffer, timeSpan);

            return new RawVideoFrameTexture()
            {
                Texture = mediaStreamSample,
                StreamFormat = rawOutgoingVideoStream.Format
            };
        }

        private bool CanSendRawVideoFrames()
        {
            return rawOutgoingVideoStream != null &&
                    rawOutgoingVideoStream.Format != null &&
                    rawOutgoingVideoStream.State == VideoStreamState.Started;
        }
    }
}
