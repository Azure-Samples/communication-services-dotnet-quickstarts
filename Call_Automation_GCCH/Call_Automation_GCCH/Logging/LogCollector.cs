using System;
using System.Collections.Generic;
using System.Linq;

namespace Call_Automation_GCCH.Logging
{
    /// <summary>
    /// Thread-safe in-memory log collector that stores recent log entries
    /// and makes them available for the Swagger UI log viewer.
    /// Logs are kept until browser reset (session-based persistence).
    /// </summary>
    public static class LogCollector
    {
    private static readonly List<LogEntry> _logs = new();
    private static readonly object _lock = new();
    private const int MAX_LOG_ENTRIES = 5000; // Increased from 500 for longer retention

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
            // Keep only last 5000 logs (persists until browser reset)
            if (_logs.Count > MAX_LOG_ENTRIES)
                _logs.RemoveAt(0);
        }
    }

    /// <summary>
    /// Returns all logs without time filtering (persists until browser reset).
    /// </summary>
    public static List<string> GetAll()
    {
        lock (_lock)
        {
            return _logs
                .Select(log => $"[{log.Timestamp:HH:mm:ss.fff}] {log.Message}")
                .ToList();
        }
    }

    /// <summary>
    /// Returns logs from the last N minutes (for backward compatibility).
    /// </summary>
    public static List<string> GetRecent(int minutes = 3)
    {
        lock (_lock)
        {
            var cutoffTime = DateTime.UtcNow.AddMinutes(-minutes);
            return _logs
                .Where(log => log.Timestamp >= cutoffTime)
                .Select(log => $"[{log.Timestamp:HH:mm:ss.fff}] {log.Message}")
                .ToList();
        }
    }

    /// <summary>
    /// Gets the count of logs currently stored in memory.
    /// </summary>
    public static int GetLogCount()
    {
        lock (_lock)
        {
            return _logs.Count;
        }
    }

    /// <summary>
    /// Clears all logs (use with caution).
    /// </summary>
    public static void Clear()
    {
        lock (_lock)
        {
            _logs.Clear();
        }
    }

    public static void Log(string message)
    {
        Console.WriteLine(message);
        Add(message);
    }

  }
}
