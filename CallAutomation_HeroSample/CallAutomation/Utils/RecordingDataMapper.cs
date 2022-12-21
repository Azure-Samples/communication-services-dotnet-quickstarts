using Azure.Communication.CallAutomation;
using System.Collections.Generic;

namespace CallAutomation
{
    public class RecordingDataMapper
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
}
