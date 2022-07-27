using IncomingCallRouting.Enums;

namespace IncomingCallRouting.Events
{
    public class CallDisconnectedEvent : AcsCallbackEvent
    {
        public override AcsEventType Type { get; set; } = AcsEventType.CallDisconnected;
    }
}
