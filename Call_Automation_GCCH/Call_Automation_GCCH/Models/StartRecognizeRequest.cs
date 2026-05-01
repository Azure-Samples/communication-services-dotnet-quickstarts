using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Call_Automation_GCCH.Models
{
    /// <summary>
    /// DTMF-specific recognition options.
    /// Used when recognizeType is "Dtmf".
    /// Maps to: CallMediaRecognizeDtmfOptions
    /// </summary>
    public class DtmfOptionsRequest
    {
        /// <summary>
        /// Maximum number of DTMF tones to collect.
        /// Maps to: CallMediaRecognizeDtmfOptions.MaxTonesToCollect
        /// </summary>
        public int MaxTonesToCollect { get; set; } = 4;

        /// <summary>
        /// Timeout in seconds between each tone press.
        /// Maps to: CallMediaRecognizeDtmfOptions.InterToneTimeout
        /// </summary>
        public int InterToneTimeoutSec { get; set; } = 5;

        /// <summary>
        /// Comma-separated stop tones (e.g. "pound" or "asterisk").
        /// Maps to: CallMediaRecognizeDtmfOptions.StopTones
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? StopTones { get; set; }
    }

    /// <summary>
    /// A single recognition choice for Choice-based recognition.
    /// </summary>
    public class RecognitionChoiceRequest
    {
        /// <summary>
        /// The label/name for this choice.
        /// </summary>
        /// <example>yes</example>
        [Required]
        public string Label { get; set; } = default!;

        /// <summary>
        /// List of phrases that map to this choice.
        /// </summary>
        /// <example>["yes", "yeah", "yep"]</example>
        [Required]
        [MinLength(1)]
        public List<string> Phrases { get; set; } = new();
    }

    /// <summary>
    /// Choice-specific recognition options.
    /// Used when recognizeType is "Choice".
    /// Maps to: CallMediaRecognizeChoiceOptions
    /// </summary>
    public class ChoiceOptionsRequest
    {
        /// <summary>
        /// List of choices the caller can pick from.
        /// Maps to: CallMediaRecognizeChoiceOptions.Choices
        /// </summary>
        [Required]
        [MinLength(1, ErrorMessage = "At least one choice is required.")]
        public List<RecognitionChoiceRequest> Choices { get; set; } = new();
    }

    /// <summary>
    /// Speech-specific recognition options.
    /// Used when recognizeType is "Speech".
    /// Maps to: CallMediaRecognizeSpeechOptions
    /// </summary>
    public class SpeechOptionsRequest
    {
        /// <summary>
        /// Silence duration in seconds after speech ends to finalize recognition.
        /// Maps to: CallMediaRecognizeSpeechOptions.EndSilenceTimeout
        /// </summary>
        public int EndSilenceTimeoutSec { get; set; } = 5;

        /// <summary>
        /// Speech language locale (e.g. en-US).
        /// Maps to: CallMediaRecognizeOptions.SpeechLanguage
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? SpeechLanguage { get; set; }
    }

    /// <summary>
    /// SpeechOrDtmf-specific recognition options (combines Speech + DTMF).
    /// Used when recognizeType is "SpeechOrDtmf".
    /// Maps to: CallMediaRecognizeSpeechOrDtmfOptions
    /// </summary>
    public class SpeechOrDtmfOptionsRequest
    {
        /// <summary>
        /// Maximum number of DTMF tones to collect.
        /// </summary>
        public int MaxTonesToCollect { get; set; } = 4;

        /// <summary>
        /// Timeout in seconds between each tone press.
        /// </summary>
        public int InterToneTimeoutSec { get; set; } = 5;

        /// <summary>
        /// Silence duration in seconds after speech ends to finalize recognition.
        /// </summary>
        public int EndSilenceTimeoutSec { get; set; } = 5;

        /// <summary>
        /// Speech language locale (e.g. en-US).
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? SpeechLanguage { get; set; }
    }

    /// <summary>
    /// Request body for starting recognition on a call.
    /// 
    /// Set "recognizeType" to one of: "Dtmf", "Choice", "Speech", "SpeechOrDtmf".
    /// Then include the matching options object; irrelevant option objects are ignored.
    /// 
    /// Example � DTMF recognition:
    /// {
    ///   "callConnectionId": "...",
    ///   "target": "+18001234567",
    ///   "recognizeType": "Dtmf",
    ///   "dtmfOptions": { "maxTonesToCollect": 4, "interToneTimeoutSec": 5 }
    /// }
    /// 
    /// Example � Choice recognition:
    /// {
    ///   "callConnectionId": "...",
    ///   "target": "+18001234567",
    ///   "recognizeType": "Choice",
    ///   "choiceOptions": {
    ///     "choices": [
    ///       { "label": "yes", "phrases": ["yes", "yeah", "yep"] },
    ///       { "label": "no",  "phrases": ["no", "nope"] }
    ///     ]
    ///   }
    /// }
    /// 
    /// Example � Speech recognition:
    /// {
    ///   "callConnectionId": "...",
    ///   "target": "+18001234567",
    ///   "recognizeType": "Speech",
    ///   "speechOptions": { "endSilenceTimeoutSec": 3, "speechLanguage": "en-US" }
    /// }
    /// 
    /// Example � SpeechOrDtmf recognition:
    /// {
    ///   "callConnectionId": "...",
    ///   "target": "+18001234567",
    ///   "recognizeType": "SpeechOrDtmf",
    ///   "speechOrDtmfOptions": { "maxTonesToCollect": 4, "endSilenceTimeoutSec": 3 }
    /// }
    /// </summary>
    public class StartRecognizeRequest
    {
        /// <summary>
        /// The call connection ID of the active call.
        /// </summary>
        [Required]
        public string CallConnectionId { get; set; } = default!;

        /// <summary>
        /// Target participant: ACS user ID (8:...) or phone number (+...).
        /// </summary>
        [Required]
        public string Target { get; set; } = default!;

        /// <summary>
        /// Recognition type: "Dtmf", "Choice", "Speech", or "SpeechOrDtmf".
        /// </summary>
        /// <example>Dtmf</example>
        [Required]
        public string? RecognizeType { get; set; }

        /// <summary>
        /// Whether to interrupt the prompt when the caller starts input.
        /// </summary>
        public bool InterruptPrompt { get; set; } = false;

        /// <summary>
        /// Seconds to wait for the caller to begin input before timing out.
        /// </summary>
        public int InitialSilenceTimeoutSec { get; set; } = 15;

        /// <summary>
        /// Custom context string for correlating callback events.
        /// </summary>
        public string? OperationContext { get; set; }

        /// <summary>
        /// DTMF-specific options. Required when recognizeType is "Dtmf". Ignored otherwise.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DtmfOptionsRequest? DtmfOptions { get; set; }

        /// <summary>
        /// Choice-specific options. Required when recognizeType is "Choice". Ignored otherwise.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ChoiceOptionsRequest? ChoiceOptions { get; set; }

        /// <summary>
        /// Speech-specific options. Required when recognizeType is "Speech". Ignored otherwise.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SpeechOptionsRequest? SpeechOptions { get; set; }

        /// <summary>
        /// SpeechOrDtmf-specific options. Required when recognizeType is "SpeechOrDtmf". Ignored otherwise.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SpeechOrDtmfOptionsRequest? SpeechOrDtmfOptions { get; set; }
    }
}
