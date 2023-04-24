using Azure.Communication;
using Azure.Communication.CallAutomation;
using CallAutomation_Playground.Interfaces;

namespace CallAutomation_Playground.Services
{
    public class TopLevelMenuService : ITopLevelMenuService
    {
        private readonly CallAutomationClient _callAutomationClient;
        private readonly PlaygroundConfig _playgroundConfig;

        public TopLevelMenuService(CallAutomationClient callAutomationClient, PlaygroundConfig playgroundConfig)
        {
            _callAutomationClient = callAutomationClient;
            _playgroundConfig = playgroundConfig;
        }

        public async Task InvokeTopLevelMenu(CommunicationIdentifier target, string callConnectionId)
        {
            CallConnectionProperties callConnectionProperties = await _callAutomationClient.GetCallConnection(callConnectionId).GetCallConnectionPropertiesAsync();

            // Top Level DTMF Menu
            CallMediaRecognizeDtmfOptions callMediaRecognizeDtmfOptions = new (target, 1)
            {
                Prompt = new FileSource(_playgroundConfig.InitialPromptUri),
                InterruptPrompt = true
            };

            IvrMenu mainMenu = new PlaygroundMainMenu(_callAutomationClient, _playgroundConfig);

            for (int i = 0; i < 3; i++)
            {
                await mainMenu.RecognizeDtmfInput(callConnectionProperties, null,
                    async success =>
                    {
                        if (success?.Tones.FirstOrDefault() == DtmfTone.One)
                        {
                            await mainMenu.OnPressOne(callConnectionProperties, target);
                        }

                        if (success?.Tones.FirstOrDefault() == DtmfTone.Two)
                        {
                            await mainMenu.OnPressTwo(callConnectionProperties, target);
                        }

                        if (success?.Tones.FirstOrDefault() == DtmfTone.Three)
                        {
                            await mainMenu.OnPressThree(callConnectionProperties, target);
                        }

                        if (success?.Tones.FirstOrDefault() == DtmfTone.Four)
                        {
                            await mainMenu.OnPressFour(callConnectionProperties, target);
                        }

                        i = 0;

                    },
                    async failed =>
                    {
                        if (failed.ReasonCode.Equals(ReasonCode.RecognizeInitialSilenceTimedOut))
                        {
                            // TODO: add retry logic
                        }
                    }, callMediaRecognizeDtmfOptions);
            }
        }
    }
}
