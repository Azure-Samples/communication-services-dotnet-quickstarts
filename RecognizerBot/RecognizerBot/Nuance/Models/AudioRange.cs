using System.Text.Json.Serialization;

namespace IncomingCallRouting.Nuance.Models
{
    public class AudioRange
    {
        /**
         * Inclusive start time in milliseconds.
         */
        [JsonPropertyName("start_time_ms")]
        public int StartTimeMs { get; set; }

        /**
         * Exclusive end time in milliseconds.
         */
        [JsonPropertyName("end_time_ms")]
        public int EndTimeMs { get; set; }
    }
}
