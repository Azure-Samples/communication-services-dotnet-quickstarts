namespace CallAutomation.Playground;

public class IvrConfiguration
{
    public const string Name = "IvrConfiguration";

    public int NumRetries { get; set; } = 3;

    public Uri? PromptUri { get; set; }

    public Uri? InvalidEntryUri { get; set; }

    public Uri? NoOptionSelectedUri { get; set; }

    public string PhoneNumber { get; set; } = string.Empty;
}