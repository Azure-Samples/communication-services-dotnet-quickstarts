using System.Collections.Generic;

namespace QuickStartApi
{
    public class RecordingChunk
    {
        public string documentId { get; set; }
        public int index { get; set; }
        public string endReason { get; set; }
        public string contentLocation { get; set; }
        public string metadataLocation { get; set; }
    }

    public class RecordingStorageInfo
    {
        public List<RecordingChunk> recordingChunks { get; set; }
    }

    public class RecordingFileData
    {
        public RecordingStorageInfo recordingStorageInfo { get; set; }
        public string recordingStartTime { get; set; }
        public int recordingDurationMs { get; set; }
        public string sessionEndReason { get; set; }
    }
}
