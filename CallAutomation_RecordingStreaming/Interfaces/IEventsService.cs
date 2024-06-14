using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.CognitiveServices.Speech;
using RecordingStreaming.Models;

namespace RecordingStreaming.Interfaces
{
    public interface IEventsService
    {
        Task SendRecordingStatusUpdatedEvent(AcsRecordingFileStatusUpdatedEventData recordingFileStatusUpdated, string serverCallId, Uri target);
    }
}
