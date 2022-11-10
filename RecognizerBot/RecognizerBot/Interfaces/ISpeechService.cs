using System;
using System.Threading.Tasks;
using IncomingCallRouting.Models;
using IncomingCallRouting.Nuance.Models;
using Microsoft.CognitiveServices.Speech;

namespace IncomingCallRouting.Interfaces
{
    public interface ISpeechService
    {
        public Task PlayIntent(string intent);

        public Task<SpeechAudioResult> TextToSpeech(string text);

        public Task<Intent> ExtractIntent(InterpretResponse response);

    }
}
