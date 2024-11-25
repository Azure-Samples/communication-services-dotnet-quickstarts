using System.Net.WebSockets;
using CallAutomationOpenAI;
using Azure.Communication.CallAutomation;
using System.Text;
using System.Collections.Generic;
using System.Collections;
using static System.Runtime.InteropServices.JavaScript.JSType;

#pragma warning disable OPENAI002

public class AcsMediaStreamingHandler
{
    private WebSocket m_webSocket;
    private CancellationTokenSource m_cts;
    private MemoryStream m_buffer;
    private AzureOpenAIService m_aiServiceHandler;
    private IConfiguration m_configuration;
    private string m_logFilePath;
    private readonly object m_fileLock = new object();
    FileStream audioDataFileStream;
    Dictionary<string, FileStream> audioDataFiles = new Dictionary<string, FileStream>();
    string fileName = string.Format("..//testWithrecordingNoSilence.pcm");

    // Constructor to inject OpenAIClient
    public AcsMediaStreamingHandler(WebSocket webSocket, IConfiguration configuration)
    {
        m_webSocket = webSocket;
        m_configuration = configuration;
        m_buffer = new MemoryStream();
        m_cts = new CancellationTokenSource();
        // Initialize the log file path in the application directory
        
       // audioDataFileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
        //audioDataFiles.Add(fileName, audioDataFileStream);
        

    }
      
    // Method to receive messages from WebSocket
    public async Task ProcessWebSocketAsync()
    {    
        if (m_webSocket == null)
        {
            return;
        }
        
        // start forwarder to AI model
       // m_aiServiceHandler = new AzureOpenAIService(this, m_configuration);
        
        try
        {
           // m_aiServiceHandler.StartConversation();
            await StartReceivingFromAcsMediaWebSocket();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception -> {ex}");
        }
        finally
        {
           // m_aiServiceHandler.Close();
            this.Close();
        }
    }

    public async Task SendMessageAsync(string message)
    {
        if (m_webSocket?.State == WebSocketState.Open)
        {

            // Console.WriteLine($"{message}");
           byte[] jsonBytes = Encoding.UTF8.GetBytes(message);
           
            //byte[] jsonBytes = message.Select(c => (byte)c).ToArray();
            //Console.WriteLine("---------------- fafa " + message);
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
        m_buffer.Dispose();
    }

    private async Task WriteToAzOpenAIServiceInputStream(string data)
    {
        var input = StreamingData.Parse(data);
        if (input is AudioData audioData)
        {
            if (!audioData.IsSilent)
            {
                // Convert ReadOnlyMemory<byte> to byte[] using ToArray() method  
                using (var ms = new MemoryStream(audioData.Data.ToArray()))
                {
                    await m_aiServiceHandler.SendAudioToExternalAI(ms);
                }
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
                WebSocketReceiveResult receiveResult = await m_webSocket.ReceiveAsync(
                    new ArraySegment<byte>(receiveBuffer),
                    m_cts.Token
                );

                if (receiveResult.MessageType != WebSocketMessageType.Close)
                {
                    string data = Encoding.UTF8.GetString(receiveBuffer).TrimEnd('\0');
                   // LogToFile(data);

                    //await WriteToAzOpenAIServiceInputStream(data);
                    Console.WriteLine("-----------: " + data);
                    //getDtmfStreaming(data).GetAwaiter().GetResult();
                }

                Task.Delay(20000).Wait(); // Simulate some delay for debugging 
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception -> {ex}");
        }
    }

    // Helper method to log to file
    public async void LogToFile(string data)
    {
        try
        {
            // Use lock to avoid file access conflicts from multiple threads
            //lock (m_fileLock)
            {
                var input = StreamingData.Parse(data);
                if (input is AudioData audioData)
                {
                    if (!audioData.IsSilent)
                    {
                        FileStream audioDataFileStream;

                        if (audioDataFiles.ContainsKey(fileName))
                        {
                            audioDataFiles.TryGetValue(fileName, out audioDataFileStream);
                        }
                        else
                        {
                            audioDataFileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
                            audioDataFiles.Add(fileName, audioDataFileStream);
                        }
                        await audioDataFileStream.WriteAsync(audioData.Data.ToArray(), 0, audioData.Data.ToArray().Length);
                    }
                }
                
               // File.AppendAllText(m_logFilePath, $"{message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to write to log file: {ex.Message}");
        }
    }

    // Method to get the path of the log file
    public string GetLogFilePath()
    {
        return m_logFilePath;
    }

    
}