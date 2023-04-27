using Azure.Communication;
using Azure.Communication.CallAutomation;
using CallAutomation.Playground.Exceptions;
using CallAutomation.Playground.Services;

namespace CallAutomation.Playground.Menus;

public abstract class IvrMenu : CallingService
{
    private readonly CallAutomationClient _callAutomationClient;
    private readonly PlaygroundConfig _playgroundConfig;

    protected IvrMenu(CallAutomationClient callAutomationClient, PlaygroundConfig playgroundConfig)
        : base(callAutomationClient)
    {
        _callAutomationClient = callAutomationClient;
        _playgroundConfig = playgroundConfig;
    }

    public virtual async Task OnPressOne(CallConnectionProperties callConnectionProperties,
        CommunicationIdentifier target, CancellationToken cancellationToken = default) =>
        await InvokeInvalidEntryAsync(callConnectionProperties);

    public virtual async Task OnPressTwo(CallConnectionProperties callConnectionProperties,
        CommunicationIdentifier target, CancellationToken cancellationToken = default) =>
        await InvokeInvalidEntryAsync(callConnectionProperties);

    public virtual async Task OnPressThree(CallConnectionProperties callConnectionProperties,
        CommunicationIdentifier target, CancellationToken cancellationToken = default) =>
        await InvokeInvalidEntryAsync(callConnectionProperties);

    public virtual async Task OnPressFour(CallConnectionProperties callConnectionProperties,
        CommunicationIdentifier target, CancellationToken cancellationToken = default) =>
        await InvokeInvalidEntryAsync(callConnectionProperties);

    public virtual async Task OnPressFive(CallConnectionProperties callConnectionProperties,
        CommunicationIdentifier target, CancellationToken cancellationToken = default) =>
        await InvokeInvalidEntryAsync(callConnectionProperties);

    public virtual async Task OnPressSix(CallConnectionProperties callConnectionProperties,
        CommunicationIdentifier target, CancellationToken cancellationToken = default) =>
        await InvokeInvalidEntryAsync(callConnectionProperties);

    public virtual async Task OnPressSeven(CallConnectionProperties callConnectionProperties,
        CommunicationIdentifier target, CancellationToken cancellationToken = default) =>
        await InvokeInvalidEntryAsync(callConnectionProperties);

    public virtual async Task OnPressEight(CallConnectionProperties callConnectionProperties,
        CommunicationIdentifier target, CancellationToken cancellationToken = default) =>
        await InvokeInvalidEntryAsync(callConnectionProperties);

    public virtual async Task OnPressNine(CallConnectionProperties callConnectionProperties,
        CommunicationIdentifier target, CancellationToken cancellationToken = default) =>
        await InvokeInvalidEntryAsync(callConnectionProperties);

    public virtual async Task OnPressZero(CallConnectionProperties callConnectionProperties,
        CommunicationIdentifier target, CancellationToken cancellationToken = default) =>
        await InvokeInvalidEntryAsync(callConnectionProperties);

    public virtual async Task OnPressPound(CallConnectionProperties callConnectionProperties,
        CommunicationIdentifier target, CancellationToken cancellationToken = default) =>
        await InvokeInvalidEntryAsync(callConnectionProperties);

    public virtual async Task OnPressStar(CallConnectionProperties callConnectionProperties,
        CommunicationIdentifier target, CancellationToken cancellationToken = default) =>
        await InvokeInvalidEntryAsync(callConnectionProperties);

    private async Task InvokeInvalidEntryAsync(CallConnectionProperties callConnectionProperties)
    {
        await _callAutomationClient
            .GetCallConnection(callConnectionProperties.CallConnectionId)
            .GetCallMedia()
            .PlayToAllAsync(new FileSource(_playgroundConfig.InvalidEntryUri));

        throw new InvalidEntryException("Invalid selection");
    }
}