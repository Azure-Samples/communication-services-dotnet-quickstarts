using System.ComponentModel.DataAnnotations;
using IncomingCallRouting.Enums;

namespace IncomingCallRouting.Events
{
    /// <summary>
    /// The information about the tone.
    /// </summary>
    public class ToneInfo
    {
        /// <summary>
        /// The sequence id which can be used to determine if the same tone was played multiple times or if any tones were missed.
        /// </summary>
        [Required]
        public uint SequenceId { get; set; }

        /// <summary>
        /// The tone value.
        /// </summary>
        [Required]
        public ToneValue Tone { get; set; }
    }
}
