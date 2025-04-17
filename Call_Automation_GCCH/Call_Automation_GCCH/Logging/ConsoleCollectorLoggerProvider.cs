using Microsoft.Extensions.Logging;

public class ConsoleCollectorLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new ConsoleCollectorLogger(categoryName);
    }

    public void Dispose()
    {
        // If you need to dispose resources, do it here
    }
}
