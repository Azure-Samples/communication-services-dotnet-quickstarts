using Azure.Communication;
using Azure.Communication.CallAutomation;
using CallAutomation.Playground.Extensions;
using CallAutomation.Playground.Interfaces;
using System.Reflection;

namespace CallAutomation.Playground.Services;

public class CallingServices : ICallingServices
{
    private readonly CallAutomationClient _callAutomationClient;

    public CallingServices(CallAutomationClient callAutomationClient)
    {
        _callAutomationClient = callAutomationClient;
    }

    public async Task AddParticipant(CallConnectionProperties callConnectionProperties, Delegate? executeWhileWaiting, Func<AddParticipantSucceeded, Task>? success, Func<AddParticipantFailed, Task>? failed, CallInvite callInvite, CancellationToken cancellationToken = default)
    {
        var result = await _callAutomationClient
            .GetCallConnection(callConnectionProperties.CallConnectionId)
            .AddParticipantAsync(callInvite);

        await InvokeWhileWaiting(executeWhileWaiting);

        var response = await result.Value.WaitForEventProcessorAsync(cancellationToken);

        switch (response.IsSuccessEvent)
        {
            case true:
                if (success is not null) await success(response.SuccessEvent);
                break;
            case false:
                if (failed is not null) await failed(response.FailureEvent);
                break;
        }
    }

    public async Task RemoveParticipant(CallConnectionProperties callConnectionProperties, Delegate? executeWhileWaiting, Func<RemoveParticipantSucceeded, Task>? success, Func<RemoveParticipantFailed, Task>? failed, CommunicationIdentifier target, CancellationToken cancellationToken = default)
    {
        var result = await _callAutomationClient
            .GetCallConnection(callConnectionProperties.CallConnectionId)
            .RemoveParticipantAsync(target, null);

        await InvokeWhileWaiting(executeWhileWaiting);

        var response = await result.Value.WaitForEventProcessorAsync(cancellationToken);

        switch (response.IsSuccessEvent)
        {
            case true:
                if (success is not null) await success(response.SuccessEvent);
                break;
            case false:
                if (failed is not null) await failed(response.FailureEvent);
                break;
        }
    }

    public async Task<IReadOnlyList<CallParticipant>> GetCallParticipantList(CallConnectionProperties callConnectionProperties, CancellationToken cancellationToken = default)
    {
        var result = await _callAutomationClient
            .GetCallConnection(callConnectionProperties.CallConnectionId)
            .GetParticipantsAsync(cancellationToken);

        return result.Value;
    }

    public async Task RecognizeDtmfInput(CallConnectionProperties callConnectionProperties, Delegate? executeWhileWaiting, Func<CollectTonesResult, Task>? success, Func<RecognizeFailed, Task>? failed, CallMediaRecognizeDtmfOptions callMediaRecognizeDtmfOptions, CancellationToken cancellationToken = default)
    {
        var result = await _callAutomationClient
            .GetCallConnection(callConnectionProperties.CallConnectionId)
            .GetCallMedia()
            .StartRecognizingAsync(callMediaRecognizeDtmfOptions);

        await InvokeWhileWaiting(executeWhileWaiting);

        var response = await result.Value.WaitForEventProcessorAsync(cancellationToken);

        switch (response.IsSuccessEvent)
        {
            case true:
                var collectTonesResult = response.SuccessEvent.RecognizeResult as CollectTonesResult ??
                                         throw new ApplicationException("Couldn't extract DTMF tones from event.");
                if (success is not null) await success(collectTonesResult);
                break;
            case false:
                if (failed is not null) await failed(response.FailureEvent);
                break;
        }
    }

    public async Task PlayAudio(CallConnectionProperties callConnectionProperties, Uri fileUri, CommunicationIdentifier? target, bool loop = default, CancellationToken cancellationToken = default)
    {
        if (target is null)
        {
            await _callAutomationClient.GetCallConnection(callConnectionProperties.CallConnectionId)
                .GetCallMedia()
                .PlayToAllAsync(new FileSource(fileUri), new PlayOptions() { Loop = loop }, cancellationToken);
        }
        else
        {
            await _callAutomationClient.GetCallConnection(callConnectionProperties.CallConnectionId)
                .GetCallMedia()
                .PlayAsync(new FileSource(fileUri), new[] { target }, new PlayOptions() { Loop = loop }, cancellationToken);
        }
    }

    public async Task StartRecording(CallConnectionProperties callConnectionProperties, CancellationToken cancellationToken = default) =>
        await _callAutomationClient.GetCallRecording()
            .StartRecordingAsync(new StartRecordingOptions(new ServerCallLocator(callConnectionProperties.ServerCallId)), cancellationToken);

    public async Task TransferCallLeg(CallConnectionProperties callConnectionProperties, Delegate? executeWhileWaiting, Func<CallTransferAccepted, Task>? success, Func<CallTransferFailed, Task>? failed, CallInvite callInvite, CancellationToken cancellationToken = default)
    {
        var result = await _callAutomationClient.GetCallConnection(callConnectionProperties.CallConnectionId)
            .TransferCallToParticipantAsync(callInvite, cancellationToken);

        await InvokeWhileWaiting(executeWhileWaiting);

        var response = await result.Value.WaitForEventProcessorAsync(cancellationToken);

        switch (response.IsSuccessEvent)
        {
            case true:
                if (success is not null)
                {
                    await success(response.SuccessEvent);
                }
                break;
            case false:
                if (failed is not null)
                {
                    await failed(response.FailureEvent);
                }
                break;
        }
    }

    public async Task CancelMedia(CallConnectionProperties callConnectionProperties, CancellationToken cancellationToken = default) =>
        await _callAutomationClient.GetCallConnection(callConnectionProperties.CallConnectionId).GetCallMedia().CancelAllMediaOperationsAsync(cancellationToken);

    public async Task HangUp(CallConnectionProperties callConnectionProperties, bool terminateCall, CancellationToken cancellationToken = default) =>
        await _callAutomationClient.GetCallConnection(callConnectionProperties.CallConnectionId)
            .HangUpAsync(terminateCall, cancellationToken);

    private async Task InvokeWhileWaiting(Delegate? @delegate)
    {
        if (@delegate is null) return;

        MethodInfo methodInfo = @delegate.Method;
        Type[] paramTypes = methodInfo.GetParameters().Select(p => p.ParameterType).ToArray();
        object[] args = new object[paramTypes.Length];

        for (int i = 0; i < paramTypes.Length; i++)
        {
            args[i] = Activator.CreateInstance(paramTypes[i]);
        }

        if (@delegate.IsAwaitable())
        {
            Task task = (Task)@delegate.DynamicInvoke(args);
            await task;
            return;
        }

        @delegate.DynamicInvoke(args);
    }
}