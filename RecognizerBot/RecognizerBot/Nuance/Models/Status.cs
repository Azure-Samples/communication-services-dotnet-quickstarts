namespace RecognizerBot.Nuance.Models
{
    public class Status
    {
        /**
         * HTTP status code. The 200 code means success, other values indicate an error.
         */
        public int Code { get; set; }

        /**
         * Brief description of the status.
         */
        public string Message { get; set; }

        /**
         * Longer description if available.
         */
        public string Details { get; set; }
    }
}
