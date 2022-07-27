using IncomingCallRouting.Enums;

namespace IncomingCallRouting.Events
{
    public abstract class AcsCallbackEvent : CallingServerEventBase
    {
        /// <summary>
        /// Call connection ID.
        /// </summary>
        public string CallConnectionId { get; set; }

        /// <summary>
        /// Server call ID.
        /// </summary>
        public string ServerCallId { get; set; }

        /// <summary>
        /// Correlation ID for event to call correlation. Also called ChainId for skype chain ID.
        /// </summary>
        public string CorrelationId { get; set; }

        /// <summary>
        /// ACS Event Type. One of <see cref="AcsEventType"/>
        /// </summary>
        public abstract AcsEventType Type { get; set; }
    }
}
