namespace RecognizerBot.Nuance.Models
{
    public class MultiIntentInterpretation
    {
        /**
         * Root node of the interpretation tree. Can be either OperatorNode or IntentNode.
         */
        public InterpretationNode Root { get; set; }
    }
}
