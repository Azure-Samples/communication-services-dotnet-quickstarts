using System.Text.Json.Serialization;

namespace CallAutomation.AzureAI.VoiceLive.Models
{
    public class InputSpeechStarted
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "input_audio_buffer.speech_started";

        [JsonPropertyName("item_id")]
        public string ItemId { get; set; } = string.Empty;

        [JsonPropertyName("audio_start_ms")]
        public int AudioStartTime { get; set; } = 0;
    }
}
