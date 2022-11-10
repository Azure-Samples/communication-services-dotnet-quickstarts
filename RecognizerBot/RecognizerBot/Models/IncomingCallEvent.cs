using System;

namespace RecognizerBot.Models
{
    public class IncomingCallEvent
    {
        /// <summary>
        /// Gets or sets the event payload as IncomingCall
        /// </summary>
        public IncomingCallData Data { get; set; }

        /// <summary> Gets or sets a unique identifier for the event. </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>Gets or sets the resource path of the event source.
        /// This must be set when publishing the event to a domain, and must not be set when publishing the event to a topic.
        /// </summary>
        public string Topic { get; set; }

        /// <summary>Gets or sets a resource path relative to the topic path.</summary>
        public string Subject { get; set; }

        /// <summary>Gets or sets the type of the event that occurred.</summary>
        public string EventType { get; set; }

        /// <summary>Gets or sets the time (in UTC) the event was generated.</summary>
        public DateTimeOffset EventTime { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>Gets or sets the schema version of the data object.</summary>
        public string DataVersion { get; set; }
    }
}
