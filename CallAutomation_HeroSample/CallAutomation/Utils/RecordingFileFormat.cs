
namespace CallAutomation
{
    public static class RecordingFileFormat
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
}
