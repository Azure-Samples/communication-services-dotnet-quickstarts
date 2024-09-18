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
    private WebSocket _webSocket;
    private readonly OpenAIClient _aiClient;
    private readonly SpeechConfig speechConfig;

    // Constructor to inject OpenAIClient
    public WebSocketHandlerService(OpenAIClient aiClient, SpeechConfig speechConfig)
    {
        _aiClient = aiClient;
        this.speechConfig = speechConfig;
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
            audioStream.Seek(0, SeekOrigin.Begin);
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

        var streamingResponse = await _aiClient.GetChatCompletionsStreamingAsync(deploymentOrModelName, chatCompletionsOptions);

        await foreach (var message in streamingResponse.Value.GetChoicesStreaming())
        {
            await foreach (var chunk in message.GetMessageStreaming())
            {
                if (!string.IsNullOrEmpty(chunk.Content))
                {
                    Console.WriteLine("Received response text from Open AI: " + chunk.Content);
                    // Convert text to PCM audio
                    using var audioStream = await ConvertTextToPcmStreamAsync(chunk.Content);

                    // Read audio data from the MemoryStream
                    byte[] audioData = audioStream.ToArray();
                   // Console.WriteLine("-----Audio byte data:" + audioData.Length);
                    // Send audio data in chunks of 2024 bytes
                    int chunkSize = 2024;
                    for (int offset = 0; offset < audioData.Length; offset += chunkSize)
                    {
                        int bytesToSend = Math.Min(chunkSize, audioData.Length - offset);
                        Console.WriteLine("-----Audio byte to send :" + bytesToSend);
                        
                        
                        var audioChunk = new byte[bytesToSend];
                        Array.Copy(audioData, offset, audioChunk, 0, bytesToSend);

                        // Create a ServerAudioData object for this chunk

                        if (bytesToSend < chunkSize)
                        {
                            Console.WriteLine("-----Last Audio byte to send :" + audioChunk);
                        }
                        var audio = new ServerAudioData(audioChunk);
                        
                        var jsonObject = new ServerStreamingData(ServerMessageType.AudioData, audio);
                        // Serialize the JSON object to a string
                        string jsonString = System.Text.Json.JsonSerializer.Serialize(jsonObject);

                        byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);

                        var base64String = Encoding.UTF8.GetString(jsonBytes).TrimEnd('\0');
                        

                        try
                        {
                            ServerStreamingData serverStreamingData = JsonConvert.DeserializeObject<ServerStreamingData>(base64String);
                        }
                        catch (Exception ex)
                        {

                            Console.WriteLine("+++++++++ exception happens:");
                        }

                        // Send the PCM audio chunk over WebSocket
                        await _webSocket.SendAsync(new ArraySegment<byte>(jsonBytes), WebSocketMessageType.Text, endOfMessage: false, CancellationToken.None);
                    }
                }
            }
        }
    }
    public async Task GetOpenAiStreamResponseAsyncFile(string deploymentOrModelName, ChatCompletionsOptions chatCompletionsOptions)
    {
        if (_webSocket == null || _webSocket.State != WebSocketState.Open)
        {
            throw new InvalidOperationException("WebSocket is not connected.");
        }

        var streamingResponse = await _aiClient.GetChatCompletionsStreamingAsync(deploymentOrModelName, chatCompletionsOptions);

        await foreach (var message in streamingResponse.Value.GetChoicesStreaming())
        {
            await foreach (var chunk in message.GetMessageStreaming())
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
            var jsonObject = new
            {
                kind = "StopAudio",
                stopAudio = ""
            };

            // Serialize the JSON object to a string
            string jsonString = System.Text.Json.JsonSerializer.Serialize(jsonObject);

            // Convert the JSON string to bytes for WebSocket transmission
            byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);

            // Send the JSON chunk over the WebSocket
            await _webSocket.SendAsync(
                new ArraySegment<byte>(jsonBytes, 0, jsonBytes.Length),
                WebSocketMessageType.Text, // Sending JSON as text
                endOfMessage: true,
                cancellationToken: CancellationToken.None
            );

            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Stream completed", CancellationToken.None);

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

    // Method to receive messages from WebSocket
    public async Task ProcessWebSocketAsync()
    {
        if (_webSocket == null)
        {
            return;
        }

        try
        {
            while (_webSocket.State == WebSocketState.Open || _webSocket.State == WebSocketState.CloseSent)
            {
                byte[] receiveBuffer = new byte[2048];
                var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(60)).Token;
                WebSocketReceiveResult receiveResult = await _webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), cancellationToken);

                if (receiveResult.MessageType != WebSocketMessageType.Close)
                {
                    string data = Encoding.UTF8.GetString(receiveBuffer).TrimEnd('\0');
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