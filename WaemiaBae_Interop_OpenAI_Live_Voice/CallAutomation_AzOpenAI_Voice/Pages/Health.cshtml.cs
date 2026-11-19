using Azure.Communication.Media;
using CallAutomation_AzOpenAI_Voice.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class HealthModel : PageModel
{
    private readonly CallSessionManager _sessionManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public string MediaServiceOrigin { get; private set; } = "Unknown";
    public bool MediaSdkHealthy { get; private set; }
    public bool AcsConfigured { get; private set; }
    public bool OpenAiConfigured { get; private set; }
    public string? OpenAiEndpoint { get; private set; }
    public string? OpenAiModel { get; private set; }
    public string? SystemPrompt { get; private set; }
    public string? AppBaseUrl { get; private set; }
    public int ActiveCallCount { get; private set; }

    public HealthModel(CallSessionManager sessionManager, IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _sessionManager = sessionManager;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    public void OnGet()
    {
        try
        {
            var mc = _serviceProvider.GetService<MediaClient>();
            MediaServiceOrigin = mc?.ServiceOrigin ?? "Not initialized";
            MediaSdkHealthy = !string.IsNullOrEmpty(mc?.ServiceOrigin);
        }
        catch
        {
            MediaSdkHealthy = false;
            MediaServiceOrigin = "Not configured";
        }

        var acsConn = _configuration.GetValue<string>("AcsConnectionString");
        AcsConfigured = !string.IsNullOrEmpty(acsConn) && !acsConn.StartsWith("<");

        OpenAiEndpoint = _configuration.GetValue<string>("AzureOpenAIServiceEndpoint");
        OpenAiModel = _configuration.GetValue<string>("AzureOpenAIDeploymentModelName");
        var openAiKey = _configuration.GetValue<string>("AzureOpenAIServiceKey");
        OpenAiConfigured = !string.IsNullOrEmpty(OpenAiEndpoint) && !OpenAiEndpoint.StartsWith("<")
                        && !string.IsNullOrEmpty(openAiKey) && !openAiKey.StartsWith("<");

        SystemPrompt = _configuration.GetValue<string>("SystemPrompt");
        AppBaseUrl = _configuration.GetValue<string>("AppBaseUrl");
        ActiveCallCount = _sessionManager.ActiveCallCount;
    }
}
