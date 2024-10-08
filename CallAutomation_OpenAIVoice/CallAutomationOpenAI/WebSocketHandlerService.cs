using System.Net.WebSockets;
using CallAutomationOpenAI;
using Azure.AI.OpenAI;
using System.ClientModel;
using OpenAI.RealtimeConversation;

#pragma warning disable OPENAI002

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

    public async Task CloseWebSocketAsync(WebSocketReceiveResult result)
    {
        await _webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
    }
    public async Task CloseNormalWebSocketAsync()
    {
        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Stream completed", CancellationToken.None);
    }

    
    // Method to receive messages from WebSocket
    public async Task ProcessWebSocketAsync(string openAiUri, string openAiKey, string openAiModelName, string systemPrompt)
    {    
        if (_webSocket == null)
        {
            return;
        }
        var aiClient = new AzureOpenAIClient(new Uri(openAiUri), new ApiKeyCredential(openAiKey));
        var RealtimeCovnClient = aiClient.GetRealtimeConversationClient(openAiModelName);
        using RealtimeConversationSession session = await RealtimeCovnClient.StartConversationSessionAsync();

        // Session options control connection-wide behavior shared across all conversations,
        // including audio input format and voice activity detection settings.
        ConversationSessionOptions sessionOptions = new()
        {
            Instructions = systemPrompt,
            Voice = ConversationVoice.Alloy,
            InputAudioFormat = ConversationAudioFormat.Pcm16,
            OutputAudioFormat = ConversationAudioFormat.Pcm16,
            InputTranscriptionOptions = new()
            {
                Model = "whisper-1",
            },
            TurnDetectionOptions = ConversationTurnDetectionOptions.CreateServerVoiceActivityTurnDetectionOptions(0.5f, TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(300)), 
        };

        await session.ConfigureSessionAsync(sessionOptions);
        // start forwarder to AI model
        OutStreamHandler outStreamHandler = new OutStreamHandler(_webSocket, session);
        
        // start listener to audio stream from ACS
        InStreamHandler inStreamHandler = new InStreamHandler(_webSocket, outStreamHandler, session);
        try
        {
            outStreamHandler.StartAiAudioReceiver();
            await inStreamHandler.ProcessWebSocketAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception -> {ex}");
        }
        finally
        {
            session.Dispose();
            inStreamHandler.Close();
            outStreamHandler.Close();
        }
    }

}