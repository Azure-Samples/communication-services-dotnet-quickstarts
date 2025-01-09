using Azure.Communication.Rooms;

namespace ConnectCall_CallAutomation
{
    public class Participant
    {
        public required string CommunicationUserId { get; set; }

        /// <summary> The role of a room participant should be Presenter or Attendee or  Consumer. The default value is Attendee. </summary>
        public string? Role { get; set; }
    }
}
