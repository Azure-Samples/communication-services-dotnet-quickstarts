namespace IncomingCallRouting.Nuance.Models
{
    public class InterpretationNode
    {
        /**
         * The relationship of the intents or entities.
         */
        public OperatorNode Operator { get; set; }

        /**
         * The intents detected in the user input.
         */
        public IntentNode Intent { get; set; }

        /**
         * The entities in the intent.
         */
        public EntityNode Entity { get; set; }
    }
}
