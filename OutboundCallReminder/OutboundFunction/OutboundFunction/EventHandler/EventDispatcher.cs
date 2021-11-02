// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace OutboundFunction
{
    using Azure.Communication.CallingServer;
    using Azure.Messaging;
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    public class EventDispatcher
    {
        public static readonly EventDispatcher Instance;
        private static readonly ConcurrentDictionary<string, NotificationCallback> NotificationCallback;
        private readonly object SubscriptionLock = new object();

        static EventDispatcher()
        {
            Instance = new EventDispatcher();
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
            if (callEventBase is CallConnectionStateChangedEvent)
            {
                var callLegId = ((CallConnectionStateChangedEvent)callEventBase).CallConnectionId;
                return BuildEventKey(CallingServerEventType.CallConnectionStateChangedEvent.ToString(), callLegId);;
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
            CloudEvent cloudEvent = CloudEvent.Parse(BinaryData.FromString(content));

            if (cloudEvent != null && cloudEvent.Data != null)
            {
                if (cloudEvent.Type.Equals(CallingServerEventType.CallConnectionStateChangedEvent.ToString()))
                {
                    return CallConnectionStateChangedEvent.Deserialize(cloudEvent.Data.ToString());
                }
            }

            return null;
        }
    }
}

