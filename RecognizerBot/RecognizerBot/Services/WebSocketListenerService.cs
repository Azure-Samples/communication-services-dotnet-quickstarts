using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RecognizerBot.Models;
using RecognizerBot.Utils;

namespace RecognizerBot.Services
{
    public class WebSocketListenerService : IHostedService
    {
        private readonly IHostApplicationLifetime _applicationLifetime;

        public WebSocketListenerService(IHostApplicationLifetime applicationLifetime, ILogger<WebSocketListenerService> logger)
        {
            Logger.SetLoggerInstance(logger);
            _applicationLifetime = applicationLifetime;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Run(async () =>
            {
                await WaitForAppStarted();
                ListenOnWebSocket();
            }, cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
        }

        private async Task WaitForAppStarted()
        {
            var tcs = new TaskCompletionSource();
            _applicationLifetime.ApplicationStarted.Register(state => ((TaskCompletionSource)state).SetResult(), tcs);
            await tcs.Task;
        }

        private static async Task ListenOnWebSocket()
        {
            Logger.LogMessage(Logger.MessageType.INFORMATION, "WebSocket Listener starting for port 8080");
            var httpListener = new HttpListener();
            httpListener.Prefixes.Add("http://localhost:8080/");
            httpListener.Start();

            while (true)
            {
                HttpListenerContext httpListenerContext = await httpListener.GetContextAsync();

                if (httpListenerContext.Request.IsWebSocketRequest)
                {
                    WebSocketContext websocketContext;
                    try
                    {
                        websocketContext = await httpListenerContext.AcceptWebSocketAsync(subProtocol: null);
                    }
                    catch (Exception ex)
                    {
                        httpListenerContext.Response.StatusCode = 500;
                        httpListenerContext.Response.Close();
                        Logger.LogMessage(Logger.MessageType.INFORMATION, $"Exception -> {ex}");
                        return;
                    }

                    WebSocket webSocket = websocketContext.WebSocket;

                    var audioStream = new AudioStream();
                    CurrentCall.AudioStream = audioStream;

                    try
                    {
                        string partialData = "";

                        while (webSocket.State is WebSocketState.Open or WebSocketState.CloseSent)
                        {
                            byte[] receiveBuffer = new byte[2048];
                            var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(60)).Token;
                            WebSocketReceiveResult receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), cancellationToken);

                            if (receiveResult.MessageType != WebSocketMessageType.Close)
                            {
                                string data = Encoding.UTF8.GetString(receiveBuffer).TrimEnd('\0');

                                try
                                {
                                    if (receiveResult.EndOfMessage)
                                    {
                                        data = partialData + data;
                                        partialData = "";

                                        if (data != null)
                                        {
                                            var jsonData = JsonSerializer.Deserialize<AudioDataPackets>(data,
                                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                                            if (jsonData is { Kind: "AudioData" })
                                            {
                                                var byteArray = jsonData.AudioData?.Data;
                                                audioStream.Write(byteArray, 0, byteArray.Length);
                                            }

                                            // Logger.LogMessage(Logger.MessageType.INFORMATION, data);
                                        }
                                    }
                                    else
                                    {
                                        partialData += data;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.LogMessage(Logger.MessageType.INFORMATION, $"Exception -> {ex}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogMessage(Logger.MessageType.INFORMATION, $"Exception -> {ex}");
                    }
                    finally
                    {
                        audioStream.Close();
                    }
                }
                else
                {
                    httpListenerContext.Response.StatusCode = 400;
                    httpListenerContext.Response.Close();
                }
            }
        }
    }
}
