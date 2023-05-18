using Azure.Communication.CallAutomation;
using CallAutomation.Scenarios.Handlers;

namespace CallAutomation.Scenarios.Interfaces
{
    public interface IEventActionEventHandler<TEvent>
    {
        Task Handle(TEvent @event);

        Task Handle(string action, string data);

        RecordingContext Handle(string data);
    }
}
