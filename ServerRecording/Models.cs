using System;
using System.Collections.Generic;

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
