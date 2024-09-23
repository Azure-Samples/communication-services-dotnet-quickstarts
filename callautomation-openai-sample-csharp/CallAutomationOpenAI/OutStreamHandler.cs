using Azure.AI.OpenAI;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech;
using System.Net.WebSockets;
using Azure;
using System.Threading.Channels;
using Azure.Communication.CallAutomation.FHL;
using System.Text;


namespace CallAutomationOpenAI
{
    public class OutStreamHandler
    {
        private WebSocket m_webSocket;
        private readonly OpenAIClient m_aiClient;
        private readonly SpeechConfig m_speechConfig;
        private CancellationTokenSource? m_aiClientCts;
        private Channel<Func<Task>> m_channel;
        private string m_openAiModelName;
        private CancellationTokenSource m_cts;
        private Task m_aiTask;
        private readonly object m_readTaskLock = new object();
        private AiContext m_aiContext;
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
            m_aiContext = new AiContext();
            // start dequeue task for new audio packets
            _ = Task.Run(async () => await StartForwardingAudioToWebSocket());
        }

        public void onRecognizedSpeech(object sender, SpeechRecognitionEventArgs e)
        {
            if (e.Result.Reason == ResultReason.RecognizedSpeech)
            {
                if (e.Result.Text.Length > 1)
                {
                    Console.WriteLine($"RECOGNIZED: Text={e.Result.Text}");
                    m_aiContext.AddChat(ChatRole.User, e.Result.Text);
                    var chatCompletionsOptions = new ChatCompletionsOptions()
                    {
                        MaxTokens = 1000
                    };
                    
                    foreach(var chat in m_aiContext.GetChatHistory())
                    {
                        chatCompletionsOptions.Messages.Add(chat);
                    }

                    lock(m_readTaskLock)
                    {
                        if (m_aiClientCts != null && !m_aiClientCts.Token.IsCancellationRequested)
                        { 
                            m_aiClientCts.Cancel(); // Cancel the previous
                        }
                        if(m_aiTask != null)
                        {
                            m_aiTask.GetAwaiter().GetResult(); // Wait for the previous to finish
                        }
                        ClearBuffer();
                        m_aiClientCts = new CancellationTokenSource();
                        m_aiTask = Task.Run(async () => await GetOpenAiStreamResponseAsync(chatCompletionsOptions));
                    }

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
            if ( e.Result.Text.Length > 1)
            {
                // stop speaking once speech is recognized
                lock(m_readTaskLock)
                    {
                        if (m_aiClientCts != null && !m_aiClientCts.Token.IsCancellationRequested)
                        {
                            m_aiClientCts.Cancel(); // Cancel the previous
                        }
                        ClearBuffer();
                    }
            }
        }
        public void Close()
        {
            m_cts.Cancel();
            m_aiClientCts?.Cancel();
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
            catch (OperationCanceledException opCanceledException)
            {
                Console.WriteLine($"OperationCanceledException received for StartForwardingAudioToPlayer : {opCanceledException}");
            }
            catch (ObjectDisposedException objDisposedException)
            {
                Console.WriteLine($"ObjectDisposedException received for StartForwardingAudioToPlayer :{objDisposedException}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception received for StartForwardingAudioToPlayer {ex}");
            }
            finally
            {
                m_cts.Dispose();
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
                var streamingResponse = await m_aiClient.GetChatCompletionsStreamingAsync(m_openAiModelName, chatCompletionsOptions);
                List<char> puntuations = new List<char> { '.', '?', '!' };
                string sentence = "";
                string fullSentence = "";
                await foreach (var message in streamingResponse.Value.GetChoicesStreaming())
                {
                    await foreach (var chunk in message.GetMessageStreaming())
                    {
                        m_aiClientCts?.Token.ThrowIfCancellationRequested();

                        if (!string.IsNullOrEmpty(chunk.Content))
                        {
                            sentence += chunk.Content;
                            if (!puntuations.Contains(chunk.Content[chunk.Content.Length - 1]))
                            {
                                continue;
                            }

                            Console.WriteLine("Received response text from Open AI: " + sentence);
                            // Convert text to PCM audio
                            using var audioStream = await ConvertTextToPcmStreamAsync(sentence);
                            fullSentence += sentence + " ";
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
                                string jsonString = System.Text.Json.JsonSerializer.Serialize<ServerStreamingData>(audio);
                                ReceiveAudioForOutBound(jsonString);
                            }
                        }
                    }
                }
                m_aiContext.AddChat(ChatRole.Assistant, fullSentence);
            }
            catch (OperationCanceledException e)
            {
                Console.WriteLine($"{nameof(OperationCanceledException)} thrown with message: {e.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception during ai streaming -> {ex}");
            }
            finally
            {

                m_aiClientCts?.Dispose();
                m_aiClientCts = null;
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
                
                ReceiveAudioForOutBound(jsonString);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception during streaming -> {ex}");
            }
        }
        public void SendInitialLearning(string userPrompt, string systemPrompt)
        {
            m_aiContext.AddChat(ChatRole.System, systemPrompt);
            m_aiContext.AddChat(ChatRole.User, userPrompt);
            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                MaxTokens = 1000
            };
            
            foreach( ChatMessage chat in m_aiContext.GetChatHistory(10000))
            {
                chatCompletionsOptions.Messages.Add(chat);
            }
            m_aiTask = Task.Run(async () => await GetOpenAiStreamResponseAsync(chatCompletionsOptions));
        }
    }
}
public class AudioPlay
{
    public ServerMessageType Kind { get; set; }
    public ServerAudioData AudioData { get; set; }
}