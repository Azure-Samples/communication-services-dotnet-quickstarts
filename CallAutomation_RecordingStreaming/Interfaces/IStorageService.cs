using Microsoft.CognitiveServices.Speech;
using RecordingStreaming.Models;

namespace RecordingStreaming.Interfaces
{
    public interface IStorageService
    {
        Task<Uri> StreamTo(Stream stream, string? fileName = null);

        public Task<Uri> UploadTo(string sourceFilePath, string? fileName = null);

        public Task<(bool, Uri)> Exists(string fileName);
    }
}
