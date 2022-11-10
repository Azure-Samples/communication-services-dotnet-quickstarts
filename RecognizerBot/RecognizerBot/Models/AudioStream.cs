using System.Threading;
using Microsoft.CognitiveServices.Speech.Audio;

namespace IncomingCallRouting.Models
{
    public class AudioStream : PullAudioInputStreamCallback
    {
        private readonly EchoStream _dataStream = new();
        private ManualResetEvent? _waitForEmptyDataStream = null;

        public override int Read(byte[] dataBuffer, uint size)
        {
            if (_waitForEmptyDataStream != null && !_dataStream.DataAvailable)
            {
                _waitForEmptyDataStream.Set();
                return 0;
            }

            return _dataStream.Read(dataBuffer, 0, dataBuffer.Length);
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            _dataStream.Write(buffer, offset, count);
        }

        public override void Close()
        {
            if (_dataStream.DataAvailable)
            {
                _waitForEmptyDataStream = new ManualResetEvent(false);
                _waitForEmptyDataStream.WaitOne();
            }

            _waitForEmptyDataStream?.Close();
            _dataStream?.Dispose();
            base.Close();
        }
    }
}
