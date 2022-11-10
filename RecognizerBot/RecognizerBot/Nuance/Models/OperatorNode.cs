using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RecognizerBot.Nuance.Models;

public class OperatorNode
{
    /**
     * Type of operator.
     */
    public Operator Operator { get; set; }

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
     * Repeated. Child nodes for this operator. An operator node always has children.
     */
    public List<InterpretationNode> Children { get; set; }

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

public enum Operator
{
    And,
    Or,
    Not
}