using System.ComponentModel.DataAnnotations;

namespace IncomingCallRouting.Events
{
    /// <summary>
    /// The result details of calling operation.
    /// </summary>
    public class CallingOperationResultDetails
    {
        /// <summary>
        /// The result code associated with the operation.
        /// </summary>
        [Required]
        public int Code { get; set; }

        /// <summary>
        /// The subcode that further classifies the result.
        /// </summary>
        [Required]
        public int Subcode { get; set; }

        /// <summary>
        /// The message is a detail explanation of subcode.
        /// </summary>
        public string Message { get; set; }
    }
}
