using System.Threading.Tasks;

namespace Call_Automation_GCCH.Services
{
    public interface IStorageService
    {
        /// <summary>
        /// Uploads a recording file to Azure Storage Account asynchronously
        /// </summary>
        /// <param name="fileName">The name of the file to upload</param>
        /// <param name="fileStream">The file stream to upload</param>
        /// <param name="mimeType">The MIME type of the file</param>
        /// <returns>The URL of the uploaded file</returns>
        Task<string> UploadRecordingAsync(string fileName, Stream fileStream, string mimeType);

        /// <summary>
        /// Uploads a recording file to Azure Storage Account synchronously
        /// </summary>
        /// <param name="fileName">The name of the file to upload</param>
        /// <param name="fileStream">The file stream to upload</param>
        /// <param name="mimeType">The MIME type of the file</param>
        /// <returns>The URL of the uploaded file</returns>
        string UploadRecording(string fileName, Stream fileStream, string mimeType);
    }
} 