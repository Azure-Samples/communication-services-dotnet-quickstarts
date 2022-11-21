using System.Collections.Generic;

namespace RecognizerBot.Nuance.Models
{
    /**
     * Single-intent interpretation results. Included in Interpretation. Theses results include one or more alternative intents, complete with entities if they occur in the text. Each intent is shown with a confidence score and whether the match was done from a grammar file or an SSM (statistical) file.
     */
    public class SingleIntentInterpretation
    {
        /**
         * Intent name as specified in the semantic model.
         */
        public string Intent { get; set; }

        /**
         * Confidence score(between 0.0 and 1.0 inclusive). The higher the score, the likelier the detected intent is correct.
         */
        public float Confidence { get; set; }

        /**
         * How the intent was detected.
         */
        public Origin Origin { get; set; }

        /**
         * Map of entity names to lists of entities: key, entity list.
         */

        public Dictionary<string, SingleIntentEntityList>? Entities { get; set; }
    }

    public class SingleIntentEntityList
    {
        public List<SingleIntentEntity> Entities { get; set; }
    }

    public enum Origin
    {
        Unknown,
        /**
         *Determined from an exact match with a grammar file in the model.
         */
        Grammar,
        /**
         * Determined statistically from the SSM file in the model.
         */
        Statistical
    }
}
