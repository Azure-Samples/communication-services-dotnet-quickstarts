using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using RecordingStreaming.Interfaces;

namespace RecordingStreaming.Services
{
    public class EventsService : IEventsService
    {
        private readonly HttpClient _httpClient;

        public EventsService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri($"{configuration["CallbackUri"]}");
        }

        public async Task SendRecordingStatusUpdatedEvent(AcsRecordingFileStatusUpdatedEventData recordingFileStatusUpdated, string serverCallId, Uri target)
        {
            var eventGridEvent = new EventGridEvent("/recording/call/{callId}/serverCallId/{serverCallId}",
                                              "Microsoft.Communication.RecordingFileStatusUpdated",
                                              "1.0",
                                              recordingFileStatusUpdated,
                                              typeof(AcsRecordingFileStatusUpdatedEventData));

            await _httpClient.PostAsJsonAsync("/api/recordingDone", eventGridEvent);
        }

        // public async Task SendRecordingStartedEvent(RecordingStartedEvent)
    }
}
