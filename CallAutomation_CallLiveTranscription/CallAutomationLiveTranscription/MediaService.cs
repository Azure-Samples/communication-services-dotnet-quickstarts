using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class MediaService
{
    private readonly WebSocketHandlerService _webSocketHandlerService;

    public MediaService(WebSocketHandlerService webSocketHandlerService)
    {
        _webSocketHandlerService = webSocketHandlerService;
    }

    public async Task HandleWebSocketConnectionAsync(WebSocket webSocket)
    {
        // Set the WebSocket connection in the WebSocketHandlerService
        _webSocketHandlerService.SetConnection(webSocket);

        // Optionally perform additional setup or processing here
        await _webSocketHandlerService.ProcessWebSocketAsync();
    }

    public async Task SendMessageAsync(string message)
    {
        await _webSocketHandlerService.SendMessageAsync(message);
    }

    public async Task SendAudioFileAsync(string filePath)
    {
        try
        {
            // Open the PCM file and stream the data
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[2048];  // Buffer for reading chunks of the file
                int bytesRead;

                while ((bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0 )
                {
                    await _webSocketHandlerService.SendAudioFileAsync(buffer, bytesRead);
                }
            }

            // After streaming, close the WebSocket connection
            await _webSocketHandlerService.CloseNormalWebSocketAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception -> {ex}");
        }
    }
}