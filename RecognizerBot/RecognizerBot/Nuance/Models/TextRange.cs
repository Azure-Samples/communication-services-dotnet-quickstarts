using System.Text.Json.Serialization;

namespace IncomingCallRouting.Nuance.Models
{
    public class TextRange
    {
        /**
         * Inclusive, 0-based character position, in relation to literal text.
         */
        [JsonPropertyName("start_index")]
        public int StartIndex { get; set; }

        /**
         * Exclusive, 0-based character position, in relation to literal text.
         */
        [JsonPropertyName("end_index")]
        public int EndIndex { get; set; }
    }
}
