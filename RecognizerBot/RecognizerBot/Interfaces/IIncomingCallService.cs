using System.Threading.Tasks;

namespace IncomingCallRouting.Interfaces
{
    public interface IIncomingCallService
    {
        public Task HandleCall(string incomingCallContext);
    }
}
