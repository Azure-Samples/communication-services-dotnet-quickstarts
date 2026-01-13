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

            var originalColor = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = GetColorForEventLevel(eventData.Level);

                if (eventData.Keywords.HasFlag(SdkEventSourceKeywords.Telemetry))
                {
                    Console.Write('.');
                    // Console.WriteLine($"Telemetry: {string.Format(eventData.Message, eventData!.Payload!.ToArray())}");
                }
                else if (eventData.Level <= EventLevel.Warning)
                {
                    // Handle the case where Payload might be null or empty
                    Console.WriteLine(eventData.Payload != null && eventData.Payload.Count > 0
                        ? string.Format(eventData.Message, eventData.Payload.ToArray())
                        : eventData.Message);
                }

                //    Console.WriteLine("Stack trace:");
                //    Console.WriteLine(Environment.StackTrace);
            }
            finally
            {
                Console.ForegroundColor = originalColor;
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