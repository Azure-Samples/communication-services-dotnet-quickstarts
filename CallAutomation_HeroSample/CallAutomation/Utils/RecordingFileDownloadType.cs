
namespace CallAutomation
{
    public static class RecordingFileDownloadType
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
}
