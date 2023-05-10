#load "EventExtensions.csx"

using Azure.Communication.CallingServer;
using Azure.Messaging;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

public class EventDispatcher
{
    private static readonly EventDispatcher instance;
    private static readonly ConcurrentDictionary<string, NotificationCallback> notificationCallbacks;

    public static EventDispatcher Instance
    {
        get
        {
            return instance;
        }
    }

    static EventDispatcher()
    {
        instance = new EventDispatcher();
        notificationCallbacks = new ConcurrentDictionary<string, NotificationCallback>(StringComparer.OrdinalIgnoreCase);
    }

    public bool Subscribe(string eventType, string eventKey, NotificationCallback notificationCallback)
    {
        string eventId = BuildEventKey(eventType, eventKey);
        bool ret = notificationCallbacks.TryAdd(eventId, notificationCallback);
        return ret;
    }

    public void Unsubscribe(string eventType, string eventKey)
    {
        string eventId = BuildEventKey(eventType, eventKey);
        notificationCallbacks.TryRemove(eventId, out _);
    }

    public void ProcessNotification(string request)
    {
        var callEvent = this.ExtractEvent(request);

        if (callEvent != null)
        {
            if (notificationCallbacks.TryGetValue(GetEventKey(callEvent), out NotificationCallback notificationCallback))
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

    public string GetEventKey(CallingServerEventBase callEventBase)
    {
        if (callEventBase is CallConnectionStateChangedEvent)
        {
            var callLegId = ((CallConnectionStateChangedEvent)callEventBase).CallConnectionId;
            return BuildEventKey(CallingServerEventType.CallConnectionStateChangedEvent.ToString(), callLegId); ;
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