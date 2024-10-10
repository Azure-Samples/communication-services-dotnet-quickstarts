using System.Net.WebSockets;
using CallAutomationOpenAI;
using Azure.Communication.CallAutomation;
using NAudio.Wave;
using System.Text;

#pragma warning disable OPENAI002

public class MediaStreaming
{
    private WebSocket m_webSocket;
    private CancellationTokenSource m_cts;
    private MemoryStream m_buffer;
    private OpenAIServiceHandler m_aiServiceHandler;

    // Constructor to inject OpenAIClient
    public MediaStreaming(WebSocket webSocket)
    {
        m_webSocket = webSocket;
        m_buffer = new MemoryStream();
        m_cts = new CancellationTokenSource();
    }
      
    // Method to receive messages from WebSocket
    public async Task ProcessWebSocketAsync(string openAiUri, string openAiKey, string openAiModelName, string systemPrompt)
    {    
        if (m_webSocket == null)
        {
            return;
        }
        
        // start forwarder to AI model
        m_aiServiceHandler = new OpenAIServiceHandler(this);
        
        try
        {
            await m_aiServiceHandler.StartConversation(openAiUri, openAiKey, openAiModelName, systemPrompt);
            _ = Task.Run(async () => await SendAudioAsync());
            await StartRecevingFromMediaWebSocket();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception -> {ex}");
        }
        finally
        {
            m_aiServiceHandler.Close();
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
        m_cts.Cancel();
        m_cts.Dispose();

    }

    private async Task WriteToAiInputStream(string data)
    {
        var input = StreamingDataParser.Parse(data);
        if (input is AudioData audioData)
        {
            var inFormat = new WaveFormat(16000, 16, 1);
            var outFormat = new WaveFormat(24000, 16, 1);
            var ms = new MemoryStream(audioData.Data);
            var rs = new RawSourceWaveStream(ms, inFormat);
            var resampler = new MediaFoundationResampler(rs, outFormat);
            int chunkSize = 640;
            byte[] buffer = new byte[chunkSize];
            int bytesRead;
            while ((bytesRead = resampler.Read(buffer, 0, chunkSize)) > 0)
            {
                // write to the memory stream and let the other thread forward it to AI model
                await m_buffer.WriteAsync(buffer, 0, bytesRead);
            }
        }
    }

    // Method to receive messages from WebSocket
    private async Task StartRecevingFromMediaWebSocket()
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
                    await WriteToAiInputStream(data);
                    //Console.WriteLine("-----------: " + data);                
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception -> {ex}");
        }
    }

    private async Task SendAudioAsync()
    {
        try
        {
            // Consume messages from channel and forward buffers to player
            while (true)
            {
                await m_aiServiceHandler.SendAudioToExternalAI(m_buffer);
            }
        }
        catch (OperationCanceledException opCanceledException)
        {
            Console.WriteLine($"OperationCanceledException received for SendAudioAsync : {opCanceledException}");
        }
        catch (ObjectDisposedException objDisposedException)
        {
            Console.WriteLine($"ObjectDisposedException received for SendAudioAsync :{objDisposedException}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception received for SendAudioAsync {ex}");
        }
    }
}