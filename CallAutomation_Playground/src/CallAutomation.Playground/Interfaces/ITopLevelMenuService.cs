using Azure.Communication;

namespace CallAutomation.Playground.Interfaces
{
    public interface ITopLevelMenuService
    {
        Task InvokeTopLevelMenu(CommunicationIdentifier target, string callConnectionId);
    }
}
