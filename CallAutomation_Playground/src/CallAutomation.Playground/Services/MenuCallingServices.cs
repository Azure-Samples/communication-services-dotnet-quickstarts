using Azure.Communication;
using Azure.Communication.CallAutomation;
using CallAutomation.Playground.Interfaces;

namespace CallAutomation.Playground.Services;

public abstract class MenuCallingServices
{
    private readonly ICallingServices _callingServices;

    protected MenuCallingServices(ICallingServices callingServices)
    {
        _callingServices = callingServices;
    }

    public async Task RecognizeDtmfInput(CallConnectionProperties callConnectionProperties, Func<Task>? executeWhileWaiting,
        Func<CollectTonesResult, Task>? success, Func<RecognizeFailed, Task>? failed,
        CallMediaRecognizeDtmfOptions callMediaRecognizeDtmfOptions, CancellationToken cancellationToken = default)
    {
        await _callingServices.RecognizeDtmfInput(callConnectionProperties, executeWhileWaiting, success, failed,
            callMediaRecognizeDtmfOptions, cancellationToken);
    }

    public async Task PlayAudio(CallConnectionProperties callConnectionProperties, Uri fileUri, CommunicationIdentifier? target,
        bool loop = default, CancellationToken cancellationToken = default)
    {
        await _callingServices.PlayAudio(callConnectionProperties, fileUri, target, loop, cancellationToken);
    }
}