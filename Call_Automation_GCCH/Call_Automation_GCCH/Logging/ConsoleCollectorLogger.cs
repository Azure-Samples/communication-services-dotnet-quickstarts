using Microsoft.Extensions.Logging;
using System;

public class ConsoleCollectorLogger : ILogger
{
    private readonly string _categoryName;

    public ConsoleCollectorLogger(string categoryName)
    {
        _categoryName = categoryName;
    }

    public IDisposable BeginScope<TState>(TState state) => default!;

    public bool IsEnabled(LogLevel logLevel)
        => true; // You can add logLevel-based filtering if desired

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        if (!string.IsNullOrEmpty(message))
        {
            // Construct a log output string combining time, level, category, etc.
            string logOutput = $"{DateTime.UtcNow:HH:mm:ss} [{logLevel}] {_categoryName}: {message}";
            if (exception != null)
            {
                logOutput += Environment.NewLine + exception;
            }

            // Write to console
            Console.WriteLine(logOutput);

            // Also write to LogCollector
            LogCollector.Log(logOutput);
        }
    }
}
