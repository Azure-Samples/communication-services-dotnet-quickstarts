using Azure.Communication.CallAutomation;
using System.Net.WebSockets;
using System.Text;

static class AcsMediaStreamProcessor
{
    public static async Task ConnectAndProcessMediaStreamAsync(
        CallAutomationClient client,
        string streamUrl,
        ILogger logger,
        IConfiguration configuration)
    {
        logger.LogInformation("Stream URL: " + streamUrl);

        // Customer connects
        try
        {
            // Minimal usage — only stream URL is required.
            var ws = await MediaWebSocketClient
                .Builder(client)
                .WithStreamUrl(streamUrl)
                .BuildAndConnectAsync();

            //// Use AcsMediaStreamingHandler for full media processing
            var mediaService = new AcsMediaStreamingHandler(ws.Socket, configuration);
            await mediaService.ProcessWebSocketAsync();
        }
        catch (WebSocketException wsEx)
        {
            logger.LogError(wsEx, "WebSocket connect failed. Stream URL: " + streamUrl);
            throw;
        }

        //var buffer = new byte[64 * 1024];
        //// Simple echo test for bidirectional streaming verification
        //logger.LogInformation("Starting echo test - received audio will be sent back.");

        //// Start receiving messages in background
        //var receiveTask = ReceiveMessagesWithAuth(ws);
        //await receiveTask;

        //// Echo: send the received data back (bidirectional test)
        //while (ws.State == WebSocketState.Open)
        //{
        //    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        //    if (result.MessageType == WebSocketMessageType.Close)
        //    {
        //        logger.LogWarning($"WS closed by remote. Status: {result.CloseStatus}, {result.CloseStatusDescription}");
        //        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing", CancellationToken.None);
        //        break;
        //    }
        //    if (result.Count > 0)
        //    {
        //        await ws.SendAsync(
        //            new ArraySegment<byte>(buffer, 0, result.Count),
        //            result.MessageType,
        //            result.EndOfMessage,
        //            CancellationToken.None);
        //        logger.LogInformation($"Echoed {result.Count} bytes back.");
        //    }
        //}
    }

    //private static async Task ReceiveMessagesWithAuth(ClientWebSocket webSocket)
    //{
    //    byte[] buffer = new byte[4096];
    //    StringBuilder messageBuilder = new StringBuilder();

    //    while (webSocket.State == WebSocketState.Open)
    //    {
    //        try
    //        {
    //            WebSocketReceiveResult result = await webSocket.ReceiveAsync(
    //                new ArraySegment<byte>(buffer), CancellationToken.None);

    //            if (result.MessageType == WebSocketMessageType.Close)
    //            {
    //                Console.WriteLine("Server initiated close");
    //                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure,
    //                    "Closing", CancellationToken.None);
    //            }
    //            else if (result.MessageType == WebSocketMessageType.Text)
    //            {
    //                string chunk = Encoding.UTF8.GetString(buffer, 0, result.Count);
    //                messageBuilder.Append(chunk);

    //                if (result.EndOfMessage)
    //                {
    //                    string completeMessage = messageBuilder.ToString();
    //                    Console.WriteLine($"-----Authenticated message received: {completeMessage}");
    //                    messageBuilder.Clear();
    //                }
    //            }
    //            else if (result.MessageType == WebSocketMessageType.Binary)
    //            {
    //                Console.WriteLine($"Binary message received: {result.Count} bytes");
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine($"Error receiving authenticated message: {ex.Message}");
    //            break;
    //        }
    //    }
    //}
}
