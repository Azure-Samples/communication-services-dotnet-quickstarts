namespace RecordingStreaming.Models;

public class AudioDataPackets
{
    public string? Kind { get; set; }
    public AudioData? AudioData { get; set; }
    public AudioMetadata? AudioMetadata { get; set; }
}