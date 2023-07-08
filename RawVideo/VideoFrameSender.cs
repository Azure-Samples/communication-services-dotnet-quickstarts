using Azure.Communication.Calling.WindowsClient;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

namespace CallingTestApp
{
    public class VideoFrameSender
    {
        private readonly RawOutgoingVideoStream rawOutgoingVideoStream;
        private readonly Random random = new Random();
        private Thread frameIteratorThread;
        private volatile bool stopFrameIterator = false;

        public VideoFrameSender(RawOutgoingVideoStream rawOutgoingVideoStream)
        {
            this.rawOutgoingVideoStream = rawOutgoingVideoStream;
        }

        public async void VideoFrameIterator()
        {
            while (!stopFrameIterator)
            {
                if (CanSendRawVideoFrames())
                {
                    await SendRandomVideoFrame();
                }
            }
        }

        private async Task SendRandomVideoFrame()
        {
            RawVideoFrame videoFrame = GenerateRawVideoFrame();

            try
            {
                using (videoFrame)
                {
                    await rawOutgoingVideoStream.SendRawVideoFrameAsync(videoFrame);
                }

                int delayBetweenFrames = (int)(1000.0 / rawOutgoingVideoStream.Format.FramesPerSecond);
                await Task.Delay(delayBetweenFrames);
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
            }
        }

        private unsafe RawVideoFrame GenerateRawVideoFrame()
        {
            var format = rawOutgoingVideoStream.Format;
            int w = format.Width;
            int h = format.Height;
            int rgbaCapacity = w * h * 4;

            var rgbaBuffer = new MemoryBuffer((uint) rgbaCapacity);

            byte* rgbaArrayBuffer = BufferExtensions.GetArrayBuffer(rgbaBuffer);

            byte r = (byte)random.Next(1, 255);
            byte g = (byte)random.Next(1, 255);
            byte b = (byte)random.Next(1, 255);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x += 4)
                {
                    rgbaArrayBuffer[(w * 4 * y) + x + 0] = (byte)(y % r);
                    rgbaArrayBuffer[(w * 4 * y) + x + 1] = (byte)(y % g);
                    rgbaArrayBuffer[(w * 4 * y) + x + 2] = (byte)(y % b);
                    rgbaArrayBuffer[(w * 4 * y) + x + 3] = 255;
                }
            }

            return new RawVideoFrameBuffer()
            {
                Buffers = new MemoryBuffer[] { rgbaBuffer },
                StreamFormat = rawOutgoingVideoStream.Format
            };
        }

        public void Start()
        {
            frameIteratorThread = new Thread(VideoFrameIterator);
            frameIteratorThread.Start();
        }

        public void Stop()
        {
            try
            {
                if (frameIteratorThread != null)
                {
                    stopFrameIterator = true;
                    frameIteratorThread.Join();
                    frameIteratorThread = null;
                    stopFrameIterator = false;
                }
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
            }
        }

        private bool CanSendRawVideoFrames()
        {
            return rawOutgoingVideoStream != null && 
                rawOutgoingVideoStream.Format != null && 
                rawOutgoingVideoStream.State == VideoStreamState.Started;
        }
    }
}
