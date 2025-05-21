using System.Net.WebSockets;
using Azure.Communication.CallAutomation;
using System.Text;
using System.Text.Json;

namespace CallAutomation.AzureAI.VoiceLive
{
    public class AzureVoiceLiveService
    {
        private CancellationTokenSource m_cts;
        private AcsMediaStreamingHandler m_mediaStreaming;
        private string m_answerPromptSystemTemplate = "You are an AI assistant that helps people find information.";
        private ClientWebSocket m_azureVoiceLiveWebsocket;

        public AzureVoiceLiveService(AcsMediaStreamingHandler mediaStreaming, IConfiguration configuration)
        {            
            m_mediaStreaming = mediaStreaming;
            m_cts = new CancellationTokenSource();
            CreateAISessionAsync(configuration).GetAwaiter().GetResult();
        }

        private async Task CreateAISessionAsync(IConfiguration configuration)
        {
            var azureVoiceLiveApiKey = configuration.GetValue<string>("AzureVoiceLiveApiKey");
            ArgumentNullException.ThrowIfNullOrEmpty(azureVoiceLiveApiKey);

            var azureVoiceLiveEndpoint = configuration.GetValue<string>("AzureVoiceLiveEndpoint");
            ArgumentNullException.ThrowIfNullOrEmpty(azureVoiceLiveEndpoint);

            var voiceLiveModel = configuration.GetValue<string>("VoiceLiveModel");
            ArgumentNullException.ThrowIfNullOrEmpty(voiceLiveModel);

            var systemPrompt = configuration.GetValue<string>("SystemPrompt") ?? m_answerPromptSystemTemplate;
            ArgumentNullException.ThrowIfNullOrEmpty(systemPrompt);

            // The URL to connect to
            var azureVoiceLiveWebsocketUrl = new Uri($"{azureVoiceLiveEndpoint.Replace("https", "wss")}/voice-agent/realtime?api-version=2025-05-01-preview&x-ms-client-request-id={Guid.NewGuid()}&model={voiceLiveModel}&api-key={azureVoiceLiveApiKey}");

            // Create a new WebSocket client
            m_azureVoiceLiveWebsocket = new ClientWebSocket();

            Console.WriteLine($"Connecting to {azureVoiceLiveWebsocketUrl}...");

            // Connect to the WebSocket server
            await m_azureVoiceLiveWebsocket.ConnectAsync(azureVoiceLiveWebsocketUrl, CancellationToken.None);
            Console.WriteLine("Connected successfully!");

            // Listen to messages over websocket
            StartConversation();

            // Update the session
            await UpdateSessionAsync();

            //Start Response from AI
            await StartResponseAsync();
        }

        private async Task UpdateSessionAsync()
        {
            var jsonObject = new
            {
                type = "session.update",
                session = new
                {
                    turn_detection = new
                    {
                        type = "azure_semantic_vad",
                        threshold = 0.3,
                        prefix_padding_ms = 200,
                        silence_duration_ms = 200,
                        remove_filler_words = false
                    },
                    input_audio_noise_reduction = new { type = "azure_deep_noise_suppression" },
                    input_audio_echo_cancellation = new { type = "server_echo_cancellation" },
                    voice = new
                    {
                        name = "en-US-Aria:DragonHDLatestNeural",
                        type = "azure-standard",
                        temperature = 0.8
                    }
                }
            };

            // Convert object to JSON string with indentation
            string sessionUpdate = JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine($"SessionUpdate: {sessionUpdate}");
            await SendMessageAsync(sessionUpdate, CancellationToken.None);
        }

        private async Task StartResponseAsync()
        {
            var jsonObject = new
            {
                type = "response.create"
            };
            var message = JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions { WriteIndented = true });
            await SendMessageAsync(message, CancellationToken.None);
        }

        // Method to send messages to the WebSocket server
        async Task SendMessageAsync(string message, CancellationToken cancellationToken)
        {
            //Console.WriteLine($"Sending Message: {message}");

            if (m_azureVoiceLiveWebsocket != null)
            {
                if (m_azureVoiceLiveWebsocket.State != WebSocketState.Open)
                    return;

                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                await m_azureVoiceLiveWebsocket.SendAsync(
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
                while (m_azureVoiceLiveWebsocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    var receiveBuffer = new ArraySegment<byte>(buffer);
                    StringBuilder messageBuilder = new StringBuilder();

                    WebSocketReceiveResult result = null;

                    do
                    {
                        result = await m_azureVoiceLiveWebsocket.ReceiveAsync(receiveBuffer, cancellationToken);
                        messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await m_azureVoiceLiveWebsocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                            return;
                        }

                    } while (!result.EndOfMessage); // Ensure full message is received

                    string receivedMessage = messageBuilder.ToString();
                    Console.WriteLine($"Received: {receivedMessage}");

                    var data = JsonSerializer.Deserialize<Dictionary<string, object>>(receivedMessage);

                    if (data != null)
                    {
                        if (data["type"].ToString() == "response.audio.delta")
                        {
                            var jsonString = OutStreamingData.GetAudioDataForOutbound(Convert.FromBase64String(data["delta"].ToString()));
                            await m_mediaStreaming.SendMessageAsync(jsonString);                            
                        }
                        else if (data["type"].ToString() == "input_audio_buffer.speech_started")
                        {
                            Console.WriteLine($"  -- Voice activity detection started");
                            // Barge-in, send stop audio
                            var jsonString = OutStreamingData.GetStopAudioForOutbound();
                            await m_mediaStreaming.SendMessageAsync(jsonString);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Received message is null or empty.");
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
            _ = Task.Run(async () => await ReceiveMessagesAsync(m_cts.Token));
        }

        public async Task SendAudioToExternalAI(byte[] data)
        {
            var audioBytes = Convert.ToBase64String(data);
            var jsonObject = new
            {
                type = "input_audio_buffer.append",
                audio = audioBytes
            };

            var message = JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions { WriteIndented = true });
            await SendMessageAsync(message, CancellationToken.None);
        }
        

        public async Task Close()
        {
            m_cts.Cancel();
            m_cts.Dispose();
            if (m_azureVoiceLiveWebsocket != null)
            {
                await m_azureVoiceLiveWebsocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Normal", CancellationToken.None);
            }
        }
    }
}