using Azure.Communication.Calling.UnityClient;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Threading;
using UnityEngine;
using Random = System.Random;

public class OutgoingVideo : MonoBehaviour
{

    private struct PendingFrame
    {
        public RawVideoFrame frame;
        public RawVideoFrameKind kind;
    }

    private readonly Random random = new Random();
    private BackgroundWorker backgroundWorker;
    ConcurrentQueue<PendingFrame> pendingOutgoingFrames = new ConcurrentQueue<PendingFrame>();
    public RenderTexture rawOutgoingVideoRenderTexture;

    private void Update()
    {
        while (pendingOutgoingFrames.Count > 15)
        {
            pendingOutgoingFrames.TryDequeue(out PendingFrame pendingFrameDiscard);
            pendingFrameDiscard.frame.Dispose();
        }

        if (pendingOutgoingFrames.TryDequeue(out PendingFrame pendingFrame))
        {
            switch (pendingFrame.kind)
            {
                case RawVideoFrameKind.Buffer:
                    var videoFrameBuffer = pendingFrame.frame as RawVideoFrameBuffer;
                    VideoStreamFormat videoFormat = videoFrameBuffer.StreamFormat;
                    int width = videoFormat.Width;
                    int height = videoFormat.Height;
                    var texture = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false);

                    var buffers = videoFrameBuffer.Buffers;
                    NativeBuffer buffer = buffers.Count > 0 ? buffers[0] : null;
                    buffer.GetData(out IntPtr bytes, out int signedSize);

                    texture.LoadRawTextureData(bytes, signedSize);
                    texture.Apply();

                    Graphics.Blit(source: texture, dest: rawOutgoingVideoRenderTexture);
                    break;

                case RawVideoFrameKind.Texture:
                    break;
            }
            pendingFrame.frame.Dispose();
        }
    }

    public void StartGenerateFrames(OutgoingVideoStream outgoingVideoStream)
    {
        backgroundWorker = new BackgroundWorker();
        backgroundWorker.WorkerReportsProgress = false;
        backgroundWorker.WorkerSupportsCancellation = true;
        backgroundWorker.DoWork += BackgroundWork;
        backgroundWorker.RunWorkerAsync(outgoingVideoStream);
    }

    private unsafe RawVideoFrame GenerateRawVideoFrame(RawOutgoingVideoStream rawOutgoingVideoStream)
    {
        var format = rawOutgoingVideoStream.Format;
        int w = format.Width;
        int h = format.Height;
        int rgbaCapacity = w * h * 4;

        var rgbaBuffer = new NativeBuffer(rgbaCapacity);
        rgbaBuffer.GetData(out IntPtr rgbaArrayBuffer, out rgbaCapacity);

        byte r = (byte)random.Next(1, 255);
        byte g = (byte)random.Next(1, 255);
        byte b = (byte)random.Next(1, 255);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w*4; x += 4)
            {
                ((byte*)rgbaArrayBuffer)[(w * 4 * y) + x + 0] = (byte)(y % r);
                ((byte*)rgbaArrayBuffer)[(w * 4 * y) + x + 1] = (byte)(y % g);
                ((byte*)rgbaArrayBuffer)[(w * 4 * y) + x + 2] = (byte)(y % b);
                ((byte*)rgbaArrayBuffer)[(w * 4 * y) + x + 3] = 255;
            }
        }

        rawOutgoingVideoStream.SendRawVideoFrameAsync(new RawVideoFrameBuffer() {
            Buffers = new NativeBuffer[] { rgbaBuffer },
            StreamFormat = rawOutgoingVideoStream.Format,
            TimestampInTicks = rawOutgoingVideoStream.TimestampInTicks
        }).Wait();

        return new RawVideoFrameBuffer()
        {
            Buffers = new NativeBuffer[] { rgbaBuffer },
            StreamFormat = rawOutgoingVideoStream.Format
        };
    }

    private void BackgroundWork(object sender, DoWorkEventArgs e)
    {
        var rawOutgoingVideoStream = e.Argument as RawOutgoingVideoStream;
        BackgroundWorker worker = (BackgroundWorker)sender;
        while (!worker.CancellationPending)
        {
            pendingOutgoingFrames.Enqueue(new PendingFrame() {
                frame = GenerateRawVideoFrame(e.Argument as RawOutgoingVideoStream),
                kind = RawVideoFrameKind.Buffer
            });

            int delayBetweenFrames = (int)(1000.0 / rawOutgoingVideoStream.Format.FramesPerSecond);
            Thread.Sleep(delayBetweenFrames);
        }
    }
}