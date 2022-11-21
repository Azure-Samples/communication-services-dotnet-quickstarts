using System;
using Microsoft.CognitiveServices.Speech;

namespace RecognizerBot.Models
{
    public class SpeechAudioResult
    {
        public SpeechSynthesisResult? SpeechSynthesisResult { get; set; }
        
        public Uri? AudioFileUri { get; set; }
        
        public SpeechSynthesisCancellationDetails? CancellationDetails { get; set; }
    }
}
