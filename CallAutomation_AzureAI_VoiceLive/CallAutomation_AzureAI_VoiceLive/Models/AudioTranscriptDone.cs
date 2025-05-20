using System.Text.Json.Serialization;

namespace CallAutomation.AzureAI.VoiceLive.Models
{
    public class AudioTranscriptDone
    {

            [JsonPropertyName("type")]
            public string Type { get; set; } = "response.audio_transcript.done";

            [JsonPropertyName("response_id")]
            public string ResponseId { get; set; } = string.Empty;

            [JsonPropertyName("item_id")]
            public string ItemId { get; set; } = string.Empty;

            [JsonPropertyName("output_index")]
            public int OutputIndex { get; set; } = 0;

            [JsonPropertyName("content_index")]
            public int ContentIndex { get; set; } = 0;

            [JsonPropertyName("transcript")]
            public int Transcript { get; set; } = 0;
    }
}
