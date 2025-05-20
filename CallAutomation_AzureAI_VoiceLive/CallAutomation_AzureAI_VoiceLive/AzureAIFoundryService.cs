using System.Net.WebSockets;
using Azure.Communication.CallAutomation;
using System.Text;
using System.Text.Json;
using CallAutomation.AzureAI.VoiceLive.Models;

namespace CallAutomation.AzureAI.VoiceLive
{
    public class AzureAIFoundryService
    {
        private CancellationTokenSource m_cts;
        private AcsMediaStreamingHandler m_mediaStreaming;
        private string m_answerPromptSystemTemplate = "You are an AI assistant that helps people find information.";
        private ClientWebSocket m_azureAIFoundryWebsocket;

        public AzureAIFoundryService(AcsMediaStreamingHandler mediaStreaming, IConfiguration configuration)
        {            
            m_mediaStreaming = mediaStreaming;
            m_cts = new CancellationTokenSource();
            CreateAISessionAsync(configuration).GetAwaiter().GetResult();
        }

        private async Task CreateAISessionAsync(IConfiguration configuration)
        {
            var azureAIFoundryKey = configuration.GetValue<string>("AzureAIFoundryKey");
            ArgumentNullException.ThrowIfNullOrEmpty(azureAIFoundryKey);

            var azureAIFoundryEndpoint = configuration.GetValue<string>("AzureAIFoundryEndpoint");
            ArgumentNullException.ThrowIfNullOrEmpty(azureAIFoundryEndpoint);

            var azureAIFoundryDeploymentModelName = configuration.GetValue<string>("AzureAIFoundryDeploymentModelName");
            ArgumentNullException.ThrowIfNullOrEmpty(azureAIFoundryDeploymentModelName);

            var systemPrompt = configuration.GetValue<string>("SystemPrompt") ?? m_answerPromptSystemTemplate;
            ArgumentNullException.ThrowIfNullOrEmpty(azureAIFoundryEndpoint);

            // The URL to connect to
            var azureAIFoundryWebsocketUrl = new Uri($"{azureAIFoundryEndpoint.Replace("https", "wss")}/voice-agent/realtime?api-version=2025-05-01-preview&x-ms-client-request-id={Guid.NewGuid()}&model={azureAIFoundryDeploymentModelName}&api-key={azureAIFoundryKey}");

            // Create a new WebSocket client
            m_azureAIFoundryWebsocket = new ClientWebSocket();

            Console.WriteLine($"Connecting to {azureAIFoundryWebsocketUrl}...");

            // Connect to the WebSocket server
            await m_azureAIFoundryWebsocket.ConnectAsync(azureAIFoundryWebsocketUrl, CancellationToken.None);
            Console.WriteLine("Connected successfully!");

            StartConversation();
  
            // Send a test message
            await SendMessageAsync(GetSessionUpdate().ToJson(), CancellationToken.None);
        }

        private SessionUpdate GetSessionUpdate()
        {
            return new SessionUpdate()
            {
               Session = new SessionUpdate.SessionConfig()
               {
                   Voice = new()
                   {
                       Name = "en-US-AvaNeural",
                       Type = "azure-standard"
                   },
                   InputAudioTranscription = new()
                   {
                       Model = "whisper-1"
                   },
                   TurnDetection = new()
                   {
                       Type = "server_vad"
                   },
                   Modalities = new List<string> { "text", "audio" },
                   Temperature = 0.9
            }
            };
        }

        // Method to send messages to the WebSocket server
        async Task SendMessageAsync(string message, CancellationToken cancellationToken)
        {
            //Console.WriteLine($"Sending Message: {message}");

            if (m_azureAIFoundryWebsocket != null)
            {
                if (m_azureAIFoundryWebsocket.State != WebSocketState.Open)
                    return;

                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                await m_azureAIFoundryWebsocket.SendAsync(
                    new ArraySegment<byte>(messageBytes),
                    WebSocketMessageType.Text,
                    true,
                    cancellationToken);

                //Console.WriteLine($"Sent: {message}");
            }
        }

        // Method to receive messages from the WebSocket server
        async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[1024 * 8]; // 8KB buffer

            try
            {
                while (m_azureAIFoundryWebsocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    var receiveBuffer = new ArraySegment<byte>(buffer);
                    StringBuilder messageBuilder = new StringBuilder();

                    WebSocketReceiveResult result = null;

                    do
                    {
                        result = await m_azureAIFoundryWebsocket.ReceiveAsync(receiveBuffer, cancellationToken);
                        messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await m_azureAIFoundryWebsocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                            return;
                        }

                    } while (!result.EndOfMessage); // Ensure full message is received

                    string receivedMessage = messageBuilder.ToString();
                    Console.WriteLine($"Received: {receivedMessage}");

                    if (receivedMessage.Contains("response.audio.delta"))
                    {
                        var responseAudio = JsonSerializer.Deserialize<ResponseAudio>(receivedMessage);

                        if (!string.IsNullOrEmpty(responseAudio?.Delta) && responseAudio.Delta.Length % 4 == 0) // Validate B
                                                                                                                // ase64
                        {
                            var jsonString = OutStreamingData.GetAudioDataForOutbound(Convert.FromBase64String(responseAudio.Delta));
                            await m_mediaStreaming.SendMessageAsync(jsonString);
                        }
                    }
                    else if (receivedMessage.Contains("input_audio_buffer.speech_started"))
                    {
                        var speechStartedUpdate = JsonSerializer.Deserialize<InputSpeechStarted>(receivedMessage);

                        Console.WriteLine(
                            $"  -- Voice activity detection started at {speechStartedUpdate.AudioStartTime} ms");
                        // Barge-in, send stop audio
                        var jsonString = OutStreamingData.GetStopAudioForOutbound();
                        await m_mediaStreaming.SendMessageAsync(jsonString);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Operation was canceled, which is fine
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while receiving: {ex.Message}");
            }
        }

        public void StartConversation()
        {
            _ = Task.Run(async () => await ReceiveMessagesAsync(CancellationToken.None));
        }

        public async Task SendAudioToExternalAI(byte[] data)
        {
            var inputAudio = new InputAudio()
            {
                Audio = Convert.ToBase64String(data)
            };

            await SendMessageAsync(inputAudio.ToJson(), CancellationToken.None);
        }
        

        public async Task Close()
        {
            m_cts.Cancel();
            m_cts.Dispose();
            if (m_azureAIFoundryWebsocket != null)
            {
                await m_azureAIFoundryWebsocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Normal", CancellationToken.None);
            }
            //m_aiSession.Dispose();
        }
    }
}