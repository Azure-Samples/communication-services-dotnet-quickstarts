using Azure.Communication;
using Azure.Communication.CallAutomation;
using CallAutomation.Playground.Interfaces;
using CallAutomation.Playground.Services;

namespace CallAutomation.Playground.Choices;

public class PressFourChoice : IvrChoice
{
    public PressFourChoice(ICallingServices callingServices)
        : base(callingServices)
    {
    }

    public override async Task OnPress<TTone>(TTone tone, CallConnectionProperties callConnectionProperties, CommunicationIdentifier target,
        CancellationToken cancellationToken = default) => await CallingServices.HangUp(callConnectionProperties, true, cancellationToken);
}