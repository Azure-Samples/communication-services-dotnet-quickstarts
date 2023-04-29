using Azure.Communication;
using Azure.Communication.CallAutomation;
using CallAutomation.Playground.Exceptions;
using CallAutomation.Playground.Interfaces;
using CallAutomation.Playground.Menus;

namespace CallAutomation.Playground.Services;

public class TopLevelMenuService : ITopLevelMenuService
{
    private readonly CallAutomationClient _callAutomationClient;
    private readonly PlaygroundConfig _playgroundConfig;
    private readonly ILogger<TopLevelMenuService> _logger;

    private readonly IvrMenu _ivrMenu;

    public TopLevelMenuService(CallAutomationClient callAutomationClient, PlaygroundConfig playgroundConfig, IvrMenuRegistry ivrMenuRegistry, ILogger<TopLevelMenuService> logger)
    {
        _callAutomationClient = callAutomationClient;
        _playgroundConfig = playgroundConfig;
        _logger = logger;

        ivrMenuRegistry.IvrMenus.TryGetValue(playgroundConfig.MainMenuName, out var ivrMenu);
        _ivrMenu = ivrMenu ?? throw new ApplicationException("Could not find valid main menu");
    }

    public async Task InvokeTopLevelMenu(CommunicationIdentifier target, string callConnectionId)
    {
        CallConnectionProperties callConnectionProperties = await _callAutomationClient.GetCallConnection(callConnectionId).GetCallConnectionPropertiesAsync();

        CallMediaRecognizeDtmfOptions callMediaRecognizeDtmfOptions = new(target, 1)
        {
            Prompt = new FileSource(_playgroundConfig.InitialPromptUri),
            InterruptPrompt = true
        };

        for (var retries = 0; retries < _playgroundConfig.NumRetries; retries++)
        {
            try
            {
                await _ivrMenu.RecognizeDtmfInput(callConnectionProperties, null,
                    async success =>
                    {
                        var tone = success.Tones.GetSingleTone();
                        await _ivrMenu.OnPress(tone, callConnectionProperties, target);
                    },
                    async failed =>
                    {
                        if (failed.ReasonCode.Equals(ReasonCode.RecognizeInitialSilenceTimedOut))
                        {
                            await _ivrMenu.PlayAudio(callConnectionProperties, _playgroundConfig.NoOptionSelectedUri, target);
                        }
                    }, callMediaRecognizeDtmfOptions);
            }
            catch (InvalidEntryException e)
            {
                _logger.LogError(e.Message);
            }
        }
    }
}