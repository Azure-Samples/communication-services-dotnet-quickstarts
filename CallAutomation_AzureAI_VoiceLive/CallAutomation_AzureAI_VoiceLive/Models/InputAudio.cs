using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CallAutomation.AzureAI.VoiceLive.Models
{
    internal class InputAudio
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "input_audio_buffer.append";

        [JsonPropertyName("audio")]
        public string Audio { get; set; }

        /// <summary>
        /// Serializes this object to a JSON string with error handling
        /// </summary>
        public string ToJson()
        {
            if (Audio == null)
            {
                throw new InvalidOperationException("Audio data cannot be null.");
            }

            try
            {
                return JsonSerializer.Serialize(this, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Serialization error: {ex.Message}");
                return string.Empty;
            }
        }

    }
}

