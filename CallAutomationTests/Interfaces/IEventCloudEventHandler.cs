
namespace CallAutomation.Scenarios.Interfaces
{
    public interface IEventCloudEventHandler<TEvent>
    {
        Task Handle(TEvent @event, string callerId = "");
    }
}