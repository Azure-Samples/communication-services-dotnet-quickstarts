using Microsoft.AspNetCore.Mvc;
using RecordingStreaming.Interfaces;
using RecordingStreaming.Models;
using RecordingStreaming.Services;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text;

namespace RecordingStreaming.Controllers
{
    public class RecordingController : ControllerBase
    {
        private readonly IStorageService _storageService;
        private readonly ITelemetryService _telemetryService;
        private readonly IEventsService _eventsService;
        private readonly ILogger<RecordingController> _logger;

        public RecordingController(IStorageService storageService, ITelemetryService telemetryService,
            IEventsService eventsService, ILogger<RecordingController> logger)
        {
            _storageService = storageService;
            _telemetryService = telemetryService;
            _eventsService = eventsService;
            _logger = logger;
        }

        [Route("/ws")]
        public async Task Get()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await ProcessMessage(webSocket);
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }

        private async Task ProcessMessage(WebSocket webSocket)
        {
            var buffer = new byte[1024 * 2];

            WebSocketReceiveResult? receiveResult = null;
            var activeCall = new ActiveCall
            {
                Stream = new MemoryStream()
            };

            try
            {
                string partialData = "";

                while (webSocket.State is WebSocketState.Open or WebSocketState.CloseSent)
                {
                    byte[] receiveBuffer = new byte[2048];
                    var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;
                    receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), cancellationToken);

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

                                    if (jsonData is { Kind: "AudioMetadata" })
                                    {
                                        _logger.LogInformation($"Audio Metadata: {JsonSerializer.Serialize(jsonData.AudioMetadata)}");
                                        if (CallContextService.MediaSubscriptionIdsToServerCallId.TryGetValue(jsonData.AudioMetadata?.SubscriptionId, out var serverCallId))
                                        {
                                            if (CallContextService.GetActiveCall(serverCallId)?.Stream != null)
                                            {
                                                _logger.LogInformation($"This stream is already being processed.  Ending this websocket connection.");
                                                return;
                                            }
                                            else
                                            {
                                                activeCall.SubscriptionId = jsonData.AudioMetadata?.SubscriptionId;
                                                activeCall = CallContextService.SetActiveCall(serverCallId, activeCall);
                                            }
                                        }
                                    }

                                    if (jsonData is { Kind: "AudioData" })
                                    {
                                        if (activeCall.StartRecordingTimer?.IsRunning ?? false)
                                        {
                                            var elapsedTime = activeCall.StartRecordingTimer.ElapsedMilliseconds;
                                            activeCall.StartRecordingTimer.Stop();
                                            _logger.LogInformation($"*******RECORDING STARTED elapsed milliseconds: {elapsedTime}  *******");
                                            await _telemetryService.LogLatenciesAsync(new[]
                                            {
                                                new LatencyRecord
                                                {
                                                    action_type = "StartRecording",
                                                    env = "Prod",
                                                    region = "USWest",
                                                    value = elapsedTime,
                                                    scenario = "RecordingMidCall",
                                                    call_id = activeCall.CallId
                                                }
                                            });
                                        }

                                        if (activeCall.StartRecordingWithAnswerTimer?.IsRunning ?? false)
                                        {
                                            var elapsedTime = activeCall.StartRecordingWithAnswerTimer.ElapsedMilliseconds;
                                            activeCall.StartRecordingWithAnswerTimer.Stop();
                                            _logger.LogInformation($"*******RECORDING STARTED WITH ANSWER elapsed milliseconds: {elapsedTime}  *******");
                                            await _telemetryService.LogLatenciesAsync(new[]
                                            {
                                                new LatencyRecord
                                                {
                                                    action_type = "StartRecordingWithAnswer",
                                                    env = "Prod",
                                                    region = "USWest",
                                                    value = elapsedTime,
                                                    scenario = "RecordingWithAnswer",
                                                    call_id = activeCall.CallId
                                                }
                                            });
                                        }

                                        var byteArray = jsonData.AudioData?.Data;
                                        await activeCall.Stream.WriteAsync(byteArray, 0, byteArray.Length, cancellationToken);
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
                            _logger.LogInformation($"Exception -> {ex}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Exception -> {ex}");
            }
            finally
            {
                if (activeCall.StopRecordingTimer?.IsRunning ?? false)
                {
                    // Takes 10 seconds for the Cancellation token to timeout after media stream is stopped
                    var elapsedTime = activeCall.StopRecordingTimer.ElapsedMilliseconds - 10000;
                    activeCall.StopRecordingTimer.Stop();
                    _logger.LogInformation($"*******RECORDING STOPPED elapsed milliseconds: {elapsedTime}  *******");
                    await _telemetryService.LogLatenciesAsync(new[]
                    {
                        new LatencyRecord
                        {
                            action_type = "StopRecording",
                            env = "Prod",
                            region = "USWest",
                            value = elapsedTime,
                            scenario = "RecordingMidCall",
                            call_id = activeCall.CallId
                        }
                    });
                }

                var blobUri = await _storageService.StreamTo(activeCall.Stream, DateTimeOffset.UtcNow.ToString("yyyy-MM-dd_H-mm-ss.wav"));
                _logger.LogInformation($"Audio stream saved to {blobUri}");
                // Send Recording EventGridEvent
                // await _eventsService.SendRecordingStatusUpdatedEvent();
                activeCall.Stream.Close();

                await webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    receiveResult.CloseStatusDescription,
                    CancellationToken.None);
            }
        }
    }
}
