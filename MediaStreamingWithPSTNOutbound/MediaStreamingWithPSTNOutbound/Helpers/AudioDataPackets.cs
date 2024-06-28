namespace incoming_call_recording.Helpers
{
    internal class AudioDataPackets
    {
        public string kind { set; get; }
        public AudioData audioData { set; get; }
    }

    internal class AudioData
    {
        public string data { set; get; } // Base64 Encoded audio buffer data
        public string timestamp { set; get; } // In ISO 8601 format (yyyy-mm-ddThh:mm:ssZ)
        public string participantRawID { set; get; }
        public bool silent { set; get; } // Indicates if the received audio buffer contains only silence.
    }

}