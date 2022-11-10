using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace RecognizerBot.Nuance.Models
{
    public class SingleIntentEntity
    {
        /**
         * Range of literal text for which this entity applies.
         */
        [JsonPropertyName("text_range")]
        public TextRange TextRange { get; set; }

        /**
         * Range of the formatted literal text for which this entity applies. When the input for interpretation comes from an ASR result, this may be absent if there is misalignment.
         */
        [JsonPropertyName("formatted_text_range")]
        public TextRange FormattedTextRange { get; set; }

        /**
         * Confidence score between 0.0 and 1.0 inclusive.The higher the score, the likelier the entity detection is correct.
         */
        public float Confidence { get; set; }

        /**
         * How the entity was detected.
         */
        public Origin Origin { get; set; }

        /**
         * For hierarchical entities, the child entities of the entity: key, entity list.
         */

        public Dictionary<string, SingleIntentEntityList> Entities { get; set; }

        /**
         * The canonical value as a string.
         */
        [JsonPropertyName("string_value")]
        public string StringValue { get; set; }

        /**
         * The entity value as an object. This object may be directly converted to a JSON representation.
         */
        [JsonPropertyName("struct_value")]
        public JsonObject StructValue { get; set; }

        /**
         * Input used for interpretation. For text input, this is always the raw input text. For input coming from ASR as a Service results,
         * this gives a concatenation of the audio tokens, separated by spaces, in minimally formatted text format. For example, "Pay five hundred dollars."
         */
        public string Literal { get; set; }

        /**
         * The formatted input literal. For input coming from ASR as a Service results, this is the text representation of the ASR result,
         * but with formatted text. For example, "Pay $500." When the input for interpretation is text, this is the same as literal.
         */
        [JsonPropertyName("formatted_literal")]
        public string FormattedLiteral { get; set; }

        /**
         * Indicates whether the literal contains entities flagged as sensitive. Sensitive entities are masked in call logs.
         */
        public bool Sensitive { get; set; }

        /**
         * Range of audio input this operator applies to. Available only when interpreting a recognition result from ASR as a Service.
         */
        [JsonPropertyName("audio_range")]
        public AudioRange AudioRange { get; set; }
    }
}
