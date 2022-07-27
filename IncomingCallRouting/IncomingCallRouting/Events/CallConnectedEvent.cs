using IncomingCallRouting.Enums;

namespace IncomingCallRouting.Events
{
    public class CallConnectedEvent : AcsCallbackEvent
    {
        public override AcsEventType Type { get; set; } = AcsEventType.CallConnected;
    }
}
