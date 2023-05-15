namespace CallAutomation.Scenarios.Interfaces
{
    public interface IEventActionEventHandler<TEvent>
    {
        Task Handle(TEvent eventName);
    }
}
