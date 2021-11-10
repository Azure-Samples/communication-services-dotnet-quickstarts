using Microsoft.Extensions.Logging;

public static class Logger
{
    public static ILogger logger = null;
    public enum MessageType
    {
        INFORMATION,
        ERROR
    }

    public static ILogger SetLoggerInstance(ILogger log)
    {
        if (logger == null)
        {
            logger = log;
        }
        return logger;
    }

    /// <summary>
    /// Log message to console
    /// </summary>
    /// <param name="messageType">Type of the message: Information or Error</param>
    /// <param name="message">Message string</param>
    public static void LogMessage(MessageType messageType, string message)
    {
        if(messageType == MessageType.ERROR)
            logger.LogError(message);
        else
            logger.LogInformation(message);
    }
}