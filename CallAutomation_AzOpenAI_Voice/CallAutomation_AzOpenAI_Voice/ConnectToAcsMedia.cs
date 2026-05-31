using Azure.Core;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;

static class AcsMediaConnector
{
    public static async Task ConnectToAcsMediaAsync(
        string streamUrl,
        string acsEndpoint,
        string acsAccessKeyBase64,
        string authMode,
        AccessToken aadToken,
        ILogger logger,
        IConfiguration configuration)
    {
        var uri = new Uri(streamUrl);
        var host = uri.Authority;

        using var ws = new ClientWebSocket();

        // Required headers (taken from your design doc)
        // X-Ms-Host must match <resource>.communication.azure.com
        ws.Options.SetRequestHeader("X-Ms-Host", new Uri(acsEndpoint).Authority);

        if (authMode.Equals("AAD", StringComparison.OrdinalIgnoreCase))
        {
            ws.Options.SetRequestHeader("Authorization", $"Bearer {aadToken.Token}");
        }
        else
        {
            string contentHash; using (var sha256 = SHA256.Create()) { contentHash = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(string.Empty))); }

            const string signedHeaders = "date;host;x-ms-content-sha256";
            var date = DateTime.UtcNow.ToString("R");
            var stringToSign = $"GET\n{uri.PathAndQuery}\n{date};{new Uri(acsEndpoint).Authority};{contentHash}";

            string signature; using (var hmac = new HMACSHA256(Convert.FromBase64String(acsAccessKeyBase64))) { signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign))); }
            var authorizationHeader = $"HMAC-SHA256 SignedHeaders={signedHeaders}&Signature={signature}";

            // Diagnostics (avoid printing secrets)
            Console.WriteLine("✅ HMAC signature generated");
            Console.WriteLine($"🛣️ PathAndQuery: {uri.PathAndQuery}");
            Console.WriteLine($"🔒 ContentHash: {contentHash}");
            Console.WriteLine($"🧾 StringToSign (verbatim):\n{stringToSign}");
            Console.WriteLine($"🔑 Signature(Base64): {signature}");

            ws.Options.SetRequestHeader("date", date);
            ws.Options.SetRequestHeader("x-ms-content-sha256", contentHash);
            ws.Options.SetRequestHeader("Authorization", authorizationHeader);
        }

        try
        {
            logger.LogInformation($"Connecting WS to {streamUrl} ...");
            await ws.ConnectAsync(uri, CancellationToken.None);
            logger.LogInformation("WS connected.");

            // Start receiving messages in background
            var receiveTask = ReceiveMessagesWithAuth(ws);

            // Wait for the receive task to complete (when connection closes)
            await receiveTask;

            // Receive loop — PCM 24k mono frames will arrive as Binary messages
            var buffer = new byte[64 * 1024];
            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(buffer: new ArraySegment<byte>(buffer), cancellationToken: CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    logger.LogWarning($"WS closed by remote. Status: {result.CloseStatus}, {result.CloseStatusDescription}");
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing", CancellationToken.None);
                    break;
                }

                var mediaService = new AcsMediaStreamingHandler(ws, configuration);

                // Set the single WebSocket connection
                await mediaService.ProcessWebSocketAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"WS connection failed for {streamUrl}");
        }
    }

    private static async Task ReceiveMessagesWithAuth(ClientWebSocket webSocket)
    {
        byte[] buffer = new byte[4096];
        StringBuilder messageBuilder = new StringBuilder();

        while (webSocket.State == WebSocketState.Open)
        {
            try
            {
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine("Server initiated close");
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure,
                        "Closing", CancellationToken.None);
                }
                else if (result.MessageType == WebSocketMessageType.Text)
                {
                    string chunk = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    messageBuilder.Append(chunk);

                    if (result.EndOfMessage)
                    {
                        string completeMessage = messageBuilder.ToString();
                        Console.WriteLine($"-----Authenticated message received: {completeMessage}");

                        // Process the complete message here
                        await ProcessReceivedMessage(webSocket, completeMessage);

                        messageBuilder.Clear();
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Binary)
                {
                    Console.WriteLine($"Binary message received: {result.Count} bytes");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving authenticated message: {ex.Message}");
                break;
            }
        }
    }

    private static async Task ProcessReceivedMessage(ClientWebSocket webSocket, string message)
    {
        try
        {
            if (message.Contains("ping"))
            {
                await SendMessage(webSocket, "pong");
                Console.WriteLine("Responded to ping with pong");
            }
            else if (message.Contains("metadata"))
            {
                Console.WriteLine("Received metadata message");
            }
            else if (message.Contains("audioData"))
            {
                Console.WriteLine("Received audio data");
            }
            else
            {
                Console.WriteLine("Received unknown message type " + message);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing message: {ex.Message}");
        }
    }

    private static async Task SendMessage(ClientWebSocket webSocket, string message)
    {
        byte[] messageBytes = Encoding.UTF8.GetBytes(message);
        await webSocket.SendAsync(
            new ArraySegment<byte>(messageBytes),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None);
        Console.WriteLine($"Message sent: {message}");
    }
}
