using Microsoft.CognitiveServices.Speech;
using System.Net.WebSockets;
using System.Text;
using CallAutomationOpenAI;

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
    private readonly SpeechConfig speechConfig;
    private CancellationTokenSource m_aiClientCts;

    // Constructor to inject OpenAIClient
    public WebSocketHandlerService(SpeechConfig speechConfig)
    {
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
    public async Task ProcessWebSocketAsync(string openAiUri, string openAiKey, string openAiModelName)
    {    
        if (_webSocket == null)
        {
            return;
        }
        OutStreamHandler outStreamHandler = new OutStreamHandler(_webSocket, openAiUri, openAiKey, openAiModelName, speechConfig);
        InStreamHandler inStreamHandler = new InStreamHandler(_webSocket, speechConfig, outStreamHandler);
        try
        {
            _ = Task.Run(async() => await outStreamHandler.SendInitialLearning("Hello"));
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