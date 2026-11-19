using CallAutomationOpenAI;

namespace CallAutomation_AzOpenAI_Voice.Models
{
    public class ManualCallRequest
    {
        public string ConversationId { get; set; } = string.Empty;
        public bool DisableOpenAI { get; set; }
    }

    public enum CallStatus
    {
        Connecting,
        Active,
        Completed,
        Failed
    }

    public class TranscriptEntry
    {
        public DateTime Timestamp { get; set; }
        public string Speaker { get; set; } = string.Empty; // "User" or "AI"
        public string Text { get; set; } = string.Empty;
    }

    public class CallSession : IDisposable
    {
        public string CallConnectionId { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public string CallerId { get; set; } = string.Empty;
        public string? ServerCallId { get; set; }
        public string? MediaSessionId { get; set; }
        public string? BotEndpointId { get; set; }
        public string? ResourceId { get; set; }
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime? EndTime { get; set; }
        public CallStatus Status { get; set; } = CallStatus.Connecting;

        public TestCallRoomConnector? RoomConnector { get; set; }
        public AcsMediaStreamingHandler? MediaHandler { get; set; }
        public AzureOpenAIService? AiService { get; set; }

        public List<TranscriptEntry> Transcript { get; } = new();
        private readonly object _transcriptLock = new();

        public void AddTranscript(string speaker, string text)
        {
            lock (_transcriptLock)
            {
                Transcript.Add(new TranscriptEntry
                {
                    Timestamp = DateTime.UtcNow,
                    Speaker = speaker,
                    Text = text
                });
            }
        }

        public List<TranscriptEntry> GetTranscriptSnapshot()
        {
            lock (_transcriptLock)
            {
                return new List<TranscriptEntry>(Transcript);
            }
        }

        public TimeSpan Duration => (EndTime ?? DateTime.UtcNow) - StartTime;

        public void Dispose()
        {
            // Stop the media handler first to prevent new writes
            try { MediaHandler?.Stop(); } catch { }
            // Close AI service (cancels the streaming loop) before disconnecting MediaSDK
            try { AiService?.Close(); } catch { }
            try { RoomConnector?.Disconnect(); } catch { }
            AiService = null;
            RoomConnector = null;
            MediaHandler = null;
        }
    }
}
