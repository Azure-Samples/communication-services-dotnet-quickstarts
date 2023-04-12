using Azure.Communication.CallAutomation;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Newtonsoft.Json;

namespace CallAutomation.Scenarios
{
    public class RecordingHandler
    {
        private readonly CallAutomationClient _callAutomationClient;
        private readonly IConfiguration _configuration;

        public RecordingHandler(IConfiguration configuration)
        {
            _configuration = configuration;
            _callAutomationClient = new CallAutomationClient(_configuration["ConnectionString"]);
        }

        public async Task<IResult> HandleRecording(EventGridEvent[] eventGridEvents)
        {
            foreach (var eventGridEvent in eventGridEvents)
            {
                Logger.LogInformation("Event " + JsonConvert.SerializeObject(eventGridEvent));

                // Handle system events
                if (eventGridEvent.TryGetSystemEventData(out object eventData))
                {
                    // Handle the subscription validation event.
                    if (eventData is SubscriptionValidationEventData subscriptionValidationEventData)
                    {
                        var responseData = new SubscriptionValidationResponse()
                        {
                            ValidationResponse = subscriptionValidationEventData.ValidationCode
                        };
                        return Results.Ok(responseData);
                    }
                }
                else if (eventData is AcsRecordingFileStatusUpdatedEventData acsRecordingFileStatusUpdatedEventData)
                {
                    try
                    {
                        var recordingDownloadUri = new Uri(acsRecordingFileStatusUpdatedEventData.RecordingStorageInfo.RecordingChunks[0].ContentLocation);
                        var downloadRespose = await _callAutomationClient.GetCallRecording().DownloadStreamingAsync(recordingDownloadUri);
                        var containerName = _configuration["BlobContainerName"];

                        string filePath = $".\\recording\\{acsRecordingFileStatusUpdatedEventData.RecordingStorageInfo.RecordingChunks[0].DocumentId}.mp4";
                        using Stream readFromStream = downloadRespose.Value;
                        using Stream writeToStream = System.IO.File.Open(filePath, FileMode.Create);
                        await readFromStream.CopyToAsync(writeToStream);
                        await writeToStream.FlushAsync();

                        Logger.LogInformation($"Starting to upload .mp4 to BlobStorage into container -- > {containerName}");

                        var blobStorageHelperInfo = await BlobStorageHelper.UploadFileAsync(_configuration["BlobStorageConnectionString"], containerName, filePath, filePath);
                        if (blobStorageHelperInfo.Status)
                        {
                            Logger.LogInformation(blobStorageHelperInfo.Message);
                            Logger.LogInformation($"Deleting temporary .mp4 file being created");
                            System.IO.File.Delete(filePath);
                        }
                        else
                        {
                            Logger.LogError($".mp4 file was not uploaded,{blobStorageHelperInfo.Message}");
                        }
                    }
                    catch(Exception ex)
                    {
                        Logger.LogError($"Failed to upload the file. error message,{ex.Message}");
                    }
                    
                }
            }
            return Results.Ok();
        }
    }
}
