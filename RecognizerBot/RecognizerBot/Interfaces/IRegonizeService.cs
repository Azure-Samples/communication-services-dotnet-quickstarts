using System.Threading.Tasks;

namespace RecognizerBot.Interfaces
{
    public interface IRegonizeService
    {

        public Task RecognizeIntentFromFile(string filePath);

        public Task RecognizeIntentFromStream(bool playback = false);
    }
}
