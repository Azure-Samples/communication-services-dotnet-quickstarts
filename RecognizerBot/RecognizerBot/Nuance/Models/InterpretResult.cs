using System.Text.Json.Serialization;

namespace IncomingCallRouting.Nuance.Models
{
    public class InterpretResult
    {
        /**
         * Input used for interpretation. For text input, this is always the raw input text. For input coming from ASR as a Service results,
         * this gives a concatenation of the audio tokens, separated by spaces, in minimally formatted text format. For example, "Pay five hundred dollars."
         */
        public string? Literal { get; set; }

        /**
         * The formatted input literal. For input coming from ASR as a Service results, this is the text representation of the ASR result,
         * but with formatted text. For example, "Pay $500." When the input for interpretation is text, this is the same as literal.
         */
        [JsonPropertyName("formatted_literal")]
        public string? FormattedLiteral { get; set; }

        /**
         * Repeated. Candidate interpretations of the input.
         */
        public Interpretation Interpretation { get; set; }

        /**
         * Indicates whether the literal contains entities flagged as sensitive. Sensitive entities are masked in call logs.
         */
        public bool Sensitive { get; set; }
    }
}
