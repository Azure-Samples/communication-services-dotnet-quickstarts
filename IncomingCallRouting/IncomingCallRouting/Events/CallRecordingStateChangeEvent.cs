using System;
using System.ComponentModel.DataAnnotations;
using Azure.Communication.CallingServer;
using IncomingCallRouting.Enums;

namespace IncomingCallRouting.Events
{
    /// <summary>
    /// The call recording state change event.
    /// </summary>
    public class CallRecordingStateChangeEvent : CallingServerEventBase
    {
        /// <summary>
        /// The call recording id
        /// </summary>
        public string RecordingId { get; set; }

        /// <summary>
        /// The call recording state.
        /// </summary>
        [Required]
        public CallRecordingState CallRecordingState { get; set; }

        /// <summary>
        /// The time of the recording started
        /// </summary>
        [Required]
        public DateTimeOffset StartDateTime { get; set; }

        /// <summary>
        /// The server call locator.
        /// </summary>
        public CallLocator CallLocator { get; set; }
    }
}