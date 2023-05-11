namespace CallAutomation.Scenarios.Interfaces
{
    public interface IEventActionsEventHandler<TEvent>
    {
        Task Handle(TEvent eventName);
    }
}
