using System.Collections.Concurrent;
using System.Collections.Generic;

namespace CallAutomation
{
    public sealed class LocalRecordingInfoCache : ILocalRecordingInfoCache
    {
        ConcurrentDictionary<string, LocalRecordingInfo> serverCallIdRecordingInfoMap;
        static ILocalRecordingInfoCache instance;
        static object lockObject = new object();
        private LocalRecordingInfoCache()
        {
            serverCallIdRecordingInfoMap = new ConcurrentDictionary<string, LocalRecordingInfo>();
        }

        public static ILocalRecordingInfoCache Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObject)
                    {
                        instance = new LocalRecordingInfoCache();
                    }
                }

                return instance;
            }
        }

        public LocalRecordingInfo AddOrUpdate(string serverCallId, string recordingId, string blobSASUri = "")
        {
            var recInfo = new LocalRecordingInfo(recordingId, blobSASUri);
            return serverCallIdRecordingInfoMap.AddOrUpdate(serverCallId, recInfo, (key, oldValue) => recInfo);
        }

        public LocalRecordingInfo Get(string serverCallId)
        {
            if (serverCallIdRecordingInfoMap.ContainsKey(serverCallId))
                return serverCallIdRecordingInfoMap[serverCallId];

            return null;
        }

        public bool UpdateByRecordingId(string recordingId, string blobSASUri)
        {
            foreach(KeyValuePair<string, LocalRecordingInfo> pair in serverCallIdRecordingInfoMap)
            {
                if(pair.Value.recordingId.Equals(recordingId))
                {
                    AddOrUpdate(pair.Key, recordingId, blobSASUri);
                    return true;
                }
            }

            return false;
        }

        public bool Remove(string serverCallId)
        {
            return serverCallIdRecordingInfoMap.TryRemove(serverCallId, out _);
        }
    }
}
