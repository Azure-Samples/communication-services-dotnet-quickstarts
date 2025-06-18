using System;
using System.Collections.Generic;
using System.Linq;

public static class LogCollector
{
    private static readonly List<LogEntry> _logs = new();
    private static readonly object _lock = new();

    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Message { get; set; }
        
        public LogEntry(string message)
        {
            Timestamp = DateTime.UtcNow;
            Message = message;
        }
    }

    public static void Add(string message)
    {
        lock (_lock)
        {
            _logs.Add(new LogEntry(message));
            // Keep only last 500 logs (or based on time)
            if (_logs.Count > 500)
                _logs.RemoveAt(0);
        }
    }

    public static List<string> GetAll()
    {
        lock (_lock)
        {
            var cutoffTime = DateTime.UtcNow.AddMinutes(-3);
            return _logs
                .Where(log => log.Timestamp >= cutoffTime)
                .Select(log => $"[{log.Timestamp:HH:mm:ss}] {log.Message}")
                .ToList();
        }
    }

    public static void Clear()
    {
        lock (_lock)
        {
            _logs.Clear();
        }
    }

    public static void Log(string message)
    {
        Console.WriteLine(message);     // Log to terminal
        Add(message);                   // Log to UI collector
    }

}
