using Azure.Communication.Calling.WindowsClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;

namespace RawVideo
{
    internal class CameraCaptureService : CaptureService
    {
        private MediaCapture mediaCapture;
        private MediaFrameReader mediaFrameReader;
        private MediaFrameSourceGroup sourceGroup;
        private MediaFrameSourceInfo sourceInfo;

        public CameraCaptureService(RawOutgoingVideoStream rawOutgoingVideoStream,
            Tuple<MediaFrameSourceGroup, MediaFrameSourceInfo> mediaFrameSource) :
            base(rawOutgoingVideoStream)
        {
            sourceGroup = mediaFrameSource.Item1;
            sourceInfo = mediaFrameSource.Item2;
        }

        public async Task StartAsync()
        {
            var settings = new MediaCaptureInitializationSettings
            {
                SourceGroup = sourceGroup,
                SharingMode = MediaCaptureSharingMode.SharedReadOnly,
                StreamingCaptureMode = StreamingCaptureMode.Video,
                MemoryPreference = MediaCaptureMemoryPreference.Cpu
            };

            mediaCapture = new MediaCapture();
            MediaFrameReaderStartStatus mediaFrameReaderStatus;

            try
            {
                await mediaCapture.InitializeAsync(settings);
                MediaFrameSource selectedSource = mediaCapture.FrameSources[sourceInfo.Id];
                mediaFrameReader = await mediaCapture.CreateFrameReaderAsync(selectedSource);
                mediaFrameReaderStatus = await mediaFrameReader.StartAsync();
            }
            catch (Exception ex)
            {
                mediaFrameReaderStatus = MediaFrameReaderStartStatus.UnknownFailure;

                Console.WriteLine(ex.Message);
            }

            if (mediaFrameReaderStatus == MediaFrameReaderStartStatus.Success)
            {
                mediaFrameReader.FrameArrived += FrameArrived;
            }
        }

        public async Task StopAsync()
        {
            if (mediaFrameReader != null)
            {
                mediaFrameReader.FrameArrived -= FrameArrived;
                await mediaFrameReader.StopAsync();
                mediaFrameReader.Dispose();
                mediaFrameReader = null;
            }

            if (mediaCapture != null)
            {
                mediaCapture.Dispose();
                mediaCapture = null;
            }
        }

        private new async void FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            using (MediaFrameReference frame = sender.TryAcquireLatestFrame())
            {
                if (frame != null)
                {
                    SoftwareBitmap bitmap = frame.VideoMediaFrame.SoftwareBitmap;

                    await SendRawVideoFrame(bitmap);
                }
            }
        }

        public static async Task<List<Tuple<MediaFrameSourceGroup, MediaFrameSourceInfo>>> GetCameraListAsync()
        {
            IReadOnlyList<MediaFrameSourceGroup> groups = await MediaFrameSourceGroup.FindAllAsync();
            var cameraList = new List<Tuple<MediaFrameSourceGroup, MediaFrameSourceInfo>>();

            foreach (MediaFrameSourceGroup sourceGroup in groups)
            {
                foreach (MediaFrameSourceInfo sourceInfo in sourceGroup.SourceInfos)
                {
                    if (sourceInfo.SourceKind == MediaFrameSourceKind.Color)
                    {
                        cameraList.Add(new Tuple<MediaFrameSourceGroup, MediaFrameSourceInfo>(sourceGroup, sourceInfo));
                    }
                }
            }

            return cameraList.OrderBy(item => item.Item2.DeviceInformation.Name).ToList();
        }
    }
}
