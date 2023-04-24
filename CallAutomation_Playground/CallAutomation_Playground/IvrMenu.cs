using Azure.Communication;
using Azure.Communication.CallAutomation;

namespace CallAutomation_Playground;

public abstract class IvrMenu
{
    public virtual Task OnPressOne(CallConnectionProperties callConnectionProperties, CommunicationIdentifier target) => throw new NotImplementedException();

    public virtual Task OnPressTwo(CallConnectionProperties callConnectionProperties, CommunicationIdentifier target) => throw new NotImplementedException();

    public virtual Task OnPressThree(CallConnectionProperties callConnectionProperties, CommunicationIdentifier target) => throw new NotImplementedException();

    public virtual Task OnPressFour(CallConnectionProperties callConnectionProperties, CommunicationIdentifier target) => throw new NotImplementedException();

    public virtual Task OnPressFive(CallConnectionProperties callConnectionProperties, CommunicationIdentifier target) => throw new NotImplementedException();

    public virtual Task OnPressSix(CallConnectionProperties callConnectionProperties, CommunicationIdentifier target) => throw new NotImplementedException();

    public virtual Task OnPressSeven(CallConnectionProperties callConnectionProperties, CommunicationIdentifier target) => throw new NotImplementedException();

    public virtual Task OnPressEight(CallConnectionProperties callConnectionProperties, CommunicationIdentifier target) => throw new NotImplementedException();

    public virtual Task OnPressNine(CallConnectionProperties callConnectionProperties, CommunicationIdentifier target) => throw new NotImplementedException();

    public virtual Task OnPressZero(CallConnectionProperties callConnectionProperties, CommunicationIdentifier target) => throw new NotImplementedException();

    public virtual Task OnPressPound(CallConnectionProperties callConnectionProperties, CommunicationIdentifier target) => throw new NotImplementedException();

    public virtual Task OnPressStar(CallConnectionProperties callConnectionProperties, CommunicationIdentifier target) => throw new NotImplementedException();
}