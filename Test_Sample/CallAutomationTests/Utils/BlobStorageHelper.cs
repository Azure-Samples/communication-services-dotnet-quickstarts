using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace CallAutomation.Scenarios
{
    public class BlobStorageHelperInfo
    {
        public string Message { set; get; }
        public bool Status { set; get; }
    }

    public static class BlobStorageHelper
    {
        /// <summary>
        /// Method to check if Blob Storage Container exists or not
        /// </summary>
        /// <param name="connectionString">Connection String details for Azure Blob Storage</param>
        /// <param name="containerName">Container Name to upload files</param>
        /// <returns>Boolean</returns>
        public static async Task<bool> IsContainerAvailableAsync(string connectionString, string containerName)
        {
            try
            {
                BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
                BlobContainerClient blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
                return await blobContainerClient.ExistsAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Exception checking the container availability. Exception: {ex.Message}");
            }
        }
        /// <summary>
        /// Method to check if Blob exists or not
        /// </summary>
        /// <param name="connectionString">Connection String details for Azure Blob Storage</param>
        /// <param name="containerName">Container Name to upload files</param>
        /// <param name="blobName">File name</param>
        /// <returns>Boolean</returns>
        public static async Task<bool> IsBlobAvailableAsync(string connectionString, string containerName, string blobName)
        {
            try
            {
                if (!await IsContainerAvailableAsync(connectionString, containerName)) return false;
                BlobClient blobClient = new BlobClient(connectionString, containerName, blobName);
                return await blobClient.ExistsAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Exception checking the blob availability. Exception: {ex.Message}");
            }
        }
        /// <summary>
        /// Method to upload a file to Blob storage
        /// </summary>
        /// <param name="connectionString">Connection String details for Azure Blob Storage</param>
        /// <param name="containerName">Container Name to upload files</param>
        /// <param name="blobName">File name</param>
        /// <param name="filePath">File path of the file to upload</param>
        /// <returns>BlobStorageHelperInfo</returns>
        public static async Task<BlobStorageHelperInfo> UploadFileAsync(string connectionString, string containerName, string blobName, string filePath)
        {
            BlobStorageHelperInfo blobStorageHelperInfo = new BlobStorageHelperInfo();
            try
            {
                //checking if container is available
                if (!await IsContainerAvailableAsync(connectionString, containerName))
                {
                    blobStorageHelperInfo.Message = $"Container {containerName} is not available";
                    blobStorageHelperInfo.Status = false;
                    return blobStorageHelperInfo;
                }

                //checking if blob is already available
                if (await IsBlobAvailableAsync(connectionString, containerName, blobName))
                {
                    blobStorageHelperInfo.Message = $"Blob \"{blobName}\" already exists";
                    blobStorageHelperInfo.Status = false;
                    return blobStorageHelperInfo;
                }

                //Upload blob
                BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
                BlobContainerClient blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
                BlobClient blobClient = blobContainerClient.GetBlobClient(blobName);

                FileStream uploadFileStream = File.OpenRead(filePath);
                BlobContentInfo status = await blobClient.UploadAsync(uploadFileStream, true);
                uploadFileStream.Close();
                blobStorageHelperInfo.Message = $"File uploaded successfully. Uri : {blobClient.Uri}";
                blobStorageHelperInfo.Status = true;
                return blobStorageHelperInfo;
            }
            catch (Exception ex)
            {
                throw new Exception($"The file upload was not successful. Exception: {ex.Message}");
            }
        }
    }
}
