using Azure.Communication.CallAutomation;
using System;
using System.Collections.Generic;

namespace RecordingApi
{
    static class FileDownloadType
    {
        public static string Recording => "recording";
        public static string Metadata => "metadata";
    }

    static class FileFormat
    {
        public static string Json => "json";
        public static string Mp4 => "mp4";
        public static string Mp3 => "mp3";
        public static string Wav => "wav";
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
