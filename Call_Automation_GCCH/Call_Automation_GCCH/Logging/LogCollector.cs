public static class LogCollector
{
    private static readonly List<string> _logs = new();
    private static readonly object _lock = new();

    public static void Add(string message)
    {
        lock (_lock)
        {
            _logs.Add($"[{DateTime.UtcNow:HH:mm:ss}] {message}");
            if (_logs.Count > 500)
                _logs.RemoveAt(0);
        }
    }

    public static List<string> GetAll()
    {
        lock (_lock) return new(_logs);
    }
    public static void Log(string message)
    {
        Console.WriteLine(message);     // Log to terminal
        Add(message);                   // Log to UI collector
    }

}
