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
            Uri endpoint = new Uri("https://verizan-media.communication.azure.com");

            CommunicationTokenCredential communicationTokenCredential = new CommunicationTokenCredential("eyJhbGciOiJSUzI1NiIsImtpZCI6IjEwNiIsIng1dCI6Im9QMWFxQnlfR3hZU3pSaXhuQ25zdE5PU2p2cyIsInR5cCI6IkpXVCJ9.eyJza3lwZWlkIjoiYWNzOjUyOWM3YjcyLTdjMzQtNGRkYi05ZTc4LTEzMThiZWJjMWU0ZF8wMDAwMDAxMi0zYWM1LTI4NDItMTI1Mi01NzNhMGQwMDcxZjIiLCJzY3AiOjE3OTIsImNzaSI6IjE2NTYxNjEwMTAiLCJleHAiOjE2NTYyNDc0MTAsImFjc1Njb3BlIjoiY2hhdCIsInJlc291cmNlSWQiOiI1MjljN2I3Mi03YzM0LTRkZGItOWU3OC0xMzE4YmViYzFlNGQiLCJpYXQiOjE2NTYxNjEwMTB9.UR20QcPpZ3Wwna9sm_GYzod02fSx7KQJ5GNdISiMNxEUSLCI4iXQUhvx3Ok5oGmp1KIihbsGsZ8vyzvbSFusDXwKWOZ0NijrdeZYrIoX4IJNmwrZcH5emp22LjtaZ4MyOPbxuY9gkTHphPzyJh8U8gELV5PCf5ugsoWF_dfFCD9_VA2Q4tDKe8Z523koQxXFoMz2aG8t8tgZgKQsymIrIWhmSro9_lRffbRVtOpYV2PdAjLzzSMYdPoAe63U_UTeAfvoDFoI1AR1fgdQMDSvbzOOqs5iMC9lEZ8u7semBmawhvanORM5jS3JyfkcLyjKs_NFlIIqQjp8xpmhyXA7tg");
            ChatClient chatClient = new ChatClient(endpoint, communicationTokenCredential);

            // <Start a chat thread>
            var chatParticipant = new ChatParticipant(identifier: new CommunicationUserIdentifier(id: "8:acs:529c7b72-7c34-4ddb-9e78-1318bebc1e4d_00000012-3ac5-2842-1252-573a0d0071f2"))
            {
                DisplayName = "UserDisplayName"
            };
            CreateChatThreadResult createChatThreadResult = await chatClient.CreateChatThreadAsync(topic: "Hello world!", participants: new[] { chatParticipant });

            // <Get a chat thread client>
            string threadId = chatThreadClient.Id;
            ChatThreadClient chatThreadClient = chatClient.GetChatThreadClient(threadId: createChatThreadResult.ChatThread.Id);
            
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