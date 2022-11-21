using System.Threading.Tasks;

namespace RecognizerBot.Interfaces
{
    public interface IIncomingCallService
    {
        public Task HandleCall(string incomingCallContext);
    }
}
