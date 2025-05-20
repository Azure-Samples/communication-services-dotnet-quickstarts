using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace CallAutomation.AzureAI.VoiceLive.Models
{
    public class SessionUpdate
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "session.update";

        [JsonPropertyName("session")]
        public SessionConfig Session { get; set; } = new SessionConfig();

        public class SessionConfig
        {
            [JsonPropertyName("input_audio_transcription")]
            public InputAudioTranscription InputAudioTranscription { get; set; } = new InputAudioTranscription();

            [JsonPropertyName("turn_detection")]
            public TurnDetection TurnDetection { get; set; } = new TurnDetection();

            [JsonPropertyName("voice")]
            public Voice Voice { get; set; } = new Voice();

            [JsonPropertyName("tools")]
            public List<object> Tools { get; set; } = new List<object>();

            [JsonPropertyName("temperature")]
            public double Temperature { get; set; } = 0.9;

            [JsonPropertyName("modalities")]
            public List<string> Modalities { get; set; } = new List<string> { "text", "audio" };

            [JsonPropertyName("input_audio_noise_reduction")]
            public object? InputAudioNoiseReduction { get; set; } = null;

            [JsonPropertyName("input_audio_echo_cancellation")]
            public object? InputAudioEchoCancellation { get; set; } = null;
        }

        public class InputAudioTranscription
        {
            [JsonPropertyName("model")]
            public string Model { get; set; } = "whisper-1";
        }

        public class TurnDetection
        {
            [JsonPropertyName("type")]
            public string Type { get; set; } = "server_vad";
        }

        public class Voice
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = "en-US-AvaNeural";

            [JsonPropertyName("type")]
            public string Type { get; set; } = "azure-standard";
        }


        /// <summary>
        /// Serializes this object to a JSON string
        /// </summary>
        public string ToJson()
        {
            return System.Text.Json.JsonSerializer.Serialize(this,
                new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
        }

        /// <summary>
        /// Deserializes a JSON string into a SessionUpdate object
        /// </summary>
        public static SessionUpdate? FromJson(string json)
        {
            return System.Text.Json.JsonSerializer.Deserialize<SessionUpdate>(json);
        }
    }
}
