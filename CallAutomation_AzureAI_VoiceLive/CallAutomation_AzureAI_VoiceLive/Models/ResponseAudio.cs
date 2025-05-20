using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CallAutomation.AzureAI.VoiceLive.Models
{
    internal class ResponseAudio
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "response.audio.delta";

        [JsonPropertyName("response_id")]
        public string ResponseId { get; set; } = string.Empty;

        [JsonPropertyName("item_id")]
        public string ItemId { get; set; } = string.Empty;

        [JsonPropertyName("output_index")]
        public int OutputIndex { get; set; } = 0;

        [JsonPropertyName("content_index")]
        public int ContentIndex { get; set; } = 0;

        [JsonPropertyName("delta")]
        public string Delta { get; set; } = string.Empty;
    }
}
