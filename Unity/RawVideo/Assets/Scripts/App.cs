using Azure.Communication.Calling.UnityClient;
using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;


public class App : MonoBehaviour
{
    private CallClient callClient;
    private CallAgent callAgent;
    private CommunicationCall call;
    private RawOutgoingVideoStream rawOutgoingVideoStream;

    public GameObject startCallButton;
    public OutgoingVideo outgoingVideoPlayer;
    public IncomingVideo incomingVideoPlayer;

    void Start()
    {
        callClient = new CallClient();
        Task.Run(async () => { 
                var callAgentOptions = new CallAgentOptions() {
                    DisplayName = $"{Environment.MachineName}/{Environment.UserName}",
                };

                var tokenCredential = new CallTokenCredential("<<Replace with auth token>>");
                callAgent = await callClient.CreateCallAgent(tokenCredential, callAgentOptions);
            });
    }

    public void OnStartCall()
    {
        Task.Run(async () =>
        {
            rawOutgoingVideoStream = new VirtualOutgoingVideoStream(new RawOutgoingVideoStreamOptions {
                    Formats = new VideoStreamFormat[] { 
                        new VideoStreamFormat {
                            PixelFormat = VideoStreamPixelFormat.Rgba,
                            Resolution = VideoStreamResolution.P360,
                            Stride1 = 640 * 4,
                            FramesPerSecond = 15
                        }
                    }
                }
            );

            rawOutgoingVideoStream.StateChanged += OnRawVideoStreamStateChanged;

            var startCallOptions = new StartCallOptions() {
                OutgoingAudioOptions = new OutgoingAudioOptions {
                    IsMuted = true //Audio is not the focus of this sample
                },
                IncomingAudioOptions = new IncomingAudioOptions {
                    IsMuted = true //Audio is not the focus of this sample
                },
                OutgoingVideoOptions = new OutgoingVideoOptions() {
                    Streams = new OutgoingVideoStream[] { rawOutgoingVideoStream }
                },
                IncomingVideoOptions = new IncomingVideoOptions {
                    StreamKind = VideoStreamKind.RawIncoming,
                    FrameKind = RawVideoFrameKind.Buffer
                }
            };

            call = await callAgent.StartCallAsync(new CallIdentifier[] { 
                new UserCallIdentifier("<<Replace with callee id>>") }, 
                startCallOptions);
            
            call.StateChanged += OnStateChanged;
            call.RemoteParticipantsUpdated += OnRemoteParticipantsUpdatedAsync;
        });
    }

    #region ACS

    private void OnStateChanged(object sender, PropertyChangedEventArgs args)
    {
        var call = (CommunicationCall)sender;
        if (call.State == CallState.Connected) {
            var remoteParticipant = call.RemoteParticipants.First();
            OnRawIncomingVideoStreamStateChanged(remoteParticipant.IncomingVideoStreams.First());
        }
    }

    private async void OnRemoteParticipantsUpdatedAsync(object sender, ParticipantsUpdatedEventArgs args)
    {
        foreach (var participant in args.RemovedParticipants)
        {
            foreach (var incomingVideoStream in participant.IncomingVideoStreams)
            {
                var remoteVideoStream = incomingVideoStream as RemoteIncomingVideoStream;
                if (remoteVideoStream != null)
                {
                    await remoteVideoStream.StopPreviewAsync();
                }
            }
            // Tear down the event handler on the departing participant
            participant.VideoStreamStateChanged -= OnRawVideoStreamStateChanged;
        }

        foreach (var participant in args.AddedParticipants)
        {
            participant.VideoStreamStateChanged += OnRawVideoStreamStateChanged;
        }
    }

    private void OnRawVideoStreamStateChanged(object sender, VideoStreamStateChangedEventArgs e)
    {
        CallVideoStream callVideoStream = e.Stream;

        switch (callVideoStream.Direction)
        {
            case StreamDirection.Outgoing:
                OnRawOutgoingVideoStreamStateChanged(callVideoStream as OutgoingVideoStream);
                break;
            case StreamDirection.Incoming:
                OnRawIncomingVideoStreamStateChanged(callVideoStream as IncomingVideoStream);
                break;
        }
    }

    private async void OnRawOutgoingVideoStreamStateChanged(OutgoingVideoStream outgoingVideoStream)
    {
        switch (outgoingVideoStream.State)
        {
            case VideoStreamState.Started:
                switch (outgoingVideoStream.Kind)
                {
                    case VideoStreamKind.VirtualOutgoing:
                        outgoingVideoPlayer.StartGenerateFrames(outgoingVideoStream);
                        break;
                    case VideoStreamKind.ScreenShareOutgoing:
                        break;
                }
                break;

            case VideoStreamState.Stopped:
                switch (outgoingVideoStream.Kind)
                {
                    case VideoStreamKind.VirtualOutgoing:
                        //videoFrameSender?.Stop();
                        break;
                    case VideoStreamKind.ScreenShareOutgoing:
                        break;
                }
                break;
        }
    }

    private void OnRawIncomingVideoStreamStateChanged(IncomingVideoStream incomingVideoStream)
    {
        switch (incomingVideoStream.State)
        {
            case VideoStreamState.Available:
                {
                    var rawIncomingVideoStream = incomingVideoStream as RawIncomingVideoStream;
                    rawIncomingVideoStream.RawVideoFrameReceived += OnRawVideoFrameReceived;
                    rawIncomingVideoStream.Start();
                    break;
                }
            case VideoStreamState.Stopped:
                break;
            case VideoStreamState.NotAvailable:
                break;
        }
    }

    private void OnRawVideoFrameReceived(object sender, RawVideoFrameReceivedEventArgs e)
    {
        incomingVideoPlayer.RenderRawVideoFrame(e.Frame);
    }
    #endregion
}
