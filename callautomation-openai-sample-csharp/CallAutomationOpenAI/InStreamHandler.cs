using Azure.Communication.CallAutomation;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech;
using System.Net.WebSockets;
using System.Text;
using Azure.AI.OpenAI;
using Azure;
using System.Threading.Channels;
using System.Text.Json.Serialization;
using Azure.Communication.CallAutomation.FHL;
using Newtonsoft.Json;

namespace CallAutomationOpenAI
{
    public class InStreamHandler
    {
        private WebSocket m_webSocket;
        private readonly SpeechConfig m_speechConfig;
        private CancellationTokenSource m_cts;
        private readonly PushAudioInputStream m_audioInputStream;

        private readonly SpeechRecognizer m_speechRecognizer;
        public InStreamHandler(WebSocket webSocket, SpeechConfig speechConfig, OutStreamHandler outStreamHandler)
        {
            m_webSocket = webSocket;
            m_speechConfig = speechConfig;
            m_speechConfig.SetProperty(PropertyId.Speech_SegmentationSilenceTimeoutMs, "500");
            m_speechConfig.SetProperty(PropertyId.SpeechServiceConnection_InitialSilenceTimeoutMs, "2000");

            m_cts = new CancellationTokenSource();
            m_audioInputStream = AudioInputStream.CreatePushStream(AudioStreamFormat.GetWaveFormatPCM(16000, 16, 1));
            m_speechRecognizer = new SpeechRecognizer(speechConfig, AudioConfig.FromStreamInput(m_audioInputStream));
            SubscribeToRecognizeEvents(outStreamHandler);
        }

        public void Close()
        {
            m_cts.Cancel();
            m_cts.Dispose();
            m_audioInputStream.Dispose();
            m_speechRecognizer.Dispose();
        }

        private void SubscribeToRecognizeEvents(OutStreamHandler outStreamHandler)
        {
            m_speechRecognizer.Recognizing += outStreamHandler.onRecognizingSpeech;
            m_speechRecognizer.Recognized += outStreamHandler.onRecognizedSpeech;

            m_speechRecognizer.Canceled += (s, e) =>
            {
                Console.WriteLine($"CANCELED: Reason={e.Reason}");

                if (e.Reason == CancellationReason.Error)
                {
                    Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                    Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                    Console.WriteLine($"CANCELED: Did you set the speech resource key and region values?");
                }

            };

            m_speechRecognizer.SessionStopped += (s, e) =>
            {
                Console.WriteLine("\n   Recognition Session stopped event.");
            };
        }

        private void WriteToSpeechConfigStream(string data)
        {
            var input = StreamingDataParser.Parse(data);
           // var input2 = JsonConvert.DeserializeObject<ServerStreamingData>(data);
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
