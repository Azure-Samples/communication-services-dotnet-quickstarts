using System.Net.WebSockets;
using System.Threading.Channels;
using OpenAI.RealtimeConversation;
using Azure.AI.OpenAI;
using System.ClientModel;
using Azure.Communication.CallAutomation;
using CliWrap.Builders;
using System.Text.Json;

#pragma warning disable OPENAI002
namespace CallAutomationOpenAI
{
    public class JobOfferParameters
    {
        public string CandidateId { get; set; }
        public string JobOfferId { get; set; }
    }

    public class AzureOpenAIService
    {
        private WebSocket m_webSocket;
        private CancellationTokenSource m_cts;
        private RealtimeConversationSession m_aiSession;
        private AcsMediaStreamingHandler m_mediaStreaming;
        private MemoryStream m_memoryStream;
        private string m_answerPromptSystemTemplate = "You are an AI assistant that helps people find information.";

        public AzureOpenAIService(AcsMediaStreamingHandler mediaStreaming, IConfiguration configuration)
        {
            m_mediaStreaming = mediaStreaming;
            m_cts = new CancellationTokenSource();
            m_aiSession = CreateAISessionAsync(configuration).GetAwaiter().GetResult();
            m_memoryStream = new MemoryStream();
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
            var session = await RealtimeCovnClient.StartConversationSessionAsync();

            // Session options control connection-wide behavior shared across all conversations,
            // including audio input format and voice activity detection settings.
            ConversationSessionOptions sessionOptions = new()
            {
                Instructions = systemPrompt,
                Voice = ConversationVoice.Alloy,
                Tools = { AcceptJobOfferTool() },
                InputAudioFormat = ConversationAudioFormat.Pcm16,
                OutputAudioFormat = ConversationAudioFormat.Pcm16,
                InputTranscriptionOptions = new()
                {
                    Model = "whisper-1",
                },
                TurnDetectionOptions = ConversationTurnDetectionOptions.CreateServerVoiceActivityTurnDetectionOptions(0.5f, TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(500)),
            };

            await session.ConfigureSessionAsync(sessionOptions);
            // get user profile here
            await session.AddItemAsync(
                ConversationItem.CreateUserMessage([GetUserProfileJson(), GetUserJobOfferJson()]));
            return session;
        }

        // Loop and wait for the AI response
        private async Task GetOpenAiStreamResponseAsync()
        {
            try
            {
                await m_aiSession.StartResponseAsync();
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
                            $"  -- Voice activity detection started at {speechStartedUpdate.AudioStartTime} ms");
                        // Barge-in, send stop audio
                        var jsonString = OutStreamingData.GetStopAudioForOutbound();
                        await m_mediaStreaming.SendMessageAsync(jsonString);
                    }

                    if (update is ConversationInputSpeechFinishedUpdate speechFinishedUpdate)
                    {
                        Console.WriteLine(
                            $"  -- Voice activity detection ended at {speechFinishedUpdate.AudioEndTime} ms");
                    }

                    // Item finished updates arrive when all streamed data for an item has arrived and the
                    // accumulated results are available. In the case of function calls, this is the point
                    // where all arguments are expected to be present.
                    if (update is ConversationItemStreamingFinishedUpdate itemFinishedUpdate)
                    {
                        Console.WriteLine();

                        if (itemFinishedUpdate.FunctionCallId is not null)
                        {
                            Console.WriteLine($"    + Responding to tool invoked by item: {itemFinishedUpdate.FunctionName}");
                            string parameters = itemFinishedUpdate.FunctionCallArguments;
                            string functionName = itemFinishedUpdate.FunctionName;
                            string toolOutput = string.Empty;
                            switch (functionName)
                            {
                                case "accept_job_offer":
                                    Console.WriteLine($"    + Tool parameters: {parameters}");
                                    // Set up JsonSerializerOptions to ignore case
                                    var options = new JsonSerializerOptions
                                    {
                                        PropertyNameCaseInsensitive = true
                                    };

                                    // Deserialize the JSON string into a JobOfferParameters object
                                    JobOfferParameters jobOfferParameters = JsonSerializer.Deserialize<JobOfferParameters>(parameters, options);
                                    // Extract the values
                                    string candidateId = jobOfferParameters.CandidateId;
                                    string jobOfferId = jobOfferParameters.JobOfferId;
                                    toolOutput = AcceptJobOffer(candidateId, jobOfferId);
                                    Console.WriteLine($"    + Tool parameters: {parameters}");
                                    break;
                                default:
                                    Console.WriteLine($"    + Tool parameters: {parameters}");
                                    break;
                            }

                            ConversationItem functionOutputItem = ConversationItem.CreateFunctionCallOutput(
                                callId: itemFinishedUpdate.FunctionCallId,
                                output: toolOutput);

                            await m_aiSession.AddItemAsync(functionOutputItem);
                        }
                        else if (itemFinishedUpdate.MessageContentParts?.Count > 0)
                        {
                            Console.Write($"    + [{itemFinishedUpdate.MessageRole}]: ");
                            foreach (ConversationContentPart contentPart in itemFinishedUpdate.MessageContentParts)
                            {
                                Console.Write(contentPart.AudioTranscript);
                            }
                            Console.WriteLine();
                        }
                        Console.WriteLine($"  -- Item streaming finished, response_id={itemFinishedUpdate.ResponseId}");
                    }


                    if (update is ConversationItemStreamingStartedUpdate itemStartedUpdate)
                    {
                        Console.WriteLine($"  -- Begin streaming of new item");
                        if (!string.IsNullOrEmpty(itemStartedUpdate.FunctionName))
                        {
                            Console.Write($"    {itemStartedUpdate.FunctionName}: ");
                        }
                    }

                    // Audio transcript  updates contain the incremental text matching the generated
                    // output audio.
                    if (update is ConversationItemStreamingAudioTranscriptionFinishedUpdate outputTranscriptDeltaUpdate)
                    {
                        Console.Write(outputTranscriptDeltaUpdate.Transcript);
                    }

                    // Audio delta updates contain the incremental binary audio data of the generated output
                    // audio, matching the output audio format configured for the session.
                    if (update is ConversationItemStreamingPartDeltaUpdate deltaUpdate)
                    {
                        if (deltaUpdate.AudioBytes != null)
                        {
                            Console.Write(deltaUpdate.FunctionArguments);
                            var jsonString = OutStreamingData.GetAudioDataForOutbound(deltaUpdate.AudioBytes.ToArray());
                            await m_mediaStreaming.SendMessageAsync(jsonString);
                        }
                    }

                    if (update is ConversationItemStreamingTextFinishedUpdate itemFinishedTextUpdate)
                    {
                        Console.WriteLine();
                        Console.WriteLine($"  -- Item streaming finished, response_id={itemFinishedTextUpdate.ResponseId}");
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
                            await m_aiSession.StartResponseAsync();
                        }
                    }

                    if (update is ConversationErrorUpdate errorUpdate)
                    {
                        Console.WriteLine();
                        Console.WriteLine($"ERROR: {errorUpdate.Message}");
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

        public void StartConversation()
        {
            _ = Task.Run(async () => await GetOpenAiStreamResponseAsync());
        }

        public async Task SendAudioToExternalAI(MemoryStream memoryStream)
        {
            await m_aiSession.SendInputAudioAsync(memoryStream);
        }

        public void Close()
        {
            m_cts.Cancel();
            m_cts.Dispose();
            m_aiSession.Dispose();
        }

        private static ConversationFunctionTool AcceptJobOfferTool()
        {
            return new ConversationFunctionTool()
            {
                Name = "accept_job_offer",
                Description = "This tool accepts a job offer for the candidate id and job id. The user can aslo say 'I want to apply for the job'. Please repeat the job offer details back to the user.",
                Parameters = BinaryData.FromString("""
            {
              "type": "object",
              "properties": {
                "candidateId": {
                  "type": "string",
                  "description": "Candidate Id from the user profile"
                },
                "jobOfferId": {
                  "type": "string",
                  "description": "this is the job offer id from the job offer"
                }
              },
              "required": ["candidateId","jobOfferId"]
            }
            """)
            };
        }

        private string GetUserProfileJson()
        {
            return "{'candidateId': '4711', 'name':'Joe Smith', 'email':'joe.smith@contoso.com','phone': '555-555-5555', 'address': '123 Main St, Seattle, WA 98101'}}";
        }

        private string GetUserJobOfferJson()
        {
            return "{'jobOfferId': '4711-4711', 'jobTitle':'Software Engineer','company':'Tech Solutions Inc.','location':'Seattle, WA','employmentType':'Full-time','jobDescription':'We are looking for a skilled Software Engineer to join our dynamic team. The ideal candidate will have experience in developing high-quality software solutions and a passion for technology.','responsibilities':['Design, develop, and maintain software applications','Collaborate with cross-functional teams to define and implement new features','Write clean, scalable, and efficient code','Perform code reviews and provide constructive feedback','Troubleshoot and debug software issues','Stay up-to-date with the latest industry trends and technologies'],'qualifications':['Bachelor\'s degree in Computer Science or related field','3+ years of experience in software development','Proficiency in C#, .NET, and JavaScript','Experience with cloud platforms such as Azure or AWS','Strong problem-solving skills','Excellent communication and teamwork abilities'],'benefits':['Competitive salary','Health, dental, and vision insurance','401(k) with company match','Flexible work hours','Remote work options','Professional development opportunities'],'applicationInstructions':'To apply, please send your resume and cover letter to careers@techsolutions.com.'}";
        }

        private static string AcceptJobOffer(string candidateId, string jobOfferId)
        {
            Console.WriteLine($"accepting the job offer for candidate {candidateId} with job offer id {jobOfferId}");
            return $"You have successfully accepted the job offer with id {jobOfferId} for candidate {candidateId}.";
        }

    }
}