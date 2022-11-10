using System;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;

namespace RecognizerBot.Interfaces
{
    public interface IStorageService
    {
        public Task<Uri> StreamTo(SpeechSynthesisResult result, string? filename = null);

        public Task<Uri> UploadTo(string sourceFilePath, string? fileName = null);

        public Task<(bool, Uri)> Exists(string fileName);
    }
}
