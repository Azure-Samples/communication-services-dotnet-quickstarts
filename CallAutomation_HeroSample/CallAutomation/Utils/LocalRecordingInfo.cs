
namespace CallAutomation
{
    public class LocalRecordingInfo
    {
        public LocalRecordingInfo(string id, string uri)
        {
            recordingId = id;
            recordingUri = uri;
        }
        public string recordingId { get; set; }
        public string recordingUri { get; set; }
    }
}
