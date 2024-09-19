using Azure.AI.OpenAI;
using Azure.Communication.CallAutomation;
using Azure.Communication.CallAutomation.FHL;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;

public class WebSocketHandlerService
{
    string answerPromptSystemTemplate = """ 
    You're an AI assistant for an elevator company called Contoso Elevators. Customers will contact you as the first point of contact when having issues with their elevators. 
    Your priority is to ensure the person contacting you or anyone else in or around the elevator is safe, if not then they should contact their local authorities.
    If everyone is safe then ask the user for information about the elevators location, such as city, building and elevator number.
    Also get the users name and number so that a technician who goes onsite can contact this person. Confirm with the user all the information 
    they've shared that it's all correct and then let them know that you've created a ticket and that a technician should be onsite within the next 24 to 48 hours.
    """;
    private WebSocket _webSocket;
    private readonly OpenAIClient _aiClient;
    private readonly SpeechConfig speechConfig;

    private readonly PushAudioInputStream m_audioInputStream;

    private readonly SpeechRecognizer m_speechRecognizer;
    private CancellationTokenSource m_aiClientCts;

    // Constructor to inject OpenAIClient
    public WebSocketHandlerService(OpenAIClient aiClient, SpeechConfig speechConfig)
    {
        _aiClient = aiClient;
        this.speechConfig = speechConfig;
        m_audioInputStream = AudioInputStream.CreatePushStream(AudioStreamFormat.GetWaveFormatPCM(16000, 16, 1));
        m_speechRecognizer = new SpeechRecognizer(speechConfig, AudioConfig.FromStreamInput(m_audioInputStream));
        SubscribeToRecognizeEvents();
    }
    private void SubscribeToRecognizeEvents()
    {
        Task aiTask = null;
        speechConfig.SetProperty(PropertyId.Speech_SegmentationSilenceTimeoutMs, "500");
        speechConfig.SetProperty(PropertyId.SpeechServiceConnection_InitialSilenceTimeoutMs, "2000");
        m_speechRecognizer.Recognizing += (s, e) =>
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
                // _ = Task.Run(async () => await ClearBuffer());
                
            }*/
        };

        m_speechRecognizer.Recognized += (s, e) =>
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
                    aiTask = Task.Run(async () => await GetOpenAiStreamResponseAsync("<name your mode>", chatCompletionsOptions));

                    // stop speaking once speech is recognized
                }
                
            }
            else if (e.Result.Reason == ResultReason.NoMatch)
            {
                Console.WriteLine($"NOMATCH: Speech could not be recognized.");
            }
        };

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

    public void SetConnection(WebSocket webSocket)
    {
        _webSocket = webSocket;
    }

    public WebSocket GetConnection()
    {
        return _webSocket;
    }

    // Method to send a message via WebSocket
    public async Task SendMessageAsync(string message)
    {
        if (_webSocket?.State == WebSocketState.Open)
        {
            var messageBuffer = Encoding.UTF8.GetBytes(message);
            var buffer = new ArraySegment<byte>(messageBuffer);
            await _webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }

    private async Task<MemoryStream> ConvertTextToPcmStreamAsync(string text)
    {
        var audioStream = new MemoryStream();
        var audioConfig = AudioConfig.FromStreamOutput(new PcmAudioStreamWriter(audioStream));

        using var synthesizer = new SpeechSynthesizer(speechConfig, audioConfig);
        var result = await synthesizer.SpeakTextAsync(text);

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

    public async Task GetOpenAiStreamResponseAsync(string deploymentOrModelName, ChatCompletionsOptions chatCompletionsOptions)
    {
        if (_webSocket == null || _webSocket.State != WebSocketState.Open)
        {
            throw new InvalidOperationException("WebSocket is not connected.");
        }
        try
        {

            var streamingResponse = await _aiClient.GetChatCompletionsStreamingAsync(deploymentOrModelName, chatCompletionsOptions);
            List<char> puntuations = new List<char> { '.', '?', '!' };
            string sentence = "";
            await foreach (var message in streamingResponse.Value.GetChoicesStreaming())
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
                            string jsonString = JsonConvert.SerializeObject(audio);

                            byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);

                            // Send the PCM audio chunk over WebSocket
                            await _webSocket.SendAsync(new ArraySegment<byte>(jsonBytes), WebSocketMessageType.Text, endOfMessage: false, CancellationToken.None);
                            sentence = "";
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
    public async Task GetOpenAiStreamResponseAsyncFile(string deploymentOrModelName, ChatCompletionsOptions chatCompletionsOptions)
    {
        if (_webSocket == null || _webSocket.State != WebSocketState.Open)
        {
            throw new InvalidOperationException("WebSocket is not connected.");
        }

        var streamingResponse = await _aiClient.GetChatCompletionsStreamingAsync(deploymentOrModelName, chatCompletionsOptions, cancellationToken:m_aiClientCts.Token);

        await foreach (var message in streamingResponse.Value.GetChoicesStreaming(cancellationToken: m_aiClientCts.Token))
        {
            await foreach (var chunk in message.GetMessageStreaming(cancellationToken: m_aiClientCts.Token))
            {
                if (!string.IsNullOrEmpty(chunk.Content))
                {
                    Console.WriteLine("Received response text from Open AI: " + chunk.Content);

                    // Convert text to PCM audio
                    using var audioStream = await ConvertTextToPcmStreamAsync(chunk.Content);

                    // Convert MemoryStream to byte array
                    byte[] audioData = audioStream.ToArray();

                    // Save the audio data to a file
                    using (var fileStream = new FileStream("..//test2.pcm", FileMode.Append, FileAccess.Write))
                    {
                        await fileStream.WriteAsync(audioData, 0, audioData.Length);
                    }

                    Console.WriteLine("Audio data written to test2.pcm");
                }
            }
        }
    }

    public async Task ClearBuffer()
    {
        try
        {
            var jsonObject = new ServerStreamingData(ServerMessageType.StopAudio)
            {
                StopAudio = new StopAudio()
            };
            
            // Serialize the JSON object to a string
            string jsonString = System.Text.Json.JsonSerializer.Serialize(jsonObject);

            // Convert the JSON string to bytes for WebSocket transmission
            byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);
            if (_webSocket?.State == WebSocketState.Open)
            {
                // Send the JSON chunk over the WebSocket
                await _webSocket.SendAsync(
                new ArraySegment<byte>(jsonBytes),
                WebSocketMessageType.Text, // Sending JSON as text
                endOfMessage: false,
                cancellationToken: CancellationToken.None
                );
            }

           // await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Stream completed", CancellationToken.None);

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception during streaming -> {ex}");
        }
    }

    public async Task CloseWebSocketAsync(WebSocketReceiveResult result)
    {
        await _webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
    }
    public async Task CloseNormalWebSocketAsync()
    {
        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Stream completed", CancellationToken.None);
    }

    public async Task SendAudioFileAsync(byte[] buffer, int bytesRead)
    {
        if (_webSocket?.State == WebSocketState.Open)
        {
            // Send the chunk over the WebSocket
            await _webSocket.SendAsync(
                new ArraySegment<byte>(buffer, 0, bytesRead),
                WebSocketMessageType.Binary,
                endOfMessage: true,
                cancellationToken: CancellationToken.None);

            Console.WriteLine($"Sent {bytesRead} bytes to the client.");
        }
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
        if (_webSocket == null)
        {
            return;
        }
        try
        {
            await m_speechRecognizer.StartContinuousRecognitionAsync();
            var cancellationToken = new CancellationTokenSource().Token;
            while (_webSocket.State == WebSocketState.Open || _webSocket.State == WebSocketState.Closed)
            {
                byte[] receiveBuffer = new byte[2048];
                WebSocketReceiveResult receiveResult = await _webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), cancellationToken);

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