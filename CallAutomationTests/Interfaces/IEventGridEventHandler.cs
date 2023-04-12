// © Microsoft Corporation. All rights reserved.

using System.Threading.Tasks;

namespace CallAutomation.Scenarios.Interfaces
{
    public interface IEventGridEventHandler<TEvent>
    {
        Task Handle(TEvent eventName);
    }
}
