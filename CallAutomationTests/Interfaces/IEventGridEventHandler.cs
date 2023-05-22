// © Microsoft Corporation. All rights reserved.

namespace CallAutomation.Scenarios.Interfaces
{
    public interface IEventGridEventHandler<TEvent>
    {
        Task Handle(TEvent @event);
        Task Handle(TEvent @event, string info);
    }
}
