using Microsoft.VisualBasic;
using NAudio.Wave;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace incoming_call_recording.Helpers
{
    public static class Utils
    {
        /// <summary>
        /// Accept WebSocket Connection, and then Loop in receiving data transmitted from client.
        /// </summary>
        /// <param name="webSocket"></param>
        public static async Task ProcessRequest(WebSocket webSocket)
        {
            Dictionary<string, FileStream> audioDataFiles = new Dictionary<string, FileStream>();
            WebSocketReceiveResult? receiveResult = null;

            try
            {
                string partialData = "";
                MemoryStream stream = new MemoryStream();

                while (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseSent)
                {
                    byte[] receiveBuffer = new byte[2048];
                    var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(60)).Token;
                    receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), cancellationToken);

                    if (receiveResult.MessageType != WebSocketMessageType.Close)
                    {
                        string data = Encoding.UTF8.GetString(receiveBuffer).TrimEnd('\0');

                        try
                        {
                            if (receiveResult.EndOfMessage)
                            {
                                data = partialData + data;
                                partialData = "";

                                if (data != null)
                                {
                                    var jsonData = JsonSerializer.Deserialize<AudioDataPackets>(data,
                                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                                    if (jsonData != null && jsonData.kind == "AudioData")
                                    {   
                                        byte[] bytes = System.Convert.FromBase64String(jsonData?.audioData?.data);
                                        string fileName = jsonData?.audioData?.participantRawID == null? "test.wav": string.Format("..//{0}.wav", jsonData?.audioData?.participantRawID).Replace(":", "");
                                        string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                                        stream.Write(bytes, 0, bytes.Length);
                                        stream.Seek(0, SeekOrigin.Begin);
                                        var wavStream = new RawSourceWaveStream(stream, new WaveFormat(16000, 1));
                                        WaveFileWriter.CreateWaveFile($"{downloadsPath}/{fileName}", wavStream);

                                    }
                                    Console.WriteLine(data);
                                }
                            }
                            else
                            {
                                partialData = partialData + data;
                            }
                        }
                        catch (Exception ex)
                        { Console.WriteLine($"Exception -> {ex}"); }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception -> {ex}");
            }
            finally
            {
                foreach (KeyValuePair<string, FileStream> file in audioDataFiles)
                {
                    file.Value.Close();
                }
                audioDataFiles.Clear();
            }
        }
    }

}
