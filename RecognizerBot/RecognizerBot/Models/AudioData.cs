namespace RecognizerBot.Models;

public class AudioData
{
    public byte[]? Data { get; set; } // Base64 Encoded audio buffer data
    public string? Timestamp { get; set; } // In ISO 8601 format (yyyy-mm-ddThh:mm:ssZ)
    public string? ParticipantRawId { get; set; }
    public bool Silent { get; set; } // Indicates if the received audio buffer contains only silence.
}