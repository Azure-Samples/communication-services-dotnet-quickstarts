using Azure.Communication;
using Azure.Communication.CallAutomation;
using CallAutomation.Playground.Exceptions;
using CallAutomation.Playground.Interfaces;
using CallAutomation.Playground.Services;

namespace CallAutomation.Playground.Menus;

public class IvrMenu : MenuCallingServices
{
    private readonly Dictionary<Type, IvrChoice> _choices = new();
    private readonly IvrConfiguration _ivrConfiguration;

    public IvrMenu(IServiceProvider serviceProvider, Dictionary<Type, Type> choices, IvrConfiguration ivrConfiguration)
        : base(serviceProvider.GetRequiredService<ICallingServices>())
    {
        _ivrConfiguration = ivrConfiguration;
        foreach (var (tone, ivrChoice) in choices)
        {
            _choices.Add(tone, (IvrChoice)serviceProvider.GetRequiredService(ivrChoice));
        }
    }

    public async Task OnPress<TTone>(TTone tone, CallConnectionProperties callConnectionProperties, CommunicationIdentifier target, CancellationToken cancellationToken = default)
        where TTone : IDtmfTone
    {
        // get the choice based on the tone
        _choices.TryGetValue(typeof(TTone), out var choice);
        if (choice is null)
        {
            await InvokeInvalidEntryAsync(callConnectionProperties, cancellationToken);
        }
        else
        {
            await choice.OnPress(tone, callConnectionProperties, target, cancellationToken);
        }
    }

    private async Task InvokeInvalidEntryAsync(CallConnectionProperties callConnectionProperties, CancellationToken cancellationToken)
    {
        await PlayAudio(callConnectionProperties, _ivrConfiguration.InvalidEntryUri, null, false, cancellationToken);
        throw new InvalidEntryException("Invalid selection");
    }
}