using IncomingCallRouting.Nuance.Models;
using System.Collections.Generic;

namespace IncomingCallRouting.Models
{
    public class Intent
    {
        /**
         * The detected intent.
         */
        public string? Value { get; set; }

        /**
         * Confidence score(between 0.0 and 1.0 inclusive). The higher the score, the likelier the detected intent is correct.
         */
        public float? Confidence { get; set; }
    }
}
