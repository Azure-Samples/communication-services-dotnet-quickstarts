using System.Text.Json.Serialization;

namespace RecognizerBot.Nuance.Models
{
    /**
     * Candidate interpretation of the input. Included in InterpretResult.
     * The interpret request specifies the type of interpretation: either single-intent or multi-intent
     * (see InterpretRequest - InterpretationParameters - interpretation_result_type).
     * Multi-intent interpretation requires a semantic model that is enabled for multi-intent (not currently supported in Mix.nlu).
     * See Interpretation results for details and more examples.
     */
    public class Interpretation
    {
        /**
         * The result contains one intent.
         */
        [JsonPropertyName("single_intent_interpretation")]
        public SingleIntentInterpretation? SingleIntentInterpretation { get; set; }

        /**
         * The result contains multiple intents. This choice requires a multi-intent semantic model, which is not currently supported in Nuance-hosted NLUaaS.
         */
        [JsonPropertyName("multi_intent_interpretation")]
        public MultiIntentInterpretation? MultiIntentInterpretation { get; set; }
    }
}
