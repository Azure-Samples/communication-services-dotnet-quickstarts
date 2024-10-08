using Azure.AI.OpenAI;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech;
using System.Net.WebSockets;
using Azure;
using System.Threading.Channels;
using Azure.Communication.CallAutomation.FHL;
using System.Text;
using System.ClientModel;
using OpenAI.RealtimeConversation;
using NAudio.Wave;
using System.Reflection.PortableExecutable;
using Newtonsoft.Json.Linq;

#pragma warning disable OPENAI002
namespace CallAutomationOpenAI
{
    public class OutStreamHandler
    {
        private WebSocket m_webSocket;
        private readonly AzureOpenAIClient m_aiClient;
        private CancellationTokenSource? m_aiClientCts;
        private Channel<Func<Task>> m_channel;
        private string m_openAiModelName;
        private CancellationTokenSource m_cts;
        private readonly object m_readTaskLock = new object();
        public OutStreamHandler(WebSocket webSocket, string openAiUri, string openAiKey, string openAiModelName)
        {
            m_webSocket = webSocket;
            m_aiClient = new AzureOpenAIClient(new Uri(openAiUri), new ApiKeyCredential(openAiKey));
            m_openAiModelName = openAiModelName;
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

        private async Task GetOpenAiStreamResponseAsync(string initialInstructions)
        {
            if (m_webSocket == null || m_webSocket.State != WebSocketState.Open)
            {
                throw new InvalidOperationException("WebSocket is not connected.");
            }
            try
            {
                var RealtimeCovnClient = m_aiClient.GetRealtimeConversationClient(m_openAiModelName);
                using RealtimeConversationSession session = await RealtimeCovnClient.StartConversationSessionAsync();

                // Session options control connection-wide behavior shared across all conversations,
                // including audio input format and voice activity detection settings.
                ConversationSessionOptions sessionOptions = new()
                {
                    Instructions = initialInstructions,
                    Voice = ConversationVoice.Alloy,
                    //Tools = {  },
                    InputAudioFormat = ConversationAudioFormat.Pcm16,
                    OutputAudioFormat = ConversationAudioFormat.Pcm16,
                    InputTranscriptionOptions = new()
                    {
                        Model = "whisper-1",
                    },
        //            TurnDetectionOptions = CreateServerVoiceActivityTurnDetectionOptions(500, 
                            //float ? detectionThreshold = null,
                            //TimeSpan ? prefixPaddingDuration = null,
                            //TimeSpan ? silenceDuration = null)()
        //            {
                        
        //            }
                };

                await foreach (ConversationUpdate update in session.ReceiveUpdatesAsync())
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
                    }

                    if (update is ConversationInputSpeechFinishedUpdate speechFinishedUpdate)
                    {
                        Console.WriteLine(
                            $"  -- Voice activity detection ended at {speechFinishedUpdate.AudioEndMs} ms");
                    }

                    // Item started updates notify that the model generation process will insert a new item into
                    // the conversation and begin streaming its content via content updates.
                    if (update is ConversationItemStartedUpdate itemStartedUpdate)
                    {
                        Console.WriteLine($"  -- Begin streaming of new item");
                        if (!string.IsNullOrEmpty(itemStartedUpdate.FunctionName))
                        {
                            Console.Write($"    {itemStartedUpdate.FunctionName}: ");
                        }
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

                    // Item finished updates arrive when all streamed data for an item has arrived and the
                    // accumulated results are available. In the case of function calls, this is the point
                    // where all arguments are expected to be present.
                    if (update is ConversationItemFinishedUpdate itemFinishedUpdate)
                    {
                        Console.WriteLine();
                        Console.WriteLine($"  -- Item streaming finished, response_id={itemFinishedUpdate.ResponseId}");

                        if (itemFinishedUpdate.FunctionCallId is not null)
                        {
                            Console.WriteLine($"    + Responding to tool invoked by item: {itemFinishedUpdate.FunctionName}");
                            ConversationItem functionOutputItem = ConversationItem.CreateFunctionCallOutput(
                                callId: itemFinishedUpdate.FunctionCallId,
                                output: "70 degrees Fahrenheit and sunny");
                            await session.AddItemAsync(functionOutputItem);
                        }
                        else if (itemFinishedUpdate.MessageContentParts?.Count > 0)
                        {
                            Console.Write($"    + [{itemFinishedUpdate.MessageRole}]: ");
                            foreach (ConversationContentPart contentPart in itemFinishedUpdate.MessageContentParts)
                            {
                                Console.Write(contentPart.AudioTranscriptValue);
                            }
                            Console.WriteLine();
                        }
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

                        // Here, if we processed tool calls in the course of the model turn, we finish the
                        // client turn to resume model generation. The next model turn will reflect the tool
                        // responses that were already provided.
                        if (turnFinishedUpdate.CreatedItems.Any(item => item.FunctionName?.Length > 0))
                        {
                            Console.WriteLine($"  -- Ending client turn for pending tool responses");
                            await session.StartResponseTurnAsync();
                        }
                        else
                        {
                            break;
                        }
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
            finally
            {

                m_aiClientCts?.Dispose();
                m_aiClientCts = null;
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
            while((bytesRead = resampler.Read(buffer, 0, chunkSize)) < chunkSize)
            {
                var audioChunk = new byte[chunkSize];
                Array.Copy(audioData, 0, audioChunk, 0, chunkSize);

                // Create a ServerAudioData object for this chunk
                var audio = new ServerStreamingData(ServerMessageType.AudioData)
                {
                    ServerAudioData = new ServerAudioData(audioChunk)
                };
                // Serialize the JSON object to a string
                string jsonString = System.Text.Json.JsonSerializer.Serialize<ServerStreamingData>(audio);
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
        public void SendInitialLearning(string userPrompt, string systemPrompt)
        {
           
            _ = Task.Run(async () => await GetOpenAiStreamResponseAsync(systemPrompt));
        }
    }
}