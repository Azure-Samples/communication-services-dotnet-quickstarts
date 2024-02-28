using Azure.Communication.Calling.UnityClient;
using System;
using System.Collections.Concurrent;
using UnityEngine;

public class IncomingVideo : MonoBehaviour
{
    private struct PendingFrame
    {
        public RawVideoFrame frame;
        public RawVideoFrameKind kind;
    }

    ConcurrentQueue<PendingFrame> pendingIncomingFrames = new ConcurrentQueue<PendingFrame>();

    public RenderTexture rawIncomingVideoRenderTexture;

    private void Update()
    {
        if (pendingIncomingFrames.TryDequeue(out PendingFrame pendingFrame))
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

                    Graphics.Blit(source: texture, dest: rawIncomingVideoRenderTexture);
                    break;

                case RawVideoFrameKind.Texture:
                    break;
            }
            pendingFrame.frame.Dispose();
        }
    }

    public void RenderRawVideoFrame(RawVideoFrame rawVideoFrame)
    {
        var videoFrameBuffer = rawVideoFrame as RawVideoFrameBuffer;
        pendingIncomingFrames.Enqueue(new PendingFrame() {
                frame = rawVideoFrame,
                kind = RawVideoFrameKind.Buffer });
    }
}