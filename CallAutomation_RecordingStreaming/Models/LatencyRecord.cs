namespace RecordingStreaming.Models
{
    public record LatencyRecord
    {
        public string action_type { get; set; }

        public string region { get; set; }

        public string env { get; set; }

        public decimal value { get; set; }

        public string scenario { get; set; }

        public string call_id { get; set; }

        public DateTimeOffset timestamp { get; set; }
    }
}
