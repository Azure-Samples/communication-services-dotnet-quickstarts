using System.Net.WebSockets;
using CallAutomationOpenAI;
using Azure.Communication.CallAutomation;
using NAudio.Wave;
using System.Text;

#pragma warning disable OPENAI002

public class AcsMediaStreamingHandler
{
    private WebSocket m_webSocket;
    private CancellationTokenSource m_cts;
    private MemoryStream m_buffer;
    private AzureOpenAIService m_aiServiceHandler;
    private IConfiguration m_configuration;
    private List<byte> m_incommingAcsBuffer;

    // Constructor to inject OpenAIClient
    public AcsMediaStreamingHandler(WebSocket webSocket, IConfiguration configuration)
    {
        m_webSocket = webSocket;
        m_configuration = configuration;
        m_buffer = new MemoryStream();
        m_cts = new CancellationTokenSource();
        m_incommingAcsBuffer = new List<byte>();

    }
      
    // Method to receive messages from WebSocket
    public async Task ProcessWebSocketAsync()
    {    
        if (m_webSocket == null)
        {
            return;
        }
        
        // start forwarder to AI model
        m_aiServiceHandler = new AzureOpenAIService(this, m_configuration);
        
        try
        {
            m_aiServiceHandler.StartConversation();
            await StartReceivingFromAcsMediaWebSocket();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception -> {ex}");
        }
        finally
        {
            m_aiServiceHandler.Close();
            this.Close();
        }
    }

    public async Task SendMessageAsync(string message)
    {
        if (m_webSocket?.State == WebSocketState.Open)
        {
            byte[] jsonBytes = Encoding.UTF8.GetBytes(message);

            // Send the PCM audio chunk over WebSocket
            await m_webSocket.SendAsync(new ArraySegment<byte>(jsonBytes), WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);
        }
    }

    public async Task CloseWebSocketAsync(WebSocketReceiveResult result)
    {
        await m_webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
    }
    public async Task CloseNormalWebSocketAsync()
    {
        await m_webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Stream completed", CancellationToken.None);
    }

    public void Close()
    {
        m_incommingAcsBuffer.Clear();
        m_cts.Cancel();
        m_cts.Dispose();
        m_buffer.Dispose();
    }

    private async Task WriteToAzOpenAIServiceInputStream(string data)
    {
        var input = StreamingDataParser.Parse(data);
        if (input is AudioData audioData)
        {
            m_incommingAcsBuffer.AddRange(audioData.Data);
            if(m_incommingAcsBuffer.Count >= 640 * 10)
            {
                var inFormat = new WaveFormat(16000, 16, 1);
                var outFormat = new WaveFormat(24000, 16, 1);
                using (var ms = new MemoryStream(m_incommingAcsBuffer.ToArray()))
                using (var rs = new RawSourceWaveStream(ms, inFormat))
                using (var resampler = new MediaFoundationResampler(rs, outFormat))
                {
                    resampler.ResamplerQuality = 60;
                    WaveFileWriter.WriteWavFileToStream(m_buffer, resampler);
                }
                await m_aiServiceHandler.SendAudioToExternalAI(m_buffer);
                m_incommingAcsBuffer.Clear();
            }
        }
    }

    // Method to receive messages from WebSocket
    private async Task StartReceivingFromAcsMediaWebSocket()
    {
        if (m_webSocket == null)
        {
            return;
        }
        try
        {
            while (m_webSocket.State == WebSocketState.Open || m_webSocket.State == WebSocketState.Closed)
            {
                byte[] receiveBuffer = new byte[2048];
                WebSocketReceiveResult receiveResult = await m_webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), m_cts.Token);

                if (receiveResult.MessageType != WebSocketMessageType.Close)
                {
                    string data = Encoding.UTF8.GetString(receiveBuffer).TrimEnd('\0');
                    await WriteToAzOpenAIServiceInputStream(data);
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