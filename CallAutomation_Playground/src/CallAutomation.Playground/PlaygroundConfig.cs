namespace CallAutomation.Playground
{
    public class PlaygroundConfig
    {
        public const string Name = "PlaygroundConfiguration";

        public string MainMenuName { get; set; } = string.Empty;

        public int NumRetries { get; set; } = 3;

        public Uri? CallbackUri { get; set; }

        public Uri? InitialPromptUri { get; set; }

        public Uri? NoOptionSelectedUri { get; set; }

        public Uri? HoldMusicPromptUri { get; set; }

        public string PhoneNumber { get; set; } = string.Empty;
    }
}
