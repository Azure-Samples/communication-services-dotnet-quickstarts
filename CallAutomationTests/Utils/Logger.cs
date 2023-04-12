namespace CallAutomation.Scenarios
{
    //Caution: Logging should be removed/disabled if you want to use this sample in production to avoid exposing sensitive information
    public static class Logger
    {
        private static ILogger? _logger;
        public static void SetLoggerInstance(ILogger log)
        {
            _logger = log;
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
        public static void LogInformation(string message)
        {
            _logger!.LogInformation(message);
        }

        public static void LogError(string message)
        {
            _logger!.LogError(message);
        }
    }

}
