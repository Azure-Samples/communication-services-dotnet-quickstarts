using Azure.Communication.CallAutomation;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Call_Automation_GCCH
{
    public static class Helper
    {
        public static async Task ProcessRequest(WebSocket webSocket)
        {
            try
            {
                var buffer = new byte[1024 * 4];
                var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(60)).Token;
                WebSocketReceiveResult receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                while (!receiveResult.CloseStatus.HasValue)
                {
                    string msg = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);

                    var response = StreamingData.Parse(msg);

                    if (response != null)
                    {
                        if (response is AudioMetadata audioMetadata)
                        {
                            LogCollector.Log("***************************************************************************************");
                            LogCollector.Log("MEDIA SUBSCRIPTION ID-->" + audioMetadata.MediaSubscriptionId);
                            LogCollector.Log("ENCODING-->" + audioMetadata.Encoding);
                            LogCollector.Log("SAMPLE RATE-->" + audioMetadata.SampleRate);
                            LogCollector.Log("CHANNELS-->" + audioMetadata.Channels);
                            LogCollector.Log("LENGTH-->" + audioMetadata.Length);
                            LogCollector.Log("***************************************************************************************");
                        }
                        if (response is AudioData audioData)
                        {
                            LogCollector.Log("***************************************************************************************");
                            LogCollector.Log("DATA-->" + JsonSerializer.Serialize(audioData.Data));
                            LogCollector.Log("TIMESTAMP-->" + audioData.Timestamp);
                            LogCollector.Log("IS SILENT-->" + audioData.IsSilent);
                            if (audioData.Participant != null && audioData.Participant.RawId != null)
                            {
                                LogCollector.Log("Participant Id-->" + audioData.Participant.RawId);
                            }

                            LogCollector.Log("***************************************************************************************");
                            // Send audio bytes back to client if bidirectional streaming is enabled
                            if (audioData.Data is byte[] audioBytes)
                            {
                                LogCollector.Log("Bidirectional Logs are given below:");
                                await webSocket.SendAsync(
                                    new ArraySegment<byte>(audioBytes, 0, audioBytes.Length),
                                    WebSocketMessageType.Binary,
                                    endOfMessage: true,
                                    cancellationToken: CancellationToken.None);
                            }
                        }

                        if (response is TranscriptionMetadata transcriptionMetadata)
                        {
                            LogCollector.Log("***************************************************************************************");
                            LogCollector.Log("TRANSCRIPTION SUBSCRIPTION ID-->" + transcriptionMetadata.TranscriptionSubscriptionId);
                            LogCollector.Log("LOCALE-->" + transcriptionMetadata.Locale);
                            LogCollector.Log("CALL CONNECTION ID--?" + transcriptionMetadata.CallConnectionId);
                            LogCollector.Log("CORRELATION ID-->" + transcriptionMetadata.CorrelationId);
                            LogCollector.Log("***************************************************************************************");
                        }
                        if (response is TranscriptionData transcriptionData)
                        {
                            LogCollector.Log("***************************************************************************************");
                            LogCollector.Log("TEXT-->" + transcriptionData.Text);
                            LogCollector.Log("FORMAT-->" + transcriptionData.Format);
                            LogCollector.Log("OFFSET-->" + transcriptionData.Offset);
                            LogCollector.Log("DURATION-->" + transcriptionData.Duration);
                            LogCollector.Log("PARTICIPANT-->" + transcriptionData.Participant.RawId);
                            LogCollector.Log("CONFIDENCE-->" + transcriptionData.Confidence);
                            LogCollector.Log("RESULT STATUS-->" + transcriptionData.ResultStatus);
                            foreach (var word in transcriptionData.Words)
                            {
                                LogCollector.Log("WORDS TEXT-->" + word.Text);
                                LogCollector.Log("WORDS OFFSET-->" + word.Offset);
                                LogCollector.Log("WORDS DURATION-->" + word.Duration);
                            }
                            LogCollector.Log("***************************************************************************************");
                        }
                    }
                    if (response == null || (response != null && response is not AudioData))
                    {
                        await webSocket.SendAsync(
                            new ArraySegment<byte>(buffer, 0, receiveResult.Count),
                            receiveResult.MessageType,
                            receiveResult.EndOfMessage,
                            CancellationToken.None);
                    }

                    receiveResult = await webSocket.ReceiveAsync(
                     new ArraySegment<byte>(buffer), CancellationToken.None);
                }

                await webSocket.CloseAsync(receiveResult.CloseStatus.Value, receiveResult.CloseStatusDescription, CancellationToken.None);
            }
            catch (Exception ex)
            {
                LogCollector.Log($"Exception -> {ex}");
            }
            finally
            {
            }
        }
    }
}
