using System.ComponentModel.DataAnnotations;
using Azure.Communication.CallingServer;

namespace IncomingCallRouting.Events
{
    /// <summary>
    /// The subscribe to tone event
    /// </summary>
    public class ToneReceivedEvent : CallingServerEventBase
    {
        /// <summary>
        /// The tone info.
        /// </summary>
        [Required]
        public ToneInfo ToneInfo { get; set; }

        /// <summary>
        /// The call connection id.
        /// </summary>
        public string CallConnectionId { get; set; }

        /// <summary>
        /// The server call locator.
        /// </summary>
        public CallLocator CallLocator { get; set; }
    }
}