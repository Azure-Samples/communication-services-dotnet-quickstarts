using RecordingStreaming.Models;

namespace RecordingStreaming.Interfaces
{
    public interface ITelemetryService
    {
        Task<string> LogLatenciesAsync(LatencyRecord[] records);
    }
}
