using System.Net.WebSockets;
using System.Threading.Channels;
using Azure.Communication.CallAutomation.FHL;
using System.Text;
using OpenAI.RealtimeConversation;
using NAudio.Wave;

#pragma warning disable OPENAI002
namespace CallAutomationOpenAI
{
    public class OutStreamHandler
    {
        private WebSocket m_webSocket;
        private CancellationTokenSource? m_aiClientCts;
        private Channel<Func<Task>> m_channel;
        private CancellationTokenSource m_cts;
        private readonly RealtimeConversationSession m_aiSession;

        public OutStreamHandler(WebSocket webSocket, RealtimeConversationSession aiSession)
        {
            m_webSocket = webSocket;
            m_aiSession = aiSession;
            m_channel = Channel.CreateUnbounded<Func<Task>>(new UnboundedChannelOptions
            {
                SingleReader = true
            });
            m_aiClientCts = new CancellationTokenSource();
            m_cts = new CancellationTokenSource();
            // start dequeue task for new audio packets
            _ = Task.Run(async () => await StartForwardingAudioToWebSocket());
        }

        public void Close()
        {
            m_cts.Cancel();
            m_aiClientCts?.Cancel();
        }

        private async Task StartForwardingAudioToWebSocket()
        {
            try
            {
                // Consume messages from channel and forward buffers to player
                while (true)
                {
                    var processBuffer = await m_channel.Reader.ReadAsync(m_cts.Token).ConfigureAwait(false);
                    await processBuffer.Invoke();
                }
            }
            catch (OperationCanceledException opCanceledException)
            {
                Console.WriteLine($"OperationCanceledException received for StartForwardingAudioToPlayer : {opCanceledException}");
            }
            catch (ObjectDisposedException objDisposedException)
            {
                Console.WriteLine($"ObjectDisposedException received for StartForwardingAudioToPlayer :{objDisposedException}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception received for StartForwardingAudioToPlayer {ex}");
            }
            finally
            {
                m_cts.Dispose();
            }
        }

        public void ReceiveAudioForOutBound(string data)
        {
            try
            {
                m_channel.Writer.TryWrite(async () => await SendMessageAsync(data));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\"Exception received on ReceiveAudioForOutBound {ex}");
            }
        }

        private async Task SendMessageAsync(string message)
        {
            if (m_webSocket?.State == WebSocketState.Open)
            {
                byte[] jsonBytes = Encoding.UTF8.GetBytes(message);

                // Send the PCM audio chunk over WebSocket
                await m_webSocket.SendAsync(new ArraySegment<byte>(jsonBytes), WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);
            }
        }


        // Loop and wait for the AI response
        private async Task GetOpenAiStreamResponseAsync()
        {
            if (m_webSocket == null || m_webSocket.State != WebSocketState.Open)
            {
                throw new InvalidOperationException("WebSocket is not connected.");
            }
            try
            {
                await foreach (ConversationUpdate update in m_aiSession.ReceiveUpdatesAsync(m_cts.Token))
                {
                    if (update is ConversationSessionStartedUpdate sessionStartedUpdate)
                    {
                        Console.WriteLine($"<<< Session started. ID: {sessionStartedUpdate.SessionId}");
                        Console.WriteLine();
                    }

                    if (update is ConversationInputSpeechStartedUpdate speechStartedUpdate)
                    {
                        Console.WriteLine(
                            $"  -- Voice activity detection started at {speechStartedUpdate.AudioStartMs} ms");
                        // Barge-in, received stop audio
                        StopAudio();
                    }

                    if (update is ConversationInputSpeechFinishedUpdate speechFinishedUpdate)
                    {
                        Console.WriteLine(
                            $"  -- Voice activity detection ended at {speechFinishedUpdate.AudioEndMs} ms");
                    }

                    if (update is ConversationItemStartedUpdate itemStartedUpdate)
                    {
                        Console.WriteLine($"  -- Begin streaming of new item");
                  
                    }

                    // Audio transcript delta updates contain the incremental text matching the generated
                    // output audio.
                    if (update is ConversationOutputTranscriptionDeltaUpdate outputTranscriptDeltaUpdate)
                    {
                        Console.Write(outputTranscriptDeltaUpdate.Delta);
                    }

                    // Audio delta updates contain the incremental binary audio data of the generated output
                    // audio, matching the output audio format configured for the session.
                    if (update is ConversationAudioDeltaUpdate audioDeltaUpdate)
                    {
                        ConvertToAcsAudioPacketAndForward(audioDeltaUpdate.Delta.ToArray());
                    }

                    if (update is ConversationFunctionCallArgumentsDeltaUpdate argumentsDeltaUpdate)
                    {
                        Console.Write(argumentsDeltaUpdate.Delta);
                    }

                    if (update is ConversationItemFinishedUpdate itemFinishedUpdate)
                    {
                        Console.WriteLine();
                        Console.WriteLine($"  -- Item streaming finished, response_id={itemFinishedUpdate.ResponseId}");

                    }

                    if (update is ConversationInputTranscriptionFinishedUpdate transcriptionCompletedUpdate)
                    {
                        Console.WriteLine();
                        Console.WriteLine($"  -- User audio transcript: {transcriptionCompletedUpdate.Transcript}");
                        Console.WriteLine();
                    }

                    if (update is ConversationResponseFinishedUpdate turnFinishedUpdate)
                    {
                        Console.WriteLine($"  -- Model turn generation finished. Status: {turnFinishedUpdate.Status}");
                    }

                    if (update is ConversationErrorUpdate errorUpdate)
                    {
                        Console.WriteLine();
                        Console.WriteLine($"ERROR: {errorUpdate.ErrorMessage}");
                        break;
                    }
                }
            }
            catch (OperationCanceledException e)
            {
                Console.WriteLine($"{nameof(OperationCanceledException)} thrown with message: {e.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception during ai streaming -> {ex}");
            }
        }

        private void ConvertToAcsAudioPacketAndForward( byte[] audioData )
        {
            var inFormat = new WaveFormat(24000, 16, 1);
            var outFormat = new WaveFormat(16000, 16, 1);
            var ms = new MemoryStream(audioData);
            var rs = new RawSourceWaveStream(ms, inFormat);
            var resampler = new MediaFoundationResampler(rs, outFormat);
            int chunkSize = 640;
            byte[] buffer = new byte[chunkSize];
            int bytesRead;
            while((bytesRead = resampler.Read(buffer, 0, chunkSize)) == chunkSize)
            {
                // Create a ServerAudioData object for this chunk
                var audio = new ServerStreamingData(ServerMessageType.AudioData)
                {
                    ServerAudioData = new ServerAudioData(buffer)
                };
                // Serialize the JSON object to a string
                string jsonString = System.Text.Json.JsonSerializer.Serialize<ServerStreamingData>(audio);
                
                // queue it to the buffer
                ReceiveAudioForOutBound(jsonString);
            }
        }

        private void StopAudio()
        {
            try
            {
                var jsonObject = new ServerStreamingData(ServerMessageType.StopAudio)
                {
                    StopAudio = new StopAudio()
                };

                // Serialize the JSON object to a string
                string jsonString = System.Text.Json.JsonSerializer.Serialize<ServerStreamingData>(jsonObject);
                
                ReceiveAudioForOutBound(jsonString);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception during streaming -> {ex}");
            }
        }

        public void StartAiAudioReceiver()
        {
            _ = Task.Run(async () => await GetOpenAiStreamResponseAsync());
        }
    }
}