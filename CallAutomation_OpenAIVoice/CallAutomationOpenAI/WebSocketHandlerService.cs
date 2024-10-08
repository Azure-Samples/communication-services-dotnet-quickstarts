using Microsoft.CognitiveServices.Speech;
using System.Net.WebSockets;
using System.Text;
using CallAutomationOpenAI;

public class WebSocketHandlerService
{
    private WebSocket _webSocket;
    private CancellationTokenSource m_aiClientCts;

    // Constructor to inject OpenAIClient
    public WebSocketHandlerService()
    {
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
    public async Task ProcessWebSocketAsync(string openAiUri, string openAiKey, string openAiModelName, string systemPrompt)
    {    
        if (_webSocket == null)
        {
            return;
        }
        OutStreamHandler outStreamHandler = new OutStreamHandler(_webSocket, openAiUri, openAiKey, openAiModelName);
        InStreamHandler inStreamHandler = new InStreamHandler(_webSocket, outStreamHandler);
        try
        {
            outStreamHandler.SendInitialLearning("Hello", systemPrompt);
            await inStreamHandler.ProcessWebSocketAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception -> {ex}");
        }
        finally
        {
            inStreamHandler.Close();
            outStreamHandler.Close();
        }
    }

}