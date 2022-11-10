namespace IncomingCallRouting.Nuance.Models
{
    public class InterpretResponse
    {
        /**
         * Whether the request was successful. The 200 code means success, other values indicate an error.
         */
        public Status? Status { get; set; }

        /**
         * The result of the interpretation.
         */
        public InterpretResult Result { get; set; }


    }
}
