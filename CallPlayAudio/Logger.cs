using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Communication.CallingServer.Sample.CallPlayAudio
{
    //Caution: Logging should be removed/disabled if you want to use this sample in production to avoid exposing sensitive information
    public static class Logger
    {
        public enum MessageType
        {
            INFORMATION,
            ERROR
        }

        /// <summary>
        /// Log message to console
        /// </summary>
        /// <param name="messageType">Type of the message: Information or Error</param>
        /// <param name="message">Message string</param>
        public static void LogMessage(MessageType messageType, string message)
        {
            Console.WriteLine(string.Format("{0} {1}\n", messageType, message));
        }
    }

}
