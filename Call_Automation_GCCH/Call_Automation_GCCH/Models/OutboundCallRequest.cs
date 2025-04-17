namespace Call_Automation_GCCH.Models
{
    /// <summary>
    /// Request model for creating outbound calls to various targets
    /// </summary>
    public class OutboundCallRequest
    {
        /// <summary>
        /// The call connection ID for an existing call (optional)
        /// </summary>
        public required string CallConnectionId { get; set; }
        
        /// <summary>
        /// The type of call to make (ACS, Teams, PSTN)
        /// </summary>
        public CallType TargetType { get; set; }
        
        /// <summary>
        /// The identifier for the target (ACS user ID, Teams ID, or phone number)
        /// </summary>
        public required string TargetId { get; set; }
    }
}