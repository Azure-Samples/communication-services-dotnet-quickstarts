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
                            Console.WriteLine("***************************************************************************************");
                            Console.WriteLine("MEDIA SUBSCRIPTION ID-->" + audioMetadata.MediaSubscriptionId);
                            Console.WriteLine("ENCODING-->" + audioMetadata.Encoding);
                            Console.WriteLine("SAMPLE RATE-->" + audioMetadata.SampleRate);
                            Console.WriteLine("CHANNELS-->" + audioMetadata.Channels);
                            //Console.WriteLine("LENGTH-->" + audioMetadata.Length);
                            Console.WriteLine("***************************************************************************************");
                        }
                        if (response is AudioData audioData)
                        {
                            Console.WriteLine("***************************************************************************************");
                            Console.WriteLine("DATA-->" + JsonSerializer.Serialize(audioData.Data));
                            Console.WriteLine("TIMESTAMP-->" + audioData.Timestamp);
                            Console.WriteLine("IS SILENT-->" + audioData.IsSilent);
                            if (audioData.Participant != null && audioData.Participant.RawId != null)
                            {
                                Console.WriteLine("Participant Id-->" + audioData.Participant.RawId);
                            }

                            Console.WriteLine("***************************************************************************************");
                        }

                        if (response is TranscriptionMetadata transcriptionMetadata)
                        {
                            Console.WriteLine("***************************************************************************************");
                            Console.WriteLine("TRANSCRIPTION SUBSCRIPTION ID-->" + transcriptionMetadata.TranscriptionSubscriptionId);
                            Console.WriteLine("LOCALE-->" + transcriptionMetadata.Locale);
                            Console.WriteLine("CALL CONNECTION ID--?" + transcriptionMetadata.CallConnectionId);
                            Console.WriteLine("CORRELATION ID-->" + transcriptionMetadata.CorrelationId);
                            Console.WriteLine("***************************************************************************************");
                        }
                        if (response is TranscriptionData transcriptionData)
                        {
                            Console.WriteLine("***************************************************************************************");
                            Console.WriteLine("TEXT-->" + transcriptionData.Text);
                            Console.WriteLine("FORMAT-->" + transcriptionData.Format);
                            Console.WriteLine("OFFSET-->" + transcriptionData.Offset);
                            Console.WriteLine("DURATION-->" + transcriptionData.Duration);
                            Console.WriteLine("PARTICIPANT-->" + transcriptionData.Participant.RawId);
                            Console.WriteLine("CONFIDENCE-->" + transcriptionData.Confidence);
                            Console.WriteLine("RESULT STATUS-->" + transcriptionData.ResultState);
                            foreach (var word in transcriptionData.Words)
                            {
                                Console.WriteLine("WORDS TEXT-->" + word.Text);
                                Console.WriteLine("WORDS OFFSET-->" + word.Offset);
                                Console.WriteLine("WORDS DURATION-->" + word.Duration);
                            }
                            Console.WriteLine("***************************************************************************************");
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
                Console.WriteLine($"Exception -> {ex}");
            }
            finally
            {
            }
        }
    }
}
