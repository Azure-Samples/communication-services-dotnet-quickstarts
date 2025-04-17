namespace Call_Automation_GCCH.Models
{
    /// <summary>
    /// Response model for call connection operations
    /// </summary>
    public class CallConnectionResponse
    {
        /// <summary>
        /// The unique identifier for the call connection
        /// </summary>
        public string? CallConnectionId { get; set; }

        /// <summary>
        /// The correlation identifier for tracking the call connection
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// The status of the call connection operation
        /// </summary>
        public required string Status { get; set; }
    }
} 