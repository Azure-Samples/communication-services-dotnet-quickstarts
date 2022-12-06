
namespace CallAutomation
{
    public interface ILocalRecordingInfoCache
    {
        LocalRecordingInfo AddOrUpdate(string serverCallId, string recordingId, string blobSASUri = "");
        LocalRecordingInfo Get(string serverCallId);
        bool UpdateByRecordingId(string recordingId, string blobUri);
        bool Remove(string serverCallId);
    }
}
