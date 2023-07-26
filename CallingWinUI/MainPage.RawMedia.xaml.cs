using Azure.Communication.Calling.WindowsClient;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Streams;
using WinRT;

namespace CallingQuickstart
{
    public sealed partial class MainPage : Page
    {
        // Set tryRawMedia to true to enable raw media
        private bool tryRawMedia = false;

        // Video frame specs
        private const int width = 640;
        private const int height = 360;
        private const int framerate = 15;

        // Sample of using VirtualOutgoingVideoStream
        private VirtualOutgoingVideoStream virtualOutgoingVideoStream = new VirtualOutgoingVideoStream(
            new RawOutgoingVideoStreamOptions {
                Formats = new VideoStreamFormat[] {
                    new VideoStreamFormat()
                    {
                        PixelFormat = VideoStreamPixelFormat.Rgba,
                        FramesPerSecond = framerate,
                        Resolution = VideoStreamResolution.P360,
                        Stride1 = width * 4
                    }
                }
            });

        private Random random = new Random();

        private CancellationTokenSource videoFrameGeneratorCancellationToken = new CancellationTokenSource();

        #region Buffer interop
        [ComImport]
        [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        unsafe interface IMemoryBufferByteAccess
        {
            void GetBuffer(out byte* buffer, out uint capacity);
        }

        [ComImport]
        [Guid("905A0FEF-BC53-11DF-8C49-001E4FC686DA")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        unsafe interface IBufferByteAccess
        {
            void Buffer(out byte* buffer);
        }

        internal static class BufferExtensions
        {
            public static unsafe byte* GetArrayBuffer(IMemoryBuffer memoryBuffer)
            {
                IMemoryBufferReference memoryBufferReference = memoryBuffer.CreateReference();
                var memoryBufferByteAccess = memoryBufferReference.As<IMemoryBufferByteAccess>();

                memoryBufferByteAccess.GetBuffer(out byte* arrayBuffer, out uint arrayBufferCapacity);

                GC.AddMemoryPressure(arrayBufferCapacity);

                return arrayBuffer;
            }

            public static unsafe byte* GetArrayBuffer(IBuffer buffer)
            {
                var bufferByteAccess = buffer as IBufferByteAccess;

                bufferByteAccess.Buffer(out byte* arrayBuffer);
                uint arrayBufferCapacity = buffer.Capacity;

                GC.AddMemoryPressure(arrayBufferCapacity);

                return arrayBuffer;
            }
        }
        #endregion

        #region API event handlers
        private void OnOutgoingVideoStreamStateChanged(OutgoingVideoStream outgoingVideoStream)
        {
            if (!tryRawMedia) return;

            switch (outgoingVideoStream.State)
            {
                case VideoStreamState.Available:
                    switch (outgoingVideoStream.Kind)
                    {
                        case VideoStreamKind.VirtualOutgoing:
                            var rawOutgoingVideoStream = outgoingVideoStream as RawOutgoingVideoStream;
                            if (rawOutgoingVideoStream != null)
                            {
                                var frameGenerator = new Task(async () => {
                                    while (!videoFrameGeneratorCancellationToken.IsCancellationRequested)
                                    {
                                        var videoFrame = GenerateRandomVideoFrameBuffer(rawOutgoingVideoStream.Format, (uint)(width * height * 4));
                                        await rawOutgoingVideoStream.SendRawVideoFrameAsync(videoFrame);
                                        await Task.Delay((int)(1000.0 / framerate));
                                    }
                                },
                                videoFrameGeneratorCancellationToken.Token);

                                frameGenerator.Start();
                            }
                            break;
                    }
                    break;

                case VideoStreamState.Started: break;
                case VideoStreamState.Stopping: break;
                case VideoStreamState.Stopped:
                    switch (outgoingVideoStream.Kind)
                    {
                        case VideoStreamKind.VirtualOutgoing:
                            videoFrameGeneratorCancellationToken.Cancel();
                            break;
                    }

                    break;

                case VideoStreamState.NotAvailable:
                    break;
            }
        }
        #endregion

        #region Raw video helpers
        private StartCallOptions GetStartCallOptionsWithRawMedia()
        {
            if (tryRawMedia)
            {
                virtualOutgoingVideoStream.StateChanged += OnVideoStreamStateChanged;
            }

            return tryRawMedia ? new StartCallOptions() {
                IncomingVideoOptions = new IncomingVideoOptions {
                    StreamKind = VideoStreamKind.RemoteIncoming,
                },
                OutgoingVideoOptions = new OutgoingVideoOptions() {
                    Streams = new OutgoingVideoStream[] { virtualOutgoingVideoStream } 
                },
                OutgoingAudioOptions = new OutgoingAudioOptions() { IsMuted = true, Stream = micStream  }
            } : null;
        }

        private StartTeamsCallOptions GetStartTeamsCallOptionsWithRawMedia()
        {
            if (tryRawMedia)
            {
                virtualOutgoingVideoStream.StateChanged += OnVideoStreamStateChanged;
            }

            return tryRawMedia ? new StartTeamsCallOptions()
            {
                IncomingVideoOptions = new IncomingVideoOptions
                {
                    StreamKind = VideoStreamKind.RemoteIncoming,
                },
                OutgoingVideoOptions = new OutgoingVideoOptions()
                {
                    Streams = new OutgoingVideoStream[] { virtualOutgoingVideoStream }
                },
                OutgoingAudioOptions = new OutgoingAudioOptions() { IsMuted = true, Stream = micStream }
            } : null;
        }

        private unsafe void GenerateRandomVideoFrame(byte** rgbaArrayBuffer, int w, int h)
        {
            byte r = (byte)random.Next(1, 255);
            byte g = (byte)random.Next(1, 255);
            byte b = (byte)random.Next(1, 255);

            int rgbaStride = w * 4;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < rgbaStride; x += 4)
                {
                    (*rgbaArrayBuffer)[(w * 4 * y) + x + 0] = (byte)(y % r);
                    (*rgbaArrayBuffer)[(w * 4 * y) + x + 1] = (byte)(y % g);
                    (*rgbaArrayBuffer)[(w * 4 * y) + x + 2] = (byte)(y % b);
                    (*rgbaArrayBuffer)[(w * 4 * y) + x + 3] = 255;
                }
            }
        }

        private unsafe RawVideoFrame GenerateRandomVideoFrameBuffer(VideoStreamFormat videoFormat, uint rgbaCapacity)
        {
            var rgbaBuffer = new MemoryBuffer(rgbaCapacity);

            byte* rgbaArrayBuffer = BufferExtensions.GetArrayBuffer(rgbaBuffer);

            GenerateRandomVideoFrame(&rgbaArrayBuffer, width, height);

            return new RawVideoFrameBuffer()
            {
                Buffers = new MemoryBuffer[] { rgbaBuffer },
                StreamFormat = videoFormat
            };
        }
        #endregion
    }
}
