using Azure.Communication.CallAutomation;

namespace CogSvcIvrSamples
{
    public interface IWorkflowHandler
    {
        Task HandleAsync(string callerId,
            CallAutomationEventBase @event,
            CallConnection callConnection,
            CallMedia callConnectionMedia);
    }
}
