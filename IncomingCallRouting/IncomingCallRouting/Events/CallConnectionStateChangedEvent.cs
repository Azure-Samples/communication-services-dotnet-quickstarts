using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Azure.Communication.CallingServer;
using IncomingCallRouting.Enums;

namespace IncomingCallRouting.Events
{
    /// <summary>
    /// The call connection state changed event.
    /// </summary>
    public class CallConnectionStateChangedEvent : CallingServerEventBase
    {
        /// <summary>
        /// The server call locator.
        /// </summary>
        public CallLocator CallLocator { get; set; }

        /// <summary>
        /// The call connection id.
        /// </summary>
        public string CallConnectionId { get; set; }

        /// <summary>
        /// The call connection state.
        /// </summary>
        [Required]
        public CallConnectionState CallConnectionState { get; set; }
    }
}