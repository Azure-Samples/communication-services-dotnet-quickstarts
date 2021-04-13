// <Create a chat client>
using Azure;
using Azure.Communication;
using Azure.Communication.Chat;
using System;

namespace ChatQuickstart
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            Uri endpoint = new Uri("https://<RESOURCE_NAME>.communication.azure.com");

            CommunicationTokenCredential communicationTokenCredential = new CommunicationTokenCredential("<Access_Token>");
            ChatClient chatClient = new ChatClient(endpoint, communicationTokenCredential);

            // <Start a chat thread>
            var chatParticipant = new ChatParticipant(identifier: new CommunicationUserIdentifier(id: "<Access_ID>"))
            {
                DisplayName = "UserDisplayName"
            };
            CreateChatThreadResult createChatThreadResult = await chatClient.CreateChatThreadAsync(topic: "Hello world!", participants: new[] { chatParticipant });

            // <Get a chat thread client>
            ChatThreadClient chatThreadClient = chatClient.GetChatThreadClient(threadId: createChatThreadResult.ChatThread.Id);
            string threadId = chatThreadClient.Id;
            
            // <List all chat threads>
            AsyncPageable<ChatThreadItem> chatThreadItems = chatClient.GetChatThreadsAsync();
            await foreach (ChatThreadItem chatThreadItem in chatThreadItems)
            {
                Console.WriteLine($"{ chatThreadItem.Id}");
            }

            // <Send a message to a chat thread>
            SendChatMessageResult sendChatMessageResult = await chatThreadClient.SendMessageAsync(content: "hello world", type: ChatMessageType.Text);
            string messageId = sendChatMessageResult.Id;

            // <Receive chat messages from a chat thread>
            AsyncPageable<ChatMessage> allMessages = chatThreadClient.GetMessagesAsync();
            await foreach (ChatMessage message in allMessages)
            {
                Console.WriteLine($"{message.Id}:{message.Content.Message}");
            }

            // <Add a user as a participant to the chat thread>
            var josh = new CommunicationUserIdentifier(id: "<Access_ID_For_Josh>");
            var gloria = new CommunicationUserIdentifier(id: "<Access_ID_For_Gloria>");
            var amy = new CommunicationUserIdentifier(id: "<Access_ID_For_Amy>");

            var participants = new[]
            {
                new ChatParticipant(josh) { DisplayName = "Josh" },
                new ChatParticipant(gloria) { DisplayName = "Gloria" },
                new ChatParticipant(amy) { DisplayName = "Amy" }
            };
            await chatThreadClient.AddParticipantsAsync(participants: participants);

            // <Get thread participants>
            AsyncPageable<ChatParticipant> allParticipants = chatThreadClient.GetParticipantsAsync();
            await foreach (ChatParticipant participant in allParticipants)
            {
                Console.WriteLine($"{((CommunicationUserIdentifier)participant.User).Id}:{participant.DisplayName}:{participant.ShareHistoryTime}");
            }

            // <Send read receipt>
            await chatThreadClient.SendReadReceiptAsync(messageId: messageId);
        }
    }
}