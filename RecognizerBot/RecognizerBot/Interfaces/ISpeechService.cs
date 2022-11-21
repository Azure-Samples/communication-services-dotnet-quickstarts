using System.Threading.Tasks;
using RecognizerBot.Models;
using RecognizerBot.Nuance.Models;

namespace RecognizerBot.Interfaces
{
    public interface ISpeechService
    {
        public Task PlayIntent(string intent);

        public Task<SpeechAudioResult> TextToSpeech(string text);

        public Task<Intent> ExtractIntent(InterpretResponse response);

    }
}
