// Custom class to write PCM data to a memory stream
using Microsoft.CognitiveServices.Speech.Audio;

public class PcmAudioStreamWriter : PushAudioOutputStreamCallback
{
    private readonly MemoryStream _memoryStream;

    public PcmAudioStreamWriter(MemoryStream memoryStream)
    {
        _memoryStream = memoryStream;
    }

    public override uint Write(byte[] dataBuffer)
    {
        _memoryStream.Write(dataBuffer, 0, dataBuffer.Length);
        return (uint)dataBuffer.Length; // Return the number of bytes written
    }

    public override void Close()
    {
        _memoryStream.Close();
        base.Close();
    }
}