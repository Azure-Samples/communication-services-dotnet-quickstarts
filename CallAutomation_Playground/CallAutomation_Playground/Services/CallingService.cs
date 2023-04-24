using Azure.Communication;
using Azure.Communication.CallAutomation;
using CallAutomation_Playground.Interfaces;

namespace CallAutomation_Playground.Services;

public class CallingService : ICallingService
{
    private readonly CallAutomationClient _callAutomationClient;

    public CallingService(CallAutomationClient callAutomationClient)
    {
        _callAutomationClient = callAutomationClient;
    }

    public async Task<CallConnectionProperties> GetCallConnectionProperties(string callConnectionId) =>
        _callAutomationClient.GetCallConnection(callConnectionId).GetCallConnectionPropertiesAsync().Result;

    public async Task AddParticipant(CallConnectionProperties callConnectionProperties, Func<AddParticipantSucceeded, Task>? success, Func<AddParticipantFailed, Task>? failed, CallInvite callInvite)
    {
        var result = await _callAutomationClient
            .GetCallConnection(callConnectionProperties.CallConnectionId)
            .AddParticipantAsync(callInvite);

        var response = await result.Value.WaitForEventProcessorAsync();

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

    public async Task RemoveParticipant(CallConnectionProperties callConnectionProperties, Func<RemoveParticipantSucceeded, Task>? success, Func<RemoveParticipantFailed, Task>? failed, CommunicationIdentifier target)
    {
        var result = await _callAutomationClient
            .GetCallConnection(callConnectionProperties.CallConnectionId)
            .RemoveParticipantAsync(target);

        var response = await result.Value.WaitForEventProcessorAsync();

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

    public async Task<IReadOnlyList<CallParticipant>> GetCallParticipantList(CallConnectionProperties callConnectionProperties)
    {
        var result =await _callAutomationClient.GetCallConnection(callConnectionProperties.CallConnectionId)
            .GetParticipantsAsync();

        return result.Value;
    }

    public async Task RecognizeDtmfInput(CallConnectionProperties callConnectionProperties,
        Func<CollectTonesResult?, Task>? success, Func<RecognizeFailed, Task>? failed,
        CallMediaRecognizeDtmfOptions callMediaRecognizeDtmfOptions)
    {
        var result = await _callAutomationClient.GetCallConnection(callConnectionProperties.CallConnectionId)
            .GetCallMedia()
            .StartRecognizingAsync(callMediaRecognizeDtmfOptions);

        var response = await result.Value.WaitForEventProcessorAsync();

        switch (response.IsSuccessEvent)
        {
            case true:
                var collectTonesResult = response.SuccessEvent.RecognizeResult as CollectTonesResult;
                if (success is not null)
                {
                    await success(collectTonesResult);
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

    public async Task PlayHoldMusic(CallConnectionProperties callConnectionProperties, Uri fileUri, CommunicationIdentifier? target, bool loop = false)
    {
        if (target is null)
        {
            await _callAutomationClient.GetCallConnection(callConnectionProperties.CallConnectionId)
                .GetCallMedia()
                .PlayToAllAsync(new FileSource(fileUri), new PlayOptions() { Loop = loop });
        }
        else
        {
            await _callAutomationClient.GetCallConnection(callConnectionProperties.CallConnectionId)
                .GetCallMedia()
                .PlayAsync(new FileSource(fileUri), new[] { target }, new PlayOptions() { Loop = loop });
        }
    }

    public async Task StartRecording(CallConnectionProperties callConnectionProperties) =>
        await _callAutomationClient.GetCallRecording()
            .StartRecordingAsync(new StartRecordingOptions(new ServerCallLocator(callConnectionProperties.ServerCallId)));

    public async Task TransferCallLeg(CallConnectionProperties callConnectionProperties, Func<CallTransferAccepted, Task>? success, Func<CallTransferFailed, Task>? failed, CallInvite callInvite)
    {
        var result = await _callAutomationClient.GetCallConnection(callConnectionProperties.CallConnectionId)
            .TransferCallToParticipantAsync(callInvite);

        var response = await result.Value.WaitForEventProcessorAsync();

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

    public async Task CancelMedia(CallConnectionProperties callConnectionProperties) =>
        await _callAutomationClient.GetCallConnection(callConnectionProperties.CallConnectionId).GetCallMedia().CancelAllMediaOperationsAsync();

    public async Task HangUp(CallConnectionProperties callConnectionProperties, bool terminateCall) =>
        await _callAutomationClient.GetCallConnection(callConnectionProperties.CallConnectionId)
            .HangUpAsync(terminateCall);
}