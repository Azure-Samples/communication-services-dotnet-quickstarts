using Azure.Communication.CallAutomation;
using System.Net.WebSockets;
using System.Text;
using OpenAI.RealtimeConversation;
using NAudio.Wave;

#pragma warning disable OPENAI002

namespace CallAutomationOpenAI
{
    public class InStreamHandler
    {
        private WebSocket m_webSocket;
        private CancellationTokenSource m_cts;
        private readonly RealtimeConversationSession m_aiSession;
        private MemoryStream m_buffer;
        public InStreamHandler(WebSocket webSocket, OutStreamHandler outStreamHandler, RealtimeConversationSession aiSession)
        {
            m_webSocket = webSocket;
            m_aiSession = aiSession;
            m_buffer = new MemoryStream();
            m_cts = new CancellationTokenSource();
            _ = Task.Run(async () => await SendAudioAsync());
        }

        private async Task SendAudioAsync()
        {
            try
            {
                // Consume messages from channel and forward buffers to player
                while (true)
                {
                    await m_aiSession.SendAudioAsync(m_buffer);
                }
            }
            catch (OperationCanceledException opCanceledException)
            {
                Console.WriteLine($"OperationCanceledException received for SendAudioAsync : {opCanceledException}");
            }
            catch (ObjectDisposedException objDisposedException)
            {
                Console.WriteLine($"ObjectDisposedException received for SendAudioAsync :{objDisposedException}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception received for SendAudioAsync {ex}");
            }
        }

        public void Close()
        {
            m_cts.Cancel();
            m_cts.Dispose();
            
        }

        private async Task WriteToAiInputStream(string data)
        {
            var input = StreamingDataParser.Parse(data);
            if (input is AudioData audioData)
            {
                    var inFormat = new WaveFormat(16000, 16, 1);
                    var outFormat = new WaveFormat(24000, 16, 1);
                    var ms = new MemoryStream(audioData.Data);
                    var rs = new RawSourceWaveStream(ms, inFormat);
                    var resampler = new MediaFoundationResampler(rs, outFormat);
                    int chunkSize = 640;
                    byte[] buffer = new byte[chunkSize];
                    int bytesRead;
                    while ((bytesRead = resampler.Read(buffer, 0, chunkSize)) > 0)
                    {
                     // write to the memory stream and let the other thread forward it to AI model
                      await m_buffer.WriteAsync(buffer, 0, bytesRead);
                    }
            }
        }

        // Method to receive messages from WebSocket
        public async Task ProcessWebSocketAsync()
        {
            if (m_webSocket == null)
            {
                return;
            }
            try
            {
                while (m_webSocket.State == WebSocketState.Open || m_webSocket.State == WebSocketState.Closed)
                {
                    byte[] receiveBuffer = new byte[2048];
                    WebSocketReceiveResult receiveResult = await m_webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), m_cts.Token);

                    if (receiveResult.MessageType != WebSocketMessageType.Close)
                    {
                        string data = Encoding.UTF8.GetString(receiveBuffer).TrimEnd('\0');
                        await WriteToAiInputStream(data);
                        //Console.WriteLine("-----------: " + data);                
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception -> {ex}");
            }
        }
    }
}
