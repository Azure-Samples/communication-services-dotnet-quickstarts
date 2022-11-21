using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Configuration;
using RecognizerBot.Interfaces;
using RecognizerBot.Utils;

namespace RecognizerBot.Services
{
    public class BlobStorageService : IStorageService
    {
        private readonly BlobContainerClient _blobContainerClient;

        public BlobStorageService(IConfiguration configuration)
        {
            _blobContainerClient = new BlobContainerClient(configuration["StorageConnectionString"], "voiceapps");
            _blobContainerClient.CreateIfNotExistsAsync();
        }

        public async Task<Uri> StreamTo(SpeechSynthesisResult result, string? fileName = null)
        {
            var blobClient = _blobContainerClient.GetBlobClient(fileName ?? new Guid().ToString());
            
            await using var outputBlob = await blobClient.OpenWriteAsync(true);

            using var audioDataStream = AudioDataStream.FromResult(result);
            var buffer = new byte[16000];
            uint totalSize = 0;
            uint filledSize;
            
            while ((filledSize = audioDataStream.ReadData(buffer)) > 0)
            {
                totalSize += filledSize;
                await outputBlob.WriteAsync(buffer);
            }

            Logger.LogMessage(Logger.MessageType.INFORMATION, $"{totalSize} bytes of audio data received for {fileName} at {blobClient.Uri}");
            
            return blobClient.Uri;
        }

        public async Task<Uri> UploadTo(string sourceFilePath, string? fileName = null)
        {
            var blobClient = _blobContainerClient.GetBlobClient(fileName ?? sourceFilePath);
            await using var outputBlob = await blobClient.OpenWriteAsync(true);
            var bytes = await File.ReadAllBytesAsync(sourceFilePath);
            await outputBlob.WriteAsync(bytes);

            Logger.LogMessage(Logger.MessageType.INFORMATION, $"{fileName} uploaded to {blobClient.Uri}.  Wrote {bytes.Length} bytes.");
            return blobClient.Uri;
        }

        public async Task<(bool, Uri)> Exists(string fileName)
        {
            var blobClient = _blobContainerClient.GetBlobClient(fileName);
            return (await blobClient.ExistsAsync(), blobClient.Uri);
        }
    }
}
