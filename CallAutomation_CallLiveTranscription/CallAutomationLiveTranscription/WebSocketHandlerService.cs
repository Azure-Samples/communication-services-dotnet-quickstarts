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
