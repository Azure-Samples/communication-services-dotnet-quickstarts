using Azure.Communication.Calling.WindowsClient;
using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media.Core;
using Buffer = Windows.Storage.Streams.Buffer;

namespace RawVideo
{
    internal abstract class CaptureService
    {
        public event EventHandler<RawVideoFrame> FrameArrived;
        protected readonly RawOutgoingVideoStream stream;
        private readonly RawVideoFrameKind rawVideoFrameKind;

        protected CaptureService(RawOutgoingVideoStream stream, RawVideoFrameKind rawVideoFrameKind)
        {
            this.stream = stream;
            this.rawVideoFrameKind = rawVideoFrameKind;
        }

        protected async Task SendRawVideoFrame(SoftwareBitmap bitmap)
        {
            var format = stream.Format;
            if (bitmap != null && format != null && CanSendRawVideoFrames())
            {
                try
                {
                    RawVideoFrame frame = ConvertSoftwareBitmapToRawVideoFrame(bitmap, format);

                    await stream.SendRawVideoFrameAsync(frame);

                    FrameArrived?.Invoke(this, frame);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private unsafe RawVideoFrame ConvertSoftwareBitmapToRawVideoFrame(
            SoftwareBitmap bitmap, 
            VideoStreamFormat format)
        {
            bitmap = SoftwareBitmap.Convert(bitmap, BitmapPixelFormat.Rgba8);

            int w = bitmap.PixelWidth;
            int h = bitmap.PixelHeight;
            uint rgbaCapacity = (uint)(w * h * 4);

            var bitmapBuffer = new Buffer(rgbaCapacity)
            {
                Length = rgbaCapacity
            };

            bitmap.CopyToBuffer(bitmapBuffer);
            RawVideoFrame frame = null;

            switch (rawVideoFrameKind)
            {
                case RawVideoFrameKind.Buffer:
                    MemoryBuffer buffer = Buffer.CreateMemoryBufferOverIBuffer(bitmapBuffer);

                    frame = new RawVideoFrameBuffer()
                    {
                        Buffers = new MemoryBuffer[] { buffer },
                        StreamFormat = format
                    };

                    break;
                case RawVideoFrameKind.Texture:
                    var timeSpan = new TimeSpan(stream.TimestampInTicks);
                    var sample = MediaStreamSample.CreateFromBuffer(bitmapBuffer, timeSpan);

                    frame = new RawVideoFrameTexture()
                    {
                        Texture = sample,
                        StreamFormat = stream.Format
                    };

                    break;
            }

            return frame;
        }

        private bool CanSendRawVideoFrames()
        {
            return stream != null && stream.State == VideoStreamState.Started;
        }
    }
}
