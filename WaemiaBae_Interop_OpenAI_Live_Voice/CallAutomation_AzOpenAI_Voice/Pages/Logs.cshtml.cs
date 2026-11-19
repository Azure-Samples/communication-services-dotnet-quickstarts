using CallAutomation_AzOpenAI_Voice.Models;
using CallAutomation_AzOpenAI_Voice.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class LogsModel : PageModel
{
    private readonly CallSessionManager _sessionManager;

    public IReadOnlyList<CallSession> CompletedCalls { get; private set; } = [];

    public LogsModel(CallSessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    public void OnGet()
    {
        CompletedCalls = _sessionManager.GetCompletedSessions();
    }
}
