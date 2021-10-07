using System;
using System.Collections.Generic;
using Azure.Communication.CallingServer;

namespace QuickStartApi
{
    public class BlobStorageHelperInfo
    {
        public string Message { set; get; }
        public bool Status { set; get; }
    }

    static class FileDownloadType
    {
        const string recordingType = "recording";
        const string metadataType = "metadata";

        public static string Recording
        {
            get
            {
                return recordingType;
            }
        }

        public static string Metadata
        {
            get
            {
                return metadataType;
            }
        }
    }

    static class FileFormat
    {
        const string json = "json";
        const string mp4 = "mp4";
        const string mp3 = "mp3";
        const string wav = "wav";

        public static string Json
        {
            get
            {
                return json;
            }
        }

        public static string Mp4
        {
            get
            {
                return mp4;
            }
        }

        public static string Mp3
        {
            get
            {
                return mp3;
            }
        }

        public static string Wav
        {
            get
            {
                return wav;
            }
        }
    }

    public class Mapper
    {
        static Dictionary<string, RecordingContent> recContentMap
            = new Dictionary<string, RecordingContent>() 
                { 
                    { "audiovideo", RecordingContent.AudioVideo },
                    { "audio", RecordingContent.Audio }
                };

        static Dictionary<string, RecordingChannel> recChannelMap
            = new Dictionary<string, RecordingChannel>()
                {
                    { "mixed", RecordingChannel.Mixed },
                    { "unmixed", RecordingChannel.Unmixed }
                };

        static Dictionary<string, RecordingFormat> recFormatMap
            = new Dictionary<string, RecordingFormat>()
                {
                    { "mp3", RecordingFormat.Mp3 },
                    { "mp4", RecordingFormat.Mp4 },
                    { "wav", RecordingFormat.Wav },
                };

        public static Dictionary<string, RecordingContent> RecordingContentMap
        {
            get { return recContentMap; }
        }

        public static Dictionary<string, RecordingChannel> RecordingChannelMap
        {
            get { return recChannelMap; }
        }

        public static Dictionary<string, RecordingFormat> RecordingFormatMap
        {
            get { return recFormatMap; }
        }
    }

    public class AudioConfiguration
    {
        public int sampleRate { get; set; }
        public int bitRate { get; set; }
        public int channels { get; set; }
    }

    public class VideoConfiguration
    {
        public int longerSideLength { get; set; }
        public int shorterSideLength { get; set; }
        public int framerate { get; set; }
        public int bitRate { get; set; }
    }

    public class RecordingInfo
    {
        public string contentType { get; set; }
        public string channelType { get; set; }
        public string format { get; set; }
        public AudioConfiguration audioConfiguration { get; set; }
        public VideoConfiguration videoConfiguration { get; set; }
    }

    public class Participant
    {
        public string participantId { get; set; }
    }

    public class Root
    {
        public string resourceId { get; set; }
        public string callId { get; set; }
        public string chunkDocumentId { get; set; }
        public int chunkIndex { get; set; }
        public DateTime chunkStartTime { get; set; }
        public double chunkDuration { get; set; }
        public List<object> pauseResumeIntervals { get; set; }
        public RecordingInfo recordingInfo { get; set; }
        public List<Participant> participants { get; set; }
    }
}
