using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Communication.Media;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using static System.Net.WebRequestMethods;

public class TestCallRoomConnector
{
    private readonly MediaClient _mediaClient;
    private readonly ILogger _logger;
    public string _sessionId { get; set;  }
    private readonly IServiceProvider _serviceProvider;
    private bool _connected;
    public string? LastError { get; private set; }
    public OutgoingAudioStream OutgoingAudioStream { get; private set; }

    public TestCallRoomConnector(MediaClient mediaClient, ILogger logger, IServiceProvider serviceProvider)
    {
        _mediaClient = mediaClient;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _sessionId = $"room";
    }

    public async Task<bool> ConnectAsync()
    {
        try
        {
            string serviceOrigin = _mediaClient.ServiceOrigin;
            _logger.LogInformation($"Service Origin URL: {serviceOrigin}");

            // This is our connection to a WB endpoint.
            var _connection = await _mediaClient.CreateMediaConnectionAsync();

            _logger.LogInformation($"MediaConnection {_connection.ConnectionState} {_connection.EndpointId}");

            // We have two sources of events: connection itself (for state change events, stats reports), and the session (for media/data events).
            _connection.OnStateChanged += OnConnectionStateChanged;
            _connection.OnStatsReportReceived += OnStatsReportReceived;

            // Now that connection is ready, we can use it to join specific sessions.
            var _session = await _connection.JoinAsync(
                sessionId: _sessionId,
                mediaSessionJoinOptions: new MediaSessionJoinOptions() { IncomingDataPayloadTypes = [5] });

            _session.OnIncomingAudioStreamAdded += OnIncomingAudioStreamAdded;
            _session.OnIncomingAudioStreamRemoved += OnIncomingAudioStreamRemoved;
            _session.OnIncomingDataStreamAdded += OnIncomingDataStreamAdded;
            _session.OnIncomingDataStreamRemoved += OnIncomingDataStreamRemoved;

            await Task.Delay(500);
            OutgoingAudioStream = _session.AddOutgoingAudioStream();
            var options = new OutgoingDataStreamOptions(5, TransmissionMode.RealTime);
            _session.AddOutgoingDataStream(options);

            _logger.LogInformation($"MediaConnection {_connection.ConnectionState}");
            // If you need to start media streaming, do it here
            // var mediaService = new AcsMediaStreamingHandler(_session, ...);
            // await mediaService.ProcessConnectionAsync();

            _connected = true;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error connecting to room: {ex.Message}");
            LastError = ex.Message;
            _connected = false;
            return false;
        }
    }

    private void OnConnectionStateChanged(object? sender, ConnectionStateChangedEventArgs e)
    {
        Console.WriteLine("OnStateChanged received");
        // Handle state changed
        switch (e.ConnectionState)
        {
            case var state when state == ConnectionState.Idle:
                Console.WriteLine("OnStateChanged.Idle event received");
                break;
            case var state when state == ConnectionState.Connecting:
                Console.WriteLine("OnStateChanged.Connecting event received");
                break;
            case var state when state == ConnectionState.Connected:
                Console.WriteLine("OnStateChanged.Connected event received");
                break;
            case var state when state == ConnectionState.Failover:
                Console.WriteLine("OnStateChanged.Failover event received");
                break;
            case var state when state == ConnectionState.Disconnected:
                Console.WriteLine("OnStateChanged.Disconnected event received");
                break;
            default:
                throw new NotImplementedException();
        }
    }

    private void OnIncomingAudioStreamAdded(object? sender, IncomingAudioStreamAddedEventArgs e)
    {
        var incomingAudioStream = e.IncomingAudioStream;
        Console.WriteLine($"OnIncomingAudioStreamAdded - StreamId({incomingAudioStream.Id}) sender {e.IncomingAudioStream.EndpointId}");

        incomingAudioStream.OnIncomingAudioStreamReceived += OnIncomingAudioStreamReceived;
    }

    private void OnIncomingAudioStreamReceived(object? sender, IncomingAudioStreamReceivedEventArgs args)
    {
        Console.WriteLine($"OnIncomingAudioStreamReceived - StreamId({args.Id}) : {args.Data.ReadDataAsSpan().Length}");
    }

    private void OnIncomingAudioStreamRemoved(object? sender, IncomingAudioStreamRemovedEventArgs e)
    {
        var incomingAudioStream = e.IncomingAudioStream;

        incomingAudioStream.OnIncomingAudioStreamReceived -= OnIncomingAudioStreamReceived;

        Console.WriteLine($"OnIncomingAudioStreamRemoved - StreamId({incomingAudioStream.Id})");

        e.IncomingAudioStream.Dispose();
    }

    internal virtual void OnIncomingDataStreamAdded(object? sender, IncomingDataStreamAddedEventArgs e)
    {
        var incomingDataStream = e.IncomingDataStream;

        incomingDataStream.OnIncomingDataStreamReceived -= OnIncomingDataStreamReceived;
        incomingDataStream.OnIncomingDataDropped -= OnIncomingDataDropped;

        Console.WriteLine($"OnIncomingDataStreamAdded - StreamId({incomingDataStream.Id}), PayloadType({incomingDataStream.PayloadType})");

        incomingDataStream.OnIncomingDataStreamReceived += OnIncomingDataStreamReceived;
        incomingDataStream.OnIncomingDataDropped += OnIncomingDataDropped;
    }

    private static void OnIncomingDataStreamReceived(object? sender, IncomingDataStreamReceivedEventArgs e)
    {
        //Console.WriteLine($"OnIncomingDataStreamReceived - StreamId({e.Id}) : {Encoding.UTF8.GetString(e.Data.ReadDataAsSpan())}");
    }

    private static void OnIncomingDataDropped(object? sender, IncomingDataDroppedEventArgs e)
    {
        Console.WriteLine($"OnIncomingDataDropped - Packet Drop Count( {e.DroppedCount} - InboundId({e.MessageId})");
    }

    private void OnIncomingDataStreamRemoved(object? sender, IncomingDataStreamRemovedEventArgs e)
    {
        var incomingDataStream = e.IncomingDataStream;
        Console.WriteLine($"OnIncomingDataStreamRemoved - StreamId({incomingDataStream.Id})");
    }

    private void OnStatsReportReceived(object? sender, StatsReportReceivedEventArgs e)
    {
         Console.WriteLine($"On Stats Reports Received... {e.StatsReport.IsConnected} {e.StatsReport.Data}");
    }

    public bool IsConnected => _connected;
}
