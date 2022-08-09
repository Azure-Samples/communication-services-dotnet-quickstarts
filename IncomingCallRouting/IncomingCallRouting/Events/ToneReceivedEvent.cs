using System.ComponentModel.DataAnnotations;
using Azure.Communication.CallingServer;

namespace IncomingCallRouting.Events
{
    /// <summary>
    /// The subscribe to tone event
    /// </summary>
    public class ToneReceivedEvent : CallAutomationEventBase
    {
        /// <summary>
        /// The tone info.
        /// </summary>
        [Required]
        public ToneInfo ToneInfo { get; set; }

        /// <summary>
        /// The server call locator.
        /// </summary>
        public CallLocator CallLocator { get; set; }
    }
}