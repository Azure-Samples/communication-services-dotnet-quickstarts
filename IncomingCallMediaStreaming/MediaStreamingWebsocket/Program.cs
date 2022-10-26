using System.Net.WebSockets;
using System.Net;
using System.Text;
using System.Text.Json;

namespace MediaStreamingWebsocket // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            HttpListener httpListener = new HttpListener();
            httpListener.Prefixes.Add("http://localhost:8080/");
            httpListener.Start();

            while (true)
            {
                HttpListenerContext httpListenerContext = await httpListener.GetContextAsync();

                if (httpListenerContext.Request.IsWebSocketRequest)
                {
                    WebSocketContext websocketContext;
                    try
                    {
                        websocketContext = await httpListenerContext.AcceptWebSocketAsync(subProtocol: null);
                        string ipAddress = httpListenerContext.Request.RemoteEndPoint.Address.ToString();
                    }
                    catch (Exception ex)
                    {
                        httpListenerContext.Response.StatusCode = 500;
                        httpListenerContext.Response.Close();
                        Console.WriteLine($"Exception -> {ex}");
                        return;
                    }

                    WebSocket webSocket = websocketContext.WebSocket;
                    try
                    {
                        string audioDataText = "";
                        string partialData = "";
                        bool isPartialData = false;
                        while (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseSent)
                        {
                            byte[] receiveBuffer = new byte[2048];
                            var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(60)).Token;
                            WebSocketReceiveResult receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), cancellationToken);

                            if (receiveResult.MessageType != WebSocketMessageType.Close)
                            {
                                string data = Encoding.UTF8.GetString(receiveBuffer).TrimEnd('\0');

                                try
                                {
                                    if (receiveResult.EndOfMessage == false)
                                    {
                                        partialData = partialData + data;
                                        isPartialData = true;
                                    }
                                    else
                                    {
                                        if (isPartialData)
                                        {
                                            data = partialData + data;
                                            partialData = "";
                                            isPartialData = false;
                                        }

                                        if (data != null)
                                        {
                                            AudioDataPackets json = JsonSerializer.Deserialize<AudioDataPackets>(data);

                                            if (json != null && json.kind == "AudioData")
                                            {
                                                AudioData audioData = json.audioData;
                                                audioDataText = audioDataText + audioData?.data;
                                            }
                                            else
                                            {
                                                Console.WriteLine(data);
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                { Console.WriteLine($"Exception -> {ex}"); }
                            }
                            else
                            {
                                string file = "..//audiodata.txt";
                                File.WriteAllText(file, audioDataText);
                            }
                        }
                    }
                    catch (Exception ex)
                    { Console.WriteLine($"Exception -> {ex}"); }
                }
                else
                { httpListenerContext.Response.StatusCode = 400; httpListenerContext.Response.Close(); }
            }
        }
    }

    public class AudioDataPackets
    {
        public string? kind { set; get; }
        public AudioData? audioData { set; get; }
    }

    public class AudioData 
    { public string? data { set; get; } // Base64 Encoded audio buffer data
      public string? timestamp { set; get; } // In ISO 8601 format (yyyy-mm-ddThh:mm:ssZ)
      public string? participantRawID { set; get; }
      public bool silent { set; get; } // Indicates if the received audio buffer contains only silence.
    }

    }
