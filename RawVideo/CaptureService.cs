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
        protected readonly RawOutgoingVideoStream rawOutgoingVideoStream;
        private readonly RawVideoFrameKind rawVideoFrameKind;

        protected CaptureService(RawOutgoingVideoStream rawOutgoingVideoStream, RawVideoFrameKind rawVideoFrameKind)
        {
            this.rawOutgoingVideoStream = rawOutgoingVideoStream;
            this.rawVideoFrameKind = rawVideoFrameKind;
        }

        protected async Task SendRawVideoFrame(SoftwareBitmap bitmap)
        {
            if (bitmap != null && CanSendRawVideoFrames())
            {
                try
                {
                    RawVideoFrame frame = ConvertSoftwareBitmapToRawVideoFrame(bitmap);

                    await rawOutgoingVideoStream.SendRawVideoFrameAsync(frame);

                    FrameArrived?.Invoke(this, frame);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private unsafe RawVideoFrame ConvertSoftwareBitmapToRawVideoFrame(SoftwareBitmap bitmap)
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
                        StreamFormat = rawOutgoingVideoStream.Format
                    };

                    break;
                case RawVideoFrameKind.Texture:
                    var timeSpan = new TimeSpan(rawOutgoingVideoStream.TimestampInTicks);
                    var sample = MediaStreamSample.CreateFromBuffer(bitmapBuffer, timeSpan);

                    frame = new RawVideoFrameTexture()
                    {
                        Texture = sample,
                        StreamFormat = rawOutgoingVideoStream.Format
                    };

                    break;
            }

            return frame;
        }

        private bool CanSendRawVideoFrames()
        {
            return rawOutgoingVideoStream != null &&
                    rawOutgoingVideoStream.Format != null &&
                    rawOutgoingVideoStream.State == VideoStreamState.Started;
        }
    }
}
