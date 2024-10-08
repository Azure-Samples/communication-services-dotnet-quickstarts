using Azure.Communication.CallAutomation;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech;
using System.Net.WebSockets;
using System.Text;

namespace CallAutomationOpenAI
{
    public class InStreamHandler
    {
        private WebSocket m_webSocket;
        private CancellationTokenSource m_cts;
        private readonly PushAudioInputStream m_audioInputStream;

        private readonly SpeechRecognizer m_speechRecognizer;
        public InStreamHandler(WebSocket webSocket, OutStreamHandler outStreamHandler)
        {
            m_webSocket = webSocket;

            m_cts = new CancellationTokenSource();
            m_audioInputStream = AudioInputStream.CreatePushStream(AudioStreamFormat.GetWaveFormatPCM(16000, 16, 1));
        }

        public void Close()
        {
            m_cts.Cancel();
            m_cts.Dispose();
            m_audioInputStream.Dispose();
            m_speechRecognizer.Dispose();
        }

        private void WriteToSpeechConfigStream(string data)
        {
            var input = StreamingDataParser.Parse(data);
            if (input is AudioData audioData)
            {
                if (!audioData.IsSilent)
                {
                    m_audioInputStream.Write(audioData.Data);
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
                await m_speechRecognizer.StartContinuousRecognitionAsync();
                while (m_webSocket.State == WebSocketState.Open || m_webSocket.State == WebSocketState.Closed)
                {
                    byte[] receiveBuffer = new byte[2048];
                    WebSocketReceiveResult receiveResult = await m_webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), m_cts.Token);

                    if (receiveResult.MessageType != WebSocketMessageType.Close)
                    {
                        string data = Encoding.UTF8.GetString(receiveBuffer).TrimEnd('\0');
                        WriteToSpeechConfigStream(data);
                        //Console.WriteLine("-----------: " + data);                
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception -> {ex}");
            }
            finally
            {
                await m_speechRecognizer.StopContinuousRecognitionAsync();
            }
        }
    }
}
