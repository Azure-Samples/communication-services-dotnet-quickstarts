using System;
using System.Threading.Tasks;
using CallAutomation.Controllers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.IO;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using Azure.Messaging.EventGrid.SystemEvents;
using Azure.Communication.CallAutomation;

namespace CallAutomation
{
    public class ProcessRecordingFiles
    {
        private ILogger<CallRecordingController> Logger { get; }
        private CallAutomationClient callAutomationClient;
        private string recFileFormat;
        private IConfiguration Config { get; }

        public ProcessRecordingFiles(ILogger<CallRecordingController> logger, IConfiguration configuration, CallAutomationClient callAutomationClient)
        {
            Logger = logger;
            Config = configuration;
            this.callAutomationClient = callAutomationClient;
            recFileFormat = string.Empty;
        }

        public async Task<bool> Process(string eventGridEventType, BinaryData eventGridEventData, string eventGridEventSubject)
        {
            Logger.LogInformation($"Event type is -- > {eventGridEventType}");
            Logger.LogInformation($"{Constants.RecFileStatusUpdatedEvent} response  -- > {eventGridEventData}");
            Logger.LogInformation($"{Constants.RecFileStatusUpdatedEvent} subject  -- > {eventGridEventSubject}");

            string recordingId = string.Empty;

            if (!string.IsNullOrWhiteSpace(eventGridEventSubject))
            {
                var recIdMatch = Regex.Match(eventGridEventSubject, @"(recordingId/[^/]*)");

                if (recIdMatch.Success && recIdMatch.Groups.Count > 1)
                {
                    recordingId = recIdMatch.Groups[1].Value.Split('/')[1]; //Accessing 1st index won't fail because of regex match
                    Logger.LogInformation("Extracted recording Id -- >" + recordingId);
                }
            }

            var eventData = eventGridEventData.ToObjectFromJson<AcsRecordingFileStatusUpdatedEventData>();

            Logger.LogInformation("Start processing metadata -- >");

            await DownloadAndUpload(eventData.RecordingStorageInfo.RecordingChunks[0].MetadataLocation,
                eventData.RecordingStorageInfo.RecordingChunks[0].DocumentId,
                RecordingFileFormat.Json,
                RecordingFileDownloadType.Metadata);

            Logger.LogInformation("Start processing recorded media -- >");

            await DownloadAndUpload(eventData.RecordingStorageInfo.RecordingChunks[0].ContentLocation,
                eventData.RecordingStorageInfo.RecordingChunks[0].DocumentId,
                string.IsNullOrWhiteSpace(recFileFormat) ? RecordingFileFormat.Mp4 : recFileFormat,
                RecordingFileDownloadType.Recording,
                recordingId);

            return true;
        }

        private async Task<bool> DownloadAndUpload(string downloadLocation, string documentId, string fileFormat, string downloadType, string recordingId = null)
        {
            var recordingDownloadUri = new Uri(downloadLocation);
            var response = await callAutomationClient.GetCallRecording().DownloadStreamingAsync(recordingDownloadUri);

            Logger.LogInformation($"Download {downloadType} response  -- >" + response.GetRawResponse());
            Logger.LogInformation($"Save downloaded {downloadType} -- >");

            string filePath = string.Format("{0}.{1}", documentId, fileFormat);
            using (Stream streamToReadFrom = response.Value)
            {
                using (Stream streamToWriteTo = File.Open(filePath, FileMode.Create))
                {
                    await streamToReadFrom.CopyToAsync(streamToWriteTo);
                    await streamToWriteTo.FlushAsync();
                }
            }

            if (string.Equals(downloadType, RecordingFileDownloadType.Metadata, StringComparison.InvariantCultureIgnoreCase) && System.IO.File.Exists(filePath))
            {
                Metadata deserializedFilePath = JsonConvert.DeserializeObject<Metadata>(System.IO.File.ReadAllText(filePath));
                recFileFormat = deserializedFilePath.recordingInfo.format;

                Logger.LogInformation($"Recording File Format is -- > {recFileFormat}");
            }

            Logger.LogInformation($"Starting to upload {downloadType} to BlobStorage into container -- > {Config["RecordingContainerName"]}");

            var blobStorageHelperInfo = await BlobStorageHelper.UploadFileAsync(
                Config["RecordingBlobStorageConnectionString"],
                Config["RecordingContainerName"],
                filePath,
                filePath);

            if (blobStorageHelperInfo.Status)
            {
                if (!string.IsNullOrEmpty(recordingId))
                {
                    Logger.LogInformation("Download uri for recording file -- > {0}", blobStorageHelperInfo.Uri);
                    LocalRecordingInfoCache.Instance.UpdateByRecordingId(recordingId, blobStorageHelperInfo.Uri);
                }

                Logger.LogInformation(blobStorageHelperInfo.Message);
                Logger.LogInformation($"Deleting temporary {downloadType} file being created");

                File.Delete(filePath);
            }
            else
            {
                Logger.LogError($"{downloadType} file was not uploaded,{blobStorageHelperInfo.Message}");
            }

            return true;
        }
    }
}
