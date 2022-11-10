using System;
using IncomingCallRouting.Models;
using Newtonsoft.Json;

namespace IncomingCallRouting.Models
{
    public class IncomingCallData
    {
        /// <summary>
        /// Gets and Sets communication identifier of Target User
        /// </summary>
        public CommunicationIdentifierModel To { get; set; }

        /// <summary>
        /// Gets and Sets communication identifier of caller
        /// </summary>
        public CommunicationIdentifierModel From { get; set; }

        /// <summary>
        /// True if incoming call is a video call
        /// </summary>
        public bool HasIncomingVideo { get; set; }

        /// <summary>
        /// Gets and Sets display name of caller
        /// </summary>
        public string CallerDisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Gets and Sets Incoming call context  == compact payload
        /// </summary>
        public string IncomingCallContext { get; set; } = string.Empty;

        /// <summary>
        /// Gets and Sets CorrelationId == CallId
        /// </summary>
        public string CorrelationId { get; set; } = string.Empty;
    }
}
