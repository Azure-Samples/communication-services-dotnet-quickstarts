using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Call_Automation_GCCH.Models
{
    // ?? Transfer ????????????????????????????????????????????????????????????????

    /// <summary>
    /// Request body for transferring a call.
    /// 
    /// Example:
    /// { "callConnectionId": "...", "transferTarget": "+18005551234", "transferee": "+18005555678", "isPstn": true }
    /// </summary>
    public class TransferCallRequest
    {
        /// <summary>The active call connection ID.</summary>
        [Required] public string CallConnectionId { get; set; } = default!;

        /// <summary>The target to transfer to: phone number (+...) or ACS user ID (8:...).</summary>
        [Required] public string TransferTarget { get; set; } = default!;

        /// <summary>The participant being transferred: phone number (+...) or ACS user ID (8:...).</summary>
        [Required] public string Transferee { get; set; } = default!;

        /// <summary>True if targets are PSTN phone numbers.</summary>
        public bool IsPstn { get; set; } = false;
    }

    // ?? Play ????????????????????????????????????????????????????????????????????

    /// <summary>
    /// Optional play behaviour. Omit to use the simple Play(PlaySource, targets) overload.
    /// Include to use Play(PlayOptions).
    /// </summary>
    public class PlayOptionsRequest
    {
        /// <summary>Interrupts hold audio. Maps to: PlayOptions.InterruptHoldAudio</summary>
        public bool InterruptHoldAudio { get; set; } = false;

        /// <summary>Loop the audio. Maps to: PlayOptions.Loop</summary>
        public bool Loop { get; set; } = false;

        /// <summary>Custom context. Maps to: PlayOptions.OperationContext</summary>
        public string? OperationContext { get; set; }
    }

    /// <summary>
    /// Request body for playing audio to a specific target.
    /// 
    /// Example � simple play:
    /// { "callConnectionId": "...", "target": "+18001234567" }
    /// 
    /// Example � play with options:
    /// {
    ///   "callConnectionId": "...",
    ///   "target": "+18001234567",
    ///   "playOptions": { "interruptHoldAudio": true, "loop": false }
    /// }
    /// </summary>
    public class PlayRequest
    {
        /// <summary>The active call connection ID.</summary>
        [Required] public string CallConnectionId { get; set; } = default!;

        /// <summary>Target participant: ACS user ID (8:...) or phone number (+...).</summary>
        [Required] public string Target { get; set; } = default!;

        /// <summary>Play options. Omit for simple play, include for PlayOptions overload.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public PlayOptionsRequest? PlayOptions { get; set; }
    }

    // ?? PlayToAll ???????????????????????????????????????????????????????????????

    /// <summary>
    /// Optional play-to-all behaviour. Omit to use simple PlayToAll(PlaySource).
    /// Include to use PlayToAll(PlayToAllOptions).
    /// </summary>
    public class PlayToAllOptionsRequest
    {
        /// <summary>Interrupts current media / barge-in. Maps to: PlayToAllOptions.InterruptCallMediaOperation</summary>
        public bool InterruptCallMediaOperation { get; set; } = false;

        /// <summary>Loop the audio. Maps to: PlayToAllOptions.Loop</summary>
        public bool Loop { get; set; } = false;

        /// <summary>Custom context. Maps to: PlayToAllOptions.OperationContext</summary>
        public string? OperationContext { get; set; }
    }

    /// <summary>
    /// Request body for playing audio to all participants.
    /// 
    /// Example � simple:
    /// { "callConnectionId": "..." }
    /// 
    /// Example � with options:
    /// { "callConnectionId": "...", "playToAllOptions": { "loop": true, "interruptCallMediaOperation": false } }
    /// </summary>
    public class PlayToAllRequest
    {
        /// <summary>The active call connection ID.</summary>
        [Required] public string CallConnectionId { get; set; } = default!;

        /// <summary>Play-to-all options. Omit for simple play, include for PlayToAllOptions overload.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public PlayToAllOptionsRequest? PlayToAllOptions { get; set; }
    }

    // ?? Hold / Unhold ???????????????????????????????????????????????????????????

    /// <summary>
    /// Optional hold behaviour. Omit for simple Hold(identifier).
    /// </summary>
    public class HoldOptionsRequest
    {
        /// <summary>When true, plays hold music.</summary>
        public bool PlayHoldMusic { get; set; } = false;

        /// <summary>Custom context. Maps to: HoldOptions.OperationContext</summary>
        public string? OperationContext { get; set; }
    }

    /// <summary>
    /// Request body for holding a participant.
    /// 
    /// Example � simple hold:
    /// { "callConnectionId": "...", "target": "+18001234567" }
    /// 
    /// Example � hold with music:
    /// { "callConnectionId": "...", "target": "+18001234567", "holdOptions": { "playHoldMusic": true } }
    /// </summary>
    public class HoldRequest
    {
        [Required] public string CallConnectionId { get; set; } = default!;
        [Required] public string Target { get; set; } = default!;

        /// <summary>Hold options. Omit for simple hold.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public HoldOptionsRequest? HoldOptions { get; set; }
    }

    /// <summary>
    /// Optional unhold behaviour. Omit for simple Unhold(identifier).
    /// </summary>
    public class UnholdOptionsRequest
    {
        /// <summary>Custom context. Maps to: UnholdOptions.OperationContext</summary>
        public string? OperationContext { get; set; }
    }

    /// <summary>
    /// Request body for unholding a participant.
    /// 
    /// Example � simple:
    /// { "callConnectionId": "...", "target": "+18001234567" }
    /// 
    /// Example � with options:
    /// { "callConnectionId": "...", "target": "+18001234567", "unholdOptions": { "operationContext": "myCtx" } }
    /// </summary>
    public class UnholdRequest
    {
        [Required] public string CallConnectionId { get; set; } = default!;
        [Required] public string Target { get; set; } = default!;

        /// <summary>Unhold options. Omit for simple unhold.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public UnholdOptionsRequest? UnholdOptions { get; set; }
    }

    // ?? Transcription (start/stop/update) ???????????????????????????????????????

    /// <summary>
    /// Request body for starting transcription on a call.
    /// 
    /// Example: { "callConnectionId": "...", "locale": "en-US" }
    /// </summary>
    public class StartTranscriptionRequest
    {
        [Required] public string CallConnectionId { get; set; } = default!;

        /// <summary>Locale for transcription (e.g. en-US).</summary>
        public string? Locale { get; set; }

        /// <summary>Custom context.</summary>
        public string? OperationContext { get; set; }
    }

    /// <summary>
    /// Request body for stopping transcription.
    /// 
    /// Example: { "callConnectionId": "..." }
    /// </summary>
    public class StopTranscriptionRequest
    {
        [Required] public string CallConnectionId { get; set; } = default!;
        public string? OperationContext { get; set; }
    }

    /// <summary>
    /// Request body for updating transcription locale.
    /// 
    /// Example: { "callConnectionId": "...", "locale": "es-ES" }
    /// </summary>
    public class UpdateTranscriptionRequest
    {
        [Required] public string CallConnectionId { get; set; } = default!;

        /// <summary>New locale (e.g. en-US, es-ES).</summary>
        public string? Locale { get; set; }

        /// <summary>Custom context.</summary>
        public string? OperationContext { get; set; }
    }

    // ?? SendDtmfTones ???????????????????????????????????????????????????????????

    /// <summary>
    /// Request body for sending DTMF tones.
    /// 
    /// Example: { "callConnectionId": "...", "target": "+18001234567", "tones": "one,two,pound" }
    /// </summary>
    public class SendDtmfTonesRequest
    {
        [Required] public string CallConnectionId { get; set; } = default!;

        /// <summary>Target participant: ACS user ID (8:...) or phone number (+...).</summary>
        [Required] public string Target { get; set; } = default!;

        /// <summary>Comma-separated tones: zero-nine, pound, asterisk (or 0-9, #, *). e.g. "one,two,pound"</summary>
        public string? Tones { get; set; }

        /// <summary>Custom context.</summary>
        public string? OperationContext { get; set; }
    }

    // ?? ContinuousDtmf ?????????????????????????????????????????????????????????

    /// <summary>
    /// Request body for starting or stopping continuous DTMF recognition.
    /// 
    /// Example: { "callConnectionId": "...", "target": "+18001234567" }
    /// </summary>
    public class ContinuousDtmfRequest
    {
        [Required] public string CallConnectionId { get; set; } = default!;

        /// <summary>Target participant: ACS user ID (8:...) or phone number (+...).</summary>
        [Required] public string Target { get; set; } = default!;

        /// <summary>Custom context.</summary>
        public string? OperationContext { get; set; }
    }

    // ?? InterruptAudioAndAnnounce ???????????????????????????????????????????????

    /// <summary>
    /// Request body for interrupting audio and making an announcement.
    /// 
    /// Example: { "callConnectionId": "...", "target": "+18001234567" }
    /// </summary>
    public class InterruptAudioAndAnnounceRequest
    {
        [Required] public string CallConnectionId { get; set; } = default!;

        /// <summary>Target participant: ACS user ID (8:...) or phone number (+...).</summary>
        [Required] public string Target { get; set; } = default!;

        /// <summary>Custom context.</summary>
        public string? OperationContext { get; set; }
    }

    // ?? StartMediaStreaming / StopMediaStreaming ?????????????????????????????????

    /// <summary>
    /// Request body for starting or stopping media streaming on a call.
    /// 
    /// Example: { "callConnectionId": "..." }
    /// </summary>
    public class MediaStreamingActionRequest
    {
        [Required] public string CallConnectionId { get; set; } = default!;

        /// <summary>Custom context.</summary>
        public string? OperationContext { get; set; }
    }
}
