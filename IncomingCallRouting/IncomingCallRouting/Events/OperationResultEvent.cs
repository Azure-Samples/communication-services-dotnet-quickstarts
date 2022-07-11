using System.ComponentModel.DataAnnotations;
using Azure.Communication.CallingServer;
using IncomingCallRouting.Enums;

namespace IncomingCallRouting.Events
{
    public abstract class OperationResultEvent : CallingServerEventBase
    {
        /// <summary>
        /// The result details.
        /// </summary>
        public CallingOperationResultDetails ResultDetails { get; set; }

        /// <summary>
        /// The operation id.
        /// </summary>
        public string OperationId { get; set; }

        /// <summary>
        /// The operation context.
        /// </summary>
        public string OperationContext { get; set; }

        /// <summary>
        /// The status of the operation.
        /// </summary>
        [Required]
        public CallingOperationStatus Status { get; set; }

        /// <summary>
        /// The server call locator.
        /// </summary>
        public CallLocator CallLocator { get; set; }
    }
}