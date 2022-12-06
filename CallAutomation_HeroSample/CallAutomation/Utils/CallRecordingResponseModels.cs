using System;
using System.Collections.Generic;

namespace CallAutomation
{
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

    public class Metadata
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
