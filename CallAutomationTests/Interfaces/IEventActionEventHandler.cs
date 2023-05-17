namespace CallAutomation.Scenarios.Interfaces
{
    public interface IEventActionEventHandler<TEvent>
    {
        Task Handle(TEvent @event);

        Task Handle(string action, string data);
    }
}
