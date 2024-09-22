using Azure.AI.OpenAI;
using Microsoft.AspNetCore.SignalR;

namespace CallAutomationOpenAI
{
    public class AiContext
    {

        private List<ChatMessage> m_chatHistory;
        public AiContext()
        {
            m_chatHistory = new List<ChatMessage>();
        }

        public void AddChat(ChatRole role, string message)
        {
            m_chatHistory.Add(new ChatMessage(role, message));
        }

        public List<ChatMessage> GetChatHistory(int numberOfMessages=-1)
        {
            if(numberOfMessages > m_chatHistory.Count || numberOfMessages < 0)
            {
                numberOfMessages = m_chatHistory.Count;
            }
            return m_chatHistory.GetRange(m_chatHistory.Count - numberOfMessages, numberOfMessages);
        }
    }
}
