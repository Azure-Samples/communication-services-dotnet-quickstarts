using IncomingCallRouting.Enums;

namespace IncomingCallRouting.Events
{
    public class CallTransferFailedEvent : AcsCallbackEvent
    {
        /// <summary>
        /// Operation context
        /// </summary>
        public string OperationContext { get; set; }

        public ResultInformation ResultInfo { get; set; }

        public override AcsEventType Type { get; set; } = AcsEventType.CallTransferFailed;
    }
}
