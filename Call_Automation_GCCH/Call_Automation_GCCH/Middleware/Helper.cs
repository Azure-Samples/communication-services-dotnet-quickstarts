using Azure.Communication.CallAutomation;
using Microsoft.Extensions.Logging;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Call_Automation_GCCH.Middleware
{
    public static class Helper
    {
        public static async Task ProcessRequest(WebSocket webSocket, ILogger logger = null)
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
                            logger?.LogInformation("***************************************************************************************");
                            logger?.LogInformation("MEDIA SUBSCRIPTION ID-->" + audioMetadata.MediaSubscriptionId);
                            logger?.LogInformation("ENCODING-->" + audioMetadata.Encoding);
                            logger?.LogInformation("SAMPLE RATE-->" + audioMetadata.SampleRate);
                            logger?.LogInformation("CHANNELS-->" + audioMetadata.Channels);
                            logger?.LogInformation("LENGTH-->" + audioMetadata.Length);
                            logger?.LogInformation("***************************************************************************************");
                        }
                        if (response is AudioData audioData)
                        {
                            logger?.LogInformation("***************************************************************************************");
                            logger?.LogInformation("DATA-->" + JsonSerializer.Serialize(audioData.Data));
                            logger?.LogInformation("TIMESTAMP-->" + audioData.Timestamp);
                            logger?.LogInformation("IS SILENT-->" + audioData.IsSilent);
                            if (audioData.Participant != null && audioData.Participant.RawId != null)
                            {
                                logger?.LogInformation("Participant Id-->" + audioData.Participant.RawId);
                            }

                            logger?.LogInformation("***************************************************************************************");
                        }

                        if (response is TranscriptionMetadata transcriptionMetadata)
                        {
                            logger?.LogInformation("***************************************************************************************");
                            logger?.LogInformation("TRANSCRIPTION SUBSCRIPTION ID-->" + transcriptionMetadata.TranscriptionSubscriptionId);
                            logger?.LogInformation("LOCALE-->" + transcriptionMetadata.Locale);
                            logger?.LogInformation("CALL CONNECTION ID--?" + transcriptionMetadata.CallConnectionId);
                            logger?.LogInformation("CORRELATION ID-->" + transcriptionMetadata.CorrelationId);
                            logger?.LogInformation("***************************************************************************************");
                        }
                        if (response is TranscriptionData transcriptionData)
                        {
                            logger?.LogInformation("***************************************************************************************");
                            logger?.LogInformation("TEXT-->" + transcriptionData.Text);
                            logger?.LogInformation("FORMAT-->" + transcriptionData.Format);
                            logger?.LogInformation("OFFSET-->" + transcriptionData.Offset);
                            logger?.LogInformation("DURATION-->" + transcriptionData.Duration);
                            logger?.LogInformation("PARTICIPANT-->" + transcriptionData.Participant.RawId);
                            logger?.LogInformation("CONFIDENCE-->" + transcriptionData.Confidence);
                            logger?.LogInformation("RESULT STATUS-->" + transcriptionData.ResultStatus);
                            foreach (var word in transcriptionData.Words)
                            {
                                logger?.LogInformation("WORDS TEXT-->" + word.Text);
                                logger?.LogInformation("WORDS OFFSET-->" + word.Offset);
                                logger?.LogInformation("WORDS DURATION-->" + word.Duration);
                            }
                            logger?.LogInformation("***************************************************************************************");
                        }
                    }

                    await webSocket.SendAsync(
                        new ArraySegment<byte>(buffer, 0, receiveResult.Count),
                        receiveResult.MessageType,
                        receiveResult.EndOfMessage,
                        CancellationToken.None);

                    receiveResult = await webSocket.ReceiveAsync(
                     new ArraySegment<byte>(buffer), CancellationToken.None);
                }

                await webSocket.CloseAsync(receiveResult.CloseStatus.Value, receiveResult.CloseStatusDescription, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger?.LogError($"Exception -> {ex}");
            }
            finally
            {
            }
        }
    }
}
