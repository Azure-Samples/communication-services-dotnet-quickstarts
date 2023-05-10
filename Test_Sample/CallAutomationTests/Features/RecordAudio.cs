using Azure.Communication.CallAutomation;

namespace CallAutomation.Scenarios
{
    public class RecordAudio
    {
        public static void StartCallRecording(string callConnectionId,
        CallAutomationClient client)
        {
            try
            {
                // Start call recording
                var serverCallId = client.GetCallConnection(callConnectionId)
                    .GetCallConnectionProperties().Value.ServerCallId;
                var startRecordingOptions = new StartRecordingOptions(new ServerCallLocator(serverCallId));

                _ = Task.Run(async () => await client.GetCallRecording().StartRecordingAsync(startRecordingOptions));
                Logger.LogInformation("Successfully started recording");
            }
            catch(Exception ex)
            {
                Logger.LogError("Failed to start recording.  error message: " + ex.Message);
            }
        }
    }
}
