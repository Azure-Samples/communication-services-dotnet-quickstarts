using System.Collections.Concurrent;
using CallAutomation_AzOpenAI_Voice.Models;

namespace CallAutomation_AzOpenAI_Voice.Services
{
    public class CallSessionManager
    {
        // Keyed by CorrelationId
        private readonly ConcurrentDictionary<string, CallSession> _activeSessions = new();
        private readonly ConcurrentDictionary<string, CallSession> _completedSessions = new();
        private readonly ILogger<CallSessionManager> _logger;

        public event Action? OnSessionsChanged;

        public CallSessionManager(ILogger<CallSessionManager> logger)
        {
            _logger = logger;
        }

        public CallSession CreateSession(string correlationId, string callConnectionId, string callerId)
        {
            var session = new CallSession
            {
                CorrelationId = correlationId,
                CallConnectionId = callConnectionId,
                CallerId = callerId,
                Status = CallStatus.Connecting
            };

            if (!_activeSessions.TryAdd(correlationId, session))
            {
                _logger.LogWarning("Session already exists for correlation {Id}", correlationId);
                return _activeSessions[correlationId];
            }

            _logger.LogInformation("Created call session for {CallerId}, correlation {Id}", callerId, correlationId);
            OnSessionsChanged?.Invoke();
            return session;
        }

        public CallSession? GetSession(string correlationId)
        {
            _activeSessions.TryGetValue(correlationId, out var session);
            return session;
        }

        public CallSession? FindByCallConnectionId(string callConnectionId)
        {
            return _activeSessions.Values.FirstOrDefault(s => s.CallConnectionId == callConnectionId);
        }

        public void ActivateSession(string correlationId)
        {
            if (_activeSessions.TryGetValue(correlationId, out var session))
            {
                session.Status = CallStatus.Active;
                OnSessionsChanged?.Invoke();
            }
        }

        public void EndSession(string correlationId)
        {
            if (_activeSessions.TryRemove(correlationId, out var session))
            {
                session.EndTime = DateTime.UtcNow;
                session.Status = CallStatus.Completed;
                _completedSessions.TryAdd($"{correlationId}_{session.StartTime:yyyyMMddHHmmss}", session);
                session.Dispose();
                _logger.LogInformation("Ended call session {Id}", correlationId);
                OnSessionsChanged?.Invoke();
            }
        }

        public void EndSessionByCallConnectionId(string callConnectionId)
        {
            var session = FindByCallConnectionId(callConnectionId);
            if (session != null)
                EndSession(session.CorrelationId);
        }

        public void FailSession(string correlationId, string reason)
        {
            if (_activeSessions.TryRemove(correlationId, out var session))
            {
                session.EndTime = DateTime.UtcNow;
                session.Status = CallStatus.Failed;
                session.AddTranscript("System", $"Call failed: {reason}");
                _completedSessions.TryAdd($"{correlationId}_{session.StartTime:yyyyMMddHHmmss}", session);
                session.Dispose();
                _logger.LogError("Call session {Id} failed: {Reason}", correlationId, reason);
                OnSessionsChanged?.Invoke();
            }
        }

        public IReadOnlyList<CallSession> GetActiveSessions()
        {
            return _activeSessions.Values.ToList().AsReadOnly();
        }

        public IReadOnlyList<CallSession> GetCompletedSessions()
        {
            return _completedSessions.Values
                .OrderByDescending(s => s.StartTime)
                .ToList()
                .AsReadOnly();
        }

        public int ActiveCallCount => _activeSessions.Count;
        public int TotalCallCount => _activeSessions.Count + _completedSessions.Count;
    }
}
