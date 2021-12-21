using Microsoft.Extensions.Logging;

namespace IncomingCallRouting
{
    //Caution: Logging should be removed/disabled if you want to use this sample in production to avoid exposing sensitive information
    public static class Logger
    {
        public static ILogger logger = null;
        public static ILogger SetLoggerInstance(ILogger log)
        {
            if (logger == null)
            {
                logger = log;
            }

            return logger;
        }
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
            if (messageType == MessageType.ERROR)
                logger.LogError(message);
            else
                logger.LogInformation(message);
        }
    }

}
