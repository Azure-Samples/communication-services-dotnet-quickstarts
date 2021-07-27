// <Create a chat client>
using Azure;
using Azure.Communication;
using Azure.Communication.Chat;
using Azure.Communication.Identity;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Azure.Storage.Queues;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatQuickstart
{
    class Program
    {
        class Config
        {
            public static Config PROD = new Config("PROD");
            public static Config INT = new Config("INT");

            private Config(string prefix)
            {
                this.prefix = prefix;
            }
            readonly string prefix;
            public string ACSResourceEndpoint => Environment.GetEnvironmentVariable($"{prefix}_ACS_RESOURCE_ENDPOINT");
            public string ACSResourceConnectionString => Environment.GetEnvironmentVariable($"{prefix}_ACS_RESOURCE_CONNECTION_STRING");
            public string StorageQueueConnectionString => Environment.GetEnvironmentVariable($"{prefix}_STORAGEQUEUE_CONNECTION_STRING");
            public string StorageQueueName => Environment.GetEnvironmentVariable($"{prefix}_STORAGEQUEUE_NAME");
        }

        static async Task Main(string[] args)
        {
            var config = Config.INT;
            uint participantCount = 2;
            Uri endpoint = new Uri(config.ACSResourceEndpoint);

            var storageQueueClient = new QueueClient(config.StorageQueueConnectionString, config.StorageQueueName);
            Console.WriteLine($"Using storage queue {config.StorageQueueName}");
            Console.WriteLine($"Clearing queue of approx {storageQueueClient.GetPropertiesAsync().Result.Value.ApproximateMessagesCount} messages");
            storageQueueClient.ClearMessages();

            var identityClient = new CommunicationIdentityClient(config.ACSResourceConnectionString);

            var chatParticipants = await CreateChatParticipants(identityClient, config, participantCount);
            var sender = chatParticipants.First();
            var senderToken = await identityClient.GetTokenAsync(sender.User as CommunicationUserIdentifier, new[] { CommunicationTokenScope.Chat });

            ChatClient chatClient = new ChatClient(endpoint, new CommunicationTokenCredential(senderToken.Value.Token));

            // <Start a chat thread>
            Console.WriteLine($"Creating new thread");
            CreateChatThreadResult createChatThreadResult = await chatClient.CreateChatThreadAsync(topic: "Hello world!", participants: chatParticipants);
            Console.WriteLine($"> Topic: {createChatThreadResult.ChatThread.Topic}, ID: { createChatThreadResult.ChatThread.Id}");

            // <Get a chat thread client>
            ChatThreadClient chatThreadClient = chatClient.GetChatThreadClient(threadId: createChatThreadResult.ChatThread.Id);

            // <Send a message to a chat thread>
            SendChatMessageResult sendChatMessageResult = await chatThreadClient.SendMessageAsync(new SendChatMessageOptions() { Content = string.Empty, Metadata = { { "contentType", "image/jpeg" }, { "fileName", "cat.jpg" } }, MessageType = ChatMessageType.Text, SenderDisplayName = sender.DisplayName});
            string sentMessageId = sendChatMessageResult.Id;

            // <Receive chat messages from a chat thread>
            Console.WriteLine($"Getting messages for thread");
            AsyncPageable<ChatMessage> allMessages = chatThreadClient.GetMessagesAsync();
            await foreach (ChatMessage message in allMessages)
            {
                Console.WriteLine($"> {message.SenderDisplayName}[{message.Id}]:{message.Content.Message}");
            }

            // Verify messages made it to StorageQueue
            Console.WriteLine($"Checking storage queue for sent message");
            var timeout = Task.Delay(TimeSpan.FromSeconds(20));

            while (!timeout.IsCompleted)
            {
                var messages = (await storageQueueClient.ReceiveMessagesAsync(10)).Value;
                if (messages.Length > 0)
                {
                    foreach (var message in messages)
                    {
                        var bodyBytes = Convert.FromBase64String(message.Body.ToString());
                        var body = System.Text.Encoding.Default.GetString(bodyBytes);

                        var parsedEvent = EventGridEvent.Parse(BinaryData.FromBytes(bodyBytes));
                        switch (parsedEvent.EventType)
                        {
                            case "Microsoft.Communication.ChatMessageReceived":
                                var chatMessageReceived = parsedEvent.Data.ToObjectFromJson<AcsChatMessageReceivedEventData>();
                                if (chatMessageReceived.MessageId == sentMessageId)
                                {
                                    Console.WriteLine($"> {parsedEvent.EventType}, Matching message {sentMessageId}, message={chatMessageReceived.MessageBody}, metadata={MetadataToString(chatMessageReceived.Metadata)}");
                                    break;
                                }
                                goto default;
                            case "Microsoft.Communication.ChatMessageReceivedInThread":
                                var chatMessageReceivedInThread = parsedEvent.Data.ToObjectFromJson<AcsChatMessageReceivedInThreadEventData>();
                                if (chatMessageReceivedInThread.MessageId == sentMessageId)
                                {
                                    Console.WriteLine($"> {parsedEvent.EventType}, Matching message {sentMessageId}, message={chatMessageReceivedInThread.MessageBody}, metadata={MetadataToString(chatMessageReceivedInThread.Metadata)}");
                                    break;
                                }
                                goto default;
                            default:
                                Console.WriteLine($"> {parsedEvent.EventType} (No Match)");
                                break;
                        }

                        await storageQueueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt);
                    }
                }
                else
                {
                    Console.WriteLine($"> No messages received, waiting.");
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }

            await Cleanup(chatParticipants, identityClient);
        }

        private static string MetadataToString(IReadOnlyDictionary<string, string> metadata)
        {
            var metadatas = metadata.Select(kvp => $"{{{kvp.Key},{kvp.Value}}}");
            return $"{string.Join(",", metadatas)}";
        }

        private static async Task<IList<ChatParticipant>> CreateChatParticipants(CommunicationIdentityClient identityClient, Config config, uint count = 1)
        {
            var chatParticipants = new List<ChatParticipant>();

            Console.WriteLine($"Creating {count} chat participants");

            for (int i = 0; i < count; i++)
            {
                var displayName = i == 0 ? "SenderDisplayName" : $"Participant-{i}";
                var participant = new ChatParticipant(await identityClient.CreateUserAsync()) { DisplayName = displayName };
                chatParticipants.Add(participant);

                Console.WriteLine($"> {participant.DisplayName} Joined");
            }

            return chatParticipants;
        }

        private static async Task Cleanup(IEnumerable<ChatParticipant> chatParticipants, CommunicationIdentityClient identityClient)
        {
            Console.WriteLine($"Deleting participants");
            var deleteTasks = new List<Task>();
            foreach (var participant in chatParticipants)
            {
                deleteTasks.Add(DeleteParticipant(identityClient, participant));
            }

            Task.WaitAll(deleteTasks.ToArray());
        }

        private static async Task DeleteParticipant(CommunicationIdentityClient identityClient, ChatParticipant participant)
        {
            var stopwatch = Stopwatch.StartNew();
            var userId = participant.User as CommunicationUserIdentifier;
            var deleteResult = await identityClient.DeleteUserAsync(userId);
            stopwatch.Stop();

            Console.WriteLine($"> DeleteUser for participant {participant.DisplayName} result: {deleteResult.Status}, time: {stopwatch.Elapsed.TotalSeconds}");
        }
    }
}