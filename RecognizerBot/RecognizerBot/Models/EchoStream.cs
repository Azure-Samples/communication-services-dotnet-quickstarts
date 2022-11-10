using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;

namespace IncomingCallRouting.Models
{
    public class EchoStream : MemoryStream
    {
        private readonly ManualResetEvent _dataReady = new ManualResetEvent(false);
        private readonly ConcurrentQueue<byte[]> _buffers = new ConcurrentQueue<byte[]>();

        public bool DataAvailable => !_buffers.IsEmpty;

        public override void Write(byte[] buffer, int offset, int count)
        {
            _buffers.Enqueue(buffer.Take(count).ToArray()); // add new data to buffer
            _dataReady.Set(); // allow waiting reader to proceed
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            _dataReady.WaitOne(); // block until there's something new to read

            if (!_buffers.TryDequeue(out var lBuffer)) // try to read
            {
                _dataReady.Reset();
                return -1;
            }

            if (!DataAvailable)
                _dataReady.Reset();

            Array.Copy(lBuffer, buffer, lBuffer.Length);
            return lBuffer.Length;
        }
    }
}
