using Azure.AI.OpenAI;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech;
using System.Net.WebSockets;
using Azure;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using System.Dynamic;
using Azure.Communication.CallAutomation.FHL;
using System.Text;


namespace CallAutomationOpenAI
{
    public class OutStreamHandler
    {
        string m_answerPromptSystemTemplate = """ 
    You're an AI assistant for an elevator company called Contoso Elevators. Customers will contact you as the first point of contact when having issues with their elevators. 
    Your priority is to ensure the person contacting you or anyone else in or around the elevator is safe, if not then they should contact their local authorities.
    If everyone is safe then ask the user for information about the elevators location, such as city, building and elevator number.
    Also get the users name and number so that a technician who goes onsite can contact this person. Confirm with the user all the information 
    they've shared that it's all correct and then let them know that you've created a ticket and that a technician should be onsite within the next 24 to 48 hours.
    """;
        private WebSocket m_webSocket;
        private readonly OpenAIClient m_aiClient;
        private readonly SpeechConfig m_speechConfig;
        private CancellationTokenSource m_aiClientCts;
        private Channel<Func<Task>> m_channel;
        private string m_openAiModelName;
        private CancellationTokenSource m_cts;
        public OutStreamHandler(WebSocket webSocket, string openAiUri, string openAiKey, string openAiModelName, SpeechConfig speechConfig)
        {
            m_webSocket = webSocket;
            m_aiClient = new OpenAIClient(new Uri(openAiUri), new AzureKeyCredential(openAiKey));
            m_speechConfig = speechConfig;
            m_openAiModelName = openAiModelName;
            m_channel = Channel.CreateUnbounded<Func<Task>>(new UnboundedChannelOptions
            {
                SingleReader = true
            });
            m_aiClientCts = new CancellationTokenSource();
            m_cts = new CancellationTokenSource();
            // start dequeue task for new audio packets
            _ = Task.Run(async () => await StartForwardingAudioToWebSocket());
        }

        public void onRecognizedSpeech(object sender, SpeechRecognitionEventArgs e)
        {
            if (e.Result.Reason == ResultReason.RecognizedSpeech)
            {
                Console.WriteLine($"RECOGNIZED: Text={e.Result.Text}");
                if (e.Result.Text.Length > 1)
                {
                    var chatCompletionsOptions = new ChatCompletionsOptions()
                    {
                        Messages = {
                                     //new ChatMessage(ChatRole.System, answerPromptSystemTemplate),
                                     new ChatMessage(ChatRole.User, e.Result.Text),
                                    },
                        MaxTokens = 1000
                    };

                    /*if(aiTask != null)
                    { 
                        m_aiClientCts.Cancel(); // Cancel the previous
                        m_aiClientCts.Dispose();
                        aiTask.GetAwaiter().GetResult(); // Wait for the previous to finish
                    }
                    m_aiClientCts = new CancellationTokenSource();*/
                    //ClearBuffer();
                    _ = Task.Run(async () => await GetOpenAiStreamResponseAsync(chatCompletionsOptions));

                    // stop speaking once speech is recognized
                }
            }
            else if (e.Result.Reason == ResultReason.NoMatch)
            {
                Console.WriteLine($"NOMATCH: Speech could not be recognized.");
            }
        }

        public void onRecognizingSpeech(object sender, SpeechRecognitionEventArgs e)
        {
            Console.WriteLine($"RECOGNIZING: Text={e.Result.Text}");
            /*if ( e.Result.Text.Length > 1)
            {
                //if (aiTask != null)
                //{
                //    m_aiClientCts.Cancel(); // Cancel the previous
                //    m_aiClientCts.Dispose();
                //    aiTask.GetAwaiter().GetResult(); // Wait for the previous to finish
                //}
                //m_aiClientCts = new CancellationTokenSource();
                //ClearBuffer());
                
            }*/
        }
        public void Close()
        {
            m_cts.Cancel();
            m_aiClientCts.Cancel();
            m_cts.Dispose();
            m_aiClientCts.Dispose();
        }

        private async Task StartForwardingAudioToWebSocket()
        {
            try
            {
                // Consume messages from channel and forward buffers to player
                while (true)
                {
                    var processBuffer = await m_channel.Reader.ReadAsync(m_cts.Token).ConfigureAwait(false);
                    await processBuffer.Invoke();
                }
            }
            catch (TaskCanceledException taskCanceledException)
            {
                Console.WriteLine($"TaskCanceledException received for StartForwardingAudioToPlayer : {taskCanceledException}");
            }
            catch (ObjectDisposedException objDisposedException)
            {
                Console.WriteLine($"ObjectDisposedException received for StartForwardingAudioToPlayer :{objDisposedException}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception received for StartForwardingAudioToPlayer {ex}");
            }
        }

        public void ReceiveAudioForOutBound(string data)
        {
            try
            {
                m_channel.Writer.TryWrite(async () => await SendMessageAsync(data));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\"Exception received on ReceiveAudioForOutBound {ex}");
            }
        }

        private async Task SendMessageAsync(string message)
        {
            if (m_webSocket?.State == WebSocketState.Open)
            {
                byte[] jsonBytes = Encoding.UTF8.GetBytes(message);

                // Send the PCM audio chunk over WebSocket
                await m_webSocket.SendAsync(new ArraySegment<byte>(jsonBytes), WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);
            }
        }

        private async Task GetOpenAiStreamResponseAsync(ChatCompletionsOptions chatCompletionsOptions)
        {
            if (m_webSocket == null || m_webSocket.State != WebSocketState.Open)
            {
                throw new InvalidOperationException("WebSocket is not connected.");
            }
            try
            {
                var streamingResponse = await m_aiClient.GetChatCompletionsStreamingAsync(m_openAiModelName, chatCompletionsOptions, m_aiClientCts.Token);
                List<char> puntuations = new List<char> { '.', '?', '!' };
                string sentence = "";
                await foreach (var message in streamingResponse.Value.GetChoicesStreaming(m_aiClientCts.Token))
                {
                    await foreach (var chunk in message.GetMessageStreaming())
                    {
                        if (!string.IsNullOrEmpty(chunk.Content))
                        {
                            Console.WriteLine("Received response text from Open AI: " + chunk.Content);
                            sentence += chunk.Content + " ";
                            if (!puntuations.Contains(chunk.Content[chunk.Content.Length - 1]))
                            {
                                continue;
                            }

                            // Convert text to PCM audio
                            using var audioStream = await ConvertTextToPcmStreamAsync(sentence);
                            sentence = "";
                            // Read audio data from the MemoryStream
                            byte[] audioData = audioStream.ToArray();
                            // Console.WriteLine("-----Audio byte data:" + audioData.Length);
                            // Send audio data in chunks of 640 bytes
                            int chunkSize = 640;
                            for (int offset = 0; offset < audioData.Length; offset += chunkSize)
                            {
                                int bytesToSend = Math.Min(chunkSize, audioData.Length - offset);
                                // Console.WriteLine("-----Audio byte to send :" + bytesToSend);
                                if (bytesToSend < 640)
                                {
                                    //Console.WriteLine("-----Last Audio byte to send :" + audioChunk.ToString());
                                    continue;
                                }

                                var audioChunk = new byte[bytesToSend];
                                Array.Copy(audioData, offset, audioChunk, 0, bytesToSend);

                                // Create a ServerAudioData object for this chunk
                                var audio = new ServerStreamingData(ServerMessageType.AudioData)
                                {
                                    ServerAudioData = new ServerAudioData(audioChunk)
                                };

                                // Serialize the JSON object to a string
                                //string jsonString = JsonConvert.SerializeObject(audio);
                                string jsonString = System.Text.Json.JsonSerializer.Serialize<ServerStreamingData>(audio);
                                byte[] test = Encoding.UTF8.GetBytes(jsonString);
                                ReceiveAudioForOutBound(jsonString);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception during ai streaming -> {ex}");
            }
        }
        private async Task<MemoryStream> ConvertTextToPcmStreamAsync(string text)
        {
            var audioStream = new MemoryStream();
            var audioConfig = AudioConfig.FromStreamOutput(new PcmAudioStreamWriter(audioStream));

            var speechSynthesizer = new SpeechSynthesizer(m_speechConfig, audioConfig);

            var result = await speechSynthesizer.SpeakTextAsync(text);

            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                // Ensure to reset the position before returning
                //audioStream.Seek(0, SeekOrigin.Begin);
                return audioStream; // The caller is responsible for disposing of this stream
            }
            else
            {
                throw new Exception($"Speech synthesis failed. Reason: {result.Reason}");
            }
        }

        private void ClearBuffer()
        {
            try
            {
                var jsonObject = new ServerStreamingData(ServerMessageType.StopAudio)
                {
                    StopAudio = new StopAudio()
                };

                // Serialize the JSON object to a string
                 
                string jsonString = System.Text.Json.JsonSerializer.Serialize<ServerStreamingData>(jsonObject);
                ;
                byte[] test = Encoding.UTF8.GetBytes(jsonString);
                /*var js = new StopAudioPlay()
                {
                    kind = ServerMessageType.StopAudio,
                    stopAudio = new StopAudio()
                };
                jsonString = System.Text.Json.JsonSerializer.Serialize<StopAudioPlay>(js);*/
                /*               byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);
                                var base64String = Encoding.UTF8.GetString(jsonBytes).TrimEnd('\0');

                                ServerStreamingData serverStreamingData = JsonConvert.DeserializeObject<ServerStreamingData>(base64String);
                */
                // Convert the JSON string to bytes for WebSocket transmission
                ReceiveAudioForOutBound(jsonString);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception during streaming -> {ex}");
            }
        }
        public async Task SendInitialLearning(string userPrompt)
        {
            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                Messages = {
                    new ChatMessage(ChatRole.System, m_answerPromptSystemTemplate),
                    new ChatMessage(ChatRole.User, userPrompt),
                    },
                MaxTokens = 1000
            };

            await GetOpenAiStreamResponseAsync( chatCompletionsOptions);
        }
    }
}
public class StopAudioPlay
{
    public ServerMessageType kind { get; set; }
    public StopAudio stopAudio { get; set; }
}