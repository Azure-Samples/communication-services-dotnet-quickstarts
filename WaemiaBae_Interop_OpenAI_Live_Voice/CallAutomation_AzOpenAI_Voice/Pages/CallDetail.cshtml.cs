using CallAutomation_AzOpenAI_Voice.Models;
using CallAutomation_AzOpenAI_Voice.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class CallDetailModel : PageModel
{
    private readonly CallSessionManager _sessionManager;

    public CallSession? Session { get; private set; }
    public List<TranscriptEntry> Transcript { get; private set; } = [];
    public bool IsCompleted { get; private set; }

    public CallDetailModel(CallSessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    public void OnGet([FromQuery] string id, [FromQuery] bool completed = false)
    {
        IsCompleted = completed;

        // Try active sessions first
        Session = _sessionManager.GetSession(id);
        if (Session != null)
        {
            Transcript = Session.GetTranscriptSnapshot();
            return;
        }

        // Try completed sessions
        var completedSessions = _sessionManager.GetCompletedSessions();
        Session = completedSessions.FirstOrDefault(s => s.CorrelationId == id);
        if (Session != null)
        {
            Transcript = Session.GetTranscriptSnapshot();
            IsCompleted = true;
        }
    }
}
