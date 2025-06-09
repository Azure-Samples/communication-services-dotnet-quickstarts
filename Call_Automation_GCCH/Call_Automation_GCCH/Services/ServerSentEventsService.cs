using System.Threading.Channels;

namespace Call_Automation_GCCH.Services
{
    public interface IServerSentEventsService
    {
        ChannelReader<string> Reader { get; }
        void Publish(string payload);
    }

    public class ServerSentEventsService : IServerSentEventsService
    {
        private readonly Channel<string> _channel;

        public ServerSentEventsService()
        {
            _channel = Channel.CreateUnbounded<string>();
        }

        public ChannelReader<string> Reader => _channel.Reader;

        public void Publish(string payload)
        {
            _channel.Writer.TryWrite($"data: {payload}\n\n");
        }
    }
}
