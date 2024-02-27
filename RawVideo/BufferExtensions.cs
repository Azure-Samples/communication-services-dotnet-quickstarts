using System;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace RawVideo
{
    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }

    [ComImport]
    [Guid("905A0FEF-BC53-11DF-8C49-001E4FC686DA")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    unsafe interface IBufferByteAccess
    {
        void Buffer(out byte* buffer);
    }

    internal static class BufferExtensions
    {
        public static unsafe byte* GetArrayBuffer(IMemoryBuffer memoryBuffer)
        {
            IMemoryBufferReference memoryBufferReference = memoryBuffer.CreateReference();
            var memoryBufferByteAccess = memoryBufferReference as IMemoryBufferByteAccess;

            memoryBufferByteAccess.GetBuffer(out byte* arrayBuffer, out uint arrayBufferCapacity);

            GC.AddMemoryPressure(arrayBufferCapacity);

            return arrayBuffer;
        }

        public static unsafe byte* GetArrayBuffer(IBuffer buffer)
        {
            var bufferByteAccess = buffer as IBufferByteAccess;

            bufferByteAccess.Buffer(out byte* arrayBuffer);
            uint arrayBufferCapacity = buffer.Capacity;

            GC.AddMemoryPressure(arrayBufferCapacity);

            return arrayBuffer;
        }
    }
}
