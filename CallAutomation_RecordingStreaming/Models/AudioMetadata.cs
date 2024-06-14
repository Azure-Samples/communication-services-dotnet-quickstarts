using System.Text.Json.Serialization;

namespace RecordingStreaming.Models
{
    public class AudioMetadata
    {
        public int Channels { get; set; }

        public int Length { get; set; }

        public string SubscriptionId { get; set; }

        public AudioEncoding Encoding { get; set; }

        public int SampleRate { get; set; }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AudioEncoding
    {
        PCM
    }
}
