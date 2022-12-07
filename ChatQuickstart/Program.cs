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
            // Your unique Azure Communication service endpoint
            Uri endpoint = new Uri("https://verizann-media.communication.azure.com");

            CommunicationTokenCredential communicationTokenCredential = new CommunicationTokenCredential("eyJhbGciOiJSUzI1NiIsImtpZCI6IjEwNiIsIng1dCI6Im9QMWFxQnlfR3hZU3pSaXhuQ25zdE5PU2p2cyIsInR5cCI6IkpXVCJ9.eyJza3lwZWlkIjoiYWNzOjUyOWM3YjcyLTdjMzQtNGRkYi05ZTc4LTEzMThiZWJjMWU0ZF8wMDAwMDAxNC1iZmJjLTM0ZjEtZjZjNy01OTNhMGQwMGRlY2YiLCJzY3AiOjE3OTIsImNzaSI6IjE2NjY5ODE3MjkiLCJleHAiOjE2NjcwNjgxMjksImFjc1Njb3BlIjoidm9pcCIsInJlc291cmNlSWQiOiI1MjljN2I3Mi03YzM0LTRkZGItOWU3OC0xMzE4YmViYzFlNGQiLCJyZXNvdXJjZUxvY2F0aW9uIjoidW5pdGVkc3RhdGVzIiwiaWF0IjoxNjY2OTgxNzI5fQ.cqUmMbdTWmvIw8OqYEEq6WvCZgFo1sdnPzORgTByaoJPYwHFHvzOrBHymzm6GkVkC_2KHymEycz1bLyO3Ym0-yZjP5xAVi2SxXBPbmkjwiwRJByP1Vx73HscjKcmuOiuzafBOV8bHZzCDrMysgxdYu3D9RKltt02HwCMjSRGim67WPOCSh-pnt91Xq4XKBC56cSyOnoR5uqlgDZJFn0vuvt605n9WkTDHw-pfYduv7zJG8h_9-mQ96WtZw7Y3mGLBAPBQ3p114svODu_1oxa5bWknPOwTNi7igNw5X9bAkgX8jw0Zn7e2nkBvO-o3drov_AOkRQJpKY4skm5wu08nw");
            ChatClient chatClient = new ChatClient(endpoint, communicationTokenCredential);
            Console.WriteLine("Success");
        }
    }
}

