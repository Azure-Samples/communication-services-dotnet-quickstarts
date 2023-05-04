using Azure.Communication;
using Azure.Communication.CallAutomation;
using CallAutomation.Playground.Interfaces;
using CallAutomation.Playground.Services;

namespace CallAutomation.Playground.Tests;

public class TestChoice : IvrChoice
{
    public TestChoice(ICallingServices callingServices) : base(callingServices)
    {
    }

    public override Task OnPress<TTone>(TTone tone, CallConnectionProperties callConnectionProperties, CommunicationIdentifier target,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}