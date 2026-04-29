using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Azure.Communication.Media;
using CallAutomationOpenAI;
using Microsoft.Extensions.Logging;

public class RemoteStreamInfo
{
    public long StreamId { get; set; }
    public string EndpointId { get; set; } = string.Empty;
    public string StreamType { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public string? LastState { get; set; }
    public DateTime? LastStateAt { get; set; }
    public object? SdkStream { get; set; }
}

public class DataMessage
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public long StreamId { get; set; }
    public uint PayloadType { get; set; }
    public int DataLength { get; set; }
    public string Content { get; set; } = string.Empty;
}

public class TestCallRoomConnector
{
    private MediaClient? _mediaClient;
    private readonly string _connectionString;
    private readonly ILogger _logger;
    private readonly IServiceProvider _serviceProvider;
    private bool _connected;
    private MediaConnection? _connection;
    private MediaSession? _session;
    public string? LastError { get; private set; }
    public string? EndpointId => _connection?.EndpointId;
    public string? ResourceId => _connection?.ResourceId;
    public string? MediaConnectionState => _connection?.ConnectionState.ToString();
    public string? ServiceOrigin { get; private set; }
    public long MediaPacketsSent => _mediaPacketsSent;
    public long MediaPacketsReceived => _mediaPacketsReceived;
    public long DataPacketsDropped => _dataPacketsDropped;
    private long _mediaPacketsSent;
    private long _mediaPacketsReceived;
    private long _dataPacketsDropped;
    private readonly ConcurrentDictionary<long, RemoteStreamInfo> _remoteStreams = new();
    private readonly ConcurrentQueue<string> _streamStateLog = new();
    private readonly ConcurrentQueue<DataMessage> _dataMessages = new();
    public IReadOnlyCollection<RemoteStreamInfo> RemoteStreams => _remoteStreams.Values.ToList().AsReadOnly();
    public IReadOnlyCollection<string> StreamStateLog => _streamStateLog.ToArray();
    public IReadOnlyList<DataMessage> DataMessages => _dataMessages.ToArray();
    public long DataMessagesReceived => _dataMessagesReceived;
    private long _dataMessagesReceived;
    public int RemoteAudioStreamCount => _remoteStreams.Values.Count(s => s.StreamType == "Audio");
    public int RemoteDataStreamCount => _remoteStreams.Values.Count(s => s.StreamType == "Data");
    public OutgoingAudioStream OutgoingAudioStream { get; private set; }
    public AzureOpenAIService aiServiceHandler { get; set; }
    private Channel<byte[]>? _audioForwardChannel;
    private Task? _audioForwardTask;

    public TestCallRoomConnector(string connectionString, ILogger logger, IServiceProvider serviceProvider)
    {
        _connectionString = connectionString;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task<bool> ConnectAsync(string sessionId)
    {
        try
        {

            // Initialize bounded channel for audio forwarding (avoids thread pool saturation)
            _audioForwardChannel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(200)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            });
            _audioForwardTask = Task.Run(ProcessAudioForwardChannelAsync);

            // Create a fresh MediaClient per call for isolation.
            _mediaClient = new MediaClient(_connectionString,
                new MediaClientOptions("dev", false, TimeSpan.FromSeconds(10)));

            ServiceOrigin = _mediaClient.ServiceOrigin;
            _logger.LogInformation("Service Origin URL: {ServiceOrigin}", ServiceOrigin);

            _connection = await _mediaClient.CreateMediaConnectionAsync(new MediaConnectionOptions());

            _logger.LogInformation("MediaConnection {State} {EndpointId}", _connection.ConnectionState, _connection.EndpointId);

            _connection.StateChanged += OnConnectionStateChanged;
            _connection.StatsReportReceived += OnStatsReportReceived;

            _session = _connection.CreateMediaSession(
                sessionId: sessionId,
                new MediaSessionOptions(new HashSet<uint> { 2,3,24 }));

            _session.IncomingAudioStreamAdded += OnIncomingAudioStreamAdded;
            _session.IncomingAudioStreamRemoved += OnIncomingAudioStreamRemoved;
            _session.IncomingDataStreamAdded += OnIncomingDataStreamAdded;
            _session.IncomingDataStreamRemoved += OnIncomingDataStreamRemoved;

            await _session.JoinAsync();

            OutgoingAudioStream = _session.AddOutgoingAudioStream();

            _logger.LogInformation("MediaConnection {State}", _connection.ConnectionState);

            _connected = true;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to room");
            LastError = ex.Message;
            _connected = false;
            return false;
        }
    }

    public void Disconnect()
    {
        _connected = false;
        _audioForwardChannel?.Writer.TryComplete();
        try { _audioForwardTask?.Wait(TimeSpan.FromSeconds(2)); } catch { }
        try { _session?.Dispose(); } catch { }
        try { _connection?.Dispose(); } catch { }
        try { _mediaClient?.Dispose(); } catch { }
        _session = null;
        _connection = null;
        _mediaClient = null;
        _remoteStreams.Clear();
        _logger.LogInformation("MediaSDK disconnected");
    }

    public event Action<string>? OnDisconnected;

    private void OnConnectionStateChanged(object? sender, ConnectionStateChangedEventArgs e)
    {
        _logger.LogInformation("ConnectionStateChanged: {State}", e.ConnectionState);

        if (e.ConnectionState.ToString().Equals("Disconnected", StringComparison.OrdinalIgnoreCase))
        {
            _connected = false;
            _audioForwardChannel?.Writer.TryComplete();
            _logger.LogWarning("MediaSDK connection lost (server-side disconnect)");
            OnDisconnected?.Invoke("Disconnected");
        }

        ThreadPool.GetAvailableThreads(out int workerThreads, out int completionPortThreads);
        ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxCompletionPortThreads);
        _logger.LogInformation("ThreadPool: Workers={Available}/{Max}, IO={IOAvailable}/{IOMax}",
            workerThreads, maxWorkerThreads, completionPortThreads, maxCompletionPortThreads);
    }

    private void OnIncomingAudioStreamAdded(object? sender, IncomingAudioStreamAddedEventArgs e)
    {
        var stream = e.IncomingAudioStream;
        _logger.LogInformation("IncomingAudioStreamAdded - StreamId({StreamId}) EndpointId({EndpointId})", stream.Id, stream.EndpointId);

        _remoteStreams[stream.Id] = new RemoteStreamInfo
        {
            StreamId = stream.Id,
            EndpointId = stream.EndpointId ?? "",
            StreamType = "Audio",
            SdkStream = stream
        };

        stream.IncomingAudioStreamReceived += OnIncomingAudioStreamReceived;
    }

    private void OnIncomingAudioStreamReceived(object? sender, IncomingAudioStreamReceivedEventArgs args)
    {
        var count = Interlocked.Increment(ref _mediaPacketsReceived);
        var dataLen = args.Data?.ReadDataAsSpan().Length ?? 0;
        if (count <= 10 || count % 100 == 1)
            _logger.LogInformation("MediaPacketsReceived: {Count}, DataLen: {DataLen}, ComfortNoise: {ComfortNoise}", count, dataLen, args.ComfortNoise);

        if (dataLen == 0)
            return;

        if (!_connected || aiServiceHandler == null || _audioForwardChannel == null)
        {
            if (count <= 5)
                _logger.LogWarning("Cannot forward audio to OpenAI: connected={Connected}, aiHandler={HasHandler}", _connected, aiServiceHandler != null);
            return;
        }

        var audioData = args.Data.ReadDataAsSpan().ToArray();
        if (!_audioForwardChannel.Writer.TryWrite(audioData))
        {
            // Channel full — drop oldest packets to avoid back-pressure buildup
            if (count % 500 == 0)
                _logger.LogWarning("Audio forward channel full, dropping packet {Count}", count);
        }
    }

    private async Task ProcessAudioForwardChannelAsync()
    {
        try
        {
            await foreach (var audioData in _audioForwardChannel!.Reader.ReadAllAsync())
            {
                var handler = aiServiceHandler;
                if (!_connected || handler == null)
                    continue;

                try
                {
                    await handler.SendAudioToExternalAI(new MemoryStream(audioData));
                }
                catch (Exception ex)
                {
                    if (_connected)
                        _logger.LogError(ex, "Error sending audio to OpenAI");
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Audio forward channel processing error");
        }
    }

    private void OnIncomingAudioStreamRemoved(object? sender, IncomingAudioStreamRemovedEventArgs e)
    {
        var stream = e.IncomingAudioStream;
        stream.IncomingAudioStreamReceived -= OnIncomingAudioStreamReceived;
        _remoteStreams.TryRemove(stream.Id, out _);
        _logger.LogInformation("IncomingAudioStreamRemoved - StreamId({StreamId})", stream.Id);
        stream.Dispose();
    }

    internal virtual void OnIncomingDataStreamAdded(object? sender, IncomingDataStreamAddedEventArgs e)
    {
        var stream = e.IncomingDataStream;
        _logger.LogInformation("IncomingDataStreamAdded - StreamId({StreamId}), PayloadType({PayloadType})", stream.Id, stream.PayloadType);

        _remoteStreams[stream.Id] = new RemoteStreamInfo
        {
            StreamId = stream.Id,
            EndpointId = $"PayloadType:{stream.PayloadType}",
            StreamType = "Data",
            SdkStream = stream
        };

        stream.IncomingDataStreamReceived += OnIncomingDataStreamReceived;
        stream.IncomingDataDropped += OnIncomingDataDropped;
    }

    private void OnIncomingDataStreamReceived(object? sender, IncomingDataStreamReceivedEventArgs e)
    {
        Interlocked.Increment(ref _dataMessagesReceived);
        try
        {
            var data = e.Data?.ReadDataAsSpan().ToArray();
            var content = data != null && data.Length > 0
                ? System.Text.Encoding.UTF8.GetString(data)
                : string.Empty;

            var streamId = (sender as IncomingDataStream)?.Id ?? 0;
            var payloadType = (sender as IncomingDataStream)?.PayloadType ?? 0;

            var msg = new DataMessage
            {
                StreamId = streamId,
                PayloadType = payloadType,
                DataLength = data?.Length ?? 0,
                Content = content
            };
            _dataMessages.Enqueue(msg);

            // Keep max 500 messages
            while (_dataMessages.Count > 500)
                _dataMessages.TryDequeue(out _);

            _logger.LogInformation("DataMessage received: StreamId={StreamId}, PayloadType={PayloadType}, Len={Len}, Content={Content}",
                streamId, payloadType, msg.DataLength, content.Length > 200 ? content[..200] + "..." : content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing incoming data message");
        }
    }

    private void OnIncomingDataDropped(object? sender, IncomingDataDroppedEventArgs e)
    {
        Interlocked.Increment(ref _dataPacketsDropped);
        _logger.LogWarning("IncomingDataDropped - DroppedCount({DroppedCount}) InboundId({MessageId})", e.DroppedCount, e.MessageId);
    }

    private void OnIncomingDataStreamRemoved(object? sender, IncomingDataStreamRemovedEventArgs e)
    {
        _remoteStreams.TryRemove(e.IncomingDataStream.Id, out _);
        _logger.LogInformation("IncomingDataStreamRemoved - StreamId({StreamId})", e.IncomingDataStream.Id);
    }

    private void OnStatsReportReceived(object? sender, StatsReportReceivedEventArgs e)
    {
        var r = e.StatsReport;
        var audioIn = r.Audio?.Inbound;
        var audioOut = r.Audio?.Outbound;

        _logger.LogInformation(
            "StatsReport: IsConnected={IsConnected} " +
            "AudioIn(Pkts={InPkts}, Bytes={InBytes}, Lost={InLost}) " +
            "AudioOut(Pkts={OutPkts}, Bytes={OutBytes}, Lost={OutLost})",
            r.IsConnected,
            audioIn?.Aggregate?.Packets ?? 0, audioIn?.Aggregate?.Bytes ?? 0, audioIn?.Aggregate?.PacketsLost ?? 0,
            audioOut?.Aggregate?.Packets ?? 0, audioOut?.Aggregate?.Bytes ?? 0, audioOut?.Aggregate?.PacketsLost ?? 0);
    }

    public bool IsConnected => _connected;
}
