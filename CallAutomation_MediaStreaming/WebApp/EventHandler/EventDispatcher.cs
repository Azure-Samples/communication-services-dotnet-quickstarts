// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Communication.CallAutomation;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace IncomingCallMediaStreaming
{
    public class EventDispatcher
    {
        public static readonly EventDispatcher Instance;
        private readonly ConcurrentDictionary<string, NotificationCallback> NotificationCallback;
        private object SubscriptionLock = new object();

        static EventDispatcher()
        {
            Instance = new EventDispatcher();
        }

        private EventDispatcher()
        {
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
            CallAutomationEventBase callEvent = CallAutomationEventParser.Parse(BinaryData.FromString(request));

            if (callEvent != null)
            {
                lock (SubscriptionLock)
                {
                    var callLegId = callEvent.CallConnectionId;
                    if (NotificationCallback.TryGetValue(BuildEventKey(callEvent.GetType().Name, callLegId), out NotificationCallback notificationCallback))
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

        public string BuildEventKey(string eventType, string eventKey)
        {
            return $"{eventType}-{eventKey}";
        }
    }

}

