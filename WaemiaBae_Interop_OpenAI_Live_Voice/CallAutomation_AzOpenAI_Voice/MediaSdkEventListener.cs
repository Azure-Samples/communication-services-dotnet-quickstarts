using System.Diagnostics.Tracing;

namespace Azure.Communication.Media.Tests
{
    /// <summary>
    /// A custom event listener that provides colored console output based on event level
    /// for Azure Communication Media SDK diagnostics.
    /// </summary>
    /// <remarks>
    /// Note: SdkEventSource.Logger is initialized in MediaClient's static constructor before any
    /// Rust FFI calls, ensuring all early events from SDK initialization are captured.
    /// </remarks>
    sealed class MediaSdkEventListener : EventListener
    {
        /// <inheritdoc/>
        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == "MediaSdk.0" /*Diagnostics.EventSourceName*/)
            {
                EnableEvents(eventSource, EventLevel.Verbose, SdkEventSourceKeywords.Native | SdkEventSourceKeywords.Telemetry);
            }
        }

        /// <inheritdoc/>
        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData.Message == null)
                return;

            try
            {
                if (eventData.Keywords.HasFlag(SdkEventSourceKeywords.Telemetry))
                {
                    // Skip telemetry dots in production log streams
                }
                else if (eventData.Level <= EventLevel.Warning)
                {
                    var prefix = eventData.Level <= EventLevel.Error ? "ERROR" : "WARN";
                    var msg = eventData.Payload != null && eventData.Payload.Count > 0
                        ? string.Format(eventData.Message, eventData.Payload.ToArray())
                        : eventData.Message;
                    Console.WriteLine($"[MediaSDK:{prefix}] {msg}");
                }
            }
            catch
            {
                // Don't let event listener crash the app
            }
        }

        /// <summary>
        /// Maps event levels to console colors for better visibility.
        /// </summary>
        private static ConsoleColor GetColorForEventLevel(EventLevel level) => level switch
        {
            EventLevel.Critical => ConsoleColor.Red,
            EventLevel.Error => ConsoleColor.DarkRed,
            EventLevel.Warning => ConsoleColor.Yellow,
            EventLevel.Informational => ConsoleColor.DarkGreen,
            EventLevel.Verbose => ConsoleColor.DarkGray,
            _ => Console.ForegroundColor
        };
    }
}