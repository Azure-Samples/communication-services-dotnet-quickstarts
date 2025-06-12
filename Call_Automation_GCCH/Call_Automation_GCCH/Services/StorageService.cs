using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Call_Automation_GCCH.Models;

namespace Call_Automation_GCCH.Services
{
    public class StorageService : IStorageService
    {
        private readonly ILogger<StorageService> _logger;
        private readonly string _connectionString;
        private readonly string _containerName;

        public StorageService(ILogger<StorageService> logger, IOptions<ConfigurationRequest> configOptions)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            var config = configOptions?.Value ?? throw new ArgumentNullException(nameof(configOptions));
            _connectionString = config.StorageConnectionString ?? throw new ArgumentNullException(nameof(config.StorageConnectionString));
            _containerName = config.ContainerName; // Default fallback
        }

        public async Task<string> UploadRecordingAsync(string fileName, Stream fileStream, string mimeType)
        {
            try
            {
                var blobServiceClient = new BlobServiceClient(_connectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
                
                // Create container if it doesn't exist
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);
                
                var blobClient = containerClient.GetBlobClient(fileName);
                
                // Reset stream position to beginning
                fileStream.Position = 0;
                
                var blobHttpHeaders = new BlobHttpHeaders
                {
                    ContentType = mimeType
                };
                
                await blobClient.UploadAsync(fileStream, new BlobUploadOptions
                {
                    HttpHeaders = blobHttpHeaders,
                    Conditions = null
                });
                
                _logger.LogInformation($"Recording uploaded successfully to blob storage. FileName: {fileName}, Url: {blobClient.Uri}");
                
                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uploading recording to blob storage: {ex.Message}. FileName: {fileName}");
                throw;
            }
        }

        public string UploadRecording(string fileName, Stream fileStream, string mimeType)
        {
            try
            {
                var blobServiceClient = new BlobServiceClient(_connectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
                
                // Create container if it doesn't exist
                containerClient.CreateIfNotExists(PublicAccessType.None);
                
                var blobClient = containerClient.GetBlobClient(fileName);
                
                // Reset stream position to beginning
                fileStream.Position = 0;
                
                var blobHttpHeaders = new BlobHttpHeaders
                {
                    ContentType = mimeType
                };
                
                blobClient.Upload(fileStream, new BlobUploadOptions
                {
                    HttpHeaders = blobHttpHeaders,
                    Conditions = null
                });
                
                _logger.LogInformation($"Recording uploaded successfully to blob storage. FileName: {fileName}, Url: {blobClient.Uri}");
                
                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uploading recording to blob storage: {ex.Message}. FileName: {fileName}");
                throw;
            }
        }
    }
} 