using Azure.Communication;
using Azure.Communication.CallAutomation;
using CallAutomation.Playground.Interfaces;
using CallAutomation.Playground.Services;

namespace CallAutomation.Playground.Choices;

public class PressTwoChoice : IvrChoice
{
    public PressTwoChoice(ICallingServices callingServices)
        : base(callingServices)
    {
    }

    public override async Task OnPress<TTone>(TTone tone, CallConnectionProperties callConnectionProperties, CommunicationIdentifier target,
        CancellationToken cancellationToken = default)
    {
        var participantsList = await CallingServices.GetCallParticipantList(callConnectionProperties, cancellationToken);

        // go through the list and remove each one
        // TODO: unhappy path
        foreach (var participant in participantsList)
        {
            // Remove all PSTN participants that is not the original target
            if (participant.Identifier is PhoneNumberIdentifier && participant.Identifier.RawId != target.RawId)
            {
                await CallingServices.RemoveParticipant(callConnectionProperties, null, null, null, participant.Identifier, cancellationToken);
            }
        }
    }
}