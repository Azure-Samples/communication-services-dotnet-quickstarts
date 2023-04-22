using Azure.Communication;

namespace CallAutomation_Playground.Interfaces
{
    public interface ITopLevelMenuService
    {
        Task InvokeTopLevelMenu(CommunicationIdentifier target, string callConnectionId);
    }
}
