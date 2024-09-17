using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class WebSocketHandlerService
{
    private WebSocket _webSocket;

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

    public async Task StreamAudioDataFromAzureAndSendAsJson(string prompt)
    {
        try
        {
            // Replace with your Azure OpenAI API endpoint (streaming enabled)
            string openAiEndpoint = "https://api.openai.azure.com/v1/";

            using (HttpClient httpClient = new HttpClient())
            {
                // Setup your API request (authentication headers, etc.)
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer YOUR_ACCESS_TOKEN");

                var requestBody = new { prompt = prompt }; // Customize as needed
                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

                HttpResponseMessage response = await httpClient.PostAsync(openAiEndpoint, content);

                if (response.IsSuccessStatusCode)
                {
                    // Read response as a stream
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        byte[] buffer = new byte[2048];  // Buffer for chunks
                        int bytesRead;
                        int totalBytesRead = 0;

                        // Read the streamed data chunk by chunk
                        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0 && _webSocket.State == WebSocketState.Open)
                        {
                            totalBytesRead += bytesRead;

                            // Convert the byte array chunk to a base64 string
                            string base64Chunk = Convert.ToBase64String(buffer, 0, bytesRead);

                            // Set isStart to true for the first chunk and isEnd to true when the stream ends
                            bool isStart = (totalBytesRead == bytesRead); // First chunk
                            bool isEnd = (stream.CanRead == false); // End of stream

                            object jsonObject;
                            if (isEnd)
                            {
                                jsonObject = new
                                {
                                    kind = "Mark",//Mark,StopAudio
                                    audioData = base64Chunk,
                                    Mark = new
                                    {
                                        sequence = "endofstreamhello"    // Mark determines the end of the stream
                                    }
                                };
                            }
                            else
                            {
                                jsonObject = new
                                {
                                    kind = "Audio",//Mark,StopAudio
                                    audioData = base64Chunk
                                };
                            }

                            // Serialize the JSON object to a string
                            string jsonString = JsonSerializer.Serialize(jsonObject);

                            // Convert the JSON string to bytes for WebSocket transmission
                            byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);

                            // Send the JSON chunk over the WebSocket
                            await _webSocket.SendAsync(
                                new ArraySegment<byte>(jsonBytes, 0, jsonBytes.Length),
                                WebSocketMessageType.Text, // Sending JSON as text
                                endOfMessage: true,
                                cancellationToken: CancellationToken.None
                            );

                            Console.WriteLine($"Sent {bytesRead} bytes as chunk over WebSocket.");
                        }
                    }

                    // After sending all chunks, close the WebSocket connection
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Stream completed", CancellationToken.None);
                    Console.WriteLine("All streamed data sent and WebSocket closed.");
                }
                else
                {
                    Console.WriteLine($"Failed to get audio stream from Azure OpenAI: {response.StatusCode}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception during streaming -> {ex}");
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
            string jsonString = JsonSerializer.Serialize(jsonObject);

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
            // await webSocket.CloseAsync(receiveResult.CloseStatus.Value, receiveResult.CloseStatusDescription, CancellationToken.None);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception -> {ex}");
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


}