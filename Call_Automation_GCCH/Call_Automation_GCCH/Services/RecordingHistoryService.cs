using System.Collections.Concurrent;

namespace Call_Automation_GCCH.Services
{
    /// <summary>
    /// Thread-safe in-memory store of recording locations captured from
    /// AcsRecordingFileStatusUpdatedEventData events. Also tracks auto-download status.
    /// </summary>
    public class CapturedRecordingInfo
    {
        public string RecordingId { get; set; } = string.Empty;
        public string ContentLocation { get; set; } = string.Empty;
        public string MetadataLocation { get; set; } = string.Empty;
        public string DeleteLocation { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public long? SizeInBytes { get; set; }
        public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
        public string? LocalFilePath { get; set; }
        public bool AutoDownloaded { get; set; }
        public string? AutoDownloadError { get; set; }
    }

    public interface IRecordingHistoryService
    {
        void Add(CapturedRecordingInfo info);
        CapturedRecordingInfo? GetLast();
        IReadOnlyList<CapturedRecordingInfo> GetAll();
        void Clear();
    }

    public class RecordingHistoryService : IRecordingHistoryService
    {
        private readonly ConcurrentQueue<CapturedRecordingInfo> _recordings = new();
        private const int MAX_RECORDINGS = 100;

        public void Add(CapturedRecordingInfo info)
        {
            _recordings.Enqueue(info);
            while (_recordings.Count > MAX_RECORDINGS && _recordings.TryDequeue(out _)) { }
        }

        public CapturedRecordingInfo? GetLast()
        {
            return _recordings.LastOrDefault();
        }

        public IReadOnlyList<CapturedRecordingInfo> GetAll()
        {
            return _recordings.ToArray();
        }

        public void Clear()
        {
            while (_recordings.TryDequeue(out _)) { }
        }
    }
}
