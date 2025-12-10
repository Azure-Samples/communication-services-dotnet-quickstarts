using System.Net.WebSockets;
using System.Text;
using Azure.Communication.CallAutomation;
using Azure.Communication.Media;
using CallAutomationOpenAI;
#pragma warning disable OPENAI002

public class AcsMediaStreamingHandler
{
    private TestCallRoomConnector m_roomConnector;
    public AzureOpenAIService aiServiceHandler { get; set; }
    // Constructor to inject OpenAIClient
    public AcsMediaStreamingHandler(TestCallRoomConnector roomConnector)
    {
        m_roomConnector = roomConnector;
    }
      
    public async Task SendMessageAsync(byte[] message)
    {
        Console.WriteLine($"SendMessageAsync -> {message}");
        m_roomConnector.OutgoingAudioStream.Write(message);
    }

    public async Task WriteInputStream(string data)
    {
        Console.WriteLine($"WriteInputStream -> {data}");
        using Stream audioStream = File.OpenRead($"whats_the_weather.wav");
        await aiServiceHandler.SendAudioToExternalAI(audioStream);
    }

    public async Task ProcessConversation(AzureOpenAIService aiServiceHandler)
    {
        this.aiServiceHandler = aiServiceHandler;
    }
}