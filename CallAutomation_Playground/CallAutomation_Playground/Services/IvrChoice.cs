using Azure.Communication;
using Azure.Communication.CallAutomation;
using CallAutomation.Playground.Interfaces;

namespace CallAutomation.Playground.Services;

public abstract class IvrChoice
{
    public ICallingServices CallingServices { get; }

    protected IvrChoice(ICallingServices callingServices)
    {
        CallingServices = callingServices;
    }

    public abstract Task OnPress<TTone>(TTone tone, CallConnectionProperties callConnectionProperties,
        CommunicationIdentifier target, CancellationToken cancellationToken = default);
}