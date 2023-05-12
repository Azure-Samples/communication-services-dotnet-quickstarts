using Azure.Communication;
using Azure.Communication.CallAutomation;

namespace CallAutomation_Playground.Interfaces
{
    public interface IOngoingEventHandler
    {
        void AttachCountParticipantsInTheCall(string callConnectionId);

        void AttachDisconnectedWrapup(string callConnectionId);
    }
}
