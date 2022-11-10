using System.Threading.Tasks;

namespace IncomingCallRouting.Interfaces
{
    public interface IRegonizeService
    {

        public Task RecognizeIntentFromFile(string filePath);

        public Task RecognizeIntentFromStream(bool playback = false);
    }
}
