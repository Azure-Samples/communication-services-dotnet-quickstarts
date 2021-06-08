// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Communication.Server.Calling.Sample.OutboundCallReminder
{
    using Azure.Communication.CallingServer;
    using Microsoft.Azure.EventGrid;
    using Microsoft.Azure.EventGrid.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading.Tasks;

    public class EventDispatcher
    {
        public static readonly EventDispatcher Instance;

        private readonly EventSerializer Serializer;
        private readonly ConcurrentDictionary<string, NotificationCallback> NotificationCallback;
        private object SubscriptionLock = new object();

        static EventDispatcher()
        {
            Instance = new EventDispatcher();
        }

        private EventDispatcher()
        {
            Serializer = new EventSerializer();
            NotificationCallback = new ConcurrentDictionary<string, NotificationCallback>(StringComparer.OrdinalIgnoreCase);
        }

        public bool Subscribe(string eventType, string eventKey, NotificationCallback notificationCallback)
        {
            string eventId = BuildEventKey(eventType, eventKey);

            lock (this.SubscriptionLock)
            {
                return NotificationCallback.TryAdd(eventId, notificationCallback);
            }
        }

        public void Unsubscribe(string eventType, string eventKey)
        {
            string eventId = BuildEventKey(eventType, eventKey);

            lock (this.SubscriptionLock)
            {
                NotificationCallback.TryRemove(eventId, out _);
            }
        }

        public void ProcessNotification(string request)
        {
            var callEvent = this.ExtractEvent(request);

            if (callEvent != null)
            {
                lock (SubscriptionLock)
                {
                    if (NotificationCallback.TryGetValue(GetEventKey(callEvent), out NotificationCallback notificationCallback))
                    {
                        if (notificationCallback != null)
                        {
                            Task.Run(() =>
                            {
                                notificationCallback.Callback.Invoke(callEvent);
                            });
                        }
                    }
                }
            }
        }

        public string GetEventKey(CallingServerEventBase callEventBase)
        {
            if (callEventBase is CallLegStateChangedEvent)
            {
                var callLegId = ((CallLegStateChangedEvent)callEventBase).CallLegId;
                return BuildEventKey(CallingServerEventType.CallLegStateChangedEvent.ToString(), callLegId);;
            }
            else if (callEventBase is ToneReceivedEvent)
            {
                var callLegId = ((ToneReceivedEvent)callEventBase).CallLegId;
                return BuildEventKey(CallingServerEventType.ToneReceivedEvent.ToString(), callLegId);
            }
            else if (callEventBase is PlayAudioResultEvent)
            {
                var operationContext = ((PlayAudioResultEvent)callEventBase).OperationContext;
                return BuildEventKey(CallingServerEventType.PlayAudioResultEvent.ToString(), operationContext);
            }
            else if (callEventBase is InviteParticipantsResultEvent)
            {
                var operationContext = ((InviteParticipantsResultEvent)callEventBase).OperationContext;
                return BuildEventKey(CallingServerEventType.InviteParticipantsResultEvent.ToString(), operationContext);
            }

            return null;
        }

        public string BuildEventKey(string eventType, string eventKey)
        {
            return $"{eventType}-{eventKey}";
        }

        /// <summary>
        /// Extracting event from the json.
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public CallingServerEventBase ExtractEvent(string content)
        {
            EventGridSubscriber eventGridSubscriber = new EventGridSubscriber();

            EventGridEvent[] eventGridEvents = eventGridSubscriber.DeserializeEventGridEvents(content);

            if (eventGridEvents != null && eventGridEvents.Any())
            {
                var eventGridEvent = eventGridEvents[0];

                if (eventGridEvent.EventType.Equals(CallingServerEventType.CallLegStateChangedEvent.ToString()))
                {
                    return CallLegStateChangedEvent.Deserialize(eventGridEvent.Data.ToString());
                }
                else if (eventGridEvent.EventType.Equals(CallingServerEventType.ToneReceivedEvent.ToString()))
                {
                    return ToneReceivedEvent.Deserialize(eventGridEvent.Data.ToString());
                }
                else if (eventGridEvent.EventType.Equals(CallingServerEventType.PlayAudioResultEvent.ToString()))
                {
                    return PlayAudioResultEvent.Deserialize(eventGridEvent.Data.ToString());
                }
                else if (eventGridEvent.EventType.Equals(CallingServerEventType.InviteParticipantsResultEvent.ToString()))
                {
                    return InviteParticipantsResultEvent.Deserialize(eventGridEvent.Data.ToString());
                }
            }

            return null;
        }

        ~EventDispatcher()
        {

        }

        public class EventSerializer
        {
            /// <summary>
            /// Default constructor.
            /// </summary>
            public EventSerializer()
            {
                this.JsonSerializerSettings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All
                };
            }

            /// <summary>
            /// Gets the JSON serializer settings.
            /// </summary>
            private JsonSerializerSettings JsonSerializerSettings { get; }

            public T DeserializeObject<T>(string inputString)
            {
                return string.IsNullOrEmpty(inputString)
                    ? default(T)
                    : JsonConvert.DeserializeObject<T>(inputString, this.JsonSerializerSettings);
            }
        }
    }
}

