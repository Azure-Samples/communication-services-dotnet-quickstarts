using System.ComponentModel.DataAnnotations;
using Azure.Communication.CallAutomation;
using Azure.Communication.Media;
using Azure.Communication.Media.Tests;
using Azure.Identity;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using CallAutomation_AzOpenAI_Voice.Models;
using CallAutomation_AzOpenAI_Voice.Services;
using CallAutomationOpenAI;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

// CLI mode: encode/decode GUID URL strings and exit
if (args.Length >= 2 && args[0].Equals("decode", StringComparison.OrdinalIgnoreCase))
{
    var encoded = args[1];
    if (encoded.TryDecodeUrlString(out var guid))
    {
        Console.WriteLine(guid);
    }
    else
    {
        Console.Error.WriteLine($"Failed to decode '{encoded}' as a GUID: invalid URL-safe base64.");
    }
    return;
}

if (args.Length >= 2 && args[0].Equals("decode-server-call-id", StringComparison.OrdinalIgnoreCase))
{
    try
    {
        var url = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(args[1]));
        Console.WriteLine($"URL:  {url}");
        var guid = Helper.ExtractMediaSessionId(args[1]);
        Console.WriteLine($"GUID: {guid}");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Failed to decode serverCallId: {ex.Message}");
    }
    return;
}

if (args.Length >= 2 && args[0].Equals("encode", StringComparison.OrdinalIgnoreCase))
{
    if (Guid.TryParse(args[1], out var guid))
    {
        Console.WriteLine(guid.ToEncodedUrlString());
    }
    else
    {
        Console.Error.WriteLine($"Failed to parse '{args[1]}' as a GUID.");
    }
    return;
}

var builder = WebApplication.CreateBuilder(args);

// Log unhandled exceptions to stdout for App Service diagnostics
AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
{
    Console.Error.WriteLine($"UNHANDLED EXCEPTION: {e.ExceptionObject}");
};

// Add Razor Pages
builder.Services.AddRazorPages();

// Register clients as transient — created fresh per request
builder.Services.AddTransient<MediaClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connStr = config.GetValue<string>("MediaSDKConnectionString");
    if (string.IsNullOrEmpty(connStr) || connStr.StartsWith("<"))
        throw new InvalidOperationException("MediaSDKConnectionString is not configured. Set it in App Service Application Settings.");
    var options = new MediaClientOptions("dev", false, TimeSpan.FromSeconds(10));
    return new MediaClient(connStr, options);
});

builder.Services.AddTransient<CallAutomationClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connStr = config.GetValue<string>("AcsConnectionString");
    if (string.IsNullOrEmpty(connStr) || connStr.StartsWith("<"))
        throw new InvalidOperationException("AcsConnectionString is not configured. Set it in App Service Application Settings.");
    return new CallAutomationClient(connStr);
});

// Register CallSessionManager as singleton
builder.Services.AddSingleton<CallSessionManager>();

// Register IConfiguration explicitly (already available, but make it clear)
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

var app = builder.Build();

// Enable EventSource error reporting (non-fatal if native SDK not available)
System.Diagnostics.Tracing.EventSource.SetCurrentThreadActivityId(Guid.NewGuid());
MediaSdkEventListener? listener = null;
try
{
    listener = new MediaSdkEventListener();
}
catch (Exception ex)
{
    Console.WriteLine($"MediaSdkEventListener init skipped: {ex.Message}");
}

var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();

var appBaseUrl = builder.Configuration.GetValue<string>("AppBaseUrl");

// Return plain JSON errors instead of HTML error pages
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = 500;
        var error = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var message = error?.Error?.Message ?? "Internal server error";
        startupLogger.LogError(error?.Error, "Unhandled exception");
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new { error = message }));
    });
});

app.UseStaticFiles();
app.UseWebSockets();
app.UseRouting();
app.MapRazorPages();

app.MapGet("/api/health", (CallSessionManager sessionManager) =>
{
    return Results.Ok(new
    {
        status = "healthy",
        activeCalls = sessionManager.ActiveCallCount,
        totalCalls = sessionManager.TotalCallCount,
        timestamp = DateTime.UtcNow
    });
});

// Polling API for active calls (replaces SSE — more reliable through IIS)
app.MapGet("/api/calls/active", (CallSessionManager sessionManager) =>
{
    var activeSessions = sessionManager.GetActiveSessions().Select(s => new
    {
        s.CorrelationId,
        s.CallConnectionId,
        s.CallerId,
        Status = s.Status.ToString(),
        Duration = s.Duration.ToString(@"hh\:mm\:ss"),
        s.StartTime,
        s.BotEndpointId,
        s.MediaSessionId,
        s.ResourceId,
        MediaSdkStatus = s.RoomConnector?.MediaConnectionState,
        ServiceOrigin = s.RoomConnector?.ServiceOrigin,
        MediaSdkSent = s.MediaHandler?.MediaPacketsSent ?? 0,
        MediaSdkRecv = s.RoomConnector?.MediaPacketsReceived ?? 0,
        OpenAISent = s.AiService?.OpenAIPacketsSent ?? 0,
        OpenAIRecv = s.AiService?.OpenAIPacketsReceived ?? 0,
        RemoteAudioStreams = s.RoomConnector?.RemoteAudioStreamCount ?? 0,
        RemoteDataStreams = s.RoomConnector?.RemoteDataStreamCount ?? 0,
        DataDropped = s.RoomConnector?.DataPacketsDropped ?? 0,
        DataMessagesReceived = s.RoomConnector?.DataMessagesReceived ?? 0,
        DataMessages = s.RoomConnector?.DataMessages.Select(dm => new
        {
            dm.Timestamp,
            dm.StreamId,
            dm.PayloadType,
            dm.DataLength,
            Content = dm.Content.Length > 500 ? dm.Content[..500] + "..." : dm.Content
        }) ?? Enumerable.Empty<object>(),
        RemoteStreams = s.RoomConnector?.RemoteStreams.Select(rs => new
        {
            rs.StreamId,
            rs.EndpointId,
            rs.StreamType,
            rs.LastState,
            LastStateAt = rs.LastStateAt?.ToString("HH:mm:ss")
        }) ?? Enumerable.Empty<object>(),
        StreamStateLog = s.RoomConnector?.StreamStateLog ?? Array.Empty<string>()
    });
    return Results.Ok(new { activeSessions, total = sessionManager.TotalCallCount });
});

// Polling API for transcript of a specific call
app.MapGet("/api/calls/{correlationId}/transcript", (
    string correlationId,
    int? since,
    CallSessionManager sessionManager) =>
{
    var session = sessionManager.GetSession(correlationId);
    if (session == null)
    {
        // Check completed sessions
        var completed = sessionManager.GetCompletedSessions()
            .FirstOrDefault(s => s.CorrelationId == correlationId);
        if (completed == null)
            return Results.NotFound(new { ended = true });
        var allEntries = completed.GetTranscriptSnapshot().Select(t => new
        {
            t.Speaker,
            t.Text,
            Timestamp = t.Timestamp.ToString("HH:mm:ss")
        });
        return Results.Ok(new { entries = allEntries, ended = true, total = allEntries.Count() });
    }

    var transcript = session.GetTranscriptSnapshot();
    var startIndex = since ?? 0;
    var newEntries = transcript.Skip(startIndex).Select(t => new
    {
        t.Speaker,
        t.Text,
        Timestamp = t.Timestamp.ToString("HH:mm:ss")
    });
    return Results.Ok(new { entries = newEntries, ended = false, total = transcript.Count });
});

// Polling API for data channel messages of a specific call
app.MapGet("/api/calls/{correlationId}/data-messages", (
    string correlationId,
    int? since,
    CallSessionManager sessionManager) =>
{
    var session = sessionManager.GetSession(correlationId);
    if (session == null)
        return Results.NotFound(new { error = "Call session not found" });

    var messages = session.RoomConnector?.DataMessages ?? Array.Empty<DataMessage>();
    var startIndex = since ?? 0;
    var newMessages = messages.Skip(startIndex).Select(dm => new
    {
        dm.StreamId,
        dm.PayloadType,
        dm.DataLength,
        dm.Content,
        Timestamp = dm.Timestamp.ToString("HH:mm:ss.fff")
    });
    return Results.Ok(new { messages = newMessages, total = messages.Count });
});

// Test endpoint: send whats_the_weather.wav to Media SDK (outgoing audio stream)
app.MapPost("/api/calls/{correlationId}/hangup", async (
    string correlationId,
    CallSessionManager sessionManager,
    CallAutomationClient acsClient,
    ILogger<Program> logger) =>
{
    var session = sessionManager.GetSession(correlationId);
    if (session == null)
        return Results.NotFound(new { error = "Call session not found" });

    // Hang up via Call Automation if this is a real call (not manual test)
    if (!string.IsNullOrEmpty(session.CallConnectionId))
    {
        try
        {
            await acsClient.GetCallConnection(session.CallConnectionId).HangUpAsync(true);
            logger.LogInformation("Call Automation hang up sent for call {Id}", correlationId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Call Automation hang up failed for {Id}, proceeding with local cleanup", correlationId);
        }
    }

    logger.LogInformation("Hang up requested for call {Id}", correlationId);
    sessionManager.EndSession(correlationId);
    return Results.Ok(new { message = "Call ended" });
});

// Test endpoint: send whats_the_weather.wav to Media SDK (outgoing audio stream)
app.MapPost("/api/calls/{correlationId}/test/media", async (
    string correlationId,
    CallSessionManager sessionManager,
    ILogger<Program> logger) =>
{
    var session = sessionManager.GetSession(correlationId);
    if (session?.MediaHandler == null)
        return Results.NotFound(new { error = "Call session or media handler not found" });

    var wavPath = Path.Combine(AppContext.BaseDirectory, "whats_the_weather.wav");
    if (!File.Exists(wavPath))
        return Results.NotFound(new { error = "whats_the_weather.wav not found" });

    // Strip WAV header — OutgoingAudioStream.Write() expects raw PCM samples
    using var fileStream = File.OpenRead(wavPath);
    using var reader = new BinaryReader(fileStream);
    reader.ReadBytes(12); // RIFF header
    while (fileStream.Position < fileStream.Length)
    {
        var chunkId = new string(reader.ReadChars(4));
        var chunkSize = reader.ReadInt32();
        if (chunkId == "data")
            break;
        reader.ReadBytes(chunkSize);
    }
    var pcmData = reader.ReadBytes((int)(fileStream.Length - fileStream.Position));
    await session.MediaHandler.SendMessageAsync(pcmData);

    logger.LogInformation("Sent test audio to Media SDK for call {Id}", correlationId);
    session.AddTranscript("System", "Sent test audio (whats_the_weather.wav) to Media SDK");
    return Results.Ok(new { message = "Audio sent to Media SDK" });
});

// Test endpoint: send whats_the_weather.wav to OpenAI service
app.MapPost("/api/calls/{correlationId}/test/openai", async (
    string correlationId,
    CallSessionManager sessionManager,
    ILogger<Program> logger) =>
{
    var session = sessionManager.GetSession(correlationId);
    if (session?.AiService == null)
        return Results.NotFound(new { error = "Call session or AI service not found" });

    var wavPath = Path.Combine(AppContext.BaseDirectory, "whats_the_weather.wav");
    if (!File.Exists(wavPath))
        return Results.NotFound(new { error = "whats_the_weather.wav not found" });

    // Strip WAV header — OpenAI Realtime expects raw PCM16 samples, not a WAV file
    using var fileStream = File.OpenRead(wavPath);
    using var reader = new BinaryReader(fileStream);
    // Read RIFF header to find "data" chunk and skip to raw PCM
    reader.ReadBytes(12); // RIFF header (12 bytes)
    while (fileStream.Position < fileStream.Length)
    {
        var chunkId = new string(reader.ReadChars(4));
        var chunkSize = reader.ReadInt32();
        if (chunkId == "data")
            break;
        reader.ReadBytes(chunkSize); // skip non-data chunks (fmt, etc.)
    }
    // Remaining stream is raw PCM data
    await session.AiService.SendAudioToExternalAI(fileStream);

    // Send 3 seconds of silence (PCM16 @ 24kHz) so the VAD can detect end of speech.
    // Without this, the VAD waits forever for more audio to confirm silence.
    var silenceBytes = new byte[24000 * 2 * 3]; // 3s of 24kHz 16-bit mono silence
    await session.AiService.SendAudioToExternalAI(new MemoryStream(silenceBytes));

    logger.LogInformation("Sent test audio to OpenAI for call {Id}", correlationId);
    session.AddTranscript("System", "Sent test audio (whats_the_weather.wav) to OpenAI");
    return Results.Ok(new { message = "Audio sent to OpenAI" });
});

// Manual test call: connect Media SDK + OpenAI using a conversationId (media session GUID)
app.MapPost("/api/calls/manual", async (
    [FromBody] ManualCallRequest request,
    ILogger<Program> logger,
    CallSessionManager sessionManager) =>
{
    if (string.IsNullOrWhiteSpace(request.ConversationId))
        return Results.BadRequest(new { error = "conversationId is required" });

    var mediaSessionId = request.ConversationId.Trim();
    var correlationId = $"manual-{Guid.NewGuid()}";

    logger.LogInformation("Manual call requested with conversationId: {ConversationId}", mediaSessionId);

    var session = sessionManager.CreateSession(correlationId, "", "manual-test");
    session.MediaSessionId = mediaSessionId;

    var mediaConnStr = app.Configuration.GetValue<string>("MediaSDKConnectionString");
    if (string.IsNullOrEmpty(mediaConnStr) || mediaConnStr.StartsWith("<"))
        return Results.Json(new { error = "MediaSDKConnectionString is not configured." }, statusCode: 500);

    var roomConnector = new TestCallRoomConnector(mediaConnStr, logger, app.Services);
    session.RoomConnector = roomConnector;

    // Handle server-side disconnects (e.g., SessionExpired)
    roomConnector.OnDisconnected += (reason) =>
    {
        logger.LogWarning("MediaSDK disconnected for call {Id}: {Reason}", correlationId, reason);
        sessionManager.FailSession(correlationId, $"MediaSDK disconnected: {reason}");
    };

    // Set up OpenAI and media handler BEFORE connecting, so aiServiceHandler
    // is available when incoming audio streams arrive immediately after join.
    try
    {
        var mediaHandler = new AcsMediaStreamingHandler(roomConnector, logger);
        session.MediaHandler = mediaHandler;

        if (!request.DisableOpenAI)
        {
            var openAIService = new AzureOpenAIService(mediaHandler, app.Configuration);
            openAIService.OnTranscript = (speaker, text) => session.AddTranscript(speaker, text);
            session.AiService = openAIService;

            mediaHandler.aiServiceHandler = openAIService;
            roomConnector.aiServiceHandler = openAIService;
        }
        else
        {
            logger.LogInformation("OpenAI disabled for manual call {Id}", correlationId);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to initialize AI service for manual call {Id}", correlationId);
        sessionManager.FailSession(correlationId, ex.Message);
        return Results.Json(new { error = ex.Message }, statusCode: 500);
    }

    var connected = await roomConnector.ConnectAsync(mediaSessionId);
    if (!connected)
    {
        logger.LogError("MediaSDK connection failed for manual call: {Error}", roomConnector.LastError);
        sessionManager.FailSession(correlationId, roomConnector.LastError ?? "MediaSDK connection failed");
        return Results.Json(new { error = roomConnector.LastError ?? "MediaSDK connection failed" }, statusCode: 500);
    }

    session.BotEndpointId = roomConnector.EndpointId;
    session.ResourceId = roomConnector.ResourceId;
    logger.LogInformation("MediaSDK connected for manual call {Id}", correlationId);

    if (session.AiService != null)
    {
        session.AiService.StartConversation();
        logger.LogInformation("AI service started for manual call {Id}", correlationId);
    }

    session.Status = CallStatus.Active;

    return Results.Ok(new { correlationId, message = request.DisableOpenAI ? "Manual call started (OpenAI disabled)" : "Manual call started", mediaSessionId });
});

app.MapPost("/api/incomingCall", async (
    [FromBody] EventGridEvent[] eventGridEvents,
    ILogger<Program> logger,
    CallSessionManager sessionManager,
    CallAutomationClient acsClient) =>
{
    var mediaConnStr = app.Configuration.GetValue<string>("MediaSDKConnectionString");
    if (string.IsNullOrEmpty(mediaConnStr) || mediaConnStr.StartsWith("<"))
    {
        logger.LogError("MediaSDKConnectionString is not configured.");
        return Results.Json(new { error = "MediaSDKConnectionString is not configured." }, statusCode: 500);
    }

    foreach (var eventGridEvent in eventGridEvents)
    {
        try
        {
            logger.LogInformation("Incoming Call event received.");

            // Handle system events
            if (eventGridEvent.TryGetSystemEventData(out object eventData))
            {
                if (eventData is SubscriptionValidationEventData subscriptionValidationEventData)
                {
                    var responseData = new SubscriptionValidationResponse
                    {
                        ValidationResponse = subscriptionValidationEventData.ValidationCode
                    };
                    return Results.Ok(responseData);
                }
            }

            var jsonObject = Helper.GetJsonObject(eventGridEvent.Data);
            logger.LogInformation("Incoming call JSON: {Json}", jsonObject.ToJsonString());
            var callerId = Helper.GetCallerId(jsonObject);
            var incomingCallContext = Helper.GetIncomingCallContext(jsonObject);
            var serverCallId = Helper.GetServerCallId(jsonObject);
            var mediaSessionId = Helper.ExtractMediaSessionId(serverCallId);
            logger.LogInformation("ServerCallId: {ServerCallId}, MediaSessionId: {MediaSessionId}", serverCallId, mediaSessionId);
            var callbackUri = new Uri(new Uri(appBaseUrl), $"/api/callbacks/{Guid.NewGuid()}?callerId={callerId}");
            logger.LogInformation("Callback Url: {CallbackUri}", callbackUri);
            var websocketUri = appBaseUrl.TrimEnd('/').Replace("https", "wss") + "/ws";
            logger.LogInformation("WebSocket Url: {WsUri}", websocketUri);

            var mediaStreamingOptions = new MediaStreamingOptions(MediaStreamingAudioChannel.Mixed)
            {
                TransportUri = new Uri(websocketUri),
                MediaStreamingContent = MediaStreamingContent.Audio,
                StartMediaStreaming = true,
                EnableBidirectional = true,
                AudioFormat = AudioFormat.Pcm24KMono
            };

            var options = new AnswerCallOptions(incomingCallContext, callbackUri)
            {
                MediaStreamingOptions = mediaStreamingOptions,
            };

            AnswerCallResult answerCallResult = await acsClient.AnswerCallAsync(options);
            var connectionId = answerCallResult.CallConnection.CallConnectionId;
            var correlationId = answerCallResult.CallConnectionProperties.CorrelationId;
            logger.LogInformation("Answered call for connection id: {Id}", connectionId);

            // Create per-call session (keyed by correlationId)
            var session = sessionManager.CreateSession(correlationId, connectionId, callerId);
            session.ServerCallId = serverCallId;
            session.MediaSessionId = mediaSessionId;

            // Connect via Media SDK
            var roomConnector = new TestCallRoomConnector(mediaConnStr, logger, app.Services);
            session.RoomConnector = roomConnector;

            // Handle server-side disconnects (e.g., SessionExpired)
            roomConnector.OnDisconnected += (reason) =>
            {
                logger.LogWarning("MediaSDK disconnected for call {Id}: {Reason}", correlationId, reason);
                sessionManager.FailSession(correlationId, $"MediaSDK disconnected: {reason}");
            };

            // Set up OpenAI and media handler BEFORE connecting, so aiServiceHandler
            // is available when incoming audio streams arrive immediately after join.
            try
            {
                var mediaHandler = new AcsMediaStreamingHandler(roomConnector, logger);
                session.MediaHandler = mediaHandler;

                var openAIService = new AzureOpenAIService(mediaHandler, builder.Configuration);
                openAIService.OnTranscript = (speaker, text) => session.AddTranscript(speaker, text);
                session.AiService = openAIService;

                mediaHandler.aiServiceHandler = openAIService;
                roomConnector.aiServiceHandler = openAIService;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize AI service for call {Id}", correlationId);
                sessionManager.FailSession(correlationId, ex.Message);
                continue;
            }

            var connected = await roomConnector.ConnectAsync(mediaSessionId);
            if (connected)
            {
                session.BotEndpointId = roomConnector.EndpointId;
                session.ResourceId = roomConnector.ResourceId;
                logger.LogInformation("MediaSDK connected for call {Id}", correlationId);

                session.AiService.StartConversation();
                session.Status = CallStatus.Active;
                logger.LogInformation("AI service started for call {Id}", correlationId);
            }
            else
            {
                logger.LogError("MediaSDK connection failed for call {Id}: {Error}", correlationId, roomConnector.LastError);
                sessionManager.FailSession(correlationId, roomConnector.LastError ?? "MediaSDK connection failed");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled error processing incoming call event");
        }
    }
    return Results.Ok();
});

// Handle call automation callback events
app.MapPost("/api/callbacks/{contextId}", async (
    [FromBody] CloudEvent[] cloudEvents,
    [FromRoute] string contextId,
    [Required] string callerId,
    ILogger<Program> logger,
    CallSessionManager sessionManager) =>
{
    foreach (var cloudEvent in cloudEvents)
    {
        CallAutomationEventBase @event = CallAutomationEventParser.Parse(cloudEvent);
        logger.LogInformation("Callback event received: {Type}", @event.GetType().Name);

        // Clean up session when call disconnects (lookup by CallConnectionId from ACS event)
        if (@event is CallDisconnected disconnected)
        {
            logger.LogInformation("Call disconnected: {Id}", disconnected.CallConnectionId);
            sessionManager.EndSessionByCallConnectionId(disconnected.CallConnectionId);
        }
    }

    return Results.Ok();
});

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            try
            {
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception received {ex}");
            }
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
    else
    {
        await next(context);
    }
});

app.Run();            
