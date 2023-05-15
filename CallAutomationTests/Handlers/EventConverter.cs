// © Microsoft Corporation. All rights reserved.

using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using System.Text.Json;

namespace CallAutomation.Scenarios.Handlers
{
    public sealed class EventConverter
    {
        internal const string OfferIssuedEventName = "RouterWorkerOfferIssued";
        internal const string OfferRevokedEventName = "RouterWorkerOfferRevoked";
        internal const string OfferExpiredEventName = "RouterWorkerOfferExpired";
        internal const string OfferAccpetedEventName = "RouterWorkerOfferAccepted";
        internal const string OfferDeclinedEventName = "RouterWorkerOfferDeclined";
        internal const string WorkerRegisteredEventName = "RouterWorkerRegistered";
        internal const string WorkerDeregisteredEventName = "RouterWorkerDeregistered";

        internal const string JobCancelledEventName = "RouterJobCancelled";
        internal const string JobClassificationFailedEventName = "RouterJobClassificationFailed";
        internal const string JobClassifiedEventName = "RouterJobClassified";
        internal const string JobClosedEventName = "RouterJobClosed";
        internal const string JobCompletedEventName = "RouterJobCompleted";
        internal const string JobExceptionTriggeredEventName = "RouterJobExceptionTriggered";
        internal const string JobQueuedEventName = "RouterJobQueued";
        internal const string JobReceivedEventName = "RouterJobReceived";
        internal const string JobUnassignedEventName = "RouterJobUnassigned";
        internal const string JobWorkerSelectorsExpiredEventName = "RouterJobWorkerSelectorsExpired";

        internal const string IncomingCallEventName = "IncomingCall";
        internal const string RecordingFileStatusUpdatedEventName = "RecordingFileStatusUpdated";

        internal const string SMSReceived = "SMSReceived";
        internal const string CrossPlatformMessageEvent = "CrossPlatformMessageReceived";
        internal const string ChatMessageReceivedInThreadEvent = "ChatMessageReceivedInThread";



        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true, IgnoreNullValues = true };

        public object? Convert(EventGridEvent eventGridEvent, bool validationEvent = false)
        {
            var data = eventGridEvent.Data.ToString() ?? throw new ArgumentNullException($"No data present: {eventGridEvent}");

            if (validationEvent)
            {
                var jsonObj = JsonDocument.Parse(data);
                jsonObj.RootElement.TryGetProperty("validationCode", out var value);
                return new SubscriptionValidationResponse { ValidationResponse = value.GetString() };
            }

            return ParseEventType(eventGridEvent.EventType) switch
            {
                IncomingCallEventName => JsonSerializer.Deserialize<IncomingCallEvent>(data, JsonOptions),
                RecordingFileStatusUpdatedEventName => JsonSerializer.Deserialize<RecordingFileStatusUpdatedEvent>(data, JsonOptions),
                _ => null
            };
        }

        private static string ParseEventType(string eventType)
        {
            var split = eventType.Split("Microsoft.Communication.");
            return split[^1];
        }
    }
}
