using CallAutomation_AzOpenAI_Voice.Models;
using CallAutomation_AzOpenAI_Voice.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class IndexModel : PageModel
{
    private readonly CallSessionManager _sessionManager;

    public IReadOnlyList<CallSession> ActiveCalls { get; private set; } = [];
    public int TotalCalls { get; private set; }

    public IndexModel(CallSessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    public void OnGet()
    {
        ActiveCalls = _sessionManager.GetActiveSessions();
        TotalCalls = _sessionManager.TotalCallCount;
    }
}
