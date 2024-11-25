using System.Net.WebSockets;
using System.Threading.Channels;
using OpenAI.RealtimeConversation;
using Azure.AI.OpenAI;
using System.ClientModel;
using Azure.Communication.CallAutomation;

#pragma warning disable OPENAI002
namespace CallAutomationOpenAI
{
    public class AzureOpenAIService
    {
        private WebSocket m_webSocket;
        private Channel<Func<Task>> m_channel;
        private CancellationTokenSource m_cts;
        private RealtimeConversationSession m_aiSession;
        private AcsMediaStreamingHandler m_mediaStreaming;
        private MemoryStream m_memoryStream;


        private string m_answerPromptSystemTemplate = """ 
    You're an AI assistant for an elevator company called Contoso Elevators. Customers will contact you as the first point of contact when having issues with their elevators. 
    Your priority is to ensure the person contacting you or anyone else in or around the elevator is safe, if not then they should contact their local authorities.
    If everyone is safe then ask the user for information about the elevators location, such as city, building and elevator number.
    Also get the users name and number so that a technician who goes onsite can contact this person. Confirm with the user all the information 
    they've shared that it's all correct and then let them know that you've created a ticket and that a technician should be onsite within the next 24 to 48 hours.
    """;

        public AzureOpenAIService(AcsMediaStreamingHandler mediaStreaming, IConfiguration configuration)
        {            
            m_mediaStreaming = mediaStreaming;
            m_channel = Channel.CreateUnbounded<Func<Task>>(new UnboundedChannelOptions
            {
                SingleReader = true
            });
            m_cts = new CancellationTokenSource();
            m_aiSession =  CreateAISessionAsync(configuration).GetAwaiter().GetResult();
            m_memoryStream = new MemoryStream();
            // start dequeue task for new audio packets
            _ = Task.Run(async () => await StartForwardingAudioToMediaStreaming());
        }


        private async Task<RealtimeConversationSession> CreateAISessionAsync(IConfiguration configuration)
        {
            var openAiKey = configuration.GetValue<string>("AzureOpenAIServiceKey");
            ArgumentNullException.ThrowIfNullOrEmpty(openAiKey);

            var openAiUri = configuration.GetValue<string>("AzureOpenAIServiceEndpoint");
            ArgumentNullException.ThrowIfNullOrEmpty(openAiUri);

            var openAiModelName = configuration.GetValue<string>("AzureOpenAIDeploymentModelName");
            ArgumentNullException.ThrowIfNullOrEmpty(openAiModelName);
            var systemPrompt = configuration.GetValue<string>("SystemPrompt") ?? m_answerPromptSystemTemplate;
            ArgumentNullException.ThrowIfNullOrEmpty(openAiUri);

            var aiClient = new AzureOpenAIClient(new Uri(openAiUri), new ApiKeyCredential(openAiKey));
            var RealtimeCovnClient = aiClient.GetRealtimeConversationClient(openAiModelName);
            var session =  await RealtimeCovnClient.StartConversationSessionAsync();

            // Session options control connection-wide behavior shared across all conversations,
            // including audio input format and voice activity detection settings.
            ConversationSessionOptions sessionOptions = new()
            {
                Instructions = systemPrompt,
                Voice = ConversationVoice.Alloy,
                InputAudioFormat = ConversationAudioFormat.Pcm16,
                OutputAudioFormat = ConversationAudioFormat.Pcm16,
                InputTranscriptionOptions = new()
                {
                    Model = "whisper-1",
                },
                TurnDetectionOptions = ConversationTurnDetectionOptions.CreateServerVoiceActivityTurnDetectionOptions(0.5f, TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(500)),
            };

            await session.ConfigureSessionAsync(sessionOptions);
            
            return session;
        }

        private async Task StartForwardingAudioToMediaStreaming()
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
        }

        public void ReceiveAudioForOutBound(string data)
        {
            try
            {
                m_channel.Writer.TryWrite(async () => await m_mediaStreaming.SendMessageAsync(data));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\"Exception received on ReceiveAudioForOutBound {ex}");
            }
        }


        // Loop and wait for the AI response
        private async Task GetOpenAiStreamResponseAsync()
        {
            try
            {
                await m_aiSession.StartResponseTurnAsync();
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

        private void ConvertToAcsAudioPacketAndForward(byte[] audioData)
        {
            var jsonString = OutStreamingData.GetAudioDataForOutbound(audioData);

            // queue it to the buffer
            ReceiveAudioForOutBound(jsonString);
        }

        private void StopAudio()
        {
            try
            {
                var jsonString = OutStreamingData.GetStopAudioForOutbound();
                ReceiveAudioForOutBound(jsonString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception during streaming -> {ex}");
            }
        }

        public void StartConversation()
        {
            _ = Task.Run(async () => await GetOpenAiStreamResponseAsync());
        }

        public async Task SendAudioToExternalAI(MemoryStream memoryStream)
        {
            await m_aiSession.SendAudioAsync(memoryStream);
        }

        public void Close()
        {
            m_cts.Cancel();
            m_cts.Dispose();
            m_aiSession.Dispose();
        }
    }
}