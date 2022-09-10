// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Azure.Communication.CallingServer;
using Azure.Messaging;
using IncomingCallRouting.Enums;

namespace IncomingCallRouting.EventHandler
{
    public class EventDispatcher
    {
        public static readonly EventDispatcher Instance;
        /// <summary>
        /// Maintaining callback events in dictionary
        /// </summary>
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

        public void ProcessNotification(string content)
        {
            var callEvent = this.ExtractEvent(content);

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

        public string GetEventKey(CallAutomationEventBase callEventBase)
        {
            if (callEventBase is CallConnected)
            {
                var callLegId = ((CallConnected)callEventBase).CallConnectionId;
                return BuildEventKey(AcsEventType.CallConnected.ToString(), callLegId);;
            }
            else if (callEventBase is RecognizeCompleted)
            {
                var callLegId = ((RecognizeCompleted)callEventBase).CallConnectionId;
                return BuildEventKey(AcsEventType.RecognizeCompleted.ToString(), callLegId);
            }
            else if (callEventBase is CallRecordingStateChanged)
            {
                var recordingId = ((CallRecordingStateChanged)callEventBase).RecordingId;
                return BuildEventKey(AcsEventType.CallRecordingStateChanged.ToString(), recordingId);
            }
            else if (callEventBase is PlayCompleted)
            {
                var operationContext = ((PlayCompleted)callEventBase).OperationContext;
                return BuildEventKey(AcsEventType.PlayCompleted.ToString(), operationContext);
            }
            else if (callEventBase is ParticipantsUpdated)
            {
                var callLegId = ((ParticipantsUpdated)callEventBase).CallConnectionId;
                return BuildEventKey(AcsEventType.ParticipantsUpdated.ToString(), callLegId);
            }
            else if (callEventBase is AddParticipantsSucceeded)
            {
                var callLegId = ((AddParticipantsSucceeded)callEventBase).CallConnectionId;
                return BuildEventKey(AcsEventType.AddParticipantsSucceeded.ToString(), callLegId);
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
        public CallAutomationEventBase ExtractEvent(string content)
        {
            CloudEvent cloudEvent = CloudEvent.Parse(BinaryData.FromString(content));

            if (cloudEvent != null && cloudEvent.Data != null)
            {
                if (cloudEvent.Type.EndsWith(AcsEventType.CallConnected.ToString(), true, null))
                {
                    return CallConnected.Deserialize(cloudEvent.Data.ToString());
                }
                else if (cloudEvent.Type.Equals(AcsEventType.RecognizeCompleted.ToString()))
                {
                    return RecognizeCompleted.Deserialize(cloudEvent.Data.ToString());
                }
                else if (cloudEvent.Type.Equals(AcsEventType.ParticipantsUpdated.ToString()))
                {
                    return ParticipantsUpdated.Deserialize(cloudEvent.Data.ToString());
                }
                else if (cloudEvent.Type.Equals(AcsEventType.CallRecordingStateChanged.ToString()))
                {
                    return CallRecordingStateChanged.Deserialize(cloudEvent.Data.ToString());
                }
                else if (cloudEvent.Type.Equals(AcsEventType.PlayCompleted.ToString()))
                {
                    return PlayCompleted.Deserialize(cloudEvent.Data.ToString());
                }
                else if (cloudEvent.Type.EndsWith(AcsEventType.ParticipantsUpdated.ToString(), true, null))
                {
                    return ParticipantsUpdated.Deserialize(cloudEvent.Data.ToString());
                }
            }

            return null;
        }
    }
}

