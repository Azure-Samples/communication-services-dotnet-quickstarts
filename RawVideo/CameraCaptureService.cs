using Azure.Communication.Calling.WindowsClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Graphics;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;

namespace RawVideo
{
    internal class MediaFrameSourceBundle
    {
        public MediaFrameSourceGroup Group { get; set; }
        public MediaFrameSourceInfo Info { get; set; }
        public MediaFrameFormat Format { get; set; }
    }

    internal class VideoFormatBundle
    {
        public SizeInt32 Size { get; set; }
        public MediaFrameFormat Format { get; set; }
    }

    internal class VideoFormatBundleComparer : IEqualityComparer<VideoFormatBundle>
    {
        public bool Equals(VideoFormatBundle s1, VideoFormatBundle s2)
        {
            return s1.Size.Width == s2.Size.Width && s1.Size.Height == s2.Size.Height;
        }

        public int GetHashCode(VideoFormatBundle obj)
        {
            return obj.Size.Width.GetHashCode() ^ obj.Size.Height.GetHashCode();
        }
    }

    internal class CameraCaptureService : CaptureService
    {
        private readonly MediaFrameSourceBundle bundle;
        private MediaCapture mediaCapture;
        private MediaFrameReader mediaFrameReader;

        public CameraCaptureService(RawOutgoingVideoStream rawOutgoingVideoStream,
            RawVideoFrameKind rawVideoFrameKind, MediaFrameSourceBundle bundle) :
            base(rawOutgoingVideoStream, rawVideoFrameKind)
        {
            this.bundle = bundle;
        }

        public async Task StartAsync()
        {
            var settings = new MediaCaptureInitializationSettings
            {
                SourceGroup = bundle.Group,
                SharingMode = MediaCaptureSharingMode.ExclusiveControl,
                StreamingCaptureMode = StreamingCaptureMode.Video,
                MemoryPreference = MediaCaptureMemoryPreference.Cpu
            };

            mediaCapture = new MediaCapture();
            MediaFrameReaderStartStatus mediaFrameReaderStatus = 
                MediaFrameReaderStartStatus.UnknownFailure;

            try
            {
                await mediaCapture.InitializeAsync(settings);
                MediaFrameSource source = mediaCapture.FrameSources[bundle.Info.Id];

                await source.SetFormatAsync(bundle.Format);

                mediaFrameReader = await mediaCapture.CreateFrameReaderAsync(source);
                mediaFrameReader.FrameArrived += FrameArrived;
                mediaFrameReaderStatus = await mediaFrameReader.StartAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
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

        public static async Task<List<MediaFrameSourceBundle>> GetCameraListAsync()
        {
            IReadOnlyList<MediaFrameSourceGroup> groups = await MediaFrameSourceGroup.FindAllAsync();
            var cameraList = new List<MediaFrameSourceBundle>();

            foreach (MediaFrameSourceGroup sourceGroup in groups)
            {
                foreach (MediaFrameSourceInfo sourceInfo in sourceGroup.SourceInfos)
                {
                    if (sourceInfo.SourceKind == MediaFrameSourceKind.Color)
                    {
                        cameraList.Add(new MediaFrameSourceBundle()
                        {
                            Group = sourceGroup,
                            Info = sourceInfo
                        });
                    }
                }
            }

            return cameraList
                .OrderBy(x => x.Info.DeviceInformation.Name)
                .ToList();
        }

        public static async Task<List<VideoFormatBundle>> GetSupportedVideoFormats(MediaFrameSourceBundle bundle)
        {
            var settings = new MediaCaptureInitializationSettings
            {
                SourceGroup = bundle.Group,
                SharingMode = MediaCaptureSharingMode.SharedReadOnly,
                StreamingCaptureMode = StreamingCaptureMode.Video,
                MemoryPreference = MediaCaptureMemoryPreference.Cpu
            };

            IReadOnlyList<MediaFrameFormat> sourceFormatList = null;
            var videoFormatList= new List<VideoFormatBundle>();

            using (var mediaCapture = new MediaCapture())
            {
                try
                {
                    await mediaCapture.InitializeAsync(settings);
                    MediaFrameSource source = mediaCapture.FrameSources[bundle.Info.Id];
                    sourceFormatList = source.SupportedFormats;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            if (sourceFormatList != null)
            {
                var resolutionList = Enum.GetValues(typeof(VideoStreamResolution))
                    .Cast<VideoStreamResolution>()
                    .ToList();

                var acsFormatList = resolutionList
                    .Select(x =>
                    {
                        var format = new VideoStreamFormat
                        {
                            Resolution = x
                        };

                        return new SizeInt32()
                        {
                            Width = format.Width,
                            Height = format.Height
                        };
                    })
                    .Distinct()
                    .ToList();

                videoFormatList = sourceFormatList
                    .Select(x => new VideoFormatBundle()
                    {
                        Size = new SizeInt32()
                        { 
                            Width = (int) x.VideoFormat.Width,
                            Height = (int) x.VideoFormat.Height
                        },
                        Format = x
                    })
                    .Distinct(new VideoFormatBundleComparer())
                    .Where(x => acsFormatList.Contains(x.Size))
                    .OrderByDescending(x => x.Size.Width)
                    .ToList();
            }

            return videoFormatList;
        }
    }
}
